namespace TokenMeter.Tests;

public class ModelCatalogTests
{
    [Fact]
    public void All_ContainsModels()
    {
        Assert.NotEmpty(ModelCatalog.All);
    }

    [Fact]
    public void FindModel_ExactMatch_ReturnsModel()
    {
        var model = ModelCatalog.FindModel("claude-sonnet-4-6");
        Assert.NotNull(model);
        Assert.Equal("claude-sonnet-4-6", model.ModelId);
        Assert.Equal("Anthropic", model.Provider);
    }

    [Fact]
    public void FindModel_AliasMatch_ReturnsCanonicalModel()
    {
        // "claude-sonnet-4.6" is a "contains" alias for "claude-sonnet-4-6"
        var model = ModelCatalog.FindModel("claude-sonnet-4.6");
        Assert.NotNull(model);
        Assert.Equal("claude-sonnet-4-6", model.ModelId);
    }

    [Fact]
    public void FindModel_UnknownId_ReturnsNull()
    {
        Assert.Null(ModelCatalog.FindModel("this-model-does-not-exist-xyz"));
    }

    [Fact]
    public void FindModel_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(ModelCatalog.FindModel(null!));
        Assert.Null(ModelCatalog.FindModel(""));
        Assert.Null(ModelCatalog.FindModel("   "));
    }

    [Fact]
    public void ByProvider_ContainsAnthropicAndOpenAI()
    {
        Assert.True(ModelCatalog.ByProvider.ContainsKey("Anthropic"));
        Assert.True(ModelCatalog.ByProvider.ContainsKey("OpenAI"));
    }

    [Fact]
    public void GetProvider_KnownProvider_ReturnsNonEmptyDict()
    {
        var openai = ModelCatalog.GetProvider("OpenAI");
        Assert.NotEmpty(openai);
        Assert.All(openai.Values, m => Assert.Equal("OpenAI", m.Provider));
    }

    [Fact]
    public void GetProvider_UnknownProvider_ReturnsEmptyDict()
    {
        Assert.Empty(ModelCatalog.GetProvider("NoSuchProvider"));
    }

    [Fact]
    public void GetProvider_CaseInsensitive()
    {
        Assert.NotEmpty(ModelCatalog.GetProvider("openai"));
    }

    [Fact]
    public void GetProvider_MatchesConvenienceProperty()
    {
        // The typed convenience properties delegate to GetProvider(string).
        Assert.Same(ModelCatalog.OpenAI, ModelCatalog.GetProvider("OpenAI"));
        Assert.Same(ModelCatalog.Anthropic, ModelCatalog.GetProvider("Anthropic"));
    }

    [Fact]
    public void GetByType_Chat_ReturnsOnlyChatModels()
    {
        var chatModels = ModelCatalog.GetByType(ModelType.Chat).ToList();
        Assert.NotEmpty(chatModels);
        Assert.All(chatModels, m => Assert.Equal(ModelType.Chat, m.ModelType));
    }

    [Fact]
    public void GetProviderNames_ReturnsKnownProviders()
    {
        var names = ModelCatalog.GetProviderNames().ToList();
        Assert.Contains("Anthropic", names);
        Assert.Contains("OpenAI", names);
        Assert.Contains("Google", names);
    }

    [Fact]
    public void LastUpdated_IsReasonablyRecent()
    {
        Assert.True(ModelCatalog.LastUpdated <= DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.True(ModelCatalog.LastUpdated >= new DateOnly(2026, 1, 1));
    }

    [Fact]
    public void IsDataStale_WithLargeThreshold_ReturnsFalse()
    {
        Assert.False(ModelCatalog.IsDataStale(maxAgeDays: 365));
    }

    [Fact]
    public void FindModel_CaseInsensitive()
    {
        var lower = ModelCatalog.FindModel("claude-sonnet-4-6");
        var upper = ModelCatalog.FindModel("CLAUDE-SONNET-4-6");
        Assert.NotNull(lower);
        Assert.NotNull(upper);
        Assert.Equal(lower!.ModelId, upper!.ModelId);
    }

    // ── Strict / bounded-fuzziness lookup (vault-ai self-hosted mismatch feedback) ──

    [Fact]
    public void FindModel_ExactStrictness_DoesNotMatchSelfHostedDerivativeName()
    {
        // A self-hosted distill name embeds the public "deepseek-r1" prefix alias.
        // Full fuzzy matching resolves it (documented trade-off) …
        Assert.NotNull(ModelCatalog.FindModel("deepseek-r1-distill-qwen-7b"));

        // … but a strict lookup must not: the catalog entry's context window and
        // pricing do not describe the local deployment.
        Assert.Null(ModelCatalog.FindModel("deepseek-r1-distill-qwen-7b", AliasMatchType.Exact));
    }

    [Fact]
    public void FindModel_ExactStrictness_StillMatchesCatalogIdAndExactAlias()
    {
        // Catalog model ID.
        Assert.NotNull(ModelCatalog.FindModel("claude-sonnet-4-6", AliasMatchType.Exact));

        // Exact-type alias ("claude-sonnet-4-0" → claude-sonnet-4 family entry).
        Assert.NotNull(ModelCatalog.FindModel("claude-sonnet-4-0", AliasMatchType.Exact));
    }

    [Fact]
    public void FindModel_PrefixStrictness_AllowsPrefixButNotContains()
    {
        // Prefix alias is within bounds.
        Assert.NotNull(ModelCatalog.FindModel("deepseek-r1-distill-qwen-7b", AliasMatchType.Prefix));

        // Bedrock-style ID only resolvable by a contains alias stays out of bounds …
        Assert.Null(ModelCatalog.FindModel("us.anthropic.claude-sonnet-4.6-v1", AliasMatchType.Prefix));

        // … and resolves again under full fuzziness.
        Assert.NotNull(ModelCatalog.FindModel("us.anthropic.claude-sonnet-4.6-v1", AliasMatchType.Contains));
    }

    [Fact]
    public void FindModel_ContainsStrictness_EqualsDefaultOverload()
    {
        var byDefault = ModelCatalog.FindModel("claude-sonnet-4.6");
        var byBound = ModelCatalog.FindModel("claude-sonnet-4.6", AliasMatchType.Contains);
        Assert.NotNull(byDefault);
        Assert.Same(byDefault, byBound);
    }

    [Fact]
    public void FindModelMatch_ReportsMatchKind()
    {
        // Catalog ID → Exact.
        var exact = ModelCatalog.FindModelMatch("claude-sonnet-4-6");
        Assert.NotNull(exact);
        Assert.Equal(AliasMatchType.Exact, exact!.MatchKind);

        // Prefix alias → Prefix.
        var prefix = ModelCatalog.FindModelMatch("deepseek-r1-distill-qwen-7b");
        Assert.NotNull(prefix);
        Assert.Equal(AliasMatchType.Prefix, prefix!.MatchKind);

        // Contains alias → Contains.
        var contains = ModelCatalog.FindModelMatch("us.anthropic.claude-sonnet-4.6-v1");
        Assert.NotNull(contains);
        Assert.Equal(AliasMatchType.Contains, contains!.MatchKind);
        Assert.Equal("claude-sonnet-4-6", contains.Model.ModelId);
    }

    [Fact]
    public void FindModelMatch_NullOrUnknown_ReturnsNull()
    {
        Assert.Null(ModelCatalog.FindModelMatch(null));
        Assert.Null(ModelCatalog.FindModelMatch("   "));
        Assert.Null(ModelCatalog.FindModelMatch("this-model-does-not-exist-xyz"));
    }
}
