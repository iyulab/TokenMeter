namespace TokenMeter.Tests;

/// <summary>
/// GPT-6.2 Sol and GPT-6.2 Astra: served by the OpenAI models endpoint and listed on the pricing page, but without a catalog
/// row until the coverage check named them. Without the rows the ids fell through the version-token guard to null, i.e. unpriced.
/// </summary>
public class Gpt62Tests
{
    [Theory]
    [InlineData("gpt-6.2-sol", 2.00, 10.00, 0.10, 2.50)]
    [InlineData("gpt-6.2-sol-2026-11-01", 2.00, 10.00, 0.10, 2.50)]
    [InlineData("gpt-6.2-astra", 8.00, 40.00, 0.40, 10.00)]
    [InlineData("gpt-6.2-astra-2026-11-01", 8.00, 40.00, 0.40, 10.00)]
    public void ServedId_ResolvesToItsOwnRow(string id, double input, double output, double cacheRead, double cacheWrite)
    {
        var model = ModelCatalog.FindModel(id);

        Assert.NotNull(model);
        Assert.StartsWith("gpt-6.2-", model.ModelId);
        Assert.Equal((decimal)input, model.InputPricePerMillion);
        Assert.Equal((decimal)output, model.OutputPricePerMillion);
        Assert.Equal((decimal)cacheRead, model.CacheReadPricePerMillion);
        Assert.Equal((decimal)cacheWrite, model.CacheWritePricePerMillion);
        Assert.Equal(1_050_000, model.ContextWindow);
        Assert.Equal(128_000, model.MaxOutputTokens);
    }

    [Theory]
    [InlineData("gpt-6.2-sol", 4.00, 15.00, 0.20, 5.00)]
    [InlineData("gpt-6.2-astra", 16.00, 60.00, 0.80, 20.00)]
    public void LongContextBand_StartsAbove272KTokens(string id, double input, double output, double cacheRead, double cacheWrite)
    {
        var model = ModelCatalog.FindModel(id, AliasMatchType.Exact);

        Assert.NotNull(model);
        var tier = Assert.Single(model.PricingTiers!);
        Assert.Equal(272000, tier.MinContextLengthTokens);
        Assert.Equal((decimal)input, tier.InputPricePerMillion);
        Assert.Equal((decimal)output, tier.OutputPricePerMillion);
        Assert.Equal((decimal)cacheRead, tier.CacheReadPricePerMillion);
        Assert.Equal((decimal)cacheWrite, tier.CacheWritePricePerMillion);
    }

    [Fact]
    public void ServiceTiers_AreOnlyTheOnesThePageListsForTheseModels()
    {
        // Batch is half and Fast twice the Standard rate on the page; the Flex table has no 6.2 row, so the row carries no Flex multiplier.
        foreach (var id in new[] { "gpt-6.2-sol", "gpt-6.2-astra" })
        {
            var multipliers = ModelCatalog.FindModel(id, AliasMatchType.Exact)!.ServiceTierMultipliers!
                .ToDictionary(m => m.Tier, m => m.Multiplier);
            Assert.Equal(0.5m, multipliers[ServiceTier.Batch]);
            Assert.Equal(2.0m, multipliers[ServiceTier.Fast]);
            Assert.False(multipliers.ContainsKey(ServiceTier.Flex));
        }
    }

    [Fact]
    public void NeighbouringGenerations_KeepTheirOwnRows()
    {
        Assert.Equal("gpt-6.1-sol", ModelCatalog.FindModel("gpt-6.1-sol")!.ModelId);
        Assert.Equal("gpt-6-astra", ModelCatalog.FindModel("gpt-6-astra")!.ModelId);
        Assert.Equal("gpt-6-sol", ModelCatalog.FindModel("gpt-6-sol")!.ModelId);
    }
}
