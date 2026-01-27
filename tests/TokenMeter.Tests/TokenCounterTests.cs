namespace TokenMeter.Tests;

public class TokenCounterTests
{
    [Fact]
    public void CountTokens_EmptyString_ReturnsZero()
    {
        var counter = TokenCounter.Default();

        var count = counter.CountTokens(string.Empty);

        Assert.Equal(0, count);
    }

    [Fact]
    public void CountTokens_NullString_ReturnsZero()
    {
        var counter = TokenCounter.Default();

        var count = counter.CountTokens((string)null!);

        Assert.Equal(0, count);
    }

    [Fact]
    public void CountTokens_SimpleText_ReturnsPositiveCount()
    {
        var counter = TokenCounter.Default();

        var count = counter.CountTokens("Hello, world!");

        Assert.True(count > 0);
    }

    [Fact]
    public void CountTokens_LongerText_ReturnsHigherCount()
    {
        var counter = TokenCounter.Default();

        var shortCount = counter.CountTokens("Hello");
        var longCount = counter.CountTokens("Hello, this is a much longer piece of text that should have more tokens.");

        Assert.True(longCount > shortCount);
    }

    [Theory]
    [InlineData("gpt-4")]
    [InlineData("gpt-4o")]
    [InlineData("gpt-3.5-turbo")]
    [InlineData("claude-3-5-sonnet")]
    public void Constructor_DifferentModels_CreatesSuccessfully(string model)
    {
        var counter = new TokenCounter(model);

        Assert.Equal(model, counter.ModelName);
    }

    [Fact]
    public void CountTokens_MultipleTexts_ReturnsTotalCount()
    {
        var counter = TokenCounter.Default();

        var texts = new[] { "Hello", "World", "Test" };
        var totalCount = counter.CountTokens(texts);
        var individualSum = texts.Sum(t => counter.CountTokens(t));

        Assert.Equal(individualSum, totalCount);
    }

    [Fact]
    public void ForGpt4_CreatesGpt4Counter()
    {
        var counter = TokenCounter.ForGpt4();

        Assert.Equal("gpt-4", counter.ModelName);
    }

    [Fact]
    public void ForGpt35Turbo_CreatesGpt35Counter()
    {
        var counter = TokenCounter.ForGpt35Turbo();

        Assert.Equal("gpt-3.5-turbo", counter.ModelName);
    }
}
