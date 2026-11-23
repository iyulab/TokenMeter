namespace TokenMeter;

// Capability tiers: which model is "the latest of a rung" at a provider. Separate from the price lookup on purpose:
// FindModel refuses to cross a version token, because a fuzzy "latest" would price a newer model as an older one. A rung
// is a different question - it returns a concrete catalog model, and the price lookup stays on that concrete id.
public static partial class ModelCatalog
{
    private static readonly IReadOnlyDictionary<string, CapabilityTier> s_noLines =
        new Dictionary<string, CapabilityTier>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The provider's lines placed on the capability scale: line name (<c>sonnet</c>) → rung. A line the vendor does not
    /// place is absent, and so are the providers whose tiers the catalog does not know. Empty for an unknown provider.
    /// </summary>
    public static IReadOnlyDictionary<string, CapabilityTier> GetLines(string providerName) =>
        s_lines.TryGetValue(providerName, out var lines) ? lines : s_noLines;

    /// <summary>
    /// Resolves a rung to the provider's latest model of it: the highest <see cref="ModelInfo.Version"/> among the
    /// generally available, not deprecated models of the rung's line. An empty rung is <c>null</c> unless
    /// <see cref="ResolveOptions.Fallback"/> names a direction, and then the answer says it was
    /// <see cref="TierResolution.Substituted"/>. Unknown provider: <c>null</c>.
    /// </summary>
    public static TierResolution? Resolve(string provider, CapabilityTier tier, ResolveOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        if (!Enum.IsDefined(tier))
            throw new ArgumentOutOfRangeException(nameof(tier), tier, "Not a rung of the capability scale.");

        options ??= new ResolveOptions();
        var filter = new TierFilter(ModelType.Chat, false, false, options.IncludePreview, false, options.AsOf ?? Today(), options.Available);
        var request = $"{provider}:{tier.Alias()}";
        var ladder = LatestPerRung(provider, filter);

        if (ladder.TryGetValue(tier, out var hit))
            return new TierResolution(request, hit.Model, tier, false, hit.Id);

        var step = options.Fallback switch
        {
            TierFallback.Lower => -1,
            TierFallback.Higher => 1,
            _ => 0,
        };
        if (step == 0) return null;

        for (var rung = (int)tier + step; Enum.IsDefined((CapabilityTier)rung); rung += step)
        {
            if (ladder.TryGetValue((CapabilityTier)rung, out var near))
                return new TierResolution(request, near.Model, (CapabilityTier)rung, true, near.Id);
        }

        return null;
    }

    /// <summary>
    /// Resolves an alias <c>&lt;provider&gt;:&lt;rung&gt;</c> (<c>anthropic:balanced</c>) or <c>&lt;provider&gt;:&lt;line&gt;</c>
    /// (<c>anthropic:sonnet</c>, a line the catalog has placed on a rung) the way
    /// <see cref="Resolve(string, CapabilityTier, ResolveOptions?)"/> does. A line alias is a convenience: the rung of a
    /// line can change from one generation to the next, so prefer the rung when the intent is a class of model.
    /// </summary>
    /// <exception cref="ArgumentException">The text is not of the form <c>provider:name</c>.</exception>
    /// <returns><c>null</c> when the provider or the name is not one the catalog knows, or nothing is eligible.</returns>
    public static TierResolution? Resolve(string alias, ResolveOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);
        var colon = alias.IndexOf(':');
        var provider = colon > 0 ? alias[..colon].Trim() : "";
        var name = colon > 0 ? alias[(colon + 1)..].Trim() : "";
        if (provider.Length == 0 || name.Length == 0)
            throw new ArgumentException($"'{alias}' is not a capability alias; expected '<provider>:<rung or line>', e.g. 'anthropic:balanced'.", nameof(alias));

        if (CapabilityTierExtensions.TryParse(name, out var tier))
            return Resolve(provider, tier, options) is { } byRung ? byRung with { Request = alias } : null;

        options ??= new ResolveOptions();
        if (!GetLines(provider).TryGetValue(name, out var lineTier)) return null;

