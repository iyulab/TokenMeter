namespace TokenMeter;

/// <summary>
/// Default implementation of usage tracker.
/// Thread-safe and supports session-based tracking.
/// </summary>
public class UsageTracker : IUsageTracker
{
    private readonly List<UsageRecord> _records = [];
    private readonly object _lock = new();
    private readonly ICostCalculator? _costCalculator;
    private readonly DateTimeOffset _sessionStart;

    /// <inheritdoc />
    public string SessionId { get; private set; }

    /// <summary>
    /// Creates a new usage tracker.
    /// </summary>
    /// <param name="costCalculator">Optional cost calculator for automatic cost calculation</param>
    public UsageTracker(ICostCalculator? costCalculator = null)
    {
        _costCalculator = costCalculator;
        _sessionStart = DateTimeOffset.UtcNow;
        SessionId = GenerateSessionId();
    }

    /// <inheritdoc />
    public void Record(UsageRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        lock (_lock)
        {
            _records.Add(record);
        }
    }

    /// <inheritdoc />
    public UsageRecord Record(string? modelId, int inputTokens, int outputTokens, string? sessionId = null)
    {
        decimal? cost = null;
        if (_costCalculator != null && modelId != null)
        {
            cost = _costCalculator.CalculateCost(modelId, inputTokens, outputTokens);
        }

        var record = new UsageRecord
        {
            ModelId = modelId,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            Cost = cost,
            SessionId = sessionId ?? SessionId
        };

        Record(record);
        return record;
    }

    /// <inheritdoc />
    public UsageStatistics GetSessionStatistics()
    {
        lock (_lock)
        {
            var sessionRecords = _records.Where(r => r.SessionId == SessionId).ToList();
            return CalculateStatistics(sessionRecords, _sessionStart, DateTimeOffset.UtcNow);
        }
    }

    /// <inheritdoc />
    public UsageStatistics GetStatistics(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        lock (_lock)
        {
            var periodRecords = _records
                .Where(r => r.Timestamp >= startTime && r.Timestamp <= endTime)
                .ToList();
            return CalculateStatistics(periodRecords, startTime, endTime);
        }
    }

    /// <inheritdoc />
    public UsageStatistics GetTodayStatistics()
    {
        var today = DateTimeOffset.UtcNow.Date;
        var start = new DateTimeOffset(today, TimeSpan.Zero);
        var end = start.AddDays(1);
        return GetStatistics(start, end);
    }

    /// <inheritdoc />
    public IReadOnlyList<UsageRecord> GetRecords()
    {
        lock (_lock)
        {
            return [.. _records];
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<UsageRecord> GetRecords(string sessionId)
    {
        lock (_lock)
        {
            return _records.Where(r => r.SessionId == sessionId).ToList();
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            _records.Clear();
        }
    }

    /// <inheritdoc />
    public string StartNewSession()
    {
        SessionId = GenerateSessionId();
        return SessionId;
    }

    private static UsageStatistics CalculateStatistics(List<UsageRecord> records, DateTimeOffset start, DateTimeOffset end)
    {
        if (records.Count == 0)
        {
            return new UsageStatistics
            {
                PeriodStart = start,
                PeriodEnd = end
            };
        }

        return new UsageStatistics
        {
            RequestCount = records.Count,
            TotalInputTokens = records.Sum(r => (long)r.InputTokens),
            TotalOutputTokens = records.Sum(r => (long)r.OutputTokens),
            TotalCost = records.Where(r => r.Cost.HasValue).Sum(r => r.Cost!.Value),
            PeriodStart = start,
            PeriodEnd = end
        };
    }

    private static string GenerateSessionId()
    {
        return $"session-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..6]}";
    }
}
