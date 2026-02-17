namespace TokenMeter.Tests;

public class UsageTrackerTests
{
    [Fact]
    public void Record_AddsToRecords()
    {
        var tracker = new UsageTracker();

        tracker.Record("gpt-4o", 100, 50);

        var records = tracker.GetRecords();
        Assert.Single(records);
        Assert.Equal(100, records[0].InputTokens);
        Assert.Equal(50, records[0].OutputTokens);
    }

    [Fact]
    public void Record_WithCostCalculator_CalculatesCost()
    {
        var costCalculator = CostCalculator.Default();
        var tracker = new UsageTracker(costCalculator);

        var record = tracker.Record("gpt-4o", 1000, 500);

        Assert.NotNull(record.Cost);
        Assert.True(record.Cost > 0);
    }

    [Fact]
    public void Record_WithoutCostCalculator_NoCost()
    {
        var tracker = new UsageTracker();

        var record = tracker.Record("gpt-4o", 1000, 500);

        Assert.Null(record.Cost);
    }

    [Fact]
    public void GetSessionStatistics_ReturnsCurrentSessionStats()
    {
        var tracker = new UsageTracker();
        tracker.Record("gpt-4o", 100, 50);
        tracker.Record("gpt-4o", 200, 100);

        var stats = tracker.GetSessionStatistics();

        Assert.Equal(2, stats.RequestCount);
        Assert.Equal(300, stats.TotalInputTokens);
        Assert.Equal(150, stats.TotalOutputTokens);
        Assert.Equal(450, stats.TotalTokens);
    }

    [Fact]
    public void GetStatistics_EmptyPeriod_ReturnsEmptyStats()
    {
        var tracker = new UsageTracker();
        var start = DateTimeOffset.UtcNow.AddDays(-10);
        var end = DateTimeOffset.UtcNow.AddDays(-9);

        var stats = tracker.GetStatistics(start, end);

        Assert.Equal(0, stats.RequestCount);
        Assert.Equal(0, stats.TotalTokens);
    }

    [Fact]
    public void GetRecords_BySessionId_FiltersCorrectly()
    {
        var tracker = new UsageTracker();
        var session1 = tracker.SessionId;
        tracker.Record("gpt-4o", 100, 50);

        tracker.StartNewSession();
        var session2 = tracker.SessionId;
        tracker.Record("gpt-4o", 200, 100);

        var session1Records = tracker.GetRecords(session1);
        var session2Records = tracker.GetRecords(session2);

        Assert.Single(session1Records);
        Assert.Equal(100, session1Records[0].InputTokens);
        Assert.Single(session2Records);
        Assert.Equal(200, session2Records[0].InputTokens);
    }

    [Fact]
    public void StartNewSession_ChangesSessionId()
    {
        var tracker = new UsageTracker();
        var originalId = tracker.SessionId;

        var newId = tracker.StartNewSession();

        Assert.NotEqual(originalId, newId);
        Assert.Equal(newId, tracker.SessionId);
    }

    [Fact]
    public void Clear_RemovesAllRecords()
    {
        var tracker = new UsageTracker();
        tracker.Record("gpt-4o", 100, 50);
        tracker.Record("gpt-4o", 200, 100);

        tracker.Clear();

        Assert.Empty(tracker.GetRecords());
    }

    [Fact]
    public void UsageStatistics_AverageCalculations()
    {
        var tracker = new UsageTracker();
        tracker.Record("gpt-4o", 100, 50);
        tracker.Record("gpt-4o", 200, 150);

        var stats = tracker.GetSessionStatistics();

        Assert.Equal(150, stats.AverageInputTokens);
        Assert.Equal(100, stats.AverageOutputTokens);
    }

    [Fact]
    public void Record_NullRecord_ThrowsArgumentNullException()
    {
        var tracker = new UsageTracker();

        Assert.Throws<ArgumentNullException>(() => tracker.Record((UsageRecord)null!));
    }

    [Fact]
    public void Record_NullModelId_NoCost()
    {
        var costCalculator = CostCalculator.Default();
        var tracker = new UsageTracker(costCalculator);

        var record = tracker.Record(null, 100, 50);

        Assert.Null(record.Cost);
    }

    [Fact]
    public void Record_CustomSessionId_UsesProvidedSessionId()
    {
        var tracker = new UsageTracker();

        var record = tracker.Record("gpt-4o", 100, 50, sessionId: "my-custom-session");

        Assert.Equal("my-custom-session", record.SessionId);
    }

    [Fact]
    public void Record_NoCustomSessionId_UsesTrackerSessionId()
    {
        var tracker = new UsageTracker();
        var expectedSession = tracker.SessionId;

        var record = tracker.Record("gpt-4o", 100, 50);

        Assert.Equal(expectedSession, record.SessionId);
    }

