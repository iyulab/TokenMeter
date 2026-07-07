namespace TokenMeter.Internal;

internal sealed class AliasRule
{
    public AliasMatchType MatchType { get; init; }
    public string Pattern { get; init; } = "";
    public ModelInfo Target { get; init; } = null!;
}
