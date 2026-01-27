namespace TokenMeter;

/// <summary>
/// Interface for counting tokens in text.
/// </summary>
public interface ITokenCounter
{
    /// <summary>
    /// Counts the number of tokens in the given text.
    /// </summary>
    /// <param name="text">The text to count tokens for</param>
    /// <returns>The number of tokens</returns>
    int CountTokens(string text);

    /// <summary>
    /// Counts the number of tokens in the given texts.
    /// </summary>
    /// <param name="texts">The texts to count tokens for</param>
    /// <returns>The total number of tokens</returns>
    int CountTokens(IEnumerable<string> texts);

    /// <summary>
    /// Gets the model/encoding name used for tokenization.
    /// </summary>
    string ModelName { get; }
}
