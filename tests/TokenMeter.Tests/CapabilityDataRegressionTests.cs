namespace TokenMeter.Tests;

/// <summary>
/// Regression guard for capability fields that ARE backed by real catalog data.
///
/// These fields are easy to mistake for dead schema: a search for the PascalCase property
/// names (e.g. <c>SupportsAudioInput</c>) finds nothing in the camelCase JSON keys
/// (<c>supportsAudioInput</c>). They are in fact populated. This test fails if the loader
/// stops surfacing them or the underlying data is removed wholesale, so removing them has
/// to be a conscious decision.
/// </summary>
public class CapabilityDataRegressionTests
{
    private static readonly IReadOnlyList<ModelInfo> AllModels = ModelCatalog.All.Values.ToList();

    [Fact]
    public void AudioInput_IsBackedByCatalogData()
        => Assert.Contains(AllModels, m => m.SupportsAudioInput);

    [Fact]
    public void VideoInput_IsBackedByCatalogData()
        => Assert.Contains(AllModels, m => m.SupportsVideoInput);

    [Fact]
    public void InterleavedThinking_IsBackedByCatalogData()
        => Assert.Contains(AllModels, m => m.SupportsInterleavedThinking);

    [Fact]
    public void MaxThinkingTokens_IsBackedByCatalogData()
        => Assert.Contains(AllModels, m => m.MaxThinkingTokens is > 0);
}
