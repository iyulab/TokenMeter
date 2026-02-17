namespace TokenMeter.Tests;

public class UsageTrackerAdditionalTests
{
    #region Record(UsageRecord) Overload

    [Fact]
    public void Record_ValidUsageRecord_AddsToRecords()
    {
        var tracker = new UsageTracker();
        var record = new UsageRecord
        {
            ModelId = "gpt-4o",
            InputTokens = 100,
            OutputTokens = 50,
            Cost = 0.005m,
            SessionId = "test-session",
            Tags = ["test", "batch"]
        };

        tracker.Record(record);

        var records = tracker.GetRecords();
        Assert.Single(records);
        Assert.Equal("gpt-4o", records[0].ModelId);
        Assert.Equal(100, records[0].InputTokens);
        Assert.Equal(50, records[0].OutputTokens);
        Assert.Equal(0.005m, records[0].Cost);
        Assert.Equal("test-session", records[0].SessionId);
        Assert.NotNull(records[0].Tags);
        Assert.Equal(2, records[0].Tags!.Count);
    }

    [Fact]
    public void Record_UsageRecord_PreservesId()
    {
        var tracker = new UsageTracker();
        var record = new UsageRecord
        {
            ModelId = "gpt-4o",
            InputTokens = 10,
            OutputTokens = 5
        };
        var originalId = record.Id;

        tracker.Record(record);

        var records = tracker.GetRecords();
        Assert.Equal(originalId, records[0].Id);
    }

    #endregion

    #region Session Isolation

    [Fact]
    public void GetSessionStatistics_AfterStartNewSession_OnlyCountsNewSession()
    {
        var tracker = new UsageTracker();

        // Record in session 1
        tracker.Record("gpt-4o", 100, 50);
        tracker.Record("gpt-4o", 200, 100);

        var session1Stats = tracker.GetSessionStatistics();
        Assert.Equal(2, session1Stats.RequestCount);
        Assert.Equal(300, session1Stats.TotalInputTokens);

        // Switch to session 2
        tracker.StartNewSession();
        tracker.Record("gpt-4o", 500, 250);

        var session2Stats = tracker.GetSessionStatistics();
        Assert.Equal(1, session2Stats.RequestCount);
        Assert.Equal(500, session2Stats.TotalInputTokens);
        Assert.Equal(250, session2Stats.TotalOutputTokens);

        // All records still exist
        Assert.Equal(3, tracker.GetRecords().Count);
    }

    [Fact]
    public void GetSessionStatistics_EmptyNewSession_ReturnsZero()
    {
        var tracker = new UsageTracker();
        tracker.Record("gpt-4o", 100, 50);

        tracker.StartNewSession();

        var stats = tracker.GetSessionStatistics();
        Assert.Equal(0, stats.RequestCount);
        Assert.Equal(0, stats.TotalTokens);
    }

    #endregion

    #region Cost Aggregation

    [Fact]
    public void GetSessionStatistics_WithCost_AggregatesCost()
    {
        var calculator = CostCalculator.Default();
        var tracker = new UsageTracker(calculator);

        tracker.Record("gpt-4o", 1_000_000, 500_000);
        tracker.Record("gpt-4o", 1_000_000, 500_000);

        var stats = tracker.GetSessionStatistics();

        Assert.True(stats.TotalCost > 0);
        // Same inputs twice → double the cost
        var singleCost = calculator.CalculateCost("gpt-4o", 1_000_000, 500_000)!.Value;
        Assert.Equal(singleCost * 2, stats.TotalCost);
    }

    [Fact]
    public void GetSessionStatistics_MixedCostAndNoCost_AggregatesOnlyCostRecords()
    {
        var calculator = CostCalculator.Default();
        var tracker = new UsageTracker(calculator);

        tracker.Record("gpt-4o", 1000, 500);       // known → has cost
        tracker.Record("unknown-xyz", 1000, 500);   // unknown → null cost

        var stats = tracker.GetSessionStatistics();

        // Only the known model's cost should be summed
        var expectedCost = calculator.CalculateCost("gpt-4o", 1000, 500)!.Value;
        Assert.Equal(expectedCost, stats.TotalCost);
    }

