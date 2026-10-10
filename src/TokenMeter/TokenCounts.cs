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
    /// Calls to server-side tools the vendor bills per call, by the vendor's tool name (<c>web_search</c>,
    /// <c>file_search</c>, <c>google_search</c> — see <see cref="ModelInfo.ToolCallPrices"/>). Null or empty when none.
    /// </summary>
    public IReadOnlyDictionary<string, int>? ToolCalls { get; init; }

    /// <summary>
    /// How many of <see cref="InputTokens"/> were in each modality, as vendors report it (Gemini
    /// <c>promptTokensDetails</c> less the cached part, OpenAI <c>input_tokens_details.audio_tokens</c>). A breakdown of the
    /// total, not an addition to it — modalities left out stay at the representative rate. Null when not reported.
    /// </summary>
    public IReadOnlyDictionary<TokenModality, int>? InputTokensByModality { get; init; }

    /// <summary>
    /// How many of <see cref="CacheReadTokens"/> were in each modality (Gemini <c>cacheTokensDetails</c>). A breakdown of the
    /// total. Null when not reported.
    /// </summary>
    public IReadOnlyDictionary<TokenModality, int>? CacheReadTokensByModality { get; init; }

    /// <summary>
    /// How many of <see cref="OutputTokens"/> were in each modality (Gemini <c>candidatesTokensDetails</c> — image output on
    /// an image model). A breakdown of the total. Null when not reported.
    /// </summary>
    public IReadOnlyDictionary<TokenModality, int>? OutputTokensByModality { get; init; }

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
/// The facts about a request the price depends on besides its tokens: <see cref="Date"/> selects the rates in effect that
/// day (<see cref="ModelInfo.AsOf(DateOnly)"/>), <see cref="CallTimeUtc"/> a time-of-day tier, <see cref="ServiceTier"/>
/// and <see cref="Region"/> the vendor's multipliers. The default is the catalog's current standard rates.
/// </summary>
public readonly record struct CostContext
{
    /// <summary>The UTC date of the call, or null for the current rates.</summary>
    public DateOnly? Date { get; init; }

    /// <summary>The UTC time of the call, for time-of-day tiers; null applies none.</summary>
    public TimeOnly? CallTimeUtc { get; init; }

    /// <summary>How the request was served (<see cref="TokenMeter.ServiceTier.Standard"/> by default).</summary>
    public ServiceTier ServiceTier { get; init; }

    /// <summary>The inference region requested (<c>us</c>), or null for the vendor's default (global) routing.</summary>
    public string? Region { get; init; }

    /// <summary>The context for a call made at <paramref name="utc"/>: its date and its time of day.</summary>
    public static CostContext At(DateTimeOffset utc)
    {
        var instant = utc.UtcDateTime;
        return new CostContext { Date = DateOnly.FromDateTime(instant), CallTimeUtc = TimeOnly.FromDateTime(instant) };
    }
}
