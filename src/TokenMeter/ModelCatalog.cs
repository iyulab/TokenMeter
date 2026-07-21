using TokenMeter.Internal;

namespace TokenMeter;

/// <summary>
/// Built-in model metadata catalog loaded from embedded provider JSON files.
/// Provides alias-aware lookup and filtering by provider or model type.
/// </summary>
public static class ModelCatalog
{
    private static readonly IReadOnlyDictionary<string, ModelInfo> s_empty =
        new Dictionary<string, ModelInfo>(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, ModelInfo> s_all;
    private static readonly Dictionary<string, IReadOnlyDictionary<string, ModelInfo>> s_byProvider;
    private static readonly List<AliasRule> s_aliasRules;

    static ModelCatalog()
    {
        var providers = ModelInfoLoader.LoadAll();

        s_all = new Dictionary<string, ModelInfo>(StringComparer.OrdinalIgnoreCase);
        s_byProvider = new Dictionary<string, IReadOnlyDictionary<string, ModelInfo>>(StringComparer.OrdinalIgnoreCase);
        s_aliasRules = [];

        foreach (var provider in providers)
        {
            foreach (var kv in provider.Models)
                s_all[kv.Key] = kv.Value;

            s_byProvider[provider.ProviderName] = provider.Models;
            s_aliasRules.AddRange(provider.AliasRules);
        }
    }

    /// <summary>Date when the embedded catalog data was last updated.</summary>
    public static DateOnly LastUpdated { get; } = new(2026, 7, 21);

    /// <summary>Days elapsed since <see cref="LastUpdated"/>.</summary>
    public static int DataAgeDays =>
        DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - LastUpdated.DayNumber;

    /// <summary>
    /// Returns <c>true</c> if catalog data is older than <paramref name="maxAgeDays"/>.
    /// Default threshold is 90 days.
    /// </summary>
    public static bool IsDataStale(int maxAgeDays = 90) => DataAgeDays > maxAgeDays;

    /// <summary>All models in the catalog, keyed by <see cref="ModelInfo.ModelId"/> (case-insensitive).</summary>
    public static IReadOnlyDictionary<string, ModelInfo> All => s_all;

    /// <summary>Models grouped by provider name (case-insensitive keys).</summary>
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, ModelInfo>> ByProvider => s_byProvider;

    // ── Provider convenience properties ───────────────────────────────────────

    /// <summary>OpenAI models.</summary>
    public static IReadOnlyDictionary<string, ModelInfo> OpenAI => GetProvider("OpenAI");

    /// <summary>Anthropic Claude models.</summary>
    public static IReadOnlyDictionary<string, ModelInfo> Anthropic => GetProvider("Anthropic");

    /// <summary>Google Gemini models.</summary>
    public static IReadOnlyDictionary<string, ModelInfo> Google => GetProvider("Google");

    /// <summary>xAI Grok models.</summary>
    public static IReadOnlyDictionary<string, ModelInfo> XAI => GetProvider("xAI");

    /// <summary>Azure OpenAI models.</summary>
    public static IReadOnlyDictionary<string, ModelInfo> Azure => GetProvider("Azure");

    /// <summary>Mistral models.</summary>
    public static IReadOnlyDictionary<string, ModelInfo> Mistral => GetProvider("Mistral");

    /// <summary>DeepSeek models.</summary>
    public static IReadOnlyDictionary<string, ModelInfo> DeepSeek => GetProvider("DeepSeek");

    /// <summary>Amazon Nova models.</summary>
    public static IReadOnlyDictionary<string, ModelInfo> AmazonNova => GetProvider("Amazon Nova");

    /// <summary>Cohere models.</summary>
    public static IReadOnlyDictionary<string, ModelInfo> Cohere => GetProvider("Cohere");

    /// <summary>Meta Llama models.</summary>
    public static IReadOnlyDictionary<string, ModelInfo> MetaLlama => GetProvider("Meta Llama");

    /// <summary>Perplexity models.</summary>
    public static IReadOnlyDictionary<string, ModelInfo> Perplexity => GetProvider("Perplexity");

    /// <summary>Qwen models.</summary>
    public static IReadOnlyDictionary<string, ModelInfo> Qwen => GetProvider("Qwen");

    // ── Lookup ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Finds a model by ID or alias using full fuzzy matching
    /// (4-pass: exact lookup → alias exact → alias prefix (longest) → alias contains (longest)).
    /// <para>
    /// The prefix/contains passes are deliberately fuzzy: they absorb cloud-specific ID variants
    /// (e.g. Bedrock <c>us.anthropic.claude-...</c>, date-suffixed IDs). The trade-off is that
    /// local/self-hosted deployment names that reuse or embed a public model name
    /// (e.g. <c>deepseek-r1-distill-qwen-7b</c>, community fine-tunes) can match a catalog entry
    /// whose <see cref="ModelInfo.ContextWindow"/> and pricing do not describe the deployment.
    /// When correctness matters more than recall, bound the fuzziness with
    /// <see cref="FindModel(string?, AliasMatchType)"/> or inspect the match pass via
    /// <see cref="FindModelMatch(string?, AliasMatchType)"/>.
    /// For self-hosted models, do not derive token budgets from the catalog — the effective
    /// context length is a per-deployment setting (e.g. llama.cpp <c>n_ctx</c>) the catalog
    /// cannot know; use your deployment configuration instead.
    /// </para>
    /// </summary>
    public static ModelInfo? FindModel(string? modelId) =>
        FindModelMatch(modelId, AliasMatchType.Contains)?.Model;

    /// <summary>
    /// Finds a model by ID or alias, bounding how fuzzy the match may be.
    /// <paramref name="maxFuzziness"/> is the loosest pass allowed:
    /// <see cref="AliasMatchType.Exact"/> = catalog ID / exact alias only,
    /// <see cref="AliasMatchType.Prefix"/> = also allow prefix aliases,
    /// <see cref="AliasMatchType.Contains"/> = full fuzzy matching (same as
    /// <see cref="FindModel(string?)"/>).
    /// </summary>
    public static ModelInfo? FindModel(string? modelId, AliasMatchType maxFuzziness) =>
        FindModelMatch(modelId, maxFuzziness)?.Model;

    /// <summary>
    /// Finds a model by ID or alias and reports which pass produced the match
    /// (<see cref="ModelMatch.MatchKind"/>), so consumers can apply confidence-based
    /// handling — e.g. trust <see cref="AliasMatchType.Exact"/>, fall back to manual
    /// configuration when the result came from a <see cref="AliasMatchType.Contains"/> inference.
    /// </summary>
    public static ModelMatch? FindModelMatch(string? modelId, AliasMatchType maxFuzziness = AliasMatchType.Contains)
    {
        if (string.IsNullOrWhiteSpace(modelId)) return null;
        if (s_all.TryGetValue(modelId, out var exact)) return new ModelMatch(exact, AliasMatchType.Exact);

        var normalized = modelId.ToLowerInvariant();

        // Pass 1: Exact alias
        foreach (var rule in s_aliasRules)
        {
            if (rule.MatchType == AliasMatchType.Exact &&
                normalized.Equals(rule.Pattern, StringComparison.Ordinal))
                return new ModelMatch(rule.Target, AliasMatchType.Exact);
        }

        if (maxFuzziness < AliasMatchType.Prefix) return null;

        // Pass 2: Prefix alias — longest wins
        AliasRule? bestPrefix = null;
        foreach (var rule in s_aliasRules)
        {
            if (rule.MatchType == AliasMatchType.Prefix &&
                normalized.StartsWith(rule.Pattern, StringComparison.Ordinal) &&
                (bestPrefix is null || rule.Pattern.Length > bestPrefix.Pattern.Length))
                bestPrefix = rule;
        }
        if (bestPrefix is not null) return new ModelMatch(bestPrefix.Target, AliasMatchType.Prefix);

        if (maxFuzziness < AliasMatchType.Contains) return null;

        // Pass 3: Contains alias — longest wins
        AliasRule? bestContains = null;
        foreach (var rule in s_aliasRules)
        {
            if (rule.MatchType == AliasMatchType.Contains &&
                normalized.Contains(rule.Pattern, StringComparison.Ordinal) &&
                (bestContains is null || rule.Pattern.Length > bestContains.Pattern.Length))
                bestContains = rule;
        }
        return bestContains is not null ? new ModelMatch(bestContains.Target, AliasMatchType.Contains) : null;
    }

    // ── Filtering ─────────────────────────────────────────────────────────────

    /// <summary>Returns all models for the given provider (case-insensitive).</summary>
    public static IEnumerable<ModelInfo> GetByProvider(string providerName) =>
        s_byProvider.TryGetValue(providerName, out var dict) ? dict.Values : [];

    /// <summary>Returns all models of the given type.</summary>
    public static IEnumerable<ModelInfo> GetByType(ModelType type) =>
        s_all.Values.Where(m => m.ModelType == type);

    /// <summary>All provider names in the catalog.</summary>
    public static IEnumerable<string> GetProviderNames() => s_byProvider.Keys;

    /// <summary>
    /// Returns the models for the given provider as a lookup dictionary (case-insensitive keys),
    /// or an empty dictionary if the provider is not in the catalog.
    /// The string-keyed counterpart to the typed convenience properties (e.g. <see cref="OpenAI"/>);
    /// prefer this when the provider name is only known at runtime.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelInfo> GetProvider(string name) =>
        s_byProvider.TryGetValue(name, out var dict) ? dict : s_empty;
}
