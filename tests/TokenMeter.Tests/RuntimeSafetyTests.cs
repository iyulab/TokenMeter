namespace TokenMeter.Tests;

/// <summary>
/// Tests for thread safety, null guards, overflow protection,
/// and other runtime safety concerns.
/// </summary>
public class CostCalculatorConcurrencyTests
{
    [Fact]
    public async Task ConcurrentRegisterAndGetPricing_NoExceptions()
    {
        var calculator = CostCalculator.Default();
        const int threadCount = 10;
        const int operationsPerThread = 200;
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var tasks = Enumerable.Range(0, threadCount).Select(t =>
            Task.Run(() =>
            {
                for (var i = 0; i < operationsPerThread && !cts.Token.IsCancellationRequested; i++)
                {
                    // Half the threads write, half read
                    if (t % 2 == 0)
                    {
                        calculator.RegisterPricing(new ModelPricing
                        {
                            ModelId = $"concurrent-model-{t}-{i}",
                            InputPricePerMillion = 1.0m,
                            OutputPricePerMillion = 2.0m,
                            Provider = "Test"
                        });
                    }
                    else
                    {
                        // Read operations — may or may not find the model
                        _ = calculator.GetPricing($"concurrent-model-{t - 1}-{i}");
                        _ = calculator.CalculateCost($"concurrent-model-{t - 1}-{i}", 100, 50);
                        _ = calculator.GetRegisteredModels().ToList();
                    }
                }
            })
        ).ToArray();

        await Task.WhenAll(tasks);

        // If we get here without exceptions, concurrency is safe
        Assert.True(true);
    }

    [Fact]
    public async Task ConcurrentRegisterAndCalculateCost_ProducesCorrectResults()
    {
        var calculator = CostCalculator.Default();
        const int threadCount = 8;
        const int iterations = 100;

        // Pre-register a model so all threads can calculate cost
        calculator.RegisterPricing(new ModelPricing
        {
            ModelId = "shared-model",
            InputPricePerMillion = 10.0m,
            OutputPricePerMillion = 20.0m,
            Provider = "Test"
        });

        var tasks = Enumerable.Range(0, threadCount).Select(_ =>
            Task.Run(() =>
            {
                for (var i = 0; i < iterations; i++)
                {
                    var cost = calculator.CalculateCost("shared-model", 1_000_000, 500_000);
                    Assert.NotNull(cost);
                    Assert.Equal(20.0m, cost); // 1M * $10 + 0.5M * $20 = $20
                }
            })
        ).ToArray();

        await Task.WhenAll(tasks);
    }
}

public class UsageTrackerConcurrencyTests
{
    [Fact]
    public async Task ConcurrentStartNewSessionAndRecord_NoExceptions()
    {
        var calculator = CostCalculator.Default();
        var tracker = new UsageTracker(calculator);
        const int durationMs = 2000;
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(durationMs));

        var recordTask = Task.Run(() =>
        {
            var i = 0;
            while (!cts.Token.IsCancellationRequested)
            {
                tracker.Record("gpt-4o", i % 1000, i % 500);
                i++;
            }
        });

        var sessionTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                tracker.StartNewSession();
            }
        });

        var statsTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                _ = tracker.GetSessionStatistics();
                _ = tracker.GetRecords();
            }
        });

        await Task.WhenAll(recordTask, sessionTask, statsTask);

        // All records should have a non-null SessionId
        var records = tracker.GetRecords();
        Assert.True(records.Count > 0);
        Assert.All(records, r => Assert.NotNull(r.SessionId));
    }

    [Fact]
    public async Task ConcurrentStartNewSession_AllSessionIdsUnique()
    {
        var tracker = new UsageTracker();
        const int threadCount = 10;
        const int sessionsPerThread = 50;
        var allSessionIds = new System.Collections.Concurrent.ConcurrentBag<string>();

        var tasks = Enumerable.Range(0, threadCount).Select(_ =>
            Task.Run(() =>
            {
                for (var i = 0; i < sessionsPerThread; i++)
                {
                    var id = tracker.StartNewSession();
                    allSessionIds.Add(id);
                }
            })
        ).ToArray();

        await Task.WhenAll(tasks);

        // All returned session IDs should be unique
        Assert.Equal(threadCount * sessionsPerThread, allSessionIds.Distinct().Count());
    }
}

public class CostCalculatorNullGuardTests
{
    [Fact]
    public void CalculateCost_NullModelId_ThrowsArgumentNullException()
    {
        var calculator = CostCalculator.Default();

        Assert.Throws<ArgumentNullException>(() => calculator.CalculateCost(null!, 100, 100));
    }

    [Fact]
    public void GetPricing_NullModelId_ThrowsArgumentNullException()
    {
        var calculator = CostCalculator.Default();

        Assert.Throws<ArgumentNullException>(() => calculator.GetPricing(null!));
    }

    [Fact]
    public void CustomOnly_GetPricing_NullModelId_ThrowsArgumentNullException()
    {
        var calculator = CostCalculator.CustomOnly();

        Assert.Throws<ArgumentNullException>(() => calculator.GetPricing(null!));
    }

    [Fact]
    public void CustomOnly_CalculateCost_NullModelId_ThrowsArgumentNullException()
    {
        var calculator = CostCalculator.CustomOnly();

        Assert.Throws<ArgumentNullException>(() => calculator.CalculateCost(null!, 100, 100));
    }
}

