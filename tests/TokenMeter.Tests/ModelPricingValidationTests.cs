namespace TokenMeter.Tests;

public class ModelPricingValidationTests
{
    #region All Models — Data Integrity

    [Fact]
    public void AllModels_HaveNonEmptyModelId()
    {
        foreach (var (key, pricing) in ModelPricingData.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(pricing.ModelId),
                $"Model with key '{key}' has null/empty ModelId");
        }
    }

    [Fact]
    public void AllModels_HaveNonNegativePrices()
    {
        foreach (var (key, pricing) in ModelPricingData.All)
        {
            Assert.True(pricing.InputPricePerMillion >= 0,
                $"Model '{key}' has negative input price: {pricing.InputPricePerMillion}");
            Assert.True(pricing.OutputPricePerMillion >= 0,
                $"Model '{key}' has negative output price: {pricing.OutputPricePerMillion}");
        }
    }

    [Fact]
    public void AllModels_HaveProviderSet()
    {
        foreach (var (key, pricing) in ModelPricingData.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(pricing.Provider),
                $"Model '{key}' has null/empty Provider");
        }
    }

    [Fact]
    public void AllModels_KeyMatchesModelId()
    {
        foreach (var (key, pricing) in ModelPricingData.All)
        {
            Assert.Equal(key, pricing.ModelId, StringComparer.OrdinalIgnoreCase);
        }
    }

    #endregion

    #region Provider Properties — Correct Provider

    [Fact]
    public void OpenAI_AllModelsHaveOpenAIProvider()
    {
        foreach (var (_, pricing) in ModelPricingData.OpenAI)
        {
            Assert.Equal("OpenAI", pricing.Provider);
        }
    }

    [Fact]
    public void Anthropic_AllModelsHaveAnthropicProvider()
    {
        foreach (var (_, pricing) in ModelPricingData.Anthropic)
        {
            Assert.Equal("Anthropic", pricing.Provider);
        }
    }

    [Fact]
    public void Google_AllModelsHaveGoogleProvider()
    {
        foreach (var (_, pricing) in ModelPricingData.Google)
        {
            Assert.Equal("Google", pricing.Provider);
        }
    }

    [Fact]
    public void XAI_AllModelsHaveXAIProvider()
    {
        foreach (var (_, pricing) in ModelPricingData.XAI)
        {
            Assert.Equal("xAI", pricing.Provider);
        }
    }

    [Theory]
    [InlineData("Mistral")]
    [InlineData("DeepSeek")]
    [InlineData("Amazon Nova")]
    [InlineData("Cohere")]
    [InlineData("Meta Llama")]
    [InlineData("Perplexity")]
    [InlineData("Qwen")]
    public void ProviderModels_HaveMatchingProviderField(string providerName)
    {
        var models = ModelPricingData.GetByProvider(providerName).ToList();
        Assert.NotEmpty(models);

        foreach (var pricing in models)
        {
            Assert.Equal(providerName, pricing.Provider);
        }
    }

    #endregion

    #region ByProvider — Consistency

    [Fact]
    public void ByProvider_MatchesGetProviderNames()
    {
        var providerNames = ModelPricingData.GetProviderNames().OrderBy(n => n).ToList();
        var byProviderKeys = ModelPricingData.ByProvider.Keys.OrderBy(k => k).ToList();

        Assert.Equal(providerNames, byProviderKeys);
    }

    [Fact]
    public void ByProvider_AllModelsAreInAll()
    {
        foreach (var (provider, models) in ModelPricingData.ByProvider)
        {
            foreach (var (modelId, pricing) in models)
            {
                Assert.True(ModelPricingData.All.ContainsKey(modelId),
                    $"Model '{modelId}' from provider '{provider}' not found in All");
            }
        }
    }

    [Fact]
    public void All_TotalCountMatchesSumOfProviders()
    {
        var sumFromProviders = ModelPricingData.ByProvider.Values.Sum(p => p.Count);

        Assert.Equal(sumFromProviders, ModelPricingData.All.Count);
    }

    #endregion

    #region All — Case Insensitive Lookup

    [Fact]
    public void All_CaseInsensitiveLookup()
    {
        // All dictionary should be case-insensitive
        var pricing = ModelPricingData.All;

        if (pricing.ContainsKey("gpt-4o"))
        {
            Assert.True(pricing.ContainsKey("GPT-4O"));
            Assert.True(pricing.ContainsKey("Gpt-4o"));
            Assert.Equal(pricing["gpt-4o"], pricing["GPT-4O"]);
        }
    }

    #endregion

    #region FindPricing — Edge Cases

    [Fact]
    public void FindPricing_EmptyString_ReturnsNull()
    {
        var pricing = ModelPricingData.FindPricing("");

        Assert.Null(pricing);
    }

    [Fact]
    public void FindPricing_ExactMatch_TakesPriorityOverAlias()
    {
        // If "gpt-4o" is an exact match, it should return before any alias check
        var pricing = ModelPricingData.FindPricing("gpt-4o");

        Assert.NotNull(pricing);
        Assert.Equal("gpt-4o", pricing.ModelId);
    }

    #endregion

    #region CostCalculator — Edge Cases

    [Fact]
    public void CostCalculator_RegisterMultipleCustom_AllRetrievable()
    {
        var calculator = CostCalculator.CustomOnly();

        calculator.RegisterPricing(new ModelPricing
        {
            ModelId = "custom-a",
            InputPricePerMillion = 1.0m,
            OutputPricePerMillion = 2.0m
        });
        calculator.RegisterPricing(new ModelPricing
        {
            ModelId = "custom-b",
            InputPricePerMillion = 3.0m,
            OutputPricePerMillion = 6.0m
        });

        var models = calculator.GetRegisteredModels().ToList();
        Assert.Equal(2, models.Count);
        Assert.Contains("custom-a", models);
        Assert.Contains("custom-b", models);

        Assert.NotNull(calculator.GetPricing("custom-a"));
        Assert.NotNull(calculator.GetPricing("custom-b"));
    }

    [Fact]
    public void CostCalculator_CustomPriority_OverBuiltIn()
    {
        var calculator = CostCalculator.Default();

        // Override gpt-4o with custom pricing
        calculator.RegisterPricing(new ModelPricing
        {
            ModelId = "gpt-4o",
            InputPricePerMillion = 999.0m,
            OutputPricePerMillion = 999.0m,
            Provider = "Custom"
        });

        var pricing = calculator.GetPricing("gpt-4o");
        Assert.NotNull(pricing);
        Assert.Equal(999.0m, pricing.InputPricePerMillion);
        Assert.Equal("Custom", pricing.Provider);
    }

    [Fact]
    public void CostCalculator_CalculateCost_LargeTokenCount_NoOverflow()
    {
        var calculator = CostCalculator.Default();
        var pricing = calculator.GetPricing("gpt-4o");
        Assert.NotNull(pricing);

        // Large but realistic token counts
        var cost = calculator.CalculateCost("gpt-4o", 10_000_000, 5_000_000);

        Assert.NotNull(cost);
        Assert.True(cost > 0);
    }

    [Fact]
    public void ModelPricing_CalculateCost_VerySmallTokens_PrecisionMaintained()
    {
        var pricing = new ModelPricing
        {
            ModelId = "test",
            InputPricePerMillion = 2.50m,
            OutputPricePerMillion = 10.00m
        };

        // 1 token: 1/1M * $2.50 = $0.0000025
        var cost = pricing.CalculateCost(1, 1);

        Assert.True(cost > 0);
        Assert.Equal(0.0000025m + 0.0000100m, cost);
    }

    #endregion
}
