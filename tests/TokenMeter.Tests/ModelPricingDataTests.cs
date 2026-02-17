namespace TokenMeter.Tests;

public class ModelPricingDataTests
{
    #region All

    [Fact]
    public void All_ContainsModels()
    {
        var all = ModelPricingData.All;

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
        var models = ModelPricingData.GetByProvider(provider);

        Assert.True(models.Any());
    }

    [Fact]
    public void GetByProvider_UnknownProvider_ReturnsEmpty()
    {
        var models = ModelPricingData.GetByProvider("UnknownProvider");

        Assert.Empty(models);
    }

    [Fact]
    public void GetProviderNames_ReturnsMultipleProviders()
    {
        var providers = ModelPricingData.GetProviderNames().ToList();

        Assert.True(providers.Count >= 6); // At least OpenAI, Anthropic, Google, xAI, Mistral, DeepSeek
    }

    [Fact]
    public void OpenAI_ContainsModels()
    {
        Assert.True(ModelPricingData.OpenAI.Count > 0);
    }

    [Fact]
    public void Anthropic_ContainsModels()
    {
        Assert.True(ModelPricingData.Anthropic.Count > 0);
    }

    [Fact]
    public void Google_ContainsModels()
    {
        Assert.True(ModelPricingData.Google.Count > 0);
    }

    #endregion

    #region FindPricing — Exact Match

    [Fact]
    public void FindPricing_KnownModel_ReturnsPricing()
    {
        // gpt-4o is a well-known model
        var pricing = ModelPricingData.FindPricing("gpt-4o");

        Assert.NotNull(pricing);
        Assert.Equal("gpt-4o", pricing.ModelId);
    }

    [Fact]
    public void FindPricing_CaseInsensitive_ReturnsPricing()
    {
        var pricing = ModelPricingData.FindPricing("GPT-4O");

        Assert.NotNull(pricing);
    }

    [Fact]
    public void FindPricing_Unknown_ReturnsNull()
    {
        var pricing = ModelPricingData.FindPricing("totally-unknown-model-xyz");

        Assert.Null(pricing);
    }

    #endregion

    #region FindPricing — Alias Matching

    [Fact]
    public void FindPricing_AnthropicModel_ReturnsPricing()
    {
        // Try a known Anthropic model
        var pricing = ModelPricingData.FindPricing("claude-sonnet-4-5-20250929");

        Assert.NotNull(pricing);
        Assert.Equal("Anthropic", pricing.Provider);
    }

    #endregion

    #region ModelPricing — CalculateCost

    [Fact]
    public void CalculateCost_ZeroTokens_ReturnsZero()
    {
        var pricing = new ModelPricing
        {
            ModelId = "test",
            InputPricePerMillion = 10m,
            OutputPricePerMillion = 30m
        };

        var cost = pricing.CalculateCost(0, 0);

        Assert.Equal(0m, cost);
    }

    [Fact]
    public void CalculateCost_OneMillionTokens_ReturnsExactPrice()
    {
        var pricing = new ModelPricing
        {
            ModelId = "test",
            InputPricePerMillion = 10m,
            OutputPricePerMillion = 30m
        };

        var cost = pricing.CalculateCost(1_000_000, 1_000_000);

        Assert.Equal(40m, cost); // 10 + 30
    }

    [Fact]
    public void CalculateCost_SmallTokenCount_ReturnsFraction()
    {
        var pricing = new ModelPricing
        {
            ModelId = "test",
            InputPricePerMillion = 10m,
            OutputPricePerMillion = 30m
        };

        var cost = pricing.CalculateCost(1000, 500);

        // 1000/1M * 10 + 500/1M * 30 = 0.01 + 0.015 = 0.025
        Assert.Equal(0.025m, cost);
    }

    [Fact]
    public void CalculateCost_InputOnly_ReturnsInputCost()
    {
        var pricing = new ModelPricing
        {
            ModelId = "test",
            InputPricePerMillion = 10m,
            OutputPricePerMillion = 30m
        };

        var cost = pricing.CalculateCost(1_000_000, 0);

        Assert.Equal(10m, cost);
    }

    [Fact]
    public void CalculateCost_OutputOnly_ReturnsOutputCost()
    {
        var pricing = new ModelPricing
        {
            ModelId = "test",
            InputPricePerMillion = 10m,
            OutputPricePerMillion = 30m
        };

        var cost = pricing.CalculateCost(0, 1_000_000);

        Assert.Equal(30m, cost);
    }

    #endregion

    #region ModelPricing — Properties

    [Fact]
    public void ModelPricing_RequiredProperties()
    {
        var pricing = new ModelPricing
        {
            ModelId = "test-model",
            InputPricePerMillion = 5m,
            OutputPricePerMillion = 15m,
            Provider = "TestProvider",
            DisplayName = "Test Model",
            ContextWindow = 128000
        };

        Assert.Equal("test-model", pricing.ModelId);
        Assert.Equal(5m, pricing.InputPricePerMillion);
        Assert.Equal(15m, pricing.OutputPricePerMillion);
        Assert.Equal("TestProvider", pricing.Provider);
        Assert.Equal("Test Model", pricing.DisplayName);
        Assert.Equal(128000, pricing.ContextWindow);
    }

    #endregion

    #region LastUpdated

    [Fact]
    public void LastUpdated_IsReasonableDate()
    {
        var date = ModelPricingData.LastUpdated;

        Assert.True(date.Year >= 2025);
    }

    #endregion

    #region ByProvider

    [Fact]
    public void ByProvider_AllProvidersHaveModels()
    {
        foreach (var (provider, models) in ModelPricingData.ByProvider)
        {
            Assert.True(models.Count > 0, $"Provider {provider} has no models");
        }
    }

    #endregion
}
