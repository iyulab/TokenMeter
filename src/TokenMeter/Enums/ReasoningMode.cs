namespace TokenMeter;

/// <summary>
/// Indicates whether and how a model supports extended reasoning/thinking.
/// </summary>
public enum ReasoningMode
{
    /// <summary>No reasoning support.</summary>
    None = 0,

    /// <summary>
    /// Reasoning can be enabled per-request (e.g., Claude extended thinking,
    /// Gemini thinking mode).
    /// </summary>
    Optional,

    /// <summary>
    /// Model always reasons before responding (e.g., OpenAI o1, o3; DeepSeek R1).
    /// </summary>
    Always,
}
