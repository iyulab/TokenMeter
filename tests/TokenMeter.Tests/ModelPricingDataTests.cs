namespace TokenMeter.Tests;

// Migrated from ModelPricingData API → ModelCatalog + ModelInfo (Task 7 refactoring)
public class ModelPricingDataTests
{
    #region All

    [Fact]
    public void All_ContainsModels()
    {
        var all = ModelCatalog.All;

        Assert.True(all.Count > 0);
    }

    #endregion

    #region Provider-Specific Access

    [Theory]
    [InlineData("OpenAI")]
    [InlineData("Anthropic")]
    [InlineData("Google")]
    [InlineData("xAI")]
    [InlineData("Mistral")]
    [InlineData("DeepSeek")]
    public void ByProvider_KnownProviders_ContainModels(string provider)
    {
        var models = ModelCatalog.GetByProvider(provider);

        Assert.True(models.Any());
    }

    [Fact]
    public void GetByProvider_UnknownProvider_ReturnsEmpty()
    {
        var models = ModelCatalog.GetByProvider("UnknownProvider");

        Assert.Empty(models);
    }

    [Fact]
    public void GetProviderNames_ReturnsMultipleProviders()
    {
        var providers = ModelCatalog.GetProviderNames().ToList();

        Assert.True(providers.Count >= 6); // At least OpenAI, Anthropic, Google, xAI, Mistral, DeepSeek
    }

    [Fact]
    public void OpenAI_ContainsModels()
    {
        Assert.True(ModelCatalog.OpenAI.Count > 0);
    }

    [Fact]
    public void Anthropic_ContainsModels()
    {
        Assert.True(ModelCatalog.Anthropic.Count > 0);
    }

    [Fact]
    public void Google_ContainsModels()
    {
        Assert.True(ModelCatalog.Google.Count > 0);
    }

    #endregion

    #region FindModel — Exact Match

    [Fact]
    public void FindModel_KnownModel_ReturnsInfo()
    {
        // gpt-4o is a well-known model
        var model = ModelCatalog.FindModel("gpt-4o");

        Assert.NotNull(model);
        Assert.Equal("gpt-4o", model.ModelId);
    }

    [Fact]
    public void FindModel_CaseInsensitive_ReturnsInfo()
    {
        var model = ModelCatalog.FindModel("GPT-4O");

        Assert.NotNull(model);
    }

    [Fact]
    public void FindModel_Unknown_ReturnsNull()
    {
        var model = ModelCatalog.FindModel("totally-unknown-model-xyz");

        Assert.Null(model);
    }

    #endregion

    #region FindModel — Alias Matching

    [Fact]
    public void FindModel_AnthropicModel_ReturnsInfo()
    {
        // Try a known Anthropic model
        var model = ModelCatalog.FindModel("claude-sonnet-4-5-20250929");

        Assert.NotNull(model);
        Assert.Equal("Anthropic", model.Provider);
    }

    #endregion

    #region ModelInfo — CalculateCost

    [Fact]
    public void CalculateCost_ZeroTokens_ReturnsZero()
    {
        var info = new ModelInfo
        {
            ModelId = "test",
            InputPricePerMillion = 10m,
            OutputPricePerMillion = 30m
        };

        var cost = info.CalculateCost(0, 0);

        Assert.NotNull(cost);
        Assert.Equal(0m, cost.Value);
    }

    [Fact]
    public void CalculateCost_OneMillionTokens_ReturnsExactPrice()
    {
        var info = new ModelInfo
        {
            ModelId = "test",
            InputPricePerMillion = 10m,
            OutputPricePerMillion = 30m
        };

        var cost = info.CalculateCost(1_000_000, 1_000_000);

        Assert.NotNull(cost);
        Assert.Equal(40m, cost.Value); // 10 + 30
    }

    [Fact]
    public void CalculateCost_SmallTokenCount_ReturnsFraction()
    {
        var info = new ModelInfo
        {
            ModelId = "test",
            InputPricePerMillion = 10m,
            OutputPricePerMillion = 30m
        };

        var cost = info.CalculateCost(1000, 500);

        Assert.NotNull(cost);
        // 1000/1M * 10 + 500/1M * 30 = 0.01 + 0.015 = 0.025
        Assert.Equal(0.025m, cost.Value);
    }

    [Fact]
    public void CalculateCost_InputOnly_ReturnsInputCost()
    {
        var info = new ModelInfo
        {
            ModelId = "test",
            InputPricePerMillion = 10m,
            OutputPricePerMillion = 30m
        };

        var cost = info.CalculateCost(1_000_000, 0);

        Assert.NotNull(cost);
        Assert.Equal(10m, cost.Value);
    }

    [Fact]
    public void CalculateCost_OutputOnly_ReturnsOutputCost()
    {
        var info = new ModelInfo
        {
            ModelId = "test",
            InputPricePerMillion = 10m,
            OutputPricePerMillion = 30m
        };

        var cost = info.CalculateCost(0, 1_000_000);

        Assert.NotNull(cost);
        Assert.Equal(30m, cost.Value);
    }

    #endregion

    #region ModelInfo — Properties

    [Fact]
    public void ModelInfo_RequiredProperties()
    {
        var info = new ModelInfo
        {
            ModelId = "test-model",
            InputPricePerMillion = 5m,
            OutputPricePerMillion = 15m,
            Provider = "TestProvider",
            DisplayName = "Test Model",
            ContextWindow = 128000
        };

        Assert.Equal("test-model", info.ModelId);
        Assert.Equal(5m, info.InputPricePerMillion);
        Assert.Equal(15m, info.OutputPricePerMillion);
        Assert.Equal("TestProvider", info.Provider);
        Assert.Equal("Test Model", info.DisplayName);
        Assert.Equal(128000, info.ContextWindow);
    }

    #endregion

    #region LastUpdated & Staleness

    [Fact]
    public void LastUpdated_IsReasonableDate()
    {
        var date = ModelCatalog.LastUpdated;

        Assert.True(date.Year >= 2025);
    }

    [Fact]
    public void DataAgeDays_ReturnsNonNegative()
    {
        var ageDays = ModelCatalog.DataAgeDays;

        Assert.True(ageDays >= 0);
    }

    [Fact]
    public void IsDataStale_VeryLargeThreshold_ReturnsFalse()
    {
        // With a threshold of 10 years, data should not be stale
        Assert.False(ModelCatalog.IsDataStale(3650));
    }

    [Fact]
    public void IsDataStale_NegativeThreshold_ReturnsTrue()
    {
        // With a negative threshold, data is always stale
        Assert.True(ModelCatalog.IsDataStale(-1));
    }

    #endregion

    #region ByProvider

    [Fact]
    public void ByProvider_AllProvidersHaveModels()
    {
        foreach (var (provider, models) in ModelCatalog.ByProvider)
        {
            Assert.True(models.Count > 0, $"Provider {provider} has no models");
        }
    }

    #endregion
}
