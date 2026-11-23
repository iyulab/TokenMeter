namespace TokenMeter;

/// <summary>How <see cref="ModelCatalog.Resolve(string, CapabilityTier, ResolveOptions?)"/> resolves a rung.</summary>
public sealed record ResolveOptions
{
    /// <summary>What to do when the rung is empty. Defaults to <see cref="TierFallback.None"/>.</summary>
    public TierFallback Fallback { get; init; }

    /// <summary>
    /// The ids the caller's account serves (a provider's live model list). When set, the answer is the latest model among
    /// those; an id that is a dated snapshot of a catalog model (<c>claude-sonnet-4-6-20260101</c>) counts as that model.
    /// <c>null</c> means every catalog model is a candidate.
    /// </summary>
    public IReadOnlyCollection<string>? Available { get; init; }

    /// <summary>Whether a preview model may be the answer. Defaults to <c>false</c>.</summary>
    public bool IncludePreview { get; init; }

    /// <summary>
    /// The day the lifecycle of each model is judged on (a deprecated or retired model is not the latest). Defaults to
    /// today (UTC).
    /// </summary>
    public DateOnly? AsOf { get; init; }
}
