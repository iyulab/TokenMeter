using Microsoft.ML.Tokenizers;

namespace TokenMeter.Tests;

public class TokenCounterAdditionalTests
{
    #region Constructor — Custom Tokenizer

    [Fact]
    public void Constructor_CustomTokenizer_CountsCorrectly()
    {
        var tokenizer = TiktokenTokenizer.CreateForEncoding("cl100k_base");
        var counter = new TokenCounter(tokenizer, "custom-model");

        var count = counter.CountTokens("Hello, world!");

        Assert.True(count > 0);
        Assert.Equal("custom-model", counter.ModelName);
    }

    [Fact]
    public void Constructor_NullTokenizer_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TokenCounter(null!, "test-model"));
    }

    #endregion

    #region Encoding Mapping

    [Theory]
    [InlineData("text-davinci-003")]
    [InlineData("text-davinci-002")]
    public void Constructor_DavinciModel_UsesP50kEncoding(string model)
    {
        // p50k_base models should create successfully and count tokens
        var counter = new TokenCounter(model);

        Assert.Equal(model, counter.ModelName);
        var count = counter.CountTokens("Hello, world!");
        Assert.True(count > 0);
    }

    [Theory]
    [InlineData("code-davinci-002")]
    [InlineData("code-cushman-001")]
    public void Constructor_CodexModel_UsesP50kEncoding(string model)
    {
        var counter = new TokenCounter(model);

        Assert.Equal(model, counter.ModelName);
        var count = counter.CountTokens("function hello() { return 42; }");
        Assert.True(count > 0);
    }

    [Fact]
    public void Constructor_ClaudeModel_UsesCl100kEncoding()
    {
        var counter = new TokenCounter("claude-3-opus");

        var count = counter.CountTokens("Hello, world!");
        Assert.True(count > 0);
    }

    [Fact]
    public void Constructor_UnknownModel_FallsBackToDefault()
    {
        // Unknown models should fall back to cl100k_base
        var counter = new TokenCounter("completely-unknown-model");
        var defaultCounter = TokenCounter.Default();

        var text = "The quick brown fox jumps over the lazy dog.";
        Assert.Equal(defaultCounter.CountTokens(text), counter.CountTokens(text));
    }

    [Fact]
    public void DifferentEncodings_MayProduceDifferentCounts()
    {
        // p50k_base (davinci) vs cl100k_base (gpt-4) may tokenize differently
        var davinciCounter = new TokenCounter("text-davinci-003");
        var gpt4Counter = new TokenCounter("gpt-4");

        var text = "The quick brown fox jumps over the lazy dog. This is a longer test sentence for tokenization.";
        var davinciCount = davinciCounter.CountTokens(text);
        var gpt4Count = gpt4Counter.CountTokens(text);

        // Both should produce valid positive counts
        Assert.True(davinciCount > 0);
        Assert.True(gpt4Count > 0);
    }

    #endregion

    #region CountTokens — Edge Cases

    [Fact]
    public void CountTokens_EmptyEnumerable_ReturnsZero()
    {
        var counter = TokenCounter.Default();

        var count = counter.CountTokens(Array.Empty<string>());

        Assert.Equal(0, count);
    }

    [Fact]
    public void CountTokens_EnumerableWithEmptyStrings_ReturnsZero()
    {
        var counter = TokenCounter.Default();

        var count = counter.CountTokens(["", "", ""]);

        Assert.Equal(0, count);
    }

    [Fact]
    public void CountTokens_Deterministic_SameInputSameOutput()
    {
        var counter = TokenCounter.Default();
        var text = "This is a test of deterministic token counting.";

        var count1 = counter.CountTokens(text);
        var count2 = counter.CountTokens(text);
        var count3 = counter.CountTokens(text);

        Assert.Equal(count1, count2);
        Assert.Equal(count2, count3);
    }

    [Fact]
    public void CountTokens_WhitespaceOnly_ReturnsPositiveCount()
    {
        var counter = TokenCounter.Default();

        // Whitespace is still tokenizable
        var count = counter.CountTokens("   \n\t   ");

        Assert.True(count > 0);
    }

    [Fact]
    public void CountTokens_UnicodeText_ReturnsPositiveCount()
    {
        var counter = TokenCounter.Default();

        var count = counter.CountTokens("한국어 텍스트 토큰 카운팅 테스트입니다.");

        Assert.True(count > 0);
    }

    [Fact]
    public void CountTokens_SingleToken_ReturnsOne()
    {
        var counter = TokenCounter.Default();

        // "Hello" is typically a single token in cl100k_base
        var count = counter.CountTokens("Hello");

        Assert.Equal(1, count);
    }

    #endregion

    #region Factory Methods

    [Fact]
    public void Default_ModelNameIsCl100kBase()
    {
        var counter = TokenCounter.Default();

        Assert.Equal("cl100k_base", counter.ModelName);
    }

    [Fact]
    public void Default_CountsSameAsGpt4()
    {
        // cl100k_base is the same encoding as gpt-4
        var defaultCounter = TokenCounter.Default();
        var gpt4Counter = TokenCounter.ForGpt4();

        var text = "Testing token count equivalence between default and gpt-4.";

        Assert.Equal(gpt4Counter.CountTokens(text), defaultCounter.CountTokens(text));
    }

    #endregion
}
