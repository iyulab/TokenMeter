namespace TokenMeter;

/// <summary>
/// Where a model stands in its vendor's lifecycle on a given date — see
/// <see cref="ModelInfo.GetLifecycleStatus(DateOnly)"/>.
/// </summary>
public enum ModelLifecycleStatus
{
    /// <summary>Served and not deprecated (or the catalog records no deprecation).</summary>
    Active,

    /// <summary>Still served, but the vendor has deprecated it and set (or will set) a retirement date.</summary>
    Deprecated,

    /// <summary>The vendor no longer serves it; requests fail.</summary>
    Retired
}