    [Fact]
    public void GetTodayStatistics_ReturnsRecordsFromToday()
    {
        var tracker = new UsageTracker();
        tracker.Record("gpt-4o", 100, 50);
        tracker.Record("gpt-4o", 200, 100);

        var stats = tracker.GetTodayStatistics();

        Assert.Equal(2, stats.RequestCount);
        Assert.Equal(300, stats.TotalInputTokens);
        Assert.Equal(150, stats.TotalOutputTokens);
    }

    [Fact]
    public void GetStatistics_WithinPeriod_IncludesRecords()
    {
        var tracker = new UsageTracker();
        tracker.Record("gpt-4o", 100, 50);

        var start = DateTimeOffset.UtcNow.AddMinutes(-1);
        var end = DateTimeOffset.UtcNow.AddMinutes(1);
        var stats = tracker.GetStatistics(start, end);

        Assert.Equal(1, stats.RequestCount);
    }

    [Fact]
    public void SessionId_StartsWithSessionPrefix()
    {
        var tracker = new UsageTracker();

        Assert.StartsWith("session-", tracker.SessionId);
    }

    [Fact]
    public void GetRecords_AllRecords_ReturnsAll()
    {
        var tracker = new UsageTracker();
        tracker.Record("model1", 100, 50);
        tracker.Record("model2", 200, 100);
        tracker.Record("model3", 300, 150);

        var records = tracker.GetRecords();

        Assert.Equal(3, records.Count);
    }
}

public class UsageRecordTests
{
    [Fact]
    public void TotalTokens_CalculatesCorrectly()
    {
        var record = new UsageRecord
        {
            InputTokens = 100,
            OutputTokens = 50
        };

        Assert.Equal(150, record.TotalTokens);
    }

    [Fact]
    public void DefaultValues_AreSet()
    {
        var record = new UsageRecord();

        Assert.NotEmpty(record.Id);
        Assert.True(record.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Tags_CanBeSet()
    {
        var record = new UsageRecord
        {
            Tags = ["production", "batch-job"]
        };

        Assert.NotNull(record.Tags);
        Assert.Equal(2, record.Tags.Count);
        Assert.Contains("production", record.Tags);
    }

    [Fact]
    public void Tags_DefaultIsNull()
    {
        var record = new UsageRecord();

        Assert.Null(record.Tags);
    }

    [Fact]
    public void SessionId_CanBeSet()
    {
        var record = new UsageRecord
        {
            SessionId = "custom-session"
        };

        Assert.Equal("custom-session", record.SessionId);
    }

    [Fact]
    public void ModelId_CanBeSet()
    {
        var record = new UsageRecord
        {
            ModelId = "gpt-4o"
        };

        Assert.Equal("gpt-4o", record.ModelId);
    }

    [Fact]
    public void Cost_CanBeSet()
    {
        var record = new UsageRecord
        {
            Cost = 0.0075m
        };

        Assert.Equal(0.0075m, record.Cost);
    }

    [Fact]
    public void Id_AutoGenerated_HasLength8()
    {
        var record = new UsageRecord();

        Assert.Equal(8, record.Id.Length);
    }
}

public class UsageStatisticsTests
{
    [Fact]
    public void TotalTokens_SumOfInputAndOutput()
    {
        var stats = new UsageStatistics
        {
            TotalInputTokens = 1000,
            TotalOutputTokens = 500
        };

        Assert.Equal(1500, stats.TotalTokens);
    }

    [Fact]
    public void AverageCost_CalculatesCorrectly()
    {
        var stats = new UsageStatistics
        {
            RequestCount = 4,
            TotalCost = 10.0m
        };

        Assert.Equal(2.5m, stats.AverageCost);
    }

    [Fact]
    public void AverageCost_ZeroRequests_ReturnsZero()
    {
        var stats = new UsageStatistics
        {
            RequestCount = 0,
            TotalCost = 0m
        };

        Assert.Equal(0m, stats.AverageCost);
    }

    [Fact]
    public void Empty_HasZeroValues()
    {
        var stats = UsageStatistics.Empty;

        Assert.Equal(0, stats.RequestCount);
        Assert.Equal(0, stats.TotalTokens);
        Assert.Equal(0m, stats.TotalCost);
    }

    [Fact]
    public void AverageInputTokens_ZeroRequests_ReturnsZero()
    {
        var stats = new UsageStatistics { RequestCount = 0 };

        Assert.Equal(0, stats.AverageInputTokens);
        Assert.Equal(0, stats.AverageOutputTokens);
    }
}
