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
}
