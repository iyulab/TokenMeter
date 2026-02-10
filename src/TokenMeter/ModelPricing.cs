namespace TokenMeter;

/// <summary>
/// Pricing information for a model.
/// Prices are per 1 million tokens.
/// </summary>
public record ModelPricing
{
    /// <summary>
    /// Model identifier.
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Cost per 1 million input tokens (USD).
    /// </summary>
    public required decimal InputPricePerMillion { get; init; }

    /// <summary>
    /// Cost per 1 million output tokens (USD).
    /// </summary>
    public required decimal OutputPricePerMillion { get; init; }

    /// <summary>
    /// Provider name (e.g., "OpenAI", "Anthropic", "Google", "xAI", "Azure").
    /// </summary>
    public string? Provider { get; init; }

    /// <summary>
    /// Display name for the model.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Context window size in tokens (if known).
    /// </summary>
    public int? ContextWindow { get; init; }

    /// <summary>
    /// Calculates the cost for the given token counts.
    /// </summary>
    public decimal CalculateCost(int inputTokens, int outputTokens)
    {
        var inputCost = (inputTokens / 1_000_000m) * InputPricePerMillion;
        var outputCost = (outputTokens / 1_000_000m) * OutputPricePerMillion;
        return inputCost + outputCost;
    }
}

/// <summary>
/// Built-in pricing data for common models.
/// Pricing data is loaded from embedded JSON resources per provider.
///
/// Sources:
/// - OpenAI: https://openai.com/api/pricing/
/// - Anthropic: https://docs.anthropic.com/en/docs/about-claude/pricing
/// - Google: https://ai.google.dev/gemini-api/docs/pricing
/// - Azure: https://azure.microsoft.com/en-us/pricing/details/cognitive-services/openai-service/
/// - xAI: https://docs.x.ai/docs/models
/// - Mistral: https://mistral.ai/technology/
/// - DeepSeek: https://platform.deepseek.com/api-docs/pricing
/// - Amazon Nova: https://aws.amazon.com/bedrock/pricing/
/// - Cohere: https://cohere.com/pricing
/// - Meta Llama: https://llama.meta.com/
/// - Perplexity: https://docs.perplexity.ai/guides/pricing
/// - Qwen: https://help.aliyun.com/zh/model-studio/
/// </summary>
public static class ModelPricingData
{
    private static readonly IReadOnlyDictionary<string, ModelPricing> s_empty =
        new Dictionary<string, ModelPricing>(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, ModelPricing> _all;
    private static readonly Dictionary<string, IReadOnlyDictionary<string, ModelPricing>> _byProvider;
    private static readonly List<AliasRule> _allAliasRules;

    static ModelPricingData()
    {
        var providers = PricingLoader.LoadAll();

        var all = new Dictionary<string, ModelPricing>(StringComparer.OrdinalIgnoreCase);
        var byProvider = new Dictionary<string, IReadOnlyDictionary<string, ModelPricing>>(StringComparer.OrdinalIgnoreCase);
        var allAliasRules = new List<AliasRule>();

        foreach (var provider in providers)
        {
            foreach (var kv in provider.Models)
            {
                all[kv.Key] = kv.Value;
            }
            byProvider[provider.ProviderName] = provider.Models;
            allAliasRules.AddRange(provider.AliasRules);
        }

        _all = all;
        _byProvider = byProvider;
        _allAliasRules = allAliasRules;
    }

    /// <summary>
    /// Last updated date for pricing data.
    /// </summary>
    public static DateOnly LastUpdated { get; } = new(2026, 2, 10);

    /// <summary>
    /// Gets pricing for OpenAI models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> OpenAI => GetProviderDict("OpenAI");

    /// <summary>
    /// Gets pricing for Anthropic models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> Anthropic => GetProviderDict("Anthropic");

    /// <summary>
    /// Gets pricing for Google Gemini models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> Google => GetProviderDict("Google");

    /// <summary>
    /// Gets pricing for xAI Grok models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> XAI => GetProviderDict("xAI");

    /// <summary>
    /// Gets pricing for Azure OpenAI models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> Azure => GetProviderDict("Azure");

    /// <summary>
    /// Gets pricing for Mistral models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> Mistral => GetProviderDict("Mistral");

    /// <summary>
    /// Gets pricing for DeepSeek models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> DeepSeek => GetProviderDict("DeepSeek");

    /// <summary>
    /// Gets pricing for Amazon Nova models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> AmazonNova => GetProviderDict("Amazon Nova");

    /// <summary>
    /// Gets pricing for Cohere models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> Cohere => GetProviderDict("Cohere");

    /// <summary>
    /// Gets pricing for Meta Llama models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> MetaLlama => GetProviderDict("Meta Llama");

    /// <summary>
    /// Gets pricing for Perplexity models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> Perplexity => GetProviderDict("Perplexity");

    /// <summary>
    /// Gets pricing for Qwen models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> Qwen => GetProviderDict("Qwen");

    /// <summary>
    /// Gets all built-in pricing data.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> All => _all;

    /// <summary>
    /// Gets all providers with their pricing dictionaries.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, ModelPricing>> ByProvider => _byProvider;

    /// <summary>
    /// Tries to find pricing for a model by ID or alias.
    /// Uses 3-pass matching: exact dictionary lookup, then alias matching (exact → prefix → contains).
    /// </summary>
    public static ModelPricing? FindPricing(string modelId)
    {
        // Direct dictionary lookup (case-insensitive)
        if (_all.TryGetValue(modelId, out var pricing))
        {
            return pricing;
        }

        var normalized = modelId.ToLowerInvariant();

        // Pass 1: Exact alias match
        foreach (var rule in _allAliasRules)
        {
            if (rule.MatchType == AliasMatchType.Exact &&
                normalized.Equals(rule.Pattern, StringComparison.Ordinal))
            {
                return rule.Target;
            }
        }

        // Pass 2: Prefix alias match
        foreach (var rule in _allAliasRules)
        {
            if (rule.MatchType == AliasMatchType.Prefix &&
                normalized.StartsWith(rule.Pattern, StringComparison.Ordinal))
            {
                return rule.Target;
            }
        }

        // Pass 3: Contains alias match
        foreach (var rule in _allAliasRules)
        {
            if (rule.MatchType == AliasMatchType.Contains &&
                normalized.Contains(rule.Pattern, StringComparison.Ordinal))
            {
                return rule.Target;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all models for a specific provider.
    /// </summary>
    public static IEnumerable<ModelPricing> GetByProvider(string providerName)
    {
        if (_byProvider.TryGetValue(providerName, out var provider))
        {
            return provider.Values;
        }

        return [];
    }

    /// <summary>
    /// Gets all available provider names.
    /// </summary>
    public static IEnumerable<string> GetProviderNames() => _byProvider.Keys;

    private static IReadOnlyDictionary<string, ModelPricing> GetProviderDict(string name) =>
        _byProvider.TryGetValue(name, out var dict) ? dict : s_empty;
}