public class NegativeTokenCountTests
{
    [Fact]
    public void Record_NegativeInputTokens_IsAccepted()
    {
        // Document current behavior: negative tokens are not rejected
        var tracker = new UsageTracker();

        var record = tracker.Record("gpt-4o", -100, 50);

        Assert.Equal(-100, record.InputTokens);
        Assert.Equal(50, record.OutputTokens);
    }

    [Fact]
    public void Record_NegativeOutputTokens_IsAccepted()
    {
        var tracker = new UsageTracker();

        var record = tracker.Record("gpt-4o", 100, -50);

        Assert.Equal(100, record.InputTokens);
        Assert.Equal(-50, record.OutputTokens);
    }

    [Fact]
    public void UsageRecord_NegativeTokens_TotalTokensReflectsNegative()
    {
        var record = new UsageRecord
        {
            InputTokens = -100,
            OutputTokens = 50
        };

        Assert.Equal(-50L, record.TotalTokens);
    }

    [Fact]
    public void CalculateCost_NegativeTokens_ReturnsNegativeCost()
    {
        // Document current behavior: negative tokens produce negative cost
        var calculator = CostCalculator.Default();

        var cost = calculator.CalculateCost("gpt-4o", -1_000_000, 0);

        Assert.NotNull(cost);
        Assert.True(cost < 0);
    }
}

public class UsageStatisticsEmptyTests
{
    [Fact]
    public void Empty_IsStableReference()
    {
        var a = UsageStatistics.Empty;
        var b = UsageStatistics.Empty;

        Assert.Same(a, b);
    }

    [Fact]
    public void Empty_HasMinValueTimestamps()
    {
        var empty = UsageStatistics.Empty;

        Assert.Equal(DateTimeOffset.MinValue, empty.PeriodStart);
        Assert.Equal(DateTimeOffset.MinValue, empty.PeriodEnd);
    }

    [Fact]
    public void Empty_HasZeroCounts()
    {
        var empty = UsageStatistics.Empty;

        Assert.Equal(0, empty.RequestCount);
        Assert.Equal(0L, empty.TotalInputTokens);
        Assert.Equal(0L, empty.TotalOutputTokens);
        Assert.Equal(0L, empty.TotalTokens);
        Assert.Equal(0m, empty.TotalCost);
        Assert.Equal(0, empty.UnpricedRequestCount);
    }
}

public class UsageRecordTotalTokensOverflowTests
{
    [Fact]
    public void TotalTokens_MaxIntValues_NoOverflow()
    {
        var record = new UsageRecord
        {
            InputTokens = int.MaxValue,
            OutputTokens = int.MaxValue
        };

        // With long return type, this should not overflow
        long expected = (long)int.MaxValue + int.MaxValue;
        Assert.Equal(expected, record.TotalTokens);
    }

    [Fact]
    public void TotalTokens_ReturnTypeLong()
    {
        var record = new UsageRecord
        {
            InputTokens = 100,
            OutputTokens = 50
        };

        // Verify the return type is long at compile time
        long total = record.TotalTokens;
        Assert.Equal(150L, total);
    }
}

public class UnpricedRequestCountTests
{
    [Fact]
    public void GetSessionStatistics_AllPriced_UnpricedCountIsZero()
    {
        var calculator = CostCalculator.Default();
        var tracker = new UsageTracker(calculator);

        tracker.Record("gpt-4o", 100, 50);
        tracker.Record("gpt-4o", 200, 100);

        var stats = tracker.GetSessionStatistics();

        Assert.Equal(0, stats.UnpricedRequestCount);
    }

    [Fact]
    public void GetSessionStatistics_AllUnpriced_CountMatchesTotal()
    {
        var tracker = new UsageTracker(); // no cost calculator

        tracker.Record("gpt-4o", 100, 50);
        tracker.Record("gpt-4o", 200, 100);

        var stats = tracker.GetSessionStatistics();

        Assert.Equal(2, stats.UnpricedRequestCount);
        Assert.Equal(2, stats.RequestCount);
    }

    [Fact]
    public void GetSessionStatistics_MixedPricing_CountsCorrectly()
    {
        var calculator = CostCalculator.Default();
        var tracker = new UsageTracker(calculator);

        tracker.Record("gpt-4o", 100, 50);           // priced
        tracker.Record("unknown-model-xyz", 200, 100); // unpriced

        var stats = tracker.GetSessionStatistics();

        Assert.Equal(2, stats.RequestCount);
        Assert.Equal(1, stats.UnpricedRequestCount);
    }

    [Fact]
    public void GetStatistics_UnpricedRequestCount_IsSet()
    {
        var tracker = new UsageTracker();
        tracker.Record("gpt-4o", 100, 50);

        var start = DateTimeOffset.UtcNow.AddMinutes(-1);
        var end = DateTimeOffset.UtcNow.AddMinutes(1);
        var stats = tracker.GetStatistics(start, end);

        Assert.Equal(1, stats.UnpricedRequestCount);
    }

    [Fact]
    public void GetStatistics_EmptyPeriod_UnpricedCountIsZero()
    {
        var tracker = new UsageTracker();
        var start = DateTimeOffset.UtcNow.AddDays(-10);
        var end = DateTimeOffset.UtcNow.AddDays(-9);

        var stats = tracker.GetStatistics(start, end);

        Assert.Equal(0, stats.UnpricedRequestCount);
    }
}
