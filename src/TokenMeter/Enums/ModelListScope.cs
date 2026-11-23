namespace TokenMeter;

/// <summary>Which models <see cref="ModelCatalog.List(ModelQuery)"/> returns. There is no default: the caller chooses.</summary>
public enum ModelListScope
{
    /// <summary>For each provider and rung, the latest model of the rung — what a model picker shows by default.</summary>
    LatestPerTier = 1,

    /// <summary>Every model that passes the query's filters, older versions of a line included.</summary>
    All = 2,
}
