namespace TokenMeter.Abstractions;

/// <summary>
/// Shared interface for counting tokens in text.
/// Provides a common contract for token counting across packages
/// (TokenMeter, MemoryIndexer, IndexThinking, etc.).
/// </summary>
public interface ITokenCounter
{
    /// <summary>
    /// Counts the number of tokens in the given text.
    /// </summary>
    /// <param name="text">The text to count tokens for.</param>
    /// <returns>The number of tokens.</returns>
    int Count(string text);

    /// <summary>
    /// Counts the total number of tokens across the given texts.
    /// </summary>
    /// <param name="texts">The texts to count tokens for.</param>
    /// <returns>The total number of tokens.</returns>
    int Count(IEnumerable<string> texts);

    /// <summary>
    /// Indicates whether this counter supports accurate counting for the specified model.
    /// </summary>
    /// <param name="modelId">The model identifier (e.g., "gpt-4o", "claude-3.5-sonnet").</param>
    /// <returns>True if this counter provides accurate counts for the model; otherwise, false.</returns>
    bool SupportsModel(string modelId);

    /// <summary>
    /// Indicates whether token counting for the specified model uses an approximate
    /// fallback tokenizer rather than the model's native tokenizer.
    /// </summary>
    /// <param name="modelId">The model identifier (e.g., "claude-3.5-sonnet", "gpt-4o").</param>
    /// <returns>
    /// <c>true</c> if the counter can produce token counts but uses a fallback encoding
    /// (e.g., cl100k_base for non-OpenAI models); <c>false</c> if counting is exact or not supported.
    /// </returns>
    bool IsApproximate(string modelId) => false;
}
