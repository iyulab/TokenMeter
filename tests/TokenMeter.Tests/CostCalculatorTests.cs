namespace TokenMeter.Tests;

public class CostCalculatorTests
{
    [Fact]
    public void CalculateCost_KnownModel_ReturnsCost()
    {
        var calculator = CostCalculator.Default();

        var cost = calculator.CalculateCost("gpt-4o", 1000, 500);

        Assert.NotNull(cost);
        Assert.True(cost > 0);
    }

    [Fact]
    public void CalculateCost_UnknownModel_ReturnsNull()
    {
        var calculator = CostCalculator.Default();

        var cost = calculator.CalculateCost("unknown-model-xyz", 1000, 500);

        Assert.Null(cost);
    }

    [Fact]
    public void GetPricing_KnownModel_ReturnsPricing()
    {
        var calculator = CostCalculator.Default();

        var pricing = calculator.GetPricing("gpt-4o");

        Assert.NotNull(pricing);
        Assert.Equal("gpt-4o", pricing.ModelId);
        Assert.True(pricing.InputPricePerMillion > 0);
        Assert.True(pricing.OutputPricePerMillion > 0);
    }

    [Fact]
    public void RegisterPricing_CustomModel_CanRetrieve()
    {
        var calculator = CostCalculator.Default();
        var customPricing = new ModelPricing
        {
            ModelId = "my-custom-model",
            InputPricePerMillion = 1.0m,
            OutputPricePerMillion = 2.0m,
            Provider = "Custom"
        };

        calculator.RegisterPricing(customPricing);

        var retrieved = calculator.GetPricing("my-custom-model");
        Assert.NotNull(retrieved);
        Assert.Equal(1.0m, retrieved.InputPricePerMillion);
        Assert.Equal(2.0m, retrieved.OutputPricePerMillion);
    }

    [Fact]
    public void RegisterPricing_OverridesBuiltIn()
    {
        var calculator = CostCalculator.Default();
        var customPricing = new ModelPricing
        {
            ModelId = "gpt-4o",
            InputPricePerMillion = 0.01m,
            OutputPricePerMillion = 0.02m
        };

        calculator.RegisterPricing(customPricing);

        var pricing = calculator.GetPricing("gpt-4o");
        Assert.Equal(0.01m, pricing!.InputPricePerMillion);
    }

    [Fact]
    public void GetRegisteredModels_ReturnsAllModels()
    {
        var calculator = CostCalculator.Default();

        var models = calculator.GetRegisteredModels().ToList();

        Assert.Contains("gpt-4o", models);
        Assert.Contains("gpt-4o-mini", models);
        Assert.Contains("claude-3-5-sonnet-20241022", models);
    }

    [Theory]
    [InlineData("gpt-4o-2024-08-06", "gpt-4o")] // Prefix match
    [InlineData("claude-3.5-sonnet", "claude-3-5-sonnet-20241022")] // Alias
    public void GetPricing_ModelAlias_FindsCorrectPricing(string alias, string expectedModelId)
    {
        var pricing = ModelPricingData.FindPricing(alias);

        Assert.NotNull(pricing);
        Assert.Equal(expectedModelId, pricing.ModelId);
    }
}

public class ModelPricingTests
{
    [Fact]
    public void CalculateCost_CorrectCalculation()
    {
        var pricing = new ModelPricing
        {
            ModelId = "test",
            InputPricePerMillion = 10.0m, // $10 per million input tokens
            OutputPricePerMillion = 20.0m // $20 per million output tokens
        };

        var cost = pricing.CalculateCost(1_000_000, 500_000);

        // 1M input * $10/M + 0.5M output * $20/M = $10 + $10 = $20
        Assert.Equal(20.0m, cost);
    }

    [Fact]
    public void CalculateCost_SmallCounts_ReturnsSmallCost()
    {
        var pricing = new ModelPricing
        {
            ModelId = "test",
            InputPricePerMillion = 2.50m,
            OutputPricePerMillion = 10.00m
        };

        var cost = pricing.CalculateCost(1000, 500);

        // 1000 input * $2.50/M + 500 output * $10/M = $0.0025 + $0.005 = $0.0075
        Assert.Equal(0.0075m, cost);
    }
}
