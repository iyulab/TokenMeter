namespace TokenMeter.Tests;

public class ModelInfoTests
{
    [Fact]
    public void RequiredModelId_MustBeSet()
    {
        var info = new ModelInfo { ModelId = "gpt-4o" };
        Assert.Equal("gpt-4o", info.ModelId);
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var info = new ModelInfo { ModelId = "test-model" };

        Assert.Equal(ModelType.Chat, info.ModelType);
        Assert.Equal(ReasoningMode.None, info.ReasoningMode);
        Assert.Equal(ThinkingFormat.None, info.ThinkingFormat);
        Assert.Equal(ToolCallingFormat.OpenAI, info.ToolCallingFormat);
        Assert.Equal(PromptCachingMode.None, info.PromptCachingMode);

        Assert.Null(info.Provider);
        Assert.Null(info.DisplayName);
        Assert.Null(info.ContextWindow);
        Assert.Null(info.MaxOutputTokens);
        Assert.Null(info.InputPricePerMillion);
        Assert.Null(info.OutputPricePerMillion);
        Assert.Null(info.CacheReadPricePerMillion);
        Assert.Null(info.CacheWritePricePerMillion);
        Assert.Null(info.ImageInputPrice);
        Assert.Null(info.AudioInputPricePerSecond);
        Assert.Null(info.ThinkingTagPattern);
        Assert.Null(info.ThinkingFieldName);
        Assert.Null(info.MaxThinkingTokens);

        Assert.False(info.IsInstructTuned);
        Assert.False(info.SupportsImageInput);
        Assert.False(info.SupportsAudioInput);
        Assert.False(info.SupportsVideoInput);
        Assert.False(info.SupportsDocumentInput);
        Assert.False(info.SupportsToolCalling);
        Assert.False(info.SupportsParallelToolCalling);
        Assert.False(info.SupportsStructuredOutput);
        Assert.False(info.SupportsJsonMode);
        Assert.False(info.SupportsStreaming);
        Assert.False(info.SupportsStopSequences);
        Assert.False(info.SupportsMcpToolUse);
        Assert.False(info.SupportsInterleavedThinking);
    }

    [Fact]
    public void CalculateCost_WithBothPrices_ReturnsCorrectTotal()
    {
        var info = new ModelInfo
        {
            ModelId = "test",
            InputPricePerMillion = 3.00m,
            OutputPricePerMillion = 15.00m
        };

        var cost = info.CalculateCost(1_000_000, 500_000);

        Assert.NotNull(cost);
        Assert.Equal(3.00m + 7.50m, cost.Value);
    }

    [Fact]
    public void CalculateCost_WithMissingPrice_ReturnsNull()
    {
        var info = new ModelInfo { ModelId = "test" };
        Assert.Null(info.CalculateCost(100, 100));
    }

    [Fact]
    public void CalculateCost_WithCache_IncludesCacheTokenCosts()
    {
        var info = new ModelInfo
        {
            ModelId = "claude-opus-4-6",
            InputPricePerMillion = 5.00m,
            OutputPricePerMillion = 25.00m,
            CacheReadPricePerMillion = 0.50m,
            CacheWritePricePerMillion = 6.25m
        };

        var cost = info.CalculateCost(1_000_000, 500_000, 2_000_000, 1_000_000);

        Assert.NotNull(cost);
        // $5 + $12.5 + $1 + $6.25 = $24.75
        Assert.Equal(24.75m, cost.Value);
    }

    [Fact]
    public void CalculateCost_WithNegativeInputTokens_Throws()
    {
        var info = new ModelInfo { ModelId = "test", InputPricePerMillion = 3.00m, OutputPricePerMillion = 15.00m };
        Assert.Throws<ArgumentOutOfRangeException>(() => info.CalculateCost(-1, 100));
    }

    [Fact]
    public void CalculateCost_WithNegativeOutputTokens_Throws()
    {
        var info = new ModelInfo { ModelId = "test", InputPricePerMillion = 3.00m, OutputPricePerMillion = 15.00m };
        Assert.Throws<ArgumentOutOfRangeException>(() => info.CalculateCost(100, -1));
    }

    [Fact]
    public void CalculateCost_WithNegativeCacheTokens_Throws()
    {
        var info = new ModelInfo { ModelId = "test", InputPricePerMillion = 3.00m, OutputPricePerMillion = 15.00m };
        Assert.Throws<ArgumentOutOfRangeException>(() => info.CalculateCost(100, 100, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => info.CalculateCost(100, 100, 0, -1));
    }

    [Fact]
    public void CalculateCost_WithCache_MissingCachePrice_FallsBackToInputPrice()
    {
        var info = new ModelInfo
        {
            ModelId = "test",
            InputPricePerMillion = 3.00m,
            OutputPricePerMillion = 15.00m
        };

        var cost = info.CalculateCost(0, 0, 1_000_000, 1_000_000);

        Assert.NotNull(cost);
        // Both cache types fall back to InputPricePerMillion: $3 + $3 = $6
        Assert.Equal(6.00m, cost.Value);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var a = new ModelInfo { ModelId = "gpt-4o", Provider = "OpenAI" };
        var b = new ModelInfo { ModelId = "gpt-4o", Provider = "OpenAI" };
        Assert.Equal(a, b);
    }
}
