namespace TokenMeter;

/// <summary>
/// Calculates LLM API costs using model pricing information.
/// Supports built-in pricing from <see cref="ModelCatalog"/> and custom model registration.
/// </summary>
public interface ICostCalculator
{
    /// <summary>
    /// Calculates cost for standard input/output token usage.
    /// Returns <c>null</c> if pricing is unavailable for the model.
    /// </summary>
    decimal? CalculateCost(string modelId, int inputTokens, int outputTokens);

    /// <summary>
    /// Calculates cost including prompt cache tokens.
    /// Cache tokens without a specific price fall back to the model's input price.
    /// Returns <c>null</c> if pricing is unavailable for the model.
    /// </summary>
    decimal? CalculateCost(
        string modelId,
        int inputTokens, int outputTokens,
        int cacheReadTokens, int cacheWriteTokens);

    /// <summary>
    /// Retrieves the model metadata used for cost calculation.
    /// Applies alias matching. Returns <c>null</c> if not found.
    /// </summary>
    ModelInfo? GetModel(string modelId);

    /// <summary>
    /// Registers a custom model, overriding any built-in entry with the same ID.
    /// </summary>
    void RegisterModel(ModelInfo model);

    /// <summary>All model IDs known to this calculator (built-in + custom).</summary>
    IEnumerable<string> GetRegisteredModels();
}
