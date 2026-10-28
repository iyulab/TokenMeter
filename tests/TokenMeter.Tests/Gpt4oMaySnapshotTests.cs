namespace TokenMeter.Tests;

/// <summary>
/// The 2024-05-13 snapshot of GPT-4o is listed on the price page at twice the rate of the current GPT-4o ($5.00 / $15.00 against
/// $2.50 / $10.00). The dated id used to fall under the <c>gpt-4o</c> prefix and was priced as the newer model, with no sign
/// that it was a guess. The row carries a <c>prefix</c> alias, not an exact one, on purpose: the longest prefix wins over
/// <c>gpt-4o</c>, and the catalog rule that a dated exact name also resolves exactly through its undated name does not apply
/// to a snapshot that is deliberately priced differently from the undated model.
/// </summary>
public class Gpt4oMaySnapshotTests
{
    [Fact]
    public void May2024Snapshot_HasItsOwnRate()
    {
        var model = ModelCatalog.FindModel("gpt-4o-2024-05-13");

        Assert.NotNull(model);
        Assert.Equal("gpt-4o-2024-05-13", model.ModelId);
        Assert.Equal(5.00m, model.InputPricePerMillion);
        Assert.Equal(15.00m, model.OutputPricePerMillion);
        Assert.Null(model.CacheReadPricePerMillion);
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-2024-08-06")]
    [InlineData("gpt-4o-2024-11-20")]
    public void OtherGpt4oIds_KeepTheCurrentRate(string id)
    {
        var model = ModelCatalog.FindModel(id);

        Assert.NotNull(model);
        Assert.Equal("gpt-4o", model.ModelId);
        Assert.Equal(2.50m, model.InputPricePerMillion);
        Assert.Equal(10.00m, model.OutputPricePerMillion);
    }
}