        var filter = new TierFilter(ModelType.Chat, false, false, options.IncludePreview, false, options.AsOf ?? Today(), options.Available);
        var latest = Latest(GetByProvider(provider).Where(m => string.Equals(m.Line, name, StringComparison.OrdinalIgnoreCase)), filter);
        return latest is { } found ? new TierResolution(alias, found.Model, lineTier, false, found.Id) : null;
    }

    /// <summary>
    /// Lists models by the query: for each provider the latest model of each rung (<see cref="ModelListScope.LatestPerTier"/>,
    /// lowest rung first), or every model that passes the filters (<see cref="ModelListScope.All"/>: by provider, then rung,
    /// then line, newest version first; models without a rung last).
    /// </summary>
    public static IReadOnlyList<ModelInfo> List(ModelQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!Enum.IsDefined(query.Scope))
            throw new ArgumentOutOfRangeException(nameof(query), query.Scope, "Choose LatestPerTier or All.");

        var filter = new TierFilter(query.Type, query.IncludeDeprecated, query.IncludeRetired, query.IncludePreview,
            query.IncludeRestricted, query.AsOf ?? Today(), query.Available);

        var providers = query.Provider is { } name
            ? s_byProvider.Keys.Where(p => string.Equals(p, name, StringComparison.OrdinalIgnoreCase))
            : s_byProvider.Keys;

        var result = new List<ModelInfo>();
        foreach (var provider in providers.Order(StringComparer.OrdinalIgnoreCase))
        {
            if (query.Scope == ModelListScope.LatestPerTier)
            {
                result.AddRange(LatestPerRung(provider, filter).OrderBy(r => r.Key).Select(r => r.Value.Model));
                continue;
            }

            result.AddRange(GetByProvider(provider)
                .Where(m => Eligible(m, filter) is not null)
                .OrderBy(m => m.Tier is null)
                .ThenBy(m => m.Tier)
                .ThenBy(m => m.Line, StringComparer.Ordinal)
                .ThenByDescending(m => m.Version)
                .ThenBy(m => m.ModelId, StringComparer.Ordinal));
        }

        return result;
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>What a query lets through. <c>Restricted</c> only matters to a list of every model: a rung never resolves to one.</summary>
    private sealed record TierFilter(
        ModelType? Type, bool Deprecated, bool Retired, bool Preview, bool Restricted, DateOnly AsOf, IReadOnlyCollection<string>? Available);

    private readonly record struct Hit(ModelInfo Model, string Id);

    /// <summary>The id to send for the model when it passes the filter, or <c>null</c> when it does not.</summary>
    private static string? Eligible(ModelInfo model, TierFilter filter)
    {
        if (filter.Type is { } type && model.ModelType != type) return null;

        switch (model.GetLifecycleStatus(filter.AsOf))
        {
            case ModelLifecycleStatus.Deprecated when !filter.Deprecated:
            case ModelLifecycleStatus.Retired when !filter.Retired:
                return null;
        }

        switch (model.Availability)
        {
            case ModelAvailability.Preview when !filter.Preview:
            case ModelAvailability.Restricted when !filter.Restricted:
                return null;
        }

        return filter.Available is null ? model.ModelId : LiveId(model.ModelId, filter.Available);
    }

    // The latest eligible model: the highest version, a general model before a preview one at the same version.
    private static Hit? Latest(IEnumerable<ModelInfo> models, TierFilter filter)
    {
        Hit? best = null;
        foreach (var model in models)
        {
            if (Eligible(model, filter) is not { } id) continue;
            if (best is not { } current || IsLater(model, current.Model))
                best = new Hit(model, id);
        }
        return best;
    }

    private static bool IsLater(ModelInfo candidate, ModelInfo current)
    {
        var byVersion = Comparer<Version?>.Default.Compare(candidate.Version, current.Version);
        if (byVersion != 0) return byVersion > 0;
        if (candidate.Availability != current.Availability) return candidate.Availability < current.Availability;
        return string.CompareOrdinal(candidate.ModelId, current.ModelId) < 0;
    }

    private static Dictionary<CapabilityTier, Hit> LatestPerRung(string provider, TierFilter filter)
    {
        // A restricted model is never "the latest of a rung": few accounts can call it.
        var rungFilter = filter with { Restricted = false };
        var result = new Dictionary<CapabilityTier, Hit>();
        foreach (var rung in GetByProvider(provider).Where(m => m.Tier is not null).GroupBy(m => m.Tier!.Value))
        {
            if (Latest(rung, rungFilter) is { } hit)
                result[rung.Key] = hit;
        }
        return result;
    }

    /// <summary>
    /// The id the account serves for a catalog model: the model's own id, or a dated snapshot of it
    /// (<c>claude-sonnet-4-6-20260101</c>, <c>-2025-08-07</c>). <c>claude-sonnet-4-6-turbo</c> and
    /// <c>claude-sonnet-4-60</c> are other models.
    /// </summary>
    private static string? LiveId(string modelId, IReadOnlyCollection<string> available)
    {
        string? snapshot = null;
        foreach (var live in available)
        {
            if (string.Equals(live, modelId, StringComparison.OrdinalIgnoreCase)) return live;
            if (snapshot is null && IsSnapshotOf(live, modelId)) snapshot = live;
        }
        return snapshot;
    }

    private static bool IsSnapshotOf(string live, string modelId)
    {
        if (live.Length <= modelId.Length + 1
            || !live.StartsWith(modelId, StringComparison.OrdinalIgnoreCase)
            || live[modelId.Length] is not ('-' or '@'))
            return false;

        var rest = live.AsSpan(modelId.Length + 1);
        var digits = 0;
        foreach (var c in rest)
        {
            if (char.IsAsciiDigit(c)) digits++;
            else if (c != '-') return false;
        }
        return char.IsAsciiDigit(rest[0]) && digits >= 3;
    }
}
