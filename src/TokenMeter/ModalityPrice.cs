namespace TokenMeter;

/// <summary>The kind of content a token carries, as vendors report usage per modality.</summary>
public enum TokenModality
{
    /// <summary>Text (the representative rate).</summary>
    Text,

    /// <summary>Images.</summary>
    Image,

    /// <summary>Audio.</summary>
    Audio,

    /// <summary>Video.</summary>
    Video,

    /// <summary>Documents (PDF and the like), where the vendor reports them apart.</summary>
    Document,
}

/// <summary>Which part of a request a token price applies to.</summary>
public enum TokenUse
{
    /// <summary>Input neither read from nor written to the prompt cache.</summary>
    Input,

    /// <summary>Input read from the prompt cache.</summary>
    CacheRead,

    /// <summary>Output.</summary>
    Output,
}

/// <summary>
/// A per-token price for one modality where the vendor prices it apart from the representative rate (Gemini 2.5 Flash audio
/// input $1.00 against $0.30; Gemini image models' image output $60.00 against $3.00 for text). Applied by
/// <see cref="ModelInfo.CalculateCost(TokenCounts, CostContext)"/> to the tokens a usage reports in that modality.
/// </summary>
public sealed record ModalityPrice
{
    /// <summary>The modality.</summary>
    public required TokenModality Modality { get; init; }

    /// <summary>The part of the request the price applies to.</summary>
    public required TokenUse Use { get; init; }

    /// <summary>Cost per 1 million tokens of this modality.</summary>
    public required decimal PricePerMillion { get; init; }
}
