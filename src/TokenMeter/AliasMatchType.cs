namespace TokenMeter;

/// <summary>
/// How an alias pattern (or a lookup) matches a model identifier, ordered from
/// strictest to fuzziest. Also used as the <c>maxFuzziness</c> bound in
/// <see cref="ModelCatalog.FindModel(string?, AliasMatchType)"/> /
/// <see cref="ModelCatalog.FindModelMatch(string?, AliasMatchType)"/> and as the
/// match diagnostic in <see cref="ModelMatch.MatchKind"/>.
/// </summary>
public enum AliasMatchType
{
    /// <summary>The identifier equals a catalog model ID or an exact alias (case-insensitive).</summary>
    Exact,

    /// <summary>The identifier starts with an alias pattern (longest pattern wins).</summary>
    Prefix,

    /// <summary>The identifier contains an alias pattern anywhere (longest pattern wins).</summary>
    Contains,
}
