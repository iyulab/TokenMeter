namespace TokenMeter.Tests;

// Migrated from ModelPricingData API → ModelCatalog + ModelInfo (Task 7 refactoring)
public class PricingBugFixTests
{
    #region Issue 1 — Prefix alias collision (longest prefix match)

    [Theory]
    [InlineData("gpt-4o-2024-08-06", "gpt-4o")]
    [InlineData("gpt-4o-mini-2024-07-18", "gpt-4o-mini")]
    [InlineData("gpt-4-turbo-2024-04-09", "gpt-4-turbo")]
    [InlineData("gpt-4.1-mini-2025-04-14", "gpt-4.1-mini")]
    [InlineData("gpt-4.1-nano-2025-04-14", "gpt-4.1-nano")]
    public void FindModel_PrefixCollision_ReturnsLongestMatch(string input, string expectedModelId)
    {
        var model = ModelCatalog.FindModel(input);

        Assert.NotNull(model);
        Assert.Equal(expectedModelId, model.ModelId);
    }

    [Theory]
    [InlineData("o1-mini-2025-01-31", "o1-mini")]
    [InlineData("o3-mini-2025-01-31", "o3-mini")]
    [InlineData("o3-pro-2025-06-10", "o3-pro")]
    public void FindModel_ReasoningModelPrefixCollision_ReturnsLongestMatch(string input, string expectedModelId)
    {
        var model = ModelCatalog.FindModel(input);

        Assert.NotNull(model);
        Assert.Equal(expectedModelId, model.ModelId);
    }

    [Fact]
    public void FindModel_Gpt4Exact_ReturnsGpt4NotGpt4o()
    {
        var model = ModelCatalog.FindModel("gpt-4");

        Assert.NotNull(model);
        Assert.Equal("gpt-4", model.ModelId);
        Assert.Equal(30.00m, model.InputPricePerMillion);
    }

    [Fact]
    public void FindModel_Gpt4DateSuffix_ReturnsGpt4()
    {
        var model = ModelCatalog.FindModel("gpt-4-0613");

        Assert.NotNull(model);
        Assert.Equal("gpt-4", model.ModelId);
    }

    [Fact]
    public void FindModel_NoPrefixAliasIsPrefixOfLongerPrefixAlias_Structural()
    {
        // Verify that for every model ID, FindModel returns itself.
        // This is a structural guard against future alias regressions.
        foreach (var (modelId, _) in ModelCatalog.All)
        {
            var found = ModelCatalog.FindModel(modelId);
            Assert.NotNull(found);
            Assert.True(string.Equals(modelId, found.ModelId, StringComparison.OrdinalIgnoreCase),
                $"FindModel(\"{modelId}\") returned \"{found.ModelId}\" instead of itself");
        }
    }

    #endregion

    #region Issue 2 — Null/empty input guard

    [Fact]
    public void FindModel_Null_ReturnsNull()
    {
        var model = ModelCatalog.FindModel(null!);

        Assert.Null(model);
    }

    [Fact]
    public void FindModel_EmptyString_ReturnsNull()
    {
        var model = ModelCatalog.FindModel("");

        Assert.Null(model);
    }

    [Fact]
    public void FindModel_Whitespace_ReturnsNull()
    {
        var model = ModelCatalog.FindModel("   ");

        Assert.Null(model);
    }

    #endregion

    #region Issue 3 — ModelInfoLoader validation

    [Fact]
    public void AllLoadedProviders_HaveNonEmptyNames()
    {
        foreach (var providerName in ModelCatalog.GetProviderNames())
        {
            Assert.False(string.IsNullOrWhiteSpace(providerName),
                "A provider was loaded with an empty or whitespace name");
        }
    }

    [Fact]
    public void AllLoadedProviders_HaveNonEmptyModelLists()
    {
        foreach (var (providerName, models) in ModelCatalog.ByProvider)
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
        var chat = ModelCatalog.FindModel("deepseek-chat");
        var reasoner = ModelCatalog.FindModel("deepseek-reasoner");

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
