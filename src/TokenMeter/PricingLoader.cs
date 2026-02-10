using System.Text.Json;

namespace TokenMeter;

internal enum AliasMatchType
{
    Exact,
    Prefix,
    Contains
}

internal sealed record AliasRule(string Pattern, AliasMatchType MatchType, ModelPricing Target);

internal sealed record ProviderData(
    string ProviderName,
    IReadOnlyDictionary<string, ModelPricing> Models,
    IReadOnlyList<AliasRule> AliasRules);

internal static class PricingLoader
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    internal static ProviderData[] LoadAll()
    {
        var assembly = typeof(PricingLoader).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("TokenMeter.Pricing.", StringComparison.Ordinal) &&
                        n.EndsWith(".json", StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal);

        var providers = new List<ProviderData>();
        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null) continue;

            var json = JsonSerializer.Deserialize<ProviderPricingJson>(stream, s_jsonOptions);
            if (json is null) continue;

            providers.Add(BuildProviderData(json));
        }
        return [.. providers];
    }

    private static ProviderData BuildProviderData(ProviderPricingJson json)
    {
        var models = new Dictionary<string, ModelPricing>(StringComparer.OrdinalIgnoreCase);
        var aliasRules = new List<AliasRule>();

        foreach (var m in json.Models)
        {
            var pricing = new ModelPricing
            {
                ModelId = m.ModelId,
                InputPricePerMillion = m.InputPricePerMillion,
                OutputPricePerMillion = m.OutputPricePerMillion,
                Provider = json.Provider,
                DisplayName = m.DisplayName,
                ContextWindow = m.ContextWindow
            };
            models[m.ModelId] = pricing;

            if (m.Aliases is not null)
            {
                foreach (var alias in m.Aliases)
                {
                    var matchType = alias.Type.ToLowerInvariant() switch
                    {
                        "prefix" => AliasMatchType.Prefix,
                        "contains" => AliasMatchType.Contains,
                        _ => AliasMatchType.Exact
                    };
                    aliasRules.Add(new AliasRule(
                        alias.Pattern.ToLowerInvariant(),
                        matchType,
                        pricing));
                }
            }
        }

        return new ProviderData(
            json.Provider,
            new Dictionary<string, ModelPricing>(models, StringComparer.OrdinalIgnoreCase),
            aliasRules);
    }
}
