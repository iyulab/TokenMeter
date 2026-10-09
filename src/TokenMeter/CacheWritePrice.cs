namespace TokenMeter;

/// <summary>
/// The price of writing to the prompt cache for one cache lifetime, for a vendor whose write price depends on how long
/// the entry lives (Anthropic: 5-minute and 1-hour writes). <see cref="ModelInfo.CacheWritePricePerMillion"/> stays the
/// price of the vendor's default lifetime.
/// </summary>
public sealed record CacheWritePrice
{
    /// <summary>How long the cache entry lives.</summary>
    public required TimeSpan Ttl { get; init; }

    /// <summary>Cost per 1 million tokens written with this lifetime.</summary>
    public required decimal PricePerMillion { get; init; }
}
