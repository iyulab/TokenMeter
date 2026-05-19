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
}
