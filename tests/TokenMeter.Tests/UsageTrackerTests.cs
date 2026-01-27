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
}
