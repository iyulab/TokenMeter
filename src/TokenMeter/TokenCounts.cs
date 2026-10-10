namespace TokenMeter;

/// <summary>
/// The tokens of one request, split the way vendors price them — the input to
/// <see cref="ModelInfo.CalculateCost(TokenCounts, CostContext)"/>.
/// </summary>
/// <remarks>
/// The three input parts do not overlap: <see cref="InputTokens"/> is the input that was neither read from nor written
/// to the prompt cache. A response that reports one total input (OpenAI <c>input_tokens</c> includes cached tokens)
/// is split by the caller: total − cache reads − cache writes.
/// </remarks>
public sealed record TokenCounts
{
    /// <summary>Input tokens billed at the input price: neither read from nor written to the prompt cache.</summary>
    public int InputTokens { get; init; }

    /// <summary>Input tokens read from the prompt cache.</summary>
    public int CacheReadTokens { get; init; }

    /// <summary>
    /// Input tokens written to the prompt cache, by the lifetime they were written with. Null or empty when the request
    /// wrote nothing.
    /// </summary>
    public IReadOnlyList<CacheWriteTokenCount>? CacheWrites { get; init; }

    /// <summary>Output tokens, reasoning included (every vendor bills reasoning at the output price).</summary>
    public int OutputTokens { get; init; }

    /// <summary>
    /// The prompt length a vendor's long-context tier is decided by: every input token, cache reads and writes
    /// included.
    /// </summary>
    public int PromptTokens => InputTokens + CacheReadTokens + (CacheWrites?.Sum(w => w.Tokens) ?? 0);
}

/// <summary>
/// Tokens written to the prompt cache with one lifetime. <paramref name="Ttl"/> null means the vendor's default
/// lifetime (priced at <see cref="ModelInfo.CacheWritePricePerMillion"/>).
/// </summary>
public sealed record CacheWriteTokenCount(TimeSpan? Ttl, int Tokens);

/// <summary>
/// When a request was made, for the prices that depend on it: <see cref="Date"/> selects the rates in effect that day
/// (<see cref="ModelInfo.AsOf(DateOnly)"/>), <see cref="CallTimeUtc"/> a time-of-day tier. Both null = the catalog's
/// current rates, no time-of-day tier.
/// </summary>
public readonly record struct CostContext
{
    /// <summary>The UTC date of the call, or null for the current rates.</summary>
    public DateOnly? Date { get; init; }

    /// <summary>The UTC time of the call, for time-of-day tiers; null applies none.</summary>
    public TimeOnly? CallTimeUtc { get; init; }

    /// <summary>The context for a call made at <paramref name="utc"/>: its date and its time of day.</summary>
    public static CostContext At(DateTimeOffset utc)
    {
        var instant = utc.UtcDateTime;
        return new CostContext { Date = DateOnly.FromDateTime(instant), CallTimeUtc = TimeOnly.FromDateTime(instant) };
    }
}
