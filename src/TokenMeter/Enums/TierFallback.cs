namespace TokenMeter;

/// <summary>What <see cref="ModelCatalog.Resolve(string, CapabilityTier, ResolveOptions?)"/> does when the rung asked for is empty.</summary>
public enum TierFallback
{
    /// <summary>Nothing: an empty rung resolves to <c>null</c>.</summary>
    None = 0,

    /// <summary>The nearest rung below that holds a model.</summary>
    Lower = 1,

    /// <summary>The nearest rung above that holds a model.</summary>
    Higher = 2,
}
