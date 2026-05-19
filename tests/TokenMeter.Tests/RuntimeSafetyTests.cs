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
