namespace TokenMeter;

/// <summary>
/// The axis along which a <see cref="PricingTier"/> varies from a model's representative rate.
/// </summary>
public enum PricingTierAxis
{
    /// <summary>Price changes once the request's prompt/context length crosses a threshold.</summary>
    ContextLength,

    /// <summary>Price changes during a provider-defined UTC time window (e.g. peak-hour surcharge).</summary>
    TimeOfDay
}

/// <summary>
/// A non-representative price band for a model — one that applies only under a specific
/// context-length or time-of-day condition, layered on top of <see cref="ModelInfo"/>'s
/// representative <c>*PricePerMillion</c> fields (which remain the price outside every tier).
/// </summary>
public sealed record PricingTier
{
    /// <summary>Which condition this tier's price depends on.</summary>
    public required PricingTierAxis Axis { get; init; }

    /// <summary>
    /// <see cref="PricingTierAxis.ContextLength"/> only: this tier applies when the request's
    /// total context length is at least this many tokens.
    /// </summary>
    public int? MinContextLengthTokens { get; init; }

    /// <summary>
    /// <see cref="PricingTierAxis.TimeOfDay"/> only: inclusive start of the UTC window this
    /// tier applies during.
    /// </summary>
    public TimeOnly? WindowStartUtc { get; init; }

    /// <summary>
    /// <see cref="PricingTierAxis.TimeOfDay"/> only: exclusive end of the UTC window this
    /// tier applies during. A provider with multiple disjoint windows (e.g. two peak periods)
    /// is represented as one <see cref="PricingTier"/> per window, all sharing the same price.
    /// </summary>
    public TimeOnly? WindowEndUtc { get; init; }

    /// <summary>Cost per 1 million input tokens while this tier applies.</summary>
    public required decimal InputPricePerMillion { get; init; }

    /// <summary>Cost per 1 million output tokens while this tier applies. Falls back to the model's representative rate if <c>null</c>.</summary>
    public decimal? OutputPricePerMillion { get; init; }

    /// <summary>Cost per 1 million cache-read tokens while this tier applies. Falls back to the model's representative rate if <c>null</c>.</summary>
    public decimal? CacheReadPricePerMillion { get; init; }
}

/// <summary>
/// The request-time facts a <see cref="PricingTier"/> lookup needs. Fields left <c>null</c>
/// simply do not activate tiers on that axis — supplying neither reproduces the untiered,
/// representative-rate calculation exactly.
/// </summary>
public readonly record struct PricingTierContext
{
    /// <summary>Total context length (prompt + expected completion) for the request, if known.</summary>
    public int? ContextLengthTokens { get; init; }

    /// <summary>Wall-clock time of the call in UTC, if the caller wants time-of-day tiers applied.</summary>
    public TimeOnly? CallTimeUtc { get; init; }
}
