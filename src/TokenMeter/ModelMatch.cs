namespace TokenMeter;

/// <summary>
/// A catalog lookup result carrying the pass that produced the match, so consumers can
/// apply confidence-based handling (e.g. trust <see cref="AliasMatchType.Exact"/> matches,
/// fall back to manual configuration for <see cref="AliasMatchType.Contains"/> inferences).
/// </summary>
/// <param name="Model">The matched catalog entry.</param>
/// <param name="MatchKind">
/// The pass that matched: <see cref="AliasMatchType.Exact"/> (catalog ID or exact alias),
/// <see cref="AliasMatchType.Prefix"/>, or <see cref="AliasMatchType.Contains"/>.
/// </param>
public sealed record ModelMatch(ModelInfo Model, AliasMatchType MatchKind);