    #endregion

    #region Thread Safety

    [Fact]
    public async Task ThreadSafety_ConcurrentRecords_AllRecorded()
    {
        var tracker = new UsageTracker();
        const int threadCount = 10;
        const int recordsPerThread = 100;

        var tasks = Enumerable.Range(0, threadCount).Select(t =>
            Task.Run(() =>
            {
                for (var i = 0; i < recordsPerThread; i++)
                {
                    tracker.Record($"model-{t}", i, i);
                }
            })
        ).ToArray();

        await Task.WhenAll(tasks);

        var records = tracker.GetRecords();
        Assert.Equal(threadCount * recordsPerThread, records.Count);
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentReadWrite_NoException()
    {
        var tracker = new UsageTracker();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var writerTask = Task.Run(() =>
        {
            var i = 0;
            while (!cts.Token.IsCancellationRequested)
            {
                tracker.Record("gpt-4o", i++, i++);
            }
        });

        var readerTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                _ = tracker.GetRecords();
                _ = tracker.GetSessionStatistics();
            }
        });

        await Task.WhenAll([writerTask, readerTask]);

        // If we get here without exceptions, thread safety is working
        Assert.True(tracker.GetRecords().Count > 0);
    }

    #endregion

    #region Statistics Period

    [Fact]
    public void GetStatistics_PeriodStartAndEnd_AreSet()
    {
        var tracker = new UsageTracker();
        var start = DateTimeOffset.UtcNow.AddMinutes(-5);
        var end = DateTimeOffset.UtcNow.AddMinutes(5);

        var stats = tracker.GetStatistics(start, end);

        Assert.Equal(start, stats.PeriodStart);
        Assert.Equal(end, stats.PeriodEnd);
    }

    [Fact]
    public void GetSessionStatistics_PeriodIsSet()
    {
        var tracker = new UsageTracker();
        tracker.Record("gpt-4o", 100, 50);

        var stats = tracker.GetSessionStatistics();

        // PeriodStart should be close to now (session start)
        Assert.True(stats.PeriodStart <= DateTimeOffset.UtcNow);
        Assert.True(stats.PeriodEnd >= stats.PeriodStart);
    }

    #endregion

    #region Clear Interactions

    [Fact]
    public void Clear_ThenGetSessionStatistics_ReturnsEmpty()
    {
        var tracker = new UsageTracker();
        tracker.Record("gpt-4o", 100, 50);
        tracker.Record("gpt-4o", 200, 100);

        tracker.Clear();

        var stats = tracker.GetSessionStatistics();
        Assert.Equal(0, stats.RequestCount);
        Assert.Equal(0, stats.TotalTokens);
        Assert.Equal(0m, stats.TotalCost);
    }

    [Fact]
    public void Clear_DoesNotResetSessionId()
    {
        var tracker = new UsageTracker();
        var originalSession = tracker.SessionId;
        tracker.Record("gpt-4o", 100, 50);

        tracker.Clear();

        Assert.Equal(originalSession, tracker.SessionId);
    }

    #endregion

    #region Multiple Models

    [Fact]
    public void Record_MultipleModels_AllTrackedSeparately()
    {
        var tracker = new UsageTracker();

        tracker.Record("gpt-4o", 100, 50);
        tracker.Record("claude-3-opus", 200, 100);
        tracker.Record("gemini-pro", 300, 150);

        var records = tracker.GetRecords();
        Assert.Equal(3, records.Count);

        var models = records.Select(r => r.ModelId).ToList();
        Assert.Contains("gpt-4o", models);
        Assert.Contains("claude-3-opus", models);
        Assert.Contains("gemini-pro", models);
    }

    [Fact]
    public void GetSessionStatistics_MultipleModels_AggregatesAll()
    {
        var tracker = new UsageTracker();
        tracker.Record("gpt-4o", 100, 50);
        tracker.Record("claude-3-opus", 200, 100);

        var stats = tracker.GetSessionStatistics();

        Assert.Equal(2, stats.RequestCount);
        Assert.Equal(300, stats.TotalInputTokens);
        Assert.Equal(150, stats.TotalOutputTokens);
        Assert.Equal(450, stats.TotalTokens);
    }

    #endregion
}

