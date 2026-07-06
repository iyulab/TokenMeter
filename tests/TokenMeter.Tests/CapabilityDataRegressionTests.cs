namespace TokenMeter.Tests;

/// <summary>
/// Regression guard for capability fields that ARE backed by real catalog data.
///
/// Context: a 2026-07-06 code-quality review initially mis-flagged these fields as
/// "dead schema" by grepping PascalCase property names (e.g. <c>SupportsAudioInput</c>)
/// against the camelCase JSON keys (<c>supportsAudioInput</c>), yielding zero hits.
/// They are in fact populated. This test fails if the loader stops surfacing them or
/// the underlying data is removed wholesale — forcing a conscious decision rather than
/// a silent PascalCase-grep deletion. See ISSUE-TokenMeter-...-surface-schema-reduction.md.
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
