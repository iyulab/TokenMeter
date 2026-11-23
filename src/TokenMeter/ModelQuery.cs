namespace TokenMeter;

/// <summary>What <see cref="ModelCatalog.List(ModelQuery)"/> returns.</summary>
public sealed record ModelQuery
{
    /// <summary>
    /// Latest model per rung, or every model. Required: a default would quietly hide older versions (or show them) for a
    /// caller that never chose.
    /// </summary>
    public required ModelListScope Scope { get; init; }

    /// <summary>Only this provider's models (case-insensitive), or every provider when <c>null</c>.</summary>
    public string? Provider { get; init; }

    /// <summary>Only models of this type. Defaults to <see cref="ModelType.Chat"/>; <c>null</c> means every type.</summary>
    public ModelType? Type { get; init; } = ModelType.Chat;

    /// <summary>Also models the vendor has deprecated. Defaults to <c>false</c>.</summary>
    public bool IncludeDeprecated { get; init; }

    /// <summary>Also models the vendor no longer serves. Defaults to <c>false</c>.</summary>
    public bool IncludeRetired { get; init; }

    /// <summary>Also preview models. Defaults to <c>false</c>.</summary>
    public bool IncludePreview { get; init; }

    /// <summary>
    /// Also models only cleared accounts can call, in an <see cref="ModelListScope.All"/> list. A restricted model is never
    /// "the latest of a rung", so <see cref="ModelListScope.LatestPerTier"/> ignores this. Defaults to <c>false</c>.
    /// </summary>
    public bool IncludeRestricted { get; init; }

    /// <summary>The ids the caller's account serves — see <see cref="ResolveOptions.Available"/>. <c>null</c> means all.</summary>
    public IReadOnlyCollection<string>? Available { get; init; }

    /// <summary>The day lifecycle is judged on. Defaults to today (UTC).</summary>
    public DateOnly? AsOf { get; init; }
}
