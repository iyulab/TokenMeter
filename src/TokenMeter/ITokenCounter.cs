namespace TokenMeter;

/// <summary>
/// Interface for counting tokens in text.
/// Extends the shared <see cref="Abstractions.ITokenCounter"/> with TokenMeter-specific members.
/// </summary>
public interface ITokenCounter : Abstractions.ITokenCounter
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

    // ── Bridges to Abstractions.ITokenCounter ──
    // Default implementations ensure all TokenMeter.ITokenCounter implementers
    // automatically satisfy the shared interface without manual wiring.

    /// <summary>
    /// Bridges <see cref="Abstractions.ITokenCounter.Count(string)"/> to <see cref="CountTokens(string)"/>.
    /// </summary>
    int Abstractions.ITokenCounter.Count(string text) => CountTokens(text);

    /// <summary>
    /// Bridges <see cref="Abstractions.ITokenCounter.Count(IEnumerable{string})"/> to <see cref="CountTokens(IEnumerable{string})"/>.
    /// </summary>
    int Abstractions.ITokenCounter.Count(IEnumerable<string> texts) => CountTokens(texts);

    /// <summary>
    /// Bridges <see cref="Abstractions.ITokenCounter.IsApproximate(string)"/> default implementation
    /// to the local <see cref="IsApproximate(string)"/> method, ensuring correct dispatch
    /// when accessed via an <see cref="Abstractions.ITokenCounter"/> typed variable.
    /// </summary>
    bool Abstractions.ITokenCounter.IsApproximate(string modelId) => IsApproximate(modelId);

    /// <summary>
    /// Indicates whether token counting for the specified model uses an approximate
    /// fallback tokenizer rather than the model's native tokenizer.
    /// </summary>
    new bool IsApproximate(string modelId);
}
