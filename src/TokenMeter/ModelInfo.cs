namespace TokenMeter;

/// <summary>
/// Comprehensive metadata for an LLM model, including pricing, token limits,
/// supported modalities, API capabilities, and reasoning/thinking configuration.
/// All capability flags default to <c>false</c> (opt-in).
/// All price fields use USD per 1 million tokens unless noted otherwise.
/// </summary>
public record ModelInfo
{
    // ── Identification ────────────────────────────────────────────────────────

    /// <summary>Canonical model identifier used in API calls (e.g., "gpt-4o", "claude-opus-4-6").</summary>
    public required string ModelId { get; init; }

    /// <summary>Provider name (e.g., "OpenAI", "Anthropic", "Google").</summary>
    public string? Provider { get; init; }

    /// <summary>Human-readable display name (e.g., "Claude Opus 4.7").</summary>
    public string? DisplayName { get; init; }

    // ── Model Classification ──────────────────────────────────────────────────

    /// <summary>Primary output type of the model. Defaults to <see cref="ModelType.Chat"/>.</summary>
    public ModelType ModelType { get; init; }

    /// <summary>Whether this is an instruction-tuned model (as opposed to a base/pre-trained model).</summary>
    public bool IsInstructTuned { get; init; }

    // ── Token Limits ──────────────────────────────────────────────────────────

    /// <summary>Maximum total tokens the model can process as input (context window).</summary>
    public int? ContextWindow { get; init; }

    /// <summary>Maximum tokens the model can generate in a single response.</summary>
    public int? MaxOutputTokens { get; init; }

    // ── Pricing (USD / 1M tokens) ─────────────────────────────────────────────

    /// <summary>Cost per 1 million input tokens. <c>null</c> if unknown or free.</summary>
    public decimal? InputPricePerMillion { get; init; }

    /// <summary>Cost per 1 million output tokens. <c>null</c> if unknown or free.</summary>
    public decimal? OutputPricePerMillion { get; init; }

    /// <summary>
    /// Cost per 1 million cache-read tokens (prompt cache hit).
    /// Typically a significant discount vs <see cref="InputPricePerMillion"/>.
    /// <c>null</c> if caching is not supported or price is unknown.
    /// </summary>
    public decimal? CacheReadPricePerMillion { get; init; }

    /// <summary>
    /// Cost per 1 million cache-write tokens (first-time cache population).
    /// May be higher than <see cref="InputPricePerMillion"/>.
    /// <c>null</c> if caching is not supported or price is unknown.
    /// </summary>
    public decimal? CacheWritePricePerMillion { get; init; }

    /// <summary>Cost per image sent as input. <c>null</c> if not applicable or unknown.</summary>
    public decimal? ImageInputPrice { get; init; }

    /// <summary>Cost per second of audio input. <c>null</c> if not applicable or unknown.</summary>
    public decimal? AudioInputPricePerSecond { get; init; }

    /// <summary>
    /// Non-representative price bands (context-length or time-of-day) layered on top of the
    /// fields above. <c>null</c> or empty means the provider bills a single flat rate.
    /// See <see cref="CalculateCost(int, int, PricingTierContext)"/> to apply them.
    /// </summary>
    public IReadOnlyList<PricingTier>? PricingTiers { get; init; }

    /// <summary>
    /// How the price fields above were obtained. Defaults to <see cref="TokenMeter.PriceSource.Official"/>.
    /// </summary>
    public PriceSource PriceSource { get; init; }

    /// <summary>
    /// Cache-write prices per cache lifetime, for a vendor whose write price depends on it (Anthropic: 5 minutes and
    /// 1 hour). <c>null</c> when the vendor has one write price (<see cref="CacheWritePricePerMillion"/>). See
    /// <see cref="GetCacheWritePrice(TimeSpan)"/>.
    /// </summary>
    public IReadOnlyList<CacheWritePrice>? CacheWritePrices { get; init; }

