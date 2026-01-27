namespace TokenMeter;

/// <summary>
/// Interface for calculating costs based on token usage.
/// </summary>
public interface ICostCalculator
{
    /// <summary>
    /// Calculates the cost for the given token usage.
    /// </summary>
    /// <param name="modelId">The model identifier</param>
    /// <param name="inputTokens">Number of input tokens</param>
    /// <param name="outputTokens">Number of output tokens</param>
    /// <returns>The calculated cost, or null if pricing is not available</returns>
    decimal? CalculateCost(string modelId, int inputTokens, int outputTokens);

    /// <summary>
    /// Gets the pricing information for a model.
    /// </summary>
    /// <param name="modelId">The model identifier</param>
    /// <returns>The pricing information, or null if not available</returns>
    ModelPricing? GetPricing(string modelId);

    /// <summary>
    /// Registers custom pricing for a model.
    /// </summary>
    /// <param name="pricing">The pricing information</param>
    void RegisterPricing(ModelPricing pricing);

    /// <summary>
    /// Gets all registered model IDs.
    /// </summary>
    IEnumerable<string> GetRegisteredModels();
}
