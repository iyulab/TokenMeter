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

    [Fact]
    public void All_IsNotEmpty_AndHasAtLeast79Models()
    {
        Assert.NotEmpty(ModelPricingData.All);
        Assert.True(ModelPricingData.All.Count >= 79, $"Expected >= 79 models, got {ModelPricingData.All.Count}");
    }

    [Fact]
    public void ByProvider_Has12Providers()
    {
        var providerNames = ModelPricingData.GetProviderNames().ToList();
        Assert.Equal(12, providerNames.Count);
    }

    [Theory]
    [InlineData("Mistral")]
    [InlineData("DeepSeek")]
    [InlineData("Amazon Nova")]
    [InlineData("Cohere")]
    [InlineData("Meta Llama")]
    [InlineData("Perplexity")]
    [InlineData("Qwen")]
    public void NewProvider_HasModels(string providerName)
    {
        var models = ModelPricingData.GetByProvider(providerName).ToList();
        Assert.NotEmpty(models);
    }

    [Fact]
    public void NewProviderProperties_AreNotEmpty()
    {
        Assert.NotEmpty(ModelPricingData.Mistral);
        Assert.NotEmpty(ModelPricingData.DeepSeek);
        Assert.NotEmpty(ModelPricingData.AmazonNova);
        Assert.NotEmpty(ModelPricingData.Cohere);
        Assert.NotEmpty(ModelPricingData.MetaLlama);
        Assert.NotEmpty(ModelPricingData.Perplexity);
        Assert.NotEmpty(ModelPricingData.Qwen);
    }

    [Theory]
    [InlineData("gpt-4.1", "gpt-4.1")]
    [InlineData("gpt-4.1-mini", "gpt-4.1-mini")]
    [InlineData("gpt-4.1-nano", "gpt-4.1-nano")]
    [InlineData("o3-pro", "o3-pro")]
    [InlineData("o4-mini", "o4-mini")]
    [InlineData("claude-opus-4-6", "claude-opus-4-6")]
    [InlineData("claude-sonnet-4", "claude-sonnet-4")]
    [InlineData("deepseek-chat", "deepseek-chat")]
    [InlineData("mistral-large-latest", "mistral-large-latest")]
    [InlineData("gemini-3-pro-preview", "gemini-3-pro-preview")]
    [InlineData("gemini-3-flash-preview", "gemini-3-flash-preview")]
    [InlineData("claude-opus-4-1", "claude-opus-4-1")]
    [InlineData("claude-opus-4-0", "claude-opus-4-0")]
    [InlineData("claude-3-7-sonnet-20250219", "claude-3-7-sonnet-20250219")]
    [InlineData("grok-4.1-fast-thinking", "grok-4.1-fast-thinking")]
    [InlineData("grok-4-fast-thinking", "grok-4-fast-thinking")]
    [InlineData("mistral-medium-latest", "mistral-medium-latest")]
    [InlineData("devstral-small-latest", "devstral-small-latest")]
    [InlineData("command-a", "command-a")]
    [InlineData("command-r7b", "command-r7b")]
    [InlineData("sonar-deep-research", "sonar-deep-research")]
    public void FindPricing_NewModels_ReturnsCorrectPricing(string modelId, string expectedModelId)
    {
        var pricing = ModelPricingData.FindPricing(modelId);

        Assert.NotNull(pricing);
        Assert.Equal(expectedModelId, pricing.ModelId);
    }

    [Theory]
    [InlineData("gpt-4.1-2025-04-14", "gpt-4.1")] // Prefix
    [InlineData("claude-opus-4.6", "claude-opus-4-6")] // Contains (dot variant)
    [InlineData("claude-4.5-sonnet", "claude-4-5-sonnet")] // Contains (dot variant)
    [InlineData("deepseek-v3-latest", "deepseek-chat")] // Prefix alias
    [InlineData("mistral-large-2025", "mistral-large-latest")] // Prefix alias
    [InlineData("claude-opus-4-1-20250805", "claude-opus-4-1")] // Exact API ID alias
    [InlineData("claude-opus-4-20250514", "claude-opus-4-0")] // Exact API ID alias
    [InlineData("claude-3-7-sonnet-latest", "claude-3-7-sonnet-20250219")] // Exact alias
    [InlineData("claude-3.7-sonnet", "claude-3-7-sonnet-20250219")] // Contains (dot variant)
    [InlineData("gemini-3-pro", "gemini-3-pro-preview")] // Contains alias
    [InlineData("gemini-3-flash", "gemini-3-flash-preview")] // Contains alias
    [InlineData("sonar-deep", "sonar-deep-research")] // Prefix alias
    public void FindPricing_AliasMatching_ReturnsCorrectModel(string alias, string expectedModelId)
    {
        var pricing = ModelPricingData.FindPricing(alias);

        Assert.NotNull(pricing);
        Assert.Equal(expectedModelId, pricing.ModelId);
    }

    [Theory]
    [InlineData("gemini-3-pro-preview", 2.00, 12.00)]
    [InlineData("gemini-2.5-flash", 0.30, 2.50)]
    [InlineData("gemini-2.5-flash-lite", 0.10, 0.40)]
    [InlineData("deepseek-chat", 0.28, 0.42)]
    [InlineData("deepseek-reasoner", 0.28, 0.42)]
    [InlineData("grok-code-fast-1", 0.20, 1.50)]
    [InlineData("command-r", 0.50, 1.50)]
    [InlineData("qwen-max", 1.20, 6.00)]
    [InlineData("amazon-nova-premier", 2.50, 12.50)]
    [InlineData("llama-4-maverick", 0.22, 0.85)]
    public void FindPricing_VerifyUpdatedPrices(string modelId, decimal expectedInput, decimal expectedOutput)
    {
        var pricing = ModelPricingData.FindPricing(modelId);

        Assert.NotNull(pricing);
        Assert.Equal(expectedInput, pricing.InputPricePerMillion);
        Assert.Equal(expectedOutput, pricing.OutputPricePerMillion);
    }

    [Fact]
    public void LastUpdated_Is20260210()
    {
        Assert.Equal(new DateOnly(2026, 2, 10), ModelPricingData.LastUpdated);
    }

    [Fact]
    public void RegisterPricing_Null_ThrowsArgumentNullException()
    {
        var calculator = CostCalculator.Default();

        Assert.Throws<ArgumentNullException>(() => calculator.RegisterPricing(null!));
    }

    [Fact]
    public void GetPricing_CaseInsensitive_FindsModel()
    {
        var calculator = CostCalculator.Default();
        calculator.RegisterPricing(new ModelPricing
        {
            ModelId = "My-Custom-Model",
            InputPricePerMillion = 1.0m,
            OutputPricePerMillion = 2.0m
        });

        var pricing = calculator.GetPricing("my-custom-model");

        Assert.NotNull(pricing);
        Assert.Equal("My-Custom-Model", pricing.ModelId);
    }

    [Fact]
    public void CalculateCost_ZeroTokens_ReturnsZero()
    {
        var calculator = CostCalculator.Default();

        var cost = calculator.CalculateCost("gpt-4o", 0, 0);

        Assert.NotNull(cost);
        Assert.Equal(0m, cost);
    }

    [Fact]
    public void CustomOnly_ReturnsInstance()
    {
        var calculator = CostCalculator.CustomOnly();

        Assert.NotNull(calculator);
    }

    [Fact]
    public void CustomOnly_WithRegisteredModel_FindsPricing()
    {
        var calculator = CostCalculator.CustomOnly();
        calculator.RegisterPricing(new ModelPricing
        {
            ModelId = "custom-model",
            InputPricePerMillion = 5.0m,
            OutputPricePerMillion = 10.0m
        });

        var cost = calculator.CalculateCost("custom-model", 1_000_000, 500_000);

        Assert.NotNull(cost);
        Assert.Equal(10.0m, cost); // 1M * $5 + 0.5M * $10 = $10
    }

    [Fact]
    public void GetRegisteredModels_IncludesCustomAndBuiltIn()
    {
        var calculator = CostCalculator.Default();
        calculator.RegisterPricing(new ModelPricing
        {
            ModelId = "unique-test-model",
            InputPricePerMillion = 1.0m,
            OutputPricePerMillion = 2.0m
        });

        var models = calculator.GetRegisteredModels().ToList();

        Assert.Contains("unique-test-model", models);
        Assert.Contains("gpt-4o", models);
    }

    [Fact]
    public void GetByProvider_NonExistent_ReturnsEmpty()
    {
        var models = ModelPricingData.GetByProvider("nonexistent-provider").ToList();

        Assert.Empty(models);
    }

    [Fact]
    public void GetProviderNames_Contains12Providers()
    {
        var names = ModelPricingData.GetProviderNames().ToList();

        Assert.Contains("OpenAI", names);
        Assert.Contains("Anthropic", names);
        Assert.Contains("Google", names);
        Assert.Contains("xAI", names);
        Assert.Contains("Azure", names);
        Assert.Contains("Mistral", names);
        Assert.Contains("DeepSeek", names);
    }

    [Fact]
    public void FindPricing_NullModelId_ReturnsNull()
    {
        // FindPricing should handle gracefully or throw
        // Testing actual behavior
        var pricing = ModelPricingData.FindPricing("   ");

        Assert.Null(pricing);
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

    [Fact]
    public void CalculateCost_ZeroTokens_ReturnsZero()
    {
        var pricing = new ModelPricing
        {
            ModelId = "test",
            InputPricePerMillion = 10.0m,
            OutputPricePerMillion = 20.0m
        };

        Assert.Equal(0m, pricing.CalculateCost(0, 0));
    }

    [Fact]
    public void CalculateCost_OnlyInputTokens_CalculatesInputCostOnly()
    {
        var pricing = new ModelPricing
        {
            ModelId = "test",
            InputPricePerMillion = 10.0m,
            OutputPricePerMillion = 20.0m
        };

        var cost = pricing.CalculateCost(1_000_000, 0);

        Assert.Equal(10.0m, cost);
    }

    [Fact]
    public void CalculateCost_OnlyOutputTokens_CalculatesOutputCostOnly()
    {
        var pricing = new ModelPricing
        {
            ModelId = "test",
            InputPricePerMillion = 10.0m,
            OutputPricePerMillion = 20.0m
        };

        var cost = pricing.CalculateCost(0, 1_000_000);

        Assert.Equal(20.0m, cost);
    }

    [Fact]
    public void CalculateCost_FreePricing_ReturnsZero()
    {
        var pricing = new ModelPricing
        {
            ModelId = "free-model",
            InputPricePerMillion = 0m,
            OutputPricePerMillion = 0m
        };

        Assert.Equal(0m, pricing.CalculateCost(1_000_000, 1_000_000));
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var pricing = new ModelPricing
        {
            ModelId = "test-model",
            InputPricePerMillion = 1.0m,
            OutputPricePerMillion = 2.0m,
            Provider = "TestProvider",
            DisplayName = "Test Model",
            ContextWindow = 128_000
        };

        Assert.Equal("TestProvider", pricing.Provider);
        Assert.Equal("Test Model", pricing.DisplayName);
        Assert.Equal(128_000, pricing.ContextWindow);
    }
}
