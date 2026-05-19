using System.Text.Json;

namespace TokenMeter.Internal;

internal static class ModelInfoLoader
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static IReadOnlyList<ProviderData> LoadAll()
    {
        var assembly = typeof(ModelInfoLoader).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("TokenMeter.Pricing.", StringComparison.Ordinal)
                     && n.EndsWith(".json", StringComparison.Ordinal));

        var results = new List<ProviderData>();
        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null) continue;

            var provider = JsonSerializer.Deserialize<ProviderDataJson>(stream, s_options);
            if (provider is null) continue;

            results.Add(ToProviderData(provider));
        }
        return results;
    }

    private static ProviderData ToProviderData(ProviderDataJson json)
    {
        var models = new Dictionary<string, ModelInfo>(StringComparer.OrdinalIgnoreCase);
        var aliasRules = new List<AliasRule>();

        foreach (var m in json.Models)
        {
            var info = ToModelInfo(m, json.Provider);
            models[m.ModelId] = info;

            foreach (var alias in m.Aliases ?? [])
            {
                var matchType = alias.Type.ToLowerInvariant() switch
                {
                    "prefix" => AliasMatchType.Prefix,
                    "contains" => AliasMatchType.Contains,
                    _ => AliasMatchType.Exact
                };
                aliasRules.Add(new AliasRule
                {
                    MatchType = matchType,
                    Pattern = alias.Pattern.ToLowerInvariant(),
                    Target = info
                });
            }
        }

        return new ProviderData(json.Provider, models, aliasRules);
    }

    private static ModelInfo ToModelInfo(ModelInfoJson j, string provider) => new()
    {
        ModelId = j.ModelId,
        Provider = provider,
        DisplayName = j.DisplayName,
        ModelType = ParseEnum<ModelType>(j.ModelType, ModelType.Chat),
        IsInstructTuned = j.IsInstructTuned,
        ContextWindow = j.ContextWindow,
        MaxOutputTokens = j.MaxOutputTokens,
        InputPricePerMillion = j.InputPricePerMillion,
        OutputPricePerMillion = j.OutputPricePerMillion,
        CacheReadPricePerMillion = j.CacheReadPricePerMillion,
        CacheWritePricePerMillion = j.CacheWritePricePerMillion,
        ImageInputPrice = j.ImageInputPrice,
        AudioInputPricePerSecond = j.AudioInputPricePerSecond,
        AudioOutputPricePerSecond = j.AudioOutputPricePerSecond,
        SupportsImageInput = j.SupportsImageInput,
        SupportsAudioInput = j.SupportsAudioInput,
        SupportsVideoInput = j.SupportsVideoInput,
        SupportsDocumentInput = j.SupportsDocumentInput,
        SupportsImageOutput = j.SupportsImageOutput,
        SupportsAudioOutput = j.SupportsAudioOutput,
        SupportsToolCalling = j.SupportsToolCalling,
        SupportsParallelToolCalling = j.SupportsParallelToolCalling,
        SupportsStructuredOutput = j.SupportsStructuredOutput,
        SupportsJsonMode = j.SupportsJsonMode,
        SupportsStreaming = j.SupportsStreaming,
        PromptCachingMode = ParseEnum<PromptCachingMode>(j.PromptCachingMode, PromptCachingMode.None),
        SupportsStopSequences = j.SupportsStopSequences,
        SupportsMcpToolUse = j.SupportsMcpToolUse,
        ReasoningMode = ParseEnum<ReasoningMode>(j.ReasoningMode, ReasoningMode.None),
        ThinkingFormat = ParseEnum<ThinkingFormat>(j.ThinkingFormat, ThinkingFormat.None),
        ThinkingTagPattern = j.ThinkingTagPattern,
        ThinkingFieldName = j.ThinkingFieldName,
        SupportsInterleavedThinking = j.SupportsInterleavedThinking,
        MaxThinkingTokens = j.MaxThinkingTokens,
        ToolCallingFormat = ParseEnum<ToolCallingFormat>(j.ToolCallingFormat, ToolCallingFormat.OpenAI),
    };

    private static T ParseEnum<T>(string value, T fallback) where T : struct, Enum
        => Enum.TryParse<T>(value, ignoreCase: true, out var result) ? result : fallback;
}

internal sealed record ProviderData(
    string ProviderName,
    IReadOnlyDictionary<string, ModelInfo> Models,
    IReadOnlyList<AliasRule> AliasRules);