public class UsageStatisticsAdditionalTests
{
    [Fact]
    public void AverageCost_WithCost_CalculatesCorrectly()
    {
        var stats = new UsageStatistics
        {
            RequestCount = 2,
            TotalCost = 0.015m
        };

        Assert.Equal(0.0075m, stats.AverageCost);
    }

    [Fact]
    public void AverageTokens_WithRecords_CalculatesCorrectly()
    {
        var stats = new UsageStatistics
        {
            RequestCount = 3,
            TotalInputTokens = 300,
            TotalOutputTokens = 150
        };

        Assert.Equal(100.0, stats.AverageInputTokens);
        Assert.Equal(50.0, stats.AverageOutputTokens);
    }

    [Fact]
    public void PeriodStartEnd_CanBeSet()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

        var stats = new UsageStatistics
        {
            PeriodStart = start,
            PeriodEnd = end
        };

        Assert.Equal(start, stats.PeriodStart);
        Assert.Equal(end, stats.PeriodEnd);
    }

    [Fact]
    public void Empty_HasPeriodSet()
    {
        var stats = UsageStatistics.Empty;

        Assert.True(stats.PeriodStart <= DateTimeOffset.UtcNow);
        Assert.True(stats.PeriodEnd <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var record1 = new UsageStatistics
        {
            RequestCount = 5,
            TotalInputTokens = 1000,
            TotalOutputTokens = 500,
            TotalCost = 0.05m
        };
        var record2 = new UsageStatistics
        {
            RequestCount = 5,
            TotalInputTokens = 1000,
            TotalOutputTokens = 500,
            TotalCost = 0.05m
        };

        Assert.Equal(record1, record2);
    }
}

public class UsageRecordAdditionalTests
{
    [Fact]
    public void Record_Equality_SameValues_AreEqual()
    {
        var id = "test1234";
        var timestamp = DateTimeOffset.UtcNow;

        var record1 = new UsageRecord
        {
            Id = id,
            ModelId = "gpt-4o",
            InputTokens = 100,
            OutputTokens = 50,
            Timestamp = timestamp
        };
        var record2 = new UsageRecord
        {
            Id = id,
            ModelId = "gpt-4o",
            InputTokens = 100,
            OutputTokens = 50,
            Timestamp = timestamp
        };

        Assert.Equal(record1, record2);
    }

    [Fact]
    public void Record_DifferentIds_AreNotEqual()
    {
        var record1 = new UsageRecord { ModelId = "gpt-4o", InputTokens = 100, OutputTokens = 50 };
        var record2 = new UsageRecord { ModelId = "gpt-4o", InputTokens = 100, OutputTokens = 50 };

        // Different auto-generated IDs
        Assert.NotEqual(record1, record2);
    }

    [Fact]
    public void TotalTokens_LargeValues_CalculatesCorrectly()
    {
        var record = new UsageRecord
        {
            InputTokens = int.MaxValue / 2,
            OutputTokens = int.MaxValue / 2
        };

        // int overflow risk: MaxValue/2 + MaxValue/2 = MaxValue - 1
        Assert.Equal(int.MaxValue - 1, record.TotalTokens);
    }

    [Fact]
    public void Timestamp_DefaultIsCloseToNow()
    {
        var before = DateTimeOffset.UtcNow;
        var record = new UsageRecord();
        var after = DateTimeOffset.UtcNow;

        Assert.True(record.Timestamp >= before);
        Assert.True(record.Timestamp <= after);
    }
}
