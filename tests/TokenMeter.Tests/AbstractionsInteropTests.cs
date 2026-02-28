using TokenMeter.Abstractions;

namespace TokenMeter.Tests;

/// <summary>
/// Tests that TokenCounter correctly implements the shared Abstractions.ITokenCounter interface.
/// </summary>
public sealed class AbstractionsInteropTests
{
    [Fact]
    public void TokenCounter_ImplementsAbstractionsInterface()
    {
        var counter = TokenCounter.Default();

        // Should be assignable to shared interface
        Abstractions.ITokenCounter shared = counter;
        Assert.NotNull(shared);
    }

    [Fact]
    public void SharedInterface_Count_DelegatesToCountTokens()
    {
        var counter = TokenCounter.Default();
        Abstractions.ITokenCounter shared = counter;

        var text = "Hello, world!";
        var expected = counter.CountTokens(text);
        var actual = shared.Count(text);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SharedInterface_CountBatch_DelegatesToCountTokens()
    {
        var counter = TokenCounter.Default();
        Abstractions.ITokenCounter shared = counter;

        var texts = new[] { "Hello", "world", "how are you?" };
        var expected = counter.CountTokens(texts);
        var actual = shared.Count(texts);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SharedInterface_CountEmpty_ReturnsZero()
    {
        Abstractions.ITokenCounter shared = TokenCounter.Default();

        Assert.Equal(0, shared.Count(string.Empty));
    }

    [Fact]
    public void SupportsModel_GptModels_ReturnsTrue()
    {
        var counter = TokenCounter.Default();

        Assert.True(counter.SupportsModel("gpt-4"));
        Assert.True(counter.SupportsModel("gpt-4o"));
        Assert.True(counter.SupportsModel("gpt-3.5-turbo"));
    }

    [Fact]
    public void SupportsModel_ClaudeModels_ReturnsFalse()
    {
        var counter = TokenCounter.Default();

        // Claude uses its own tokenizer — our tiktoken is approximate
        Assert.False(counter.SupportsModel("claude-3.5-sonnet"));
        Assert.False(counter.SupportsModel("claude-3-opus"));
    }

    [Fact]
    public void SupportsModel_NullOrEmpty_ReturnsFalse()
    {
        var counter = TokenCounter.Default();

        Assert.False(counter.SupportsModel(""));
        Assert.False(counter.SupportsModel(null!));
    }

    [Fact]
    public void SharedInterface_SupportsModel_SameAsDirectCall()
    {
        var counter = TokenCounter.Default();

        // Access via interface to verify it delegates correctly
        Assert.Equal(
            counter.SupportsModel("gpt-4"),
            ((Abstractions.ITokenCounter)counter).SupportsModel("gpt-4"));
        Assert.Equal(
            counter.SupportsModel("claude-3"),
            ((Abstractions.ITokenCounter)counter).SupportsModel("claude-3"));
    }

    [Fact]
    public void IsApproximate_GptModels_ReturnsFalse()
    {
        var counter = TokenCounter.Default();

        Assert.False(counter.IsApproximate("gpt-4"));
        Assert.False(counter.IsApproximate("gpt-4o"));
        Assert.False(counter.IsApproximate("gpt-3.5-turbo"));
        Assert.False(counter.IsApproximate("text-davinci-003"));
        Assert.False(counter.IsApproximate("code-davinci-002"));
    }

    [Fact]
    public void IsApproximate_ClaudeModels_ReturnsTrue()
    {
        var counter = TokenCounter.Default();

        // Claude uses cl100k_base as fallback — approximate
        Assert.True(counter.IsApproximate("claude-3.5-sonnet"));
        Assert.True(counter.IsApproximate("claude-3-opus"));
        Assert.True(counter.IsApproximate("claude-sonnet-4-5-20250929"));
    }

    [Fact]
    public void IsApproximate_GeminiModels_ReturnsTrue()
    {
        var counter = TokenCounter.Default();

        Assert.True(counter.IsApproximate("gemini-2.0-flash"));
        Assert.True(counter.IsApproximate("gemini-1.5-pro"));
    }

    [Fact]
    public void IsApproximate_NullOrEmpty_ReturnsFalse()
    {
        var counter = TokenCounter.Default();

        Assert.False(counter.IsApproximate(""));
        Assert.False(counter.IsApproximate(null!));
    }

    [Fact]
    public void IsApproximate_DefaultInterfaceImplementation_ReturnsFalse()
    {
        // Default interface method should return false
        var mock = new MinimalTokenCounter();
        Abstractions.ITokenCounter shared = mock;

        Assert.False(shared.IsApproximate("any-model"));
    }

    /// <summary>
    /// Minimal implementation to test default interface method behavior.
    /// </summary>
    private sealed class MinimalTokenCounter : Abstractions.ITokenCounter
    {
        public int Count(string text) => 0;
        public int Count(IEnumerable<string> texts) => 0;
        public bool SupportsModel(string modelId) => false;
    }
}
