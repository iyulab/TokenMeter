namespace TokenMeter.Internal;

internal enum AliasMatchType { Exact, Prefix, Contains }

internal sealed class AliasRule
{
    public AliasMatchType MatchType { get; init; }
    public string Pattern { get; init; } = "";
    public ModelInfo Target { get; init; } = null!;
}
