namespace TokenMeter.Tests;

/// <summary>
/// The 0.7.8 refresh: Azure's GPT-5.4 / 5.5 / 5.6 / 6 Astra rows (Azure Retail Prices API, Global Standard), and the
/// GPT-5.6 cache-write rates that a key written twice in <c>openai.json</c> had left at a stale value.
/// </summary>
public class Refresh078Tests
{
    [Theory]
    // Before 0.7.8 each of these fell under the shorter `azure-gpt-5` prefix and was priced as GPT-5 ($1.25 / $10).
    [InlineData("azure-gpt-5.4", "azure-gpt-5.4", 2.50, 15.00)]
    [InlineData("azure-gpt-5.4-2026-03-05", "azure-gpt-5.4", 2.50, 15.00)]
    [InlineData("azure-gpt-5.4-pro", "azure-gpt-5.4-pro", 30.00, 180.00)]
    [InlineData("azure-gpt-5.4-mini", "azure-gpt-5.4-mini", 0.75, 4.50)]
    [InlineData("azure-gpt-5.4-nano", "azure-gpt-5.4-nano", 0.20, 1.25)]
    [InlineData("azure-gpt-5.5", "azure-gpt-5.5", 5.00, 30.00)]
    [InlineData("azure-gpt-5.6-sol", "azure-gpt-5.6-sol", 4.00, 20.00)]
    [InlineData("azure-gpt-5.6-terra", "azure-gpt-5.6-terra", 2.00, 12.00)]
    [InlineData("azure-gpt-5.6-luna", "azure-gpt-5.6-luna", 0.20, 1.20)]
    [InlineData("azure-gpt-6-astra", "azure-gpt-6-astra", 10.00, 50.00)]
    public void AzureId_ResolvesToItsOwnRow(string id, string expectedModelId, double input, double output)
    {
        var model = ModelCatalog.FindModel(id);

        Assert.NotNull(model);
        Assert.Equal(expectedModelId, model.ModelId);
        Assert.Equal((decimal)input, model.InputPricePerMillion);
        Assert.Equal((decimal)output, model.OutputPricePerMillion);
        Assert.Equal(PriceSource.Official, model.PriceSource);
    }

    [Theory]
    [InlineData("azure-gpt-5.4", 100_000, 2.50, 15.00)]
    [InlineData("azure-gpt-5.4", 300_000, 5.00, 22.50)]
    [InlineData("azure-gpt-5.6-sol", 300_000, 8.00, 30.00)]
    [InlineData("azure-gpt-6-astra", 300_000, 20.00, 75.00)]
    public void AzureLongContextBand_AppliesAbove272kPromptTokens(string id, int promptTokens, double input, double output)
    {
        var model = ModelCatalog.FindModel(id)!;

        var cost = model.CalculateCost(1_000_000, 1_000_000, new PricingTierContext { ContextLengthTokens = promptTokens });

        Assert.Equal((decimal)(input + output), cost);
    }
}
