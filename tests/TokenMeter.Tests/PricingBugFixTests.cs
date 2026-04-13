namespace TokenMeter.Tests;

public class PricingBugFixTests
{
    #region Issue 1 — Prefix alias collision (longest prefix match)

    [Theory]
    [InlineData("gpt-4o-2024-08-06", "gpt-4o")]
    [InlineData("gpt-4o-mini-2024-07-18", "gpt-4o-mini")]
    [InlineData("gpt-4-turbo-2024-04-09", "gpt-4-turbo")]
    [InlineData("gpt-4.1-mini-2025-04-14", "gpt-4.1-mini")]
    [InlineData("gpt-4.1-nano-2025-04-14", "gpt-4.1-nano")]
    public void FindPricing_PrefixCollision_ReturnsLongestMatch(string input, string expectedModelId)
    {
        var pricing = ModelPricingData.FindPricing(input);

        Assert.NotNull(pricing);
        Assert.Equal(expectedModelId, pricing.ModelId);
    }

    [Theory]
    [InlineData("o1-mini-2025-01-31", "o1-mini")]
    [InlineData("o3-mini-2025-01-31", "o3-mini")]
    [InlineData("o3-pro-2025-06-10", "o3-pro")]
    public void FindPricing_ReasoningModelPrefixCollision_ReturnsLongestMatch(string input, string expectedModelId)
    {
        var pricing = ModelPricingData.FindPricing(input);

        Assert.NotNull(pricing);
        Assert.Equal(expectedModelId, pricing.ModelId);
    }

    [Fact]
    public void FindPricing_Gpt4Exact_ReturnsGpt4NotGpt4o()
    {
        var pricing = ModelPricingData.FindPricing("gpt-4");

        Assert.NotNull(pricing);
        Assert.Equal("gpt-4", pricing.ModelId);
        Assert.Equal(30.00m, pricing.InputPricePerMillion);
    }

    [Fact]
    public void FindPricing_Gpt4DateSuffix_ReturnsGpt4()
    {
        var pricing = ModelPricingData.FindPricing("gpt-4-0613");

        Assert.NotNull(pricing);
        Assert.Equal("gpt-4", pricing.ModelId);
    }

    [Fact]
    public void FindPricing_NoPrefixAliasIsPrefixOfLongerPrefixAlias_Structural()
    {
        // Verify that for every prefix alias, if a shorter prefix alias is a prefix of it,
        // both are defined — so longest-match always has the right candidate.
        // This is a structural guard against future regressions.
        var all = ModelPricingData.All;
        var providers = ModelPricingData.ByProvider;

        // Collect all prefix aliases across all providers
        // We can't directly access alias rules, but we can verify via FindPricing behavior:
        // For each model ID that is also a prefix alias target, verify FindPricing returns itself.
        foreach (var (modelId, pricing) in all)
        {
            var found = ModelPricingData.FindPricing(modelId);
            Assert.NotNull(found);
            Assert.True(string.Equals(modelId, found.ModelId, StringComparison.OrdinalIgnoreCase),
                $"FindPricing(\"{modelId}\") returned \"{found.ModelId}\" instead of itself");
        }
    }

    #endregion

    #region Issue 2 — Null/empty input guard

    [Fact]
    public void FindPricing_Null_ReturnsNull()
    {
        var pricing = ModelPricingData.FindPricing(null!);

        Assert.Null(pricing);
    }

    [Fact]
    public void FindPricing_EmptyString_ReturnsNull()
    {
        var pricing = ModelPricingData.FindPricing("");

        Assert.Null(pricing);
    }

    [Fact]
    public void FindPricing_Whitespace_ReturnsNull()
    {
        var pricing = ModelPricingData.FindPricing("   ");

        Assert.Null(pricing);
    }

    #endregion

    #region Issue 3 — PricingLoader validation

    [Fact]
    public void AllLoadedProviders_HaveNonEmptyNames()
    {
        foreach (var providerName in ModelPricingData.GetProviderNames())
        {
            Assert.False(string.IsNullOrWhiteSpace(providerName),
                "A provider was loaded with an empty or whitespace name");
        }
    }

    [Fact]
    public void AllLoadedProviders_HaveNonEmptyModelLists()
    {
        foreach (var (providerName, models) in ModelPricingData.ByProvider)
        {
            Assert.True(models.Count > 0,
                $"Provider '{providerName}' has no models");
        }
    }

    #endregion

    #region Issue 4 — DeepSeek pricing note

    [Fact]
    public void DeepSeek_ReasonerAndChat_PricingLoaded()
    {
        var chat = ModelPricingData.FindPricing("deepseek-chat");
        var reasoner = ModelPricingData.FindPricing("deepseek-reasoner");

        Assert.NotNull(chat);
        Assert.NotNull(reasoner);

        // Note: As of early 2026, DeepSeek R1 (reasoner) and V3 (chat) share the same
        // pricing ($0.28/$0.42 per million tokens). This may reflect a promotional rate.
        // If pricing diverges in the future, update deepseek.json accordingly.
        Assert.Equal(chat.InputPricePerMillion, reasoner.InputPricePerMillion);
        Assert.Equal(chat.OutputPricePerMillion, reasoner.OutputPricePerMillion);
    }

    #endregion
}
