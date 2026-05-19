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

    private CostCalculator(bool useBuiltIn) => _useBuiltIn = useBuiltIn;

    /// <summary>Returns a calculator backed by the full built-in <see cref="ModelCatalog"/>.</summary>
    public static CostCalculator Default() => new(useBuiltIn: true);

    /// <summary>Returns a calculator that uses only explicitly registered models (no built-in catalog).</summary>
    public static CostCalculator CustomOnly() => new(useBuiltIn: false);

    /// <inheritdoc/>
    public decimal? CalculateCost(string modelId, int inputTokens, int outputTokens)
        => GetModel(modelId)?.CalculateCost(inputTokens, outputTokens);

    /// <inheritdoc/>
    public decimal? CalculateCost(
        string modelId,
        int inputTokens, int outputTokens,
        int cacheReadTokens, int cacheWriteTokens)
        => GetModel(modelId)?.CalculateCost(inputTokens, outputTokens, cacheReadTokens, cacheWriteTokens);

    /// <inheritdoc/>
    public ModelInfo? GetModel(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId)) return null;
        if (_custom.TryGetValue(modelId, out var custom)) return custom;
        return _useBuiltIn ? ModelCatalog.FindModel(modelId) : null;
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
