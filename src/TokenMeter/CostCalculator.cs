using System.Collections.Concurrent;

namespace TokenMeter;

/// <summary>
/// Default implementation of cost calculator.
/// Uses built-in pricing data and supports custom pricing registration.
/// Thread-safe for concurrent reads and writes.
/// </summary>
public class CostCalculator : ICostCalculator
{
    private readonly ConcurrentDictionary<string, ModelPricing> _customPricing = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public decimal? CalculateCost(string modelId, int inputTokens, int outputTokens)
    {
        ArgumentNullException.ThrowIfNull(modelId);

        var pricing = GetPricing(modelId);
        if (pricing == null)
        {
            return null;
        }

        return pricing.CalculateCost(inputTokens, outputTokens);
    }

    /// <inheritdoc />
    public virtual ModelPricing? GetPricing(string modelId)
    {
        ArgumentNullException.ThrowIfNull(modelId);

        // Check custom pricing first
        if (_customPricing.TryGetValue(modelId, out var customPricing))
        {
            return customPricing;
        }

        // Fall back to built-in pricing
        return ModelPricingData.FindPricing(modelId);
    }

    /// <inheritdoc />
    public void RegisterPricing(ModelPricing pricing)
    {
        ArgumentNullException.ThrowIfNull(pricing);
        _customPricing[pricing.ModelId] = pricing;
    }

    /// <inheritdoc />
    public virtual IEnumerable<string> GetRegisteredModels()
    {
        return _customPricing.Keys.Concat(ModelPricingData.All.Keys).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a cost calculator with default built-in pricing.
    /// </summary>
    public static CostCalculator Default() => new();

    /// <summary>
    /// Creates a cost calculator with custom pricing only (no built-in pricing).
    /// </summary>
    public static CostCalculator CustomOnly() => new CustomOnlyCostCalculator();

    private sealed class CustomOnlyCostCalculator : CostCalculator
    {
        public override ModelPricing? GetPricing(string modelId)
        {
            ArgumentNullException.ThrowIfNull(modelId);

            return _customPricing.TryGetValue(modelId, out var pricing) ? pricing : null;
        }

        public override IEnumerable<string> GetRegisteredModels()
        {
            return _customPricing.Keys;
        }
    }
}
