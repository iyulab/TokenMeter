namespace TokenMeter;

/// <summary>
/// A price change the vendor has announced with a date — the rates that take effect on <see cref="EffectiveFrom"/>.
/// A <c>null</c> rate is unchanged. Apply with <see cref="ModelInfo.AsOf(DateOnly)"/>, which returns the model with
/// the rates in effect on a date.
/// </summary>
public sealed record ScheduledPrice
{
    /// <summary>The first UTC date the new rates apply.</summary>
    public required DateOnly EffectiveFrom { get; init; }

    /// <summary>New cost per 1 million input tokens, or <c>null</c> when unchanged.</summary>
    public decimal? InputPricePerMillion { get; init; }

    /// <summary>New cost per 1 million output tokens, or <c>null</c> when unchanged.</summary>
    public decimal? OutputPricePerMillion { get; init; }

    /// <summary>New cost per 1 million cache-read tokens, or <c>null</c> when unchanged.</summary>
    public decimal? CacheReadPricePerMillion { get; init; }

    /// <summary>New cost per 1 million cache-write tokens (default lifetime), or <c>null</c> when unchanged.</summary>
    public decimal? CacheWritePricePerMillion { get; init; }
}
