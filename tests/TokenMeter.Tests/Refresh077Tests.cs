namespace TokenMeter.Tests;

/// <summary>
/// The 0.7.7 refresh of the five providers whose verification waivers expired on 2026-10-01. New rows sit next to
/// shorter prefix aliases (<c>azure-gpt-5</c>, <c>command-r</c>, <c>qwen-max</c>), so each id must resolve to its own
/// row rather than to the older model the shorter alias belongs to.
/// </summary>
public class Refresh077Tests
{
    [Theory]
    [InlineData("azure-gpt-5-mini", "azure-gpt-5-mini", 0.25, 2.00)]
    [InlineData("azure-gpt-5-nano", "azure-gpt-5-nano", 0.05, 0.40)]
    [InlineData("azure-gpt-5.2", "azure-gpt-5.2", 1.75, 14.00)]
    [InlineData("azure-gpt-5-2025-08-07", "azure-gpt-5", 1.25, 10.00)]
    [InlineData("command-r-08-2024", "command-r-08-2024", 0.15, 0.60)]
    [InlineData("command-r-plus-08-2024", "command-r-plus-08-2024", 2.50, 10.00)]
    [InlineData("command-r", "command-r", 0.50, 1.50)]
    [InlineData("qwen3-max-2026-01-23", "qwen3-max", 1.20, 6.00)]
    [InlineData("qwen-max", "qwen-max", 1.60, 6.40)]
    [InlineData("qwen3.8-max", "qwen3.8-max", 2.00, 6.00)]
    public void Id_ResolvesToItsOwnRow(string id, string expectedModelId, double input, double output)
    {
        var model = ModelCatalog.FindModel(id);

        Assert.NotNull(model);
        Assert.Equal(expectedModelId, model.ModelId);
        Assert.Equal((decimal)input, model.InputPricePerMillion);
        Assert.Equal((decimal)output, model.OutputPricePerMillion);
    }

    [Theory]
    [InlineData(100_000, 0.40, 1.20)]
    [InlineData(300_000, 1.20, 3.60)]
    public void QwenPlus_PricesByPromptLength(int promptTokens, double input, double output)
    {
        var model = ModelCatalog.FindModel("qwen-plus")!;

        var cost = model.CalculateCost(1_000_000, 1_000_000, new PricingTierContext { ContextLengthTokens = promptTokens });

        Assert.Equal((decimal)(input + output), cost);
    }

    [Theory]
    [InlineData("Amazon Nova")]
    [InlineData("Azure")]
    [InlineData("Qwen")]
    public void OfficiallySourcedProviders_CarryNoThirdPartyFlag(string provider)
    {
        var models = ModelCatalog.All.Values.Where(m => m.Provider == provider).ToList();

        Assert.NotEmpty(models);
        Assert.All(models, m => Assert.Equal(PriceSource.Official, m.PriceSource));
    }

    [Fact]
    public void CommandA_HasNoFirstPartyRate_AndSaysSo()
        => Assert.Equal(PriceSource.ThirdParty, ModelCatalog.FindModel("command-a")!.PriceSource);
}
