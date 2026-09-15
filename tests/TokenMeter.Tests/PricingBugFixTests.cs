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
    [InlineData("gpt-5.6-sol", 4.00, 20.00, 0.40)]
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

    #region Issue 7 — 2026-09 generations (catalogued 2026-09-15 from the vendor rate cards)

    // Three vendors shipped a new generation in the first week of September 2026 while every
    // provider file was inside the 30-day freshness window, so the scheduled staleness job stayed
    // green: an age check cannot see an absent model. These cases pin the new rows and the one
    // repricing found on the same read (GPT-5.6 Sol, promotional through at least 2026-11-21).
    // Claude 5.1's cache read is 0.025x base input (the 4.x/5.0 rule is 0.1x) — pinned so a
    // "fix the family" edit does not quietly restore the old multiplier.

    [Theory]
    [InlineData("claude-fable-5-1", 10.00, 50.00, 0.25)]
    [InlineData("claude-mythos-5-1", 10.00, 50.00, 0.25)]
    [InlineData("gemini-3.8-flash", 0.75, 3.75, 0.075)]
    [InlineData("gpt-6-astra", 10.00, 50.00, 1.00)]
    public void September2026_Generations_CarryPublishedRates(
        string modelId, decimal input, decimal output, decimal cacheRead)
    {
        var model = ModelCatalog.FindModel(modelId);

        Assert.NotNull(model);
        Assert.Equal(modelId, model.ModelId);
        Assert.Equal(input, model.InputPricePerMillion);
        Assert.Equal(output, model.OutputPricePerMillion);
        Assert.Equal(cacheRead, model.CacheReadPricePerMillion);
    }

    [Fact]
    public void ClaudeFable51_DatedSnapshot_ResolvesToThe51Row_NotThe50Row()
    {
        // "claude-fable-5" is a contains-alias of the 5.0 row and also a substring of every 5.1 id;
        // the longest contains-pattern wins, so a dated 5.1 snapshot must land on the 5.1 row.
        var match = ModelCatalog.FindModelMatch("claude-fable-5-1-20260901");

        Assert.NotNull(match);
        Assert.Equal("claude-fable-5-1", match.Model.ModelId);
    }

    [Fact]
    public void Gpt6Astra_LongContextTier_AppliesAbove272K()
    {
        // developers.openai.com model page: "Prompts with more than 272K input tokens are priced at
        // 2x input and cache rates and 1.5x output for the full request."
        var model = ModelCatalog.FindModel("gpt-6-astra");

        Assert.NotNull(model);
        Assert.Equal(60.00m, model.CalculateCost(1_000_000, 1_000_000, new PricingTierContext { ContextLengthTokens = 200_000 }));
        Assert.Equal(95.00m, model.CalculateCost(1_000_000, 1_000_000, new PricingTierContext { ContextLengthTokens = 300_000 }));
    }

    [Fact]
    public void Gemini38Flash_CostAtIntroductoryRate()
    {
        var model = ModelCatalog.FindModel("gemini-3.8-flash");

        Assert.NotNull(model);
        // The vendor states this rate doubles on 2027-01-01 — scripts/check-catalog-staleness.ps1
        // ($announced) is what reminds the maintainer; this case pins today's row.
        Assert.Equal(4.50m, model.CalculateCost(1_000_000, 1_000_000));
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

    #region Issue 7 — flagship models absent from the catalogue

    // Auditing every provider against its published rate card showed the prices that were
    // present were largely right — what had gone stale was the model list. Current flagship
    // models were missing entirely, so looking them up returned nothing (or, worse, fuzzy-
    // matched an older sibling whose limits and rates do not describe them).

    [Theory]
    [InlineData("claude-opus-5", 5.00, 25.00)]
    [InlineData("claude-mythos-5", 10.00, 50.00)]
    [InlineData("gemini-3.6-flash", 0.75, 3.75)]
    [InlineData("gemini-3.5-flash-lite", 0.30, 2.50)]
    [InlineData("deepseek-v4-flash", 0.22, 0.66)]
    [InlineData("deepseek-v4-pro", 0.66, 1.98)]
    public void CurrentFlagshipModels_ResolveExactly_AndCarryPublishedRates(
        string modelId, decimal input, decimal output)
    {
        var match = ModelCatalog.FindModelMatch(modelId);

        Assert.NotNull(match);
        Assert.Equal(modelId, match.Model.ModelId);
        Assert.Equal(input, match.Model.InputPricePerMillion);
        Assert.Equal(output, match.Model.OutputPricePerMillion);

        // Every one of these carries a context window; a catalogue entry that cannot state
        // the limit is only half an answer for the caller sizing a request.
        Assert.NotNull(match.Model.ContextWindow);
    }

    [Fact]
    public void ModelIdThatIsAPrefixOfAnother_DoesNotSwallowTheLongerOne()
    {
        // "gemini-3.5-flash" is a proper prefix of "gemini-3.5-flash-lite", and the two are
        // priced an order of magnitude apart. Longest-match has to pick the specific one.
        var lite = ModelCatalog.FindModel("gemini-3.5-flash-lite");
        var flash = ModelCatalog.FindModel("gemini-3.5-flash");

        Assert.NotNull(lite);
        Assert.NotNull(flash);
        Assert.Equal("gemini-3.5-flash-lite", lite.ModelId);
        Assert.Equal("gemini-3.5-flash", flash.ModelId);
        Assert.True(lite.InputPricePerMillion < flash.InputPricePerMillion);
    }

    // An id ending in "-latest" is a moving pointer, not a pinned release. When the provider
    // advances it to a new version, the rate has to move with it — otherwise the entry keeps
    // quoting a superseded version's price under a name that no longer refers to it. Two of
    // these were understating cost by more than three times.
    [Theory]
    [InlineData("mistral-large-latest", 0.50, 1.50)]
    [InlineData("mistral-medium-latest", 1.50, 7.50)]
    [InlineData("mistral-small-latest", 0.15, 0.60)]
    public void EvergreenModelIds_QuoteTheVersionTheyCurrentlyResolveTo(
        string modelId, decimal input, decimal output)
    {
        var model = ModelCatalog.FindModel(modelId);

        Assert.NotNull(model);
        Assert.Equal(input, model.InputPricePerMillion);
        Assert.Equal(output, model.OutputPricePerMillion);
    }

    #endregion

    #region Issue 8 — capability metadata audit (non-price fields)

    // HD-40(f): auditing capability flags (not prices) on the 4 providers just re-verified for
    // pricing (0.7.2) turned up gaps unrelated to price — modalities and context windows that
    // were never filled in, or were carrying a stale/incorrect figure. Cross-referenced against
    // each vendor's own published FAQ/spec via a third-party host page (same PriceSource.ThirdParty
    // sourcing as the price fields on these models).

    [Theory]
    [InlineData("amazon-nova-lite", true, true, false)]   // image + video; no document input
    [InlineData("amazon-nova-2-lite", true, true, true)]  // image + video + document (PDF)
    public void AmazonNova_MultimodalFlags_MatchVendorFaq(
        string modelId, bool image, bool video, bool document)
    {
        var model = ModelCatalog.FindModel(modelId);

        Assert.NotNull(model);
        Assert.Equal(image, model.SupportsImageInput);
        Assert.Equal(video, model.SupportsVideoInput);
        Assert.Equal(document, model.SupportsDocumentInput);
    }

    [Fact]
    public void AzureO4Mini_CachingMirrorsOpenAIsO4Mini()
    {
        // azure-o4-mini carried no promptCachingMode/cacheReadPricePerMillion at all, unlike every
        // other reasoning-tier Azure model — an omission, not a documented Azure limitation (Azure
        // mirrors OpenAI's o4-mini, which does support automatic caching at a discounted rate).
        var azure = ModelCatalog.FindModel("azure-o4-mini");
        var openai = ModelCatalog.FindModel("o4-mini");

        Assert.NotNull(azure);
        Assert.NotNull(openai);
        Assert.Equal(openai.PromptCachingMode, azure.PromptCachingMode);
        Assert.Equal(openai.CacheReadPricePerMillion, azure.CacheReadPricePerMillion);
    }

    [Theory]
    [InlineData("llama-4-maverick")]
    [InlineData("llama-4-scout")]
    public void MetaLlama4Family_SupportsImageInputAndStructuredOutput(string modelId)
    {
        // Both models accept text+image input and support response_format JSON-schema structured
        // output per their vendor page — neither flag was set in the catalogue.
        var model = ModelCatalog.FindModel(modelId);

        Assert.NotNull(model);
        Assert.True(model.SupportsImageInput);
        Assert.True(model.SupportsStructuredOutput);
    }

    [Theory]
    [InlineData("qwen-max", 262144, 65536)]
    [InlineData("qwen-plus", 1000000, 32768)]
    public void Qwen_ContextWindow_MatchesVendorSpec_NotAnOlderGenerationsLimit(
        string modelId, int contextWindow, int maxOutputTokens)
    {
        // Both entries carried a flat 128K context window — Qwen3 Max's real window is 262,144 and
        // Qwen Plus's is 1,000,000; the stale figure understated both by a wide margin (roughly
        // 2x and 8x respectively). Structured output was also unset despite both models supporting
        // response_format JSON-schema output.
        var model = ModelCatalog.FindModel(modelId);

        Assert.NotNull(model);
        Assert.Equal(contextWindow, model.ContextWindow);
        Assert.Equal(maxOutputTokens, model.MaxOutputTokens);
        Assert.True(model.SupportsStructuredOutput);
    }

    #endregion
}
