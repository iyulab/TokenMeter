namespace TokenMeter;

/// <summary>
/// Pricing information for a model.
/// Prices are per 1 million tokens.
/// </summary>
public record ModelPricing
{
    /// <summary>
    /// Model identifier.
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Cost per 1 million input tokens (USD).
    /// </summary>
    public required decimal InputPricePerMillion { get; init; }

    /// <summary>
    /// Cost per 1 million output tokens (USD).
    /// </summary>
    public required decimal OutputPricePerMillion { get; init; }

    /// <summary>
    /// Provider name (e.g., "OpenAI", "Anthropic", "Azure").
    /// </summary>
    public string? Provider { get; init; }

    /// <summary>
    /// Display name for the model.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Calculates the cost for the given token counts.
    /// </summary>
    public decimal CalculateCost(int inputTokens, int outputTokens)
    {
        var inputCost = (inputTokens / 1_000_000m) * InputPricePerMillion;
        var outputCost = (outputTokens / 1_000_000m) * OutputPricePerMillion;
        return inputCost + outputCost;
    }
}

/// <summary>
/// Built-in pricing data for common models.
/// Prices as of January 2025.
/// </summary>
public static class ModelPricingData
{
    /// <summary>
    /// Gets pricing for OpenAI models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> OpenAI { get; } = new Dictionary<string, ModelPricing>(StringComparer.OrdinalIgnoreCase)
    {
        ["gpt-4o"] = new()
        {
            ModelId = "gpt-4o",
            InputPricePerMillion = 2.50m,
            OutputPricePerMillion = 10.00m,
            Provider = "OpenAI",
            DisplayName = "GPT-4o"
        },
        ["gpt-4o-mini"] = new()
        {
            ModelId = "gpt-4o-mini",
            InputPricePerMillion = 0.15m,
            OutputPricePerMillion = 0.60m,
            Provider = "OpenAI",
            DisplayName = "GPT-4o mini"
        },
        ["gpt-4-turbo"] = new()
        {
            ModelId = "gpt-4-turbo",
            InputPricePerMillion = 10.00m,
            OutputPricePerMillion = 30.00m,
            Provider = "OpenAI",
            DisplayName = "GPT-4 Turbo"
        },
        ["gpt-4"] = new()
        {
            ModelId = "gpt-4",
            InputPricePerMillion = 30.00m,
            OutputPricePerMillion = 60.00m,
            Provider = "OpenAI",
            DisplayName = "GPT-4"
        },
        ["gpt-3.5-turbo"] = new()
        {
            ModelId = "gpt-3.5-turbo",
            InputPricePerMillion = 0.50m,
            OutputPricePerMillion = 1.50m,
            Provider = "OpenAI",
            DisplayName = "GPT-3.5 Turbo"
        },
        ["o1"] = new()
        {
            ModelId = "o1",
            InputPricePerMillion = 15.00m,
            OutputPricePerMillion = 60.00m,
            Provider = "OpenAI",
            DisplayName = "o1"
        },
        ["o1-mini"] = new()
        {
            ModelId = "o1-mini",
            InputPricePerMillion = 3.00m,
            OutputPricePerMillion = 12.00m,
            Provider = "OpenAI",
            DisplayName = "o1-mini"
        }
    };

    /// <summary>
    /// Gets pricing for Anthropic models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> Anthropic { get; } = new Dictionary<string, ModelPricing>(StringComparer.OrdinalIgnoreCase)
    {
        ["claude-3-5-sonnet-20241022"] = new()
        {
            ModelId = "claude-3-5-sonnet-20241022",
            InputPricePerMillion = 3.00m,
            OutputPricePerMillion = 15.00m,
            Provider = "Anthropic",
            DisplayName = "Claude 3.5 Sonnet"
        },
        ["claude-3-5-haiku-20241022"] = new()
        {
            ModelId = "claude-3-5-haiku-20241022",
            InputPricePerMillion = 0.80m,
            OutputPricePerMillion = 4.00m,
            Provider = "Anthropic",
            DisplayName = "Claude 3.5 Haiku"
        },
        ["claude-3-opus-20240229"] = new()
        {
            ModelId = "claude-3-opus-20240229",
            InputPricePerMillion = 15.00m,
            OutputPricePerMillion = 75.00m,
            Provider = "Anthropic",
            DisplayName = "Claude 3 Opus"
        },
        ["claude-3-sonnet-20240229"] = new()
        {
            ModelId = "claude-3-sonnet-20240229",
            InputPricePerMillion = 3.00m,
            OutputPricePerMillion = 15.00m,
            Provider = "Anthropic",
            DisplayName = "Claude 3 Sonnet"
        },
        ["claude-3-haiku-20240307"] = new()
        {
            ModelId = "claude-3-haiku-20240307",
            InputPricePerMillion = 0.25m,
            OutputPricePerMillion = 1.25m,
            Provider = "Anthropic",
            DisplayName = "Claude 3 Haiku"
        }
    };

    /// <summary>
    /// Gets all built-in pricing data.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> All { get; } =
        OpenAI.Concat(Anthropic).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tries to find pricing for a model by ID or alias.
    /// </summary>
    public static ModelPricing? FindPricing(string modelId)
    {
        if (All.TryGetValue(modelId, out var pricing))
        {
            return pricing;
        }

        // Try to match by prefix
        var normalized = modelId.ToLowerInvariant();

        if (normalized.StartsWith("gpt-4o-mini", StringComparison.Ordinal))
        {
            return OpenAI["gpt-4o-mini"];
        }

        if (normalized.StartsWith("gpt-4o", StringComparison.Ordinal))
        {
            return OpenAI["gpt-4o"];
        }

        if (normalized.StartsWith("gpt-4-turbo", StringComparison.Ordinal))
        {
            return OpenAI["gpt-4-turbo"];
        }

        if (normalized.StartsWith("gpt-4", StringComparison.Ordinal))
        {
            return OpenAI["gpt-4"];
        }

        if (normalized.StartsWith("gpt-3.5", StringComparison.Ordinal) || normalized.StartsWith("gpt-35", StringComparison.Ordinal))
        {
            return OpenAI["gpt-3.5-turbo"];
        }

        if (normalized.Contains("claude-3-5-sonnet", StringComparison.Ordinal) || normalized.Contains("claude-3.5-sonnet", StringComparison.Ordinal))
        {
            return Anthropic["claude-3-5-sonnet-20241022"];
        }

        if (normalized.Contains("claude-3-5-haiku", StringComparison.Ordinal) || normalized.Contains("claude-3.5-haiku", StringComparison.Ordinal))
        {
            return Anthropic["claude-3-5-haiku-20241022"];
        }

        if (normalized.Contains("claude-3-opus", StringComparison.Ordinal))
        {
            return Anthropic["claude-3-opus-20240229"];
        }

        if (normalized.Contains("claude-3-sonnet", StringComparison.Ordinal))
        {
            return Anthropic["claude-3-sonnet-20240229"];
        }

        if (normalized.Contains("claude-3-haiku", StringComparison.Ordinal))
        {
            return Anthropic["claude-3-haiku-20240307"];
        }

        return null;
    }
}
