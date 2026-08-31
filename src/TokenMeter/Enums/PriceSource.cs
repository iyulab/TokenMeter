namespace TokenMeter;

/// <summary>
/// How a model's <see cref="ModelInfo"/> pricing fields were obtained.
/// </summary>
public enum PriceSource
{
    /// <summary>
    /// Verified directly against the vendor's own published rate card.
    /// </summary>
    Official = 0,

    /// <summary>
    /// The vendor's own rate card could not be read directly (client-side-rendered pricing page,
    /// no direct API pricing published, docs-only landing page, etc.), so the figure was
    /// cross-referenced from a third-party aggregator or another vendor's official rate card
    /// (e.g. an Azure OpenAI price mirrored from OpenAI's own published rate under Microsoft's
    /// documented price-parity policy). Treat as an estimate for cost-sensitive accounting.
    /// </summary>
    ThirdParty,
}
