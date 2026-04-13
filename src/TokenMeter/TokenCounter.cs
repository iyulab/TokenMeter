using Microsoft.ML.Tokenizers;

namespace TokenMeter;

/// <summary>
/// Token counter implementation using Microsoft.ML.Tokenizers.
/// Supports various models including GPT-4, GPT-3.5, Claude, etc.
/// </summary>
public class TokenCounter : ITokenCounter, IDisposable
{
    private readonly TiktokenTokenizer _tokenizer;

    /// <inheritdoc />
    public string ModelName { get; }

    /// <summary>
    /// Creates a token counter for the specified model.
    /// </summary>
    /// <param name="modelName">Model name (e.g., "gpt-4", "gpt-3.5-turbo", "cl100k_base")</param>
    public TokenCounter(string modelName = "gpt-4")
    {
        ModelName = modelName;
        _tokenizer = CreateTokenizer(modelName);
    }

    /// <summary>
    /// Creates a token counter with a custom tokenizer.
    /// </summary>
    public TokenCounter(TiktokenTokenizer tokenizer, string modelName)
    {
        _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
        ModelName = modelName;
    }

    /// <inheritdoc />
    public int CountTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var encoded = _tokenizer.EncodeToIds(text);
        return encoded.Count;
    }

    /// <inheritdoc />
    public int CountTokens(IEnumerable<string> texts)
    {
        if (texts is null) return 0;
        return texts.Sum(CountTokens);
    }

    /// <inheritdoc />
    public bool SupportsModel(string modelId)
    {
        if (string.IsNullOrEmpty(modelId))
        {
            return false;
        }

        var lower = modelId.ToLowerInvariant();

        // GPT models — native support via tiktoken
        if (lower.StartsWith("gpt-", StringComparison.Ordinal) ||
            lower.StartsWith("text-davinci", StringComparison.Ordinal) ||
            lower.StartsWith("code-", StringComparison.Ordinal))
        {
            return true;
        }

        // Claude, Gemini, etc. — approximate (cl100k_base fallback)
        return false;
    }

    /// <inheritdoc />
    public bool IsApproximate(string modelId)
    {
        if (string.IsNullOrEmpty(modelId))
        {
            return false;
        }

        var lower = modelId.ToLowerInvariant();

        // GPT models have native tiktoken support — not approximate
        if (lower.StartsWith("gpt-", StringComparison.Ordinal) ||
            lower.StartsWith("text-davinci", StringComparison.Ordinal) ||
            lower.StartsWith("code-", StringComparison.Ordinal))
        {
            return false;
        }

        // All other models (Claude, Gemini, etc.) use cl100k_base as fallback
        return true;
    }

    private static TiktokenTokenizer CreateTokenizer(string modelName)
    {
        // Map model names to encoding names
        var encoding = modelName.ToLowerInvariant() switch
        {
            // GPT-4 and GPT-3.5-turbo use cl100k_base
            "gpt-4" or "gpt-4-turbo" or "gpt-4o" or "gpt-4o-mini" => "cl100k_base",
            "gpt-3.5-turbo" or "gpt-35-turbo" => "cl100k_base",

            // GPT-3 models use p50k_base
            "text-davinci-003" or "text-davinci-002" => "p50k_base",

            // Codex models
            "code-davinci-002" or "code-cushman-001" => "p50k_base",

            // Claude models - use cl100k_base as approximation
            var m when m.StartsWith("claude", StringComparison.Ordinal) => "cl100k_base",

            // Default to cl100k_base (most common for modern models)
            _ => "cl100k_base"
        };

        return TiktokenTokenizer.CreateForEncoding(encoding);
    }

    /// <summary>
    /// Creates a token counter for GPT-4 models.
    /// </summary>
    public static TokenCounter ForGpt4() => new("gpt-4");

    /// <summary>
    /// Creates a token counter for GPT-3.5-turbo models.
    /// </summary>
    public static TokenCounter ForGpt35Turbo() => new("gpt-3.5-turbo");

    /// <summary>
    /// Creates a token counter using cl100k_base encoding (default for most modern models).
    /// </summary>
    public static TokenCounter Default() => new("cl100k_base");

    /// <summary>
    /// Releases resources used by the token counter.
    /// Currently a no-op because <see cref="TiktokenTokenizer"/> does not implement
    /// <see cref="IDisposable"/>, but included so callers can use a <c>using</c> pattern
    /// that will automatically benefit when the underlying tokenizer becomes disposable.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
