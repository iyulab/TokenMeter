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

    #region Issue 5 — GPT-5.6 mid-generation price reduction

    // The GPT-5.6 tiers were repriced after their launch rates were first catalogued:
    // the small tier dropped by 80% and the mid tier by 20%, while the large tier was
    // left unchanged. A flat catalogue has no way to signal that its numbers went stale,
    // so cost estimates stayed silently high until the data was corrected. These cases
    // pin the corrected rates and, just as importantly, pin the tier that did NOT move —
    // a bulk edit that "fixes the family" is the likeliest way to reintroduce the error.

    [Theory]
    [InlineData("gpt-5.6-luna", 0.20, 1.20, 0.02)]
    [InlineData("gpt-5.6-terra", 2.00, 12.00, 0.20)]
    [InlineData("gpt-5.6-sol", 5.00, 30.00, 0.50)]
    public void Gpt56_Tiers_CarryCurrentPublishedRates(
        string modelId, decimal input, decimal output, decimal cacheRead)
    {
        var model = ModelCatalog.FindModel(modelId);

        Assert.NotNull(model);
        Assert.Equal(input, model.InputPricePerMillion);
        Assert.Equal(output, model.OutputPricePerMillion);
        Assert.Equal(cacheRead, model.CacheReadPricePerMillion);
    }

    [Fact]
    public void Gpt56_SmallTier_CostReflectsReducedRates()
    {
        var model = ModelCatalog.FindModel("gpt-5.6-luna");

        Assert.NotNull(model);

        // One million input + one million output tokens. At the pre-reduction rates this
        // returned 7.00 — a five-fold overstatement reported by a downstream consumer.
        Assert.Equal(1.40m, model.CalculateCost(1_000_000, 1_000_000));
    }

    [Fact]
    public void Gpt56_TierOrdering_SmallCheaperThanMidCheaperThanLarge()
    {
        // Structural guard: the tiers are priced as a ladder. Any future refresh that
        // inverts the ordering has almost certainly transcribed a row into the wrong model.
        var small = ModelCatalog.FindModel("gpt-5.6-luna");
        var mid = ModelCatalog.FindModel("gpt-5.6-terra");
        var large = ModelCatalog.FindModel("gpt-5.6-sol");

        Assert.NotNull(small);
        Assert.NotNull(mid);
        Assert.NotNull(large);

        Assert.True(small.InputPricePerMillion < mid.InputPricePerMillion);
        Assert.True(mid.InputPricePerMillion < large.InputPricePerMillion);
        Assert.True(small.OutputPricePerMillion < mid.OutputPricePerMillion);
        Assert.True(mid.OutputPricePerMillion < large.OutputPricePerMillion);
    }

    #endregion

    #region Issue 6 — cached-token pricing coverage

    // Two gaps found while auditing the catalogue against the published rate card.
    //
    // (a) Cache writes. Automatic prompt caching historically billed the first pass at the
    //     normal input rate, so leaving the cache-write price unset — and letting
    //     CalculateCost fall back to the input rate — produced the right number. The GPT-5.6
    //     tiers broke that assumption by charging a premium over input for cache writes, at
    //     which point the fallback silently understated the cache-write component.
    //
    // (b) Cached reads on the reasoning series. Those models were catalogued as having no
    //     prompt caching at all, so cache-read tokens fell back to the full input rate —
    //     an overstatement of up to four times on the cached portion of a request.

    [Theory]
    [InlineData("gpt-5.6-sol", 6.25)]
    [InlineData("gpt-5.6-terra", 2.50)]
    [InlineData("gpt-5.6-luna", 0.25)]
    public void CacheWrite_PricedAboveInput_IsNotLeftToTheInputFallback(string modelId, decimal cacheWrite)
    {
        var model = ModelCatalog.FindModel(modelId);

        Assert.NotNull(model);
        Assert.Equal(cacheWrite, model.CacheWritePricePerMillion);

        // A premium over input is exactly the case the fallback cannot express.
        Assert.True(model.CacheWritePricePerMillion > model.InputPricePerMillion);

        // One million cache-write tokens, nothing else: the vendor rate, not the input rate.
        Assert.Equal(cacheWrite, model.CalculateCost(0, 0, 0, 1_000_000));
    }

    [Theory]
    [InlineData("o1", 7.50)]
    [InlineData("o3", 0.50)]
    [InlineData("o3-mini", 0.55)]
    [InlineData("o4-mini", 0.275)]
    public void ReasoningSeries_CachedReads_ArePricedAndAdvertised(string modelId, decimal cacheRead)
    {
        var model = ModelCatalog.FindModel(modelId);

        Assert.NotNull(model);
        Assert.Equal(cacheRead, model.CacheReadPricePerMillion);

        // Price and capability flag have to agree — a discounted rate with caching reported
        // as unsupported is how the gap stayed invisible.
        Assert.Equal(PromptCachingMode.Automatic, model.PromptCachingMode);
        Assert.True(model.CacheReadPricePerMillion < model.InputPricePerMillion);
    }

    [Fact]
    public void PricedCacheRead_ImpliesCachingIsAdvertised_Structural()
    {
        // Structural guard across every provider: a model that carries a cache-read price
        // must not simultaneously claim it has no prompt caching.
        foreach (var (modelId, model) in ModelCatalog.All)
        {
            if (model.CacheReadPricePerMillion is null) continue;

            Assert.True(model.PromptCachingMode != PromptCachingMode.None,
                $"\"{modelId}\" has a cache-read price but reports PromptCachingMode.None");
        }
    }

    #endregion
}
