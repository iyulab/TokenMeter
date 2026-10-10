using System.Text.Json;

namespace TokenMeter.Internal;

internal static class ModelInfoLoader
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNameCaseInsensitive = true,
        // A key written twice in a model entry is an editing slip, and the serializer would otherwise keep the
        // last value silently — a GPT-5.6 Sol cache-write rate was billed at a stale value that way.
        AllowDuplicateProperties = false,
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

            var provider = JsonSerializer.Deserialize<ProviderDataJson>(stream, s_options)
                ?? throw new InvalidOperationException(
                    $"Failed to deserialize model data from embedded resource '{resourceName}'. " +
                    "The JSON may be malformed or empty.");

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

        // A missing or malformed date leaves the provider undated rather than throwing:
        // the loader only fails on unreadable JSON, and an undated provider is reported as
        // stale by the catalog. Completeness is enforced by the test suite.
        DateOnly? lastUpdated =
            DateOnly.TryParse(json.LastUpdated, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;

        return new ProviderData(json.Provider, models, aliasRules, lastUpdated);
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
        CacheStoragePricePerMillionPerHour = j.CacheStoragePricePerMillionPerHour,
        ImageInputPrice = j.ImageInputPrice,
        AudioInputPricePerSecond = j.AudioInputPricePerSecond,
        PricingTiers = j.PricingTiers?.Select(ToPricingTier).ToList(),
        // An entry whose lifetime or date does not parse is left out rather than thrown on, as a malformed
        // lastUpdated is: the loader fails only on unreadable JSON. The test suite holds every entry to parsing.
        CacheWritePrices = ToCacheWritePrices(j.CacheWritePrices),
        // A tier name that does not parse is left out, as an unparseable lifetime is; the test suite holds every entry to
        // parsing.
        ServiceTierMultipliers = j.ServiceTierMultipliers?
            .Where(kv => Enum.TryParse<ServiceTier>(kv.Key, ignoreCase: true, out _))
            .Select(kv => new ServiceTierMultiplier { Tier = Enum.Parse<ServiceTier>(kv.Key, ignoreCase: true), Multiplier = kv.Value })
            .ToList(),
        RegionalMultipliers = j.RegionalMultipliers?
            .Select(kv => new RegionalMultiplier { Region = kv.Key, Multiplier = kv.Value })
            .ToList(),
        ToolCallPrices = j.ToolCallPricesPerThousand?
            .Select(kv => new ToolCallPrice { Tool = kv.Key, PricePerThousandCalls = kv.Value })
            .ToList(),
        ScheduledPrices = j.ScheduledPrices?
            .Where(p => ParseDate(p.EffectiveFrom) is not null)
            .Select(p => new ScheduledPrice
            {
                EffectiveFrom = ParseDate(p.EffectiveFrom)!.Value,
                InputPricePerMillion = p.InputPricePerMillion,
                OutputPricePerMillion = p.OutputPricePerMillion,
                CacheReadPricePerMillion = p.CacheReadPricePerMillion,
                CacheWritePricePerMillion = p.CacheWritePricePerMillion,
                CacheStoragePricePerMillionPerHour = p.CacheStoragePricePerMillionPerHour,
            })
            .OrderBy(p => p.EffectiveFrom)
            .ToList(),
        DeprecationDate = ParseDate(j.DeprecationDate),
        RetirementDate = ParseDate(j.RetirementDate),
        ReplacementModelId = j.ReplacementModelId,
        PriceSource = ParseEnum<PriceSource>(j.PriceSource, PriceSource.Official),
        SupportsImageInput = j.SupportsImageInput,
        SupportsAudioInput = j.SupportsAudioInput,
        SupportsVideoInput = j.SupportsVideoInput,
        SupportsDocumentInput = j.SupportsDocumentInput,
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

    private static PricingTier ToPricingTier(PricingTierJson j) => new()
    {
        Axis = ParseEnum(j.Axis, PricingTierAxis.ContextLength),
        MinContextLengthTokens = j.MinContextLengthTokens,
        WindowStartUtc = ParseTimeOfDay(j.WindowStartUtc),
        WindowEndUtc = ParseTimeOfDay(j.WindowEndUtc),
        InputPricePerMillion = j.InputPricePerMillion,
        OutputPricePerMillion = j.OutputPricePerMillion,
        CacheReadPricePerMillion = j.CacheReadPricePerMillion,
        CacheWritePricePerMillion = j.CacheWritePricePerMillion,
        CacheWritePrices = ToCacheWritePrices(j.CacheWritePrices),
    };

    private static List<CacheWritePrice>? ToCacheWritePrices(List<CacheWritePriceJson>? prices) => prices?
        .Where(p => ParseTtl(p.Ttl) is not null)
        .Select(p => new CacheWritePrice { Ttl = ParseTtl(p.Ttl)!.Value, PricePerMillion = p.PricePerMillion })
        .ToList();

    internal static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var parsed)
            ? parsed
            : null;

    // "5m", "1h", "30s" — the way vendors name cache lifetimes.
    internal static TimeSpan? ParseTtl(string? value)
    {
        if (value is not { Length: >= 2 }
            || !int.TryParse(value.AsSpan(0, value.Length - 1), System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var amount))
            return null;

        return value[^1] switch
        {
            's' => TimeSpan.FromSeconds(amount),
            'm' => TimeSpan.FromMinutes(amount),
            'h' => TimeSpan.FromHours(amount),
            _ => null,
        };
    }

    private static TimeOnly? ParseTimeOfDay(string? value) =>
        TimeOnly.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
}

internal sealed record ProviderData(
    string ProviderName,
    IReadOnlyDictionary<string, ModelInfo> Models,
    IReadOnlyList<AliasRule> AliasRules,
    DateOnly? LastUpdated);
