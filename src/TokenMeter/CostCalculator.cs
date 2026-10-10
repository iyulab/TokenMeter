using System.Collections.Concurrent;

namespace TokenMeter;

/// <summary>
/// Default implementation of <see cref="ICostCalculator"/>.
/// Uses <see cref="ModelCatalog"/> built-in data as the primary source,
/// with custom registrations taking precedence.
/// Thread-safe.
/// </summary>
public sealed class CostCalculator : ICostCalculator
{
    private readonly ConcurrentDictionary<string, ModelInfo> _custom =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly bool _useBuiltIn;
    private readonly AliasMatchType _maxFuzziness;

    private CostCalculator(bool useBuiltIn, AliasMatchType maxFuzziness)
    {
        _useBuiltIn = useBuiltIn;
        _maxFuzziness = maxFuzziness;
    }

    /// <summary>Returns a calculator backed by the full built-in <see cref="ModelCatalog"/>, matched fully fuzzily.</summary>
    public static CostCalculator Default() => new(useBuiltIn: true, AliasMatchType.Contains);

    /// <summary>
    /// Returns a calculator backed by the built-in <see cref="ModelCatalog"/>, matching an unregistered id no looser than
    /// <paramref name="maxFuzziness"/>. With <see cref="AliasMatchType.Exact"/>, a self-hosted model whose name embeds a
    /// public one (<c>qwen3-8b-local</c>) is unknown until registered (<see cref="RegisterModel"/>) instead of being priced
    /// as the public model. Registered models always win, matched by exact id.
    /// </summary>
    public static CostCalculator Default(AliasMatchType maxFuzziness) => new(useBuiltIn: true, maxFuzziness);

    /// <summary>Returns a calculator that uses only explicitly registered models (no built-in catalog).</summary>
    public static CostCalculator CustomOnly() => new(useBuiltIn: false, AliasMatchType.Exact);

    /// <inheritdoc/>
    public decimal? CalculateCost(string modelId, int inputTokens, int outputTokens)
    {
        // Validate arguments before model lookup so a negative token count is
        // rejected consistently, whether or not the model resolves.
        ArgumentOutOfRangeException.ThrowIfNegative(inputTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(outputTokens);
        return GetModel(modelId)?.CalculateCost(inputTokens, outputTokens);
    }

    /// <inheritdoc/>
    public decimal? CalculateCost(
        string modelId,
        int inputTokens, int outputTokens,
        int cacheReadTokens, int cacheWriteTokens)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(inputTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(outputTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(cacheReadTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(cacheWriteTokens);
        return GetModel(modelId)?.CalculateCost(inputTokens, outputTokens, cacheReadTokens, cacheWriteTokens);
    }

    /// <inheritdoc/>
    public decimal? CalculateCost(string modelId, int inputTokens, int outputTokens, PricingTierContext context)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(inputTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(outputTokens);
        return GetModel(modelId)?.CalculateCost(inputTokens, outputTokens, context);
    }

    /// <inheritdoc/>
    public decimal? CalculateCost(string modelId, TokenCounts usage, CostContext context = default)
    {
        ArgumentNullException.ThrowIfNull(usage);
        return GetModel(modelId)?.CalculateCost(usage, context);
    }

    /// <inheritdoc/>
    public ModelInfo? GetModel(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId)) return null;
        if (_custom.TryGetValue(modelId, out var custom)) return custom;
        return _useBuiltIn ? ModelCatalog.FindModel(modelId, _maxFuzziness) : null;
    }

    /// <inheritdoc/>
    public void RegisterModel(ModelInfo model) => _custom[model.ModelId] = model;

    /// <inheritdoc/>
    public IEnumerable<string> GetRegisteredModels()
    {
        if (!_useBuiltIn) return _custom.Keys;
        return ModelCatalog.All.Keys.Union(_custom.Keys, StringComparer.OrdinalIgnoreCase);
    }
}
