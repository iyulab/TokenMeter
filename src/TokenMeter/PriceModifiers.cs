namespace TokenMeter;

/// <summary>
/// How a request was served, when the vendor prices that differently from its standard rate.
/// </summary>
public enum ServiceTier
{
    /// <summary>The standard, synchronous rate — the model's representative prices.</summary>
    Standard,

    /// <summary>Asynchronous batch processing (OpenAI Batch API, Anthropic Message Batches).</summary>
    Batch,

    /// <summary>Lower-priority, slower processing at a discount (OpenAI Flex).</summary>
    Flex,

    /// <summary>Faster output at a premium (OpenAI Fast — formerly Priority; Anthropic fast mode).</summary>
    Fast,
}

/// <summary>
/// A <see cref="ServiceTier"/> the model is offered at, as a multiplier on every token price (input, cache reads and
/// writes, output) — the form every vendor in the catalog publishes it in today.
/// </summary>
public sealed record ServiceTierMultiplier
{
    /// <summary>The service tier.</summary>
    public required ServiceTier Tier { get; init; }

    /// <summary>The factor applied to every token price (Batch 0.5, Fast 2.0).</summary>
    public required decimal Multiplier { get; init; }
}

/// <summary>
/// A surcharge for keeping inference in a region, as a multiplier on every token price (Anthropic
/// <c>inference_geo: "us"</c> 1.1x on Claude 4.6 and later).
/// </summary>
public sealed record RegionalMultiplier
{
    /// <summary>The vendor's name for the region, as sent in the request (<c>us</c>).</summary>
    public required string Region { get; init; }

    /// <summary>The factor applied to every token price.</summary>
    public required decimal Multiplier { get; init; }
}

/// <summary>
/// A fee for a server-side tool the vendor runs, on top of the tokens the tool's results add to the context (those are
/// already in the token counts).
/// </summary>
public sealed record ToolCallPrice
{
    /// <summary>The vendor's tool name (<c>web_search</c>, <c>file_search</c>, <c>google_search</c>).</summary>
    public required string Tool { get; init; }

    /// <summary>
    /// Cost per 1,000 of <see cref="Unit"/>. A vendor's monthly free allowance is not applied — that is account state.
    /// </summary>
    public required decimal PricePerThousand { get; init; }

    /// <summary>
    /// What one billed unit is: each call the response reports (<see cref="ToolFeeUnit.Call"/>, the default), or each
    /// request that used the tool at all (<see cref="ToolFeeUnit.Request"/>).
    /// </summary>
    public ToolFeeUnit Unit { get; init; } = ToolFeeUnit.Call;
}

/// <summary>How a vendor counts a <see cref="ToolCallPrice"/>.</summary>
public enum ToolFeeUnit
{
    /// <summary>
    /// Every call is billed: each search, query or tool call the response reports (Anthropic and OpenAI web search,
    /// Gemini 3 Search grounding — «each individual search query»).
    /// </summary>
    Call = 1,

    /// <summary>
    /// One fee per request that used the tool, however many calls it made (Gemini 2.5 Search grounding — «per grounded
    /// prompt»).
    /// </summary>
    Request = 2,
}
