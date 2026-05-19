namespace TokenMeter.Tests;

public class CostCalculatorTests
{
    [Fact]
    public void CalculateCost_UnknownModel_ReturnsNull()
    {
        var calc = CostCalculator.Default();
        Assert.Null(calc.CalculateCost("nonexistent-model-xyz", 1000, 500));
    }

    [Fact]
    public void GetModel_UnknownId_ReturnsNull()
    {
        var calc = CostCalculator.Default();
        Assert.Null(calc.GetModel("nonexistent-model-xyz"));
    }

    [Fact]
    public void GetModel_NullOrEmpty_ReturnsNull()
    {
        var calc = CostCalculator.Default();
        Assert.Null(calc.GetModel(null!));
        Assert.Null(calc.GetModel(""));
        Assert.Null(calc.GetModel("   "));
    }

    [Fact]
    public void RegisterModel_OverridesBuiltIn()
    {
        var calc = CostCalculator.Default();
        var custom = new ModelInfo
        {
            ModelId = "gpt-4o",
            InputPricePerMillion = 999m,
            OutputPricePerMillion = 999m
        };
        calc.RegisterModel(custom);

        var result = calc.GetModel("gpt-4o");
        Assert.NotNull(result);
        Assert.Equal(999m, result.InputPricePerMillion);
    }

    [Fact]
    public void CustomOnly_DoesNotUseBuiltIn()
    {
        var calc = CostCalculator.CustomOnly();
        Assert.Null(calc.GetModel("gpt-4o"));
    }

    [Fact]
    public void CustomOnly_UsesRegistered()
    {
        var calc = CostCalculator.CustomOnly();
        var model = new ModelInfo
        {
            ModelId = "my-model",
            InputPricePerMillion = 1.00m,
            OutputPricePerMillion = 2.00m
        };
        calc.RegisterModel(model);

        var cost = calc.CalculateCost("my-model", 1_000_000, 1_000_000);
        Assert.NotNull(cost);
        Assert.Equal(3.00m, cost.Value);
    }

    [Fact]
    public void CalculateCost_WithCacheTokens_CustomModel()
    {
        var calc = CostCalculator.CustomOnly();
        calc.RegisterModel(new ModelInfo
        {
            ModelId = "test-model",
            InputPricePerMillion = 5.00m,
            OutputPricePerMillion = 25.00m,
            CacheReadPricePerMillion = 0.50m,
            CacheWritePricePerMillion = 6.25m
        });

        var cost = calc.CalculateCost("test-model", 0, 0, 1_000_000, 1_000_000);
        Assert.NotNull(cost);
        Assert.Equal(6.75m, cost.Value); // $0.5 + $6.25
    }

    [Fact]
    public void GetRegisteredModels_CustomOnly_ExcludesBuiltIn()
    {
        var calc = CostCalculator.CustomOnly();
        calc.RegisterModel(new ModelInfo
        {
            ModelId = "my-model",
            InputPricePerMillion = 1.0m,
            OutputPricePerMillion = 2.0m
        });

        var models = calc.GetRegisteredModels().ToList();

        Assert.Single(models);
        Assert.Equal("my-model", models[0]);
        Assert.DoesNotContain("gpt-4o", models);
    }

    [Fact]
    public void GetRegisteredModels_Default_IncludesCustom()
    {
        var calc = CostCalculator.Default();
        calc.RegisterModel(new ModelInfo
        {
            ModelId = "unique-test-model-xyz",
            InputPricePerMillion = 1.0m,
            OutputPricePerMillion = 2.0m
        });

        var models = calc.GetRegisteredModels().ToList();

        Assert.Contains("unique-test-model-xyz", models);
    }

    [Fact]
    public void GetModel_CaseInsensitive_FindsRegisteredModel()
    {
        var calc = CostCalculator.Default();
        calc.RegisterModel(new ModelInfo
        {
            ModelId = "My-Custom-Model",
            InputPricePerMillion = 1.0m,
            OutputPricePerMillion = 2.0m
        });

        var model = calc.GetModel("my-custom-model");

        Assert.NotNull(model);
        Assert.Equal("My-Custom-Model", model.ModelId);
    }

    [Fact]
    public void CustomOnly_CalculateCost_BuiltInModel_ReturnsNull()
    {
        var calc = CostCalculator.CustomOnly();

        var cost = calc.CalculateCost("gpt-4o", 1000, 500);

        Assert.Null(cost);
    }
}
