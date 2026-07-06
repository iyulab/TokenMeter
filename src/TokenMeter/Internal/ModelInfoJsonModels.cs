using System.Text.Json.Serialization;

namespace TokenMeter.Internal;

internal sealed class ProviderDataJson
{
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "";

    [JsonPropertyName("models")]
    public List<ModelInfoJson> Models { get; set; } = [];
}

internal sealed class ModelInfoJson
{
    // Identification
    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = "";

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    // Classification
    [JsonPropertyName("modelType")]
    public string ModelType { get; set; } = "Chat";

    [JsonPropertyName("isInstructTuned")]
    public bool IsInstructTuned { get; set; }

    // Token limits
    [JsonPropertyName("contextWindow")]
    public int? ContextWindow { get; set; }

    [JsonPropertyName("maxOutputTokens")]
    public int? MaxOutputTokens { get; set; }

    // Pricing
    [JsonPropertyName("inputPricePerMillion")]
    public decimal? InputPricePerMillion { get; set; }

    [JsonPropertyName("outputPricePerMillion")]
    public decimal? OutputPricePerMillion { get; set; }

    [JsonPropertyName("cacheReadPricePerMillion")]
    public decimal? CacheReadPricePerMillion { get; set; }

    [JsonPropertyName("cacheWritePricePerMillion")]
    public decimal? CacheWritePricePerMillion { get; set; }

    [JsonPropertyName("imageInputPrice")]
    public decimal? ImageInputPrice { get; set; }

    [JsonPropertyName("audioInputPricePerSecond")]
    public decimal? AudioInputPricePerSecond { get; set; }

    // Input modalities
    [JsonPropertyName("supportsImageInput")]
    public bool SupportsImageInput { get; set; }

    [JsonPropertyName("supportsAudioInput")]
    public bool SupportsAudioInput { get; set; }

    [JsonPropertyName("supportsVideoInput")]
    public bool SupportsVideoInput { get; set; }

    [JsonPropertyName("supportsDocumentInput")]
    public bool SupportsDocumentInput { get; set; }

    // API capabilities
    [JsonPropertyName("supportsToolCalling")]
    public bool SupportsToolCalling { get; set; }

    [JsonPropertyName("supportsParallelToolCalling")]
    public bool SupportsParallelToolCalling { get; set; }

    [JsonPropertyName("supportsStructuredOutput")]
    public bool SupportsStructuredOutput { get; set; }

    [JsonPropertyName("supportsJsonMode")]
    public bool SupportsJsonMode { get; set; }

    [JsonPropertyName("supportsStreaming")]
    public bool SupportsStreaming { get; set; }

    [JsonPropertyName("promptCachingMode")]
    public string PromptCachingMode { get; set; } = "None";

    [JsonPropertyName("supportsStopSequences")]
    public bool SupportsStopSequences { get; set; }

    [JsonPropertyName("supportsMcpToolUse")]
    public bool SupportsMcpToolUse { get; set; }

    // Reasoning / Thinking
    [JsonPropertyName("reasoningMode")]
    public string ReasoningMode { get; set; } = "None";

    [JsonPropertyName("thinkingFormat")]
    public string ThinkingFormat { get; set; } = "None";

    [JsonPropertyName("thinkingTagPattern")]
    public string? ThinkingTagPattern { get; set; }

    [JsonPropertyName("thinkingFieldName")]
    public string? ThinkingFieldName { get; set; }

    [JsonPropertyName("supportsInterleavedThinking")]
    public bool SupportsInterleavedThinking { get; set; }

    [JsonPropertyName("maxThinkingTokens")]
    public int? MaxThinkingTokens { get; set; }

    // Tool calling format
    [JsonPropertyName("toolCallingFormat")]
    public string ToolCallingFormat { get; set; } = "OpenAI";

    // Aliases
    [JsonPropertyName("aliases")]
    public List<AliasJson>? Aliases { get; set; }
}

internal sealed class AliasJson
{
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "exact";
}
