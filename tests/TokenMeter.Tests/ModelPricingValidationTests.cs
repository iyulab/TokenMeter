namespace TokenMeter.Tests;

// Migrated from ModelPricingData API → ModelCatalog + ModelInfo (Task 7 refactoring)
public class ModelPricingValidationTests
{
    #region All Models — Data Integrity

    [Fact]
    public void AllModels_HaveNonEmptyModelId()
    {
        foreach (var (key, info) in ModelCatalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(info.ModelId),
                $"Model with key '{key}' has null/empty ModelId");
        }
    }

    [Fact]
    public void AllModels_HaveNonNegativePrices()
    {
        foreach (var (key, info) in ModelCatalog.All)
        {
            Assert.True(info.InputPricePerMillion is null or >= 0,
                $"Model '{key}' has negative input price: {info.InputPricePerMillion}");
            Assert.True(info.OutputPricePerMillion is null or >= 0,
                $"Model '{key}' has negative output price: {info.OutputPricePerMillion}");
        }
    }

    [Fact]
    public void AllModels_HaveProviderSet()
    {
        foreach (var (key, info) in ModelCatalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(info.Provider),
                $"Model '{key}' has null/empty Provider");
        }
    }

    [Fact]
    public void AllModels_KeyMatchesModelId()
    {
        foreach (var (key, info) in ModelCatalog.All)
        {
            Assert.Equal(key, info.ModelId, StringComparer.OrdinalIgnoreCase);
        }
    }

    #endregion

    #region Provider Properties — Correct Provider

    [Fact]
    public void OpenAI_AllModelsHaveOpenAIProvider()
    {
        foreach (var (_, info) in ModelCatalog.OpenAI)
        {
            Assert.Equal("OpenAI", info.Provider);
        }
    }

    [Fact]
    public void Anthropic_AllModelsHaveAnthropicProvider()
    {
        foreach (var (_, info) in ModelCatalog.Anthropic)
        {
            Assert.Equal("Anthropic", info.Provider);
        }
    }

    [Fact]
    public void Google_AllModelsHaveGoogleProvider()
    {
        foreach (var (_, info) in ModelCatalog.Google)
        {
            Assert.Equal("Google", info.Provider);
        }
    }

    [Fact]
    public void XAI_AllModelsHaveXAIProvider()
    {
        foreach (var (_, info) in ModelCatalog.XAI)
        {
            Assert.Equal("xAI", info.Provider);
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
        var models = ModelCatalog.GetByProvider(providerName).ToList();
        Assert.NotEmpty(models);

        foreach (var info in models)
        {
            Assert.Equal(providerName, info.Provider);
        }
    }

    #endregion

    #region ByProvider — Consistency

    [Fact]
    public void ByProvider_MatchesGetProviderNames()
    {
        var providerNames = ModelCatalog.GetProviderNames().OrderBy(n => n).ToList();
        var byProviderKeys = ModelCatalog.ByProvider.Keys.OrderBy(k => k).ToList();

        Assert.Equal(providerNames, byProviderKeys);
    }

    [Fact]
    public void ByProvider_AllModelsAreInAll()
    {
        foreach (var (provider, models) in ModelCatalog.ByProvider)
        {
            foreach (var (modelId, _) in models)
            {
                Assert.True(ModelCatalog.All.ContainsKey(modelId),
                    $"Model '{modelId}' from provider '{provider}' not found in All");
            }
        }
    }

    [Fact]
    public void All_TotalCountMatchesSumOfProviders()
    {
        var sumFromProviders = ModelCatalog.ByProvider.Values.Sum(p => p.Count);

        Assert.Equal(sumFromProviders, ModelCatalog.All.Count);
    }

    #endregion

    #region All — Case Insensitive Lookup

    [Fact]
    public void All_CaseInsensitiveLookup()
    {
        // All dictionary should be case-insensitive
        var all = ModelCatalog.All;

        if (all.ContainsKey("gpt-4o"))
        {
            Assert.True(all.ContainsKey("GPT-4O"));
            Assert.True(all.ContainsKey("Gpt-4o"));
            Assert.Equal(all["gpt-4o"], all["GPT-4O"]);
        }
    }

    #endregion

    #region FindModel — Edge Cases

    [Fact]
    public void FindModel_EmptyString_ReturnsNull()
    {
        var model = ModelCatalog.FindModel("");

        Assert.Null(model);
    }

    [Fact]
    public void FindModel_ExactMatch_TakesPriorityOverAlias()
    {
        // If "gpt-4o" is an exact match, it should return before any alias check
        var model = ModelCatalog.FindModel("gpt-4o");

        Assert.NotNull(model);
        Assert.Equal("gpt-4o", model.ModelId);
    }

    #endregion

    #region CostCalculator — Edge Cases

    [Fact]
    public void CostCalculator_RegisterMultipleCustom_AllRetrievable()
    {
        var calculator = CostCalculator.CustomOnly();

        calculator.RegisterModel(new ModelInfo
        {
            ModelId = "custom-a",
            InputPricePerMillion = 1.0m,
            OutputPricePerMillion = 2.0m
        });
        calculator.RegisterModel(new ModelInfo
        {
            ModelId = "custom-b",
            InputPricePerMillion = 3.0m,
            OutputPricePerMillion = 6.0m
        });

        var models = calculator.GetRegisteredModels().ToList();
        Assert.Equal(2, models.Count);
        Assert.Contains("custom-a", models);
        Assert.Contains("custom-b", models);

        Assert.NotNull(calculator.GetModel("custom-a"));
        Assert.NotNull(calculator.GetModel("custom-b"));
    }

    [Fact]
    public void CostCalculator_CustomPriority_OverBuiltIn()
    {
        var calculator = CostCalculator.Default();

        // Override gpt-4o with custom pricing
        calculator.RegisterModel(new ModelInfo
        {
            ModelId = "gpt-4o",
            InputPricePerMillion = 999.0m,
            OutputPricePerMillion = 999.0m,
            Provider = "Custom"
        });

        var model = calculator.GetModel("gpt-4o");
        Assert.NotNull(model);
        Assert.Equal(999.0m, model.InputPricePerMillion);
        Assert.Equal("Custom", model.Provider);
    }

    [Fact]
    public void CostCalculator_CalculateCost_LargeTokenCount_NoOverflow()
    {
        var calculator = CostCalculator.Default();
        var model = calculator.GetModel("gpt-4o");
        Assert.NotNull(model);

        // Large but realistic token counts
        var cost = calculator.CalculateCost("gpt-4o", 10_000_000, 5_000_000);

        Assert.NotNull(cost);
        Assert.True(cost > 0);
    }

    [Fact]
    public void ModelInfo_CalculateCost_VerySmallTokens_PrecisionMaintained()
    {
        var info = new ModelInfo
        {
            ModelId = "test",
            InputPricePerMillion = 2.50m,
            OutputPricePerMillion = 10.00m
        };

        // 1 token: 1/1M * $2.50 = $0.0000025
        var cost = info.CalculateCost(1, 1);

        Assert.NotNull(cost);
        Assert.True(cost > 0);
        Assert.Equal(0.0000025m + 0.0000100m, cost.Value);
    }

    #endregion
}
