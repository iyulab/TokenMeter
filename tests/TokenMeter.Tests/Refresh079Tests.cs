namespace TokenMeter.Tests;

/// <summary>
/// The 0.7.9 refresh: two chat models the vendors serve that the catalog did not know (Claude Sonnet 5.5, GPT-6.1 Sol),
/// and fuzzy matching that no longer crosses a version token — before it, an unknown newer id such as
/// <c>claude-sonnet-5-5</c> matched the older <c>claude-sonnet-5</c> row through its <c>contains</c> alias and was priced
/// as that model, with no sign that it was a guess.
/// </summary>
public class Refresh079Tests
{
    [Theory]
    [InlineData("claude-sonnet-5-5", "claude-sonnet-5-5", 2.00, 10.00, 0.20)]
    [InlineData("anthropic.claude-sonnet-5-5", "claude-sonnet-5-5", 2.00, 10.00, 0.20)]
    [InlineData("gpt-6.1-sol", "gpt-6.1-sol", 2.00, 10.00, 0.10)]
    public void ServedModel_ResolvesToItsOwnRow(string id, string expectedModelId, double input, double output, double cacheRead)
    {
        var model = ModelCatalog.FindModel(id);

        Assert.NotNull(model);
        Assert.Equal(expectedModelId, model.ModelId);
        Assert.Equal((decimal)input, model.InputPricePerMillion);
        Assert.Equal((decimal)output, model.OutputPricePerMillion);
        Assert.Equal((decimal)cacheRead, model.CacheReadPricePerMillion);
    }

    [Fact]
    public void Gpt61Sol_LongContextBand_HalvesTheCacheDiscountLikeTheRest()
    {
        var model = ModelCatalog.FindModel("gpt-6.1-sol", AliasMatchType.Exact);

        Assert.NotNull(model);
        var tier = Assert.Single(model.PricingTiers!);
        Assert.Equal(272000, tier.MinContextLengthTokens);
        Assert.Equal(4.00m, tier.InputPricePerMillion);
        Assert.Equal(15.00m, tier.OutputPricePerMillion);
        Assert.Equal(0.20m, tier.CacheReadPricePerMillion);
    }

    [Theory]
    // A continuation of the alias's version number is a different, newer model — not this row.
    [InlineData("claude-sonnet-5-9")]
    [InlineData("claude-opus-5-5-1")]
    [InlineData("claude-fable-5-7")]
    public void UnknownNewerVersion_DoesNotFallBackToTheOlderRow(string id)
    {
        Assert.Null(ModelCatalog.FindModel(id));
    }

    [Theory]
    // Snapshot and deployment suffixes are not versions: they still resolve to the row they name.
    [InlineData("claude-sonnet-5-20261001", "claude-sonnet-5")]
    [InlineData("claude-opus-4-1-20250805", "claude-opus-4-1")]
    [InlineData("gpt-5-2025-08-07", "gpt-5")]
    [InlineData("grok-4.20-0309-reasoning", "grok-4.20")]
    [InlineData("us.anthropic.claude-sonnet-4.6-v1", "claude-sonnet-4-6")]
    [InlineData("claude-sonnet-5-latest", "claude-sonnet-5")]
    public void SnapshotSuffix_StillResolves(string id, string expectedModelId)
    {
        var model = ModelCatalog.FindModel(id);

        Assert.NotNull(model);
        Assert.Equal(expectedModelId, model.ModelId);
    }
}