    /// <summary>
    /// Price changes the vendor has announced with an effective date. The price fields above are the rates in effect
    /// when the catalog was last verified; <see cref="AsOf(DateOnly)"/> applies the changes in effect on a later date.
    /// <c>null</c> when none is announced. Past prices are not kept — record the cost of a call when it is made.
    /// </summary>
    public IReadOnlyList<ScheduledPrice>? ScheduledPrices { get; init; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    /// <summary>
    /// The date the vendor deprecated the model (announced its retirement) on its own API; <c>null</c> when it has not.
    /// Partner clouds (Bedrock, Vertex AI, Azure) keep their own schedules.
    /// </summary>
    public DateOnly? DeprecationDate { get; init; }

    /// <summary>
    /// The date the vendor stops serving the model on its own API — requests fail from then on; <c>null</c> when no
    /// date is set.
    /// </summary>
    public DateOnly? RetirementDate { get; init; }

    /// <summary>The model the vendor recommends moving to, as the vendor names it; <c>null</c> when none is named.</summary>
    public string? ReplacementModelId { get; init; }

    /// <summary>
    /// The model's lifecycle state on <paramref name="asOf"/>: <see cref="ModelLifecycleStatus.Retired"/> from
    /// <see cref="RetirementDate"/>, <see cref="ModelLifecycleStatus.Deprecated"/> from <see cref="DeprecationDate"/>,
    /// otherwise <see cref="ModelLifecycleStatus.Active"/>. Computed from the dates rather than stored, so a catalog
    /// read months after it was published still answers correctly.
    /// </summary>
    public ModelLifecycleStatus GetLifecycleStatus(DateOnly asOf)
    {
        if (RetirementDate is { } retired && asOf >= retired) return ModelLifecycleStatus.Retired;
        if (DeprecationDate is { } deprecated && asOf >= deprecated) return ModelLifecycleStatus.Deprecated;
        return ModelLifecycleStatus.Active;
    }

    // ── Input Modalities ──────────────────────────────────────────────────────

    /// <summary>Model accepts image data in requests.</summary>
    public bool SupportsImageInput { get; init; }

    /// <summary>Model accepts audio data in requests.</summary>
    public bool SupportsAudioInput { get; init; }

    /// <summary>Model accepts video data in requests.</summary>
    public bool SupportsVideoInput { get; init; }

    /// <summary>Model accepts document files (PDF, DOCX, etc.) natively in requests.</summary>
    public bool SupportsDocumentInput { get; init; }

    // ── API Capabilities ──────────────────────────────────────────────────────

    /// <summary>Model supports tool/function calling.</summary>
    public bool SupportsToolCalling { get; init; }

    /// <summary>Model can invoke multiple tools in a single turn (parallel tool calls).</summary>
    public bool SupportsParallelToolCalling { get; init; }

    /// <summary>
    /// Model enforces strict JSON Schema output (constrained decoding).
    /// Guarantees syntactically valid JSON conforming to the provided schema.
    /// </summary>
    public bool SupportsStructuredOutput { get; init; }

    /// <summary>
    /// Model supports JSON mode — guides output toward JSON but does not guarantee schema compliance.
    /// Weaker than <see cref="SupportsStructuredOutput"/>.
    /// </summary>
    public bool SupportsJsonMode { get; init; }

    /// <summary>Model supports server-sent event (SSE) streaming responses.</summary>
    public bool SupportsStreaming { get; init; }

    /// <summary>
    /// How prompt caching is implemented. <see cref="PromptCachingMode.None"/> means no caching support.
    /// </summary>
    public PromptCachingMode PromptCachingMode { get; init; }

    /// <summary>Model respects custom stop sequences in requests.</summary>
    public bool SupportsStopSequences { get; init; }

    /// <summary>Model has native MCP (Model Context Protocol) tool integration.</summary>
    public bool SupportsMcpToolUse { get; init; }

    // ── Reasoning / Thinking ──────────────────────────────────────────────────

    /// <summary>
    /// Whether and how the model supports extended reasoning.
    /// <see cref="ReasoningMode.None"/> means no reasoning support.
    /// </summary>
    public ReasoningMode ReasoningMode { get; init; }

    /// <summary>
    /// How reasoning/thinking content is delivered in the API response.
    /// <see cref="ThinkingFormat.None"/> means thinking is not exposed.
    /// </summary>
    public ThinkingFormat ThinkingFormat { get; init; }

    /// <summary>
    /// The inline tag pattern used when <see cref="ThinkingFormat"/> is <see cref="ThinkingFormat.InlineTag"/>.
    /// Example: <c>"&lt;think&gt;...&lt;/think&gt;"</c> for DeepSeek R1.
    /// </summary>
    public string? ThinkingTagPattern { get; init; }

    /// <summary>
    /// The response field name used when <see cref="ThinkingFormat"/> is <see cref="ThinkingFormat.SeparateField"/>.
    /// Example: <c>"reasoning_content"</c> for OpenAI o-series.
    /// </summary>
    public string? ThinkingFieldName { get; init; }

    /// <summary>
    /// Model supports interleaved thinking — reasoning steps can occur between tool calls
    /// (e.g., Anthropic Adaptive Reasoning / extended thinking with tool use).
    /// </summary>
    public bool SupportsInterleavedThinking { get; init; }

    /// <summary>Maximum tokens that can be allocated for the thinking/reasoning phase.</summary>
    public int? MaxThinkingTokens { get; init; }

    // ── Tool Calling Wire Format ───────────────────────────────────────────────

    /// <summary>
    /// Wire format for tool invocation and result exchange.
    /// Determines how to construct tool-calling requests and parse responses.
    /// Defaults to <see cref="ToolCallingFormat.OpenAI"/> (most common).
    /// </summary>
    public ToolCallingFormat ToolCallingFormat { get; init; }

    // ── Cost Calculation ──────────────────────────────────────────────────────

    /// <summary>
    /// Calculates total cost for a request with input and output tokens.
    /// Returns <c>null</c> if pricing is unknown for this model.
    /// </summary>
    public decimal? CalculateCost(int inputTokens, int outputTokens)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(inputTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(outputTokens);
        if (InputPricePerMillion is null || OutputPricePerMillion is null) return null;
        return (inputTokens / 1_000_000m) * InputPricePerMillion.Value
             + (outputTokens / 1_000_000m) * OutputPricePerMillion.Value;
    }

    /// <summary>
    /// Calculates total cost including prompt cache tokens.
    /// Cache tokens without a specific price fall back to <see cref="InputPricePerMillion"/>.
    /// Returns <c>null</c> if base pricing is unknown.
    /// </summary>
    public decimal? CalculateCost(
        int inputTokens, int outputTokens,
        int cacheReadTokens, int cacheWriteTokens)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(inputTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(outputTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(cacheReadTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(cacheWriteTokens);
        if (InputPricePerMillion is null || OutputPricePerMillion is null) return null;

        var inputCost = (inputTokens / 1_000_000m) * InputPricePerMillion.Value;
        var outputCost = (outputTokens / 1_000_000m) * OutputPricePerMillion.Value;
        var cacheReadCost = (cacheReadTokens / 1_000_000m)
            * (CacheReadPricePerMillion ?? InputPricePerMillion.Value);
        var cacheWriteCost = (cacheWriteTokens / 1_000_000m)
            * (CacheWritePricePerMillion ?? InputPricePerMillion.Value);

        return inputCost + outputCost + cacheReadCost + cacheWriteCost;
    }

    /// <summary>
    /// Calculates total cost including prompt cache tokens written with a specific cache lifetime: the write is priced at
    /// <see cref="GetCacheWritePrice(TimeSpan)"/> for <paramref name="cacheWriteTtl"/>. Otherwise the same as
    /// <see cref="CalculateCost(int, int, int, int)"/>.
    /// </summary>
    public decimal? CalculateCost(
        int inputTokens, int outputTokens,
        int cacheReadTokens, int cacheWriteTokens, TimeSpan cacheWriteTtl)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cacheWriteTokens);
        var cost = CalculateCost(inputTokens, outputTokens, cacheReadTokens, 0);
        if (cost is null) return null;
        var writePrice = GetCacheWritePrice(cacheWriteTtl) ?? InputPricePerMillion!.Value;
        return cost + (cacheWriteTokens / 1_000_000m) * writePrice;
    }

    /// <summary>
    /// The cache-write price for entries that live <paramref name="ttl"/>: the matching <see cref="CacheWritePrices"/>
    /// entry, else <see cref="CacheWritePricePerMillion"/> (the vendor's default lifetime). <c>null</c> when the
    /// catalog has no cache-write price.
    /// </summary>
    public decimal? GetCacheWritePrice(TimeSpan ttl)
        => CacheWritePrices?.FirstOrDefault(p => p.Ttl == ttl)?.PricePerMillion ?? CacheWritePricePerMillion;

    /// <summary>
    /// This model with the rates in effect on <paramref name="date"/> (UTC): every <see cref="ScheduledPrices"/> entry
    /// whose <see cref="ScheduledPrice.EffectiveFrom"/> is on or before it is applied, oldest first. Returns this
    /// instance when none applies. A per-lifetime cache-write price (<see cref="CacheWritePrices"/>) keeps its ratio to
    /// the default write price when a change moves that price.
    /// </summary>
    public ModelInfo AsOf(DateOnly date)
    {
        if (ScheduledPrices is null or []) return this;

        var result = this;
        foreach (var change in ScheduledPrices.Where(p => p.EffectiveFrom <= date).OrderBy(p => p.EffectiveFrom))
        {
            var oldWrite = result.CacheWritePricePerMillion;
            var newWrite = change.CacheWritePricePerMillion ?? oldWrite;
            result = result with
            {
                InputPricePerMillion = change.InputPricePerMillion ?? result.InputPricePerMillion,
                OutputPricePerMillion = change.OutputPricePerMillion ?? result.OutputPricePerMillion,
                CacheReadPricePerMillion = change.CacheReadPricePerMillion ?? result.CacheReadPricePerMillion,
                CacheWritePricePerMillion = newWrite,
                CacheWritePrices = oldWrite is > 0 && newWrite is { } write && write != oldWrite
                    ? result.CacheWritePrices?.Select(p => p with { PricePerMillion = p.PricePerMillion * write / oldWrite.Value }).ToList()
                    : result.CacheWritePrices,
            };
        }
        return result;
    }

    /// <summary>
    /// The cost of one request, every part priced the way the vendor prices it: the rates in effect on
    /// <see cref="CostContext.Date"/> (<see cref="AsOf(DateOnly)"/>), the <see cref="PricingTiers"/> entry its prompt length
    /// (<see cref="TokenCounts.PromptTokens"/> — cache reads and writes included) or call time falls in, applied to the
    /// whole request, cache reads at the read price and each cache write at the price for its lifetime. A price the
    /// catalog lacks falls back the way the other overloads do: tier → model rate → input price for cache tokens.
    /// Returns <c>null</c> when the model has no input or output price.
    /// </summary>
    public decimal? CalculateCost(TokenCounts usage, CostContext context = default)
    {
        ArgumentNullException.ThrowIfNull(usage);
        ArgumentOutOfRangeException.ThrowIfNegative(usage.InputTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(usage.CacheReadTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(usage.OutputTokens);
        foreach (var write in usage.CacheWrites ?? [])
            ArgumentOutOfRangeException.ThrowIfNegative(write.Tokens);

        var model = context.Date is { } date ? AsOf(date) : this;
        if (model.InputPricePerMillion is null || model.OutputPricePerMillion is null) return null;

        var tier = model.FindApplicableTier(new PricingTierContext
        {
            ContextLengthTokens = usage.PromptTokens,
            CallTimeUtc = context.CallTimeUtc,
        });

        var inputPrice = tier?.InputPricePerMillion ?? model.InputPricePerMillion.Value;
        var outputPrice = tier?.OutputPricePerMillion ?? model.OutputPricePerMillion.Value;
        var readPrice = tier?.CacheReadPricePerMillion ?? model.CacheReadPricePerMillion ?? inputPrice;

        var cost = (usage.InputTokens / 1_000_000m) * inputPrice
                 + (usage.OutputTokens / 1_000_000m) * outputPrice
                 + (usage.CacheReadTokens / 1_000_000m) * readPrice;

        foreach (var write in usage.CacheWrites ?? [])
        {
            var writePrice = WritePrice(tier, write.Ttl) ?? model.WritePrice(write.Ttl) ?? inputPrice;
            cost += (write.Tokens / 1_000_000m) * writePrice;
        }

        return cost;
    }

    // The tier's write price for a lifetime: its per-lifetime entry, else its default-lifetime price.
    private static decimal? WritePrice(PricingTier? tier, TimeSpan? ttl)
        => tier is null
            ? null
            : (ttl is { } t ? tier.CacheWritePrices?.FirstOrDefault(p => p.Ttl == t)?.PricePerMillion : null)
                ?? tier.CacheWritePricePerMillion;

    // The model's write price for a lifetime (null lifetime = the vendor's default).
    private decimal? WritePrice(TimeSpan? ttl)
        => ttl is { } t ? GetCacheWritePrice(t) : CacheWritePricePerMillion;

    /// <summary>
    /// Calculates cost for input/output token usage, applying a matching <see cref="PricingTiers"/>
    /// entry (if any) instead of the representative rate. Supplying a <paramref name="context"/>
    /// with both fields <c>null</c> — or a model with no <see cref="PricingTiers"/> — reproduces
    /// <see cref="CalculateCost(int, int)"/> exactly.
    /// </summary>
    public decimal? CalculateCost(int inputTokens, int outputTokens, PricingTierContext context)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(inputTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(outputTokens);
        if (InputPricePerMillion is null || OutputPricePerMillion is null) return null;

        var tier = FindApplicableTier(context);
        var inputPrice = tier?.InputPricePerMillion ?? InputPricePerMillion.Value;
        var outputPrice = tier?.OutputPricePerMillion ?? OutputPricePerMillion.Value;

        return (inputTokens / 1_000_000m) * inputPrice
             + (outputTokens / 1_000_000m) * outputPrice;
    }

    /// <summary>
    /// Selects the <see cref="PricingTiers"/> entry that applies to <paramref name="context"/>, if
    /// any. A <see cref="PricingTierAxis.ContextLength"/> match picks the highest qualifying
    /// threshold; a <see cref="PricingTierAxis.TimeOfDay"/> match picks the first window containing
    /// <see cref="PricingTierContext.CallTimeUtc"/>. Returns <c>null</c> when no tier applies —
    /// callers fall back to the representative rate.
    /// </summary>
    private PricingTier? FindApplicableTier(PricingTierContext context)
    {
        if (PricingTiers is null or []) return null;

        PricingTier? bestByContext = null;
        PricingTier? bestByTime = null;

        foreach (var tier in PricingTiers)
        {
            switch (tier.Axis)
            {
                case PricingTierAxis.ContextLength:
                    if (context.ContextLengthTokens is int len
                        && tier.MinContextLengthTokens is int min
                        && len >= min
                        && (bestByContext?.MinContextLengthTokens is not int bestMin || min > bestMin))
                    {
                        bestByContext = tier;
                    }
                    break;

                case PricingTierAxis.TimeOfDay:
                    if (context.CallTimeUtc is TimeOnly time
                        && tier.WindowStartUtc is TimeOnly start
                        && tier.WindowEndUtc is TimeOnly end
                        && time >= start && time < end)
                    {
                        bestByTime = tier;
                    }
                    break;
            }
        }

        // A model billed on two independent axes at once (not seen in the catalog today): the
        // higher rate is the conservative, never-understate choice.
        if (bestByContext is not null && bestByTime is not null)
        {
            return bestByContext.InputPricePerMillion >= bestByTime.InputPricePerMillion
                ? bestByContext : bestByTime;
        }
        return bestByContext ?? bestByTime;
    }
}
