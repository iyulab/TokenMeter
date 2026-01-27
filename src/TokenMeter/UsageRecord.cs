namespace TokenMeter;

/// <summary>
/// Record of token usage for a single request/response.
/// </summary>
public record UsageRecord
{
    /// <summary>
    /// Unique identifier for this usage record.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Model identifier used for this request.
    /// </summary>
    public string? ModelId { get; init; }

    /// <summary>
    /// Number of input (prompt) tokens.
    /// </summary>
    public int InputTokens { get; init; }

    /// <summary>
    /// Number of output (completion) tokens.
    /// </summary>
    public int OutputTokens { get; init; }

    /// <summary>
    /// Total tokens (input + output).
    /// </summary>
    public int TotalTokens => InputTokens + OutputTokens;

    /// <summary>
    /// Calculated cost for this usage (if pricing available).
    /// </summary>
    public decimal? Cost { get; init; }

    /// <summary>
    /// Timestamp when this usage was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional session/request identifier for grouping.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Optional tags for categorization.
    /// </summary>
    public IReadOnlyList<string>? Tags { get; init; }
}

/// <summary>
/// Aggregated usage statistics.
/// </summary>
public record UsageStatistics
{
    /// <summary>
    /// Total number of requests.
    /// </summary>
    public int RequestCount { get; init; }

    /// <summary>
    /// Total input tokens across all requests.
    /// </summary>
    public long TotalInputTokens { get; init; }

    /// <summary>
    /// Total output tokens across all requests.
    /// </summary>
    public long TotalOutputTokens { get; init; }

    /// <summary>
    /// Total tokens (input + output).
    /// </summary>
    public long TotalTokens => TotalInputTokens + TotalOutputTokens;

    /// <summary>
    /// Total cost across all requests.
    /// </summary>
    public decimal TotalCost { get; init; }

    /// <summary>
    /// Start of the period for these statistics.
    /// </summary>
    public DateTimeOffset PeriodStart { get; init; }

    /// <summary>
    /// End of the period for these statistics.
    /// </summary>
    public DateTimeOffset PeriodEnd { get; init; }

    /// <summary>
    /// Average input tokens per request.
    /// </summary>
    public double AverageInputTokens => RequestCount > 0 ? (double)TotalInputTokens / RequestCount : 0;

    /// <summary>
    /// Average output tokens per request.
    /// </summary>
    public double AverageOutputTokens => RequestCount > 0 ? (double)TotalOutputTokens / RequestCount : 0;

    /// <summary>
    /// Average cost per request.
    /// </summary>
    public decimal AverageCost => RequestCount > 0 ? TotalCost / RequestCount : 0;

    /// <summary>
    /// Empty statistics.
    /// </summary>
    public static UsageStatistics Empty => new()
    {
        PeriodStart = DateTimeOffset.UtcNow,
        PeriodEnd = DateTimeOffset.UtcNow
    };
}
