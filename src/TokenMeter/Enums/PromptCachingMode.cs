namespace TokenMeter;

/// <summary>
/// How the provider implements prompt caching.
/// </summary>
public enum PromptCachingMode
{
    /// <summary>Prompt caching is not supported.</summary>
    None = 0,

    /// <summary>
    /// Developer explicitly marks cache breakpoints via API parameters
    /// (e.g., Anthropic <c>cache_control</c> on content blocks).
    /// </summary>
    Explicit,

    /// <summary>
    /// Provider automatically detects and caches repeated prefix content
    /// (e.g., OpenAI automatic caching for prompts above a token threshold).
    /// </summary>
    Automatic,
}
