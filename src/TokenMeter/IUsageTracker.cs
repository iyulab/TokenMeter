namespace TokenMeter;

/// <summary>
/// Interface for tracking token usage over time.
/// </summary>
public interface IUsageTracker
{
    /// <summary>
    /// Records a usage entry.
    /// </summary>
    /// <param name="record">The usage record to add</param>
    void Record(UsageRecord record);

    /// <summary>
    /// Records usage with the given parameters.
    /// </summary>
    /// <param name="modelId">Model identifier</param>
    /// <param name="inputTokens">Number of input tokens</param>
    /// <param name="outputTokens">Number of output tokens</param>
    /// <param name="sessionId">Optional session identifier</param>
    /// <returns>The created usage record</returns>
    UsageRecord Record(string? modelId, int inputTokens, int outputTokens, string? sessionId = null);

    /// <summary>
    /// Gets usage statistics for the current session.
    /// </summary>
    UsageStatistics GetSessionStatistics();

    /// <summary>
    /// Gets usage statistics for a specific time period.
    /// </summary>
    /// <param name="startTime">Start of the period</param>
    /// <param name="endTime">End of the period</param>
    UsageStatistics GetStatistics(DateTimeOffset startTime, DateTimeOffset endTime);

    /// <summary>
    /// Gets usage statistics for today.
    /// </summary>
    UsageStatistics GetTodayStatistics();

    /// <summary>
    /// Gets all usage records.
    /// </summary>
    IReadOnlyList<UsageRecord> GetRecords();

    /// <summary>
    /// Gets usage records for a specific session.
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    IReadOnlyList<UsageRecord> GetRecords(string sessionId);

    /// <summary>
    /// Clears all usage records.
    /// </summary>
    void Clear();

    /// <summary>
    /// Current session ID.
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Starts a new session.
    /// </summary>
    /// <returns>The new session ID</returns>
    string StartNewSession();
}
