namespace TokenMeter.Tests;

/// <summary>
/// GPT-6.1 Terra: served by the OpenAI models endpoint and listed on the pricing page, but without a catalog row until the
/// coverage check named it. Without the row the id fell through the version-token guard to null, i.e. unpriced.
/// </summary>
public class Gpt61TerraTests
{
    [Theory]
    [InlineData("gpt-6.1-terra")]
    [InlineData("gpt-6.1-terra-2026-10-01")]
    public void ServedId_ResolvesToItsOwnRow(string id)
    {
        var model = ModelCatalog.FindModel(id);

        Assert.NotNull(model);
        Assert.Equal("gpt-6.1-terra", model.ModelId);
        Assert.Equal(1.00m, model.InputPricePerMillion);
        Assert.Equal(5.00m, model.OutputPricePerMillion);
        Assert.Equal(0.05m, model.CacheReadPricePerMillion);
        Assert.Equal(1.25m, model.CacheWritePricePerMillion);
    }

    [Fact]
    public void LongContextBand_StartsAbove272KTokens()
    {
        var model = ModelCatalog.FindModel("gpt-6.1-terra", AliasMatchType.Exact);

        Assert.NotNull(model);
        var tier = Assert.Single(model.PricingTiers!);
        Assert.Equal(272000, tier.MinContextLengthTokens);
        Assert.Equal(2.00m, tier.InputPricePerMillion);
        Assert.Equal(7.50m, tier.OutputPricePerMillion);
        Assert.Equal(0.10m, tier.CacheReadPricePerMillion);
        Assert.Equal(2.50m, tier.CacheWritePricePerMillion);
    }

    [Fact]
    public void NeighbouringGenerations_KeepTheirOwnRows()
    {
        // The new row must not capture the older Terra, nor be captured by the Sol sibling's prefix.
        Assert.Equal("gpt-5.6-terra", ModelCatalog.FindModel("gpt-5.6-terra")!.ModelId);
        Assert.Equal("gpt-6.1-sol", ModelCatalog.FindModel("gpt-6.1-sol")!.ModelId);
    }
}
