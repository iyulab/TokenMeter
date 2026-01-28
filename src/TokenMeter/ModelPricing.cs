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
    /// Provider name (e.g., "OpenAI", "Anthropic", "Google", "xAI", "Azure").
    /// </summary>
    public string? Provider { get; init; }

    /// <summary>
    /// Display name for the model.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Context window size in tokens (if known).
    /// </summary>
    public int? ContextWindow { get; init; }

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
/// Prices as of January 2026.
///
/// Sources:
/// - OpenAI: https://openai.com/api/pricing/
/// - Anthropic: https://docs.anthropic.com/en/docs/about-claude/pricing
/// - Google: https://ai.google.dev/gemini-api/docs/pricing
/// - Azure: https://azure.microsoft.com/en-us/pricing/details/cognitive-services/openai-service/
/// - xAI: https://docs.x.ai/docs/models
/// </summary>
public static class ModelPricingData
{
    /// <summary>
    /// Last updated date for pricing data.
    /// </summary>
    public static DateOnly LastUpdated { get; } = new(2026, 1, 28);

    /// <summary>
    /// Gets pricing for OpenAI models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> OpenAI { get; } = new Dictionary<string, ModelPricing>(StringComparer.OrdinalIgnoreCase)
    {
        // GPT-4o Series
        ["gpt-4o"] = new()
        {
            ModelId = "gpt-4o",
            InputPricePerMillion = 2.50m,
            OutputPricePerMillion = 10.00m,
            Provider = "OpenAI",
            DisplayName = "GPT-4o",
            ContextWindow = 128000
        },
        ["gpt-4o-mini"] = new()
        {
            ModelId = "gpt-4o-mini",
            InputPricePerMillion = 0.15m,
            OutputPricePerMillion = 0.60m,
            Provider = "OpenAI",
            DisplayName = "GPT-4o mini",
            ContextWindow = 128000
        },

        // GPT-4 Series (Legacy)
        ["gpt-4-turbo"] = new()
        {
            ModelId = "gpt-4-turbo",
            InputPricePerMillion = 10.00m,
            OutputPricePerMillion = 30.00m,
            Provider = "OpenAI",
            DisplayName = "GPT-4 Turbo",
            ContextWindow = 128000
        },
        ["gpt-4"] = new()
        {
            ModelId = "gpt-4",
            InputPricePerMillion = 30.00m,
            OutputPricePerMillion = 60.00m,
            Provider = "OpenAI",
            DisplayName = "GPT-4",
            ContextWindow = 8192
        },
        ["gpt-3.5-turbo"] = new()
        {
            ModelId = "gpt-3.5-turbo",
            InputPricePerMillion = 0.50m,
            OutputPricePerMillion = 1.50m,
            Provider = "OpenAI",
            DisplayName = "GPT-3.5 Turbo",
            ContextWindow = 16385
        },

        // o-Series (Reasoning Models)
        ["o1"] = new()
        {
            ModelId = "o1",
            InputPricePerMillion = 15.00m,
            OutputPricePerMillion = 60.00m,
            Provider = "OpenAI",
            DisplayName = "o1",
            ContextWindow = 200000
        },
        ["o1-mini"] = new()
        {
            ModelId = "o1-mini",
            InputPricePerMillion = 3.00m,
            OutputPricePerMillion = 12.00m,
            Provider = "OpenAI",
            DisplayName = "o1-mini",
            ContextWindow = 128000
        },
        ["o3"] = new()
        {
            ModelId = "o3",
            InputPricePerMillion = 2.00m,
            OutputPricePerMillion = 8.00m,
            Provider = "OpenAI",
            DisplayName = "o3",
            ContextWindow = 200000
        },
        ["o3-mini"] = new()
        {
            ModelId = "o3-mini",
            InputPricePerMillion = 1.10m,
            OutputPricePerMillion = 4.40m,
            Provider = "OpenAI",
            DisplayName = "o3-mini",
            ContextWindow = 200000
        }
    };

    /// <summary>
    /// Gets pricing for Anthropic models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> Anthropic { get; } = new Dictionary<string, ModelPricing>(StringComparer.OrdinalIgnoreCase)
    {
        // Claude 4.5 Series (Latest)
        ["claude-4-5-opus"] = new()
        {
            ModelId = "claude-4-5-opus",
            InputPricePerMillion = 5.00m,
            OutputPricePerMillion = 25.00m,
            Provider = "Anthropic",
            DisplayName = "Claude 4.5 Opus",
            ContextWindow = 200000
        },
        ["claude-4-5-sonnet"] = new()
        {
            ModelId = "claude-4-5-sonnet",
            InputPricePerMillion = 3.00m,
            OutputPricePerMillion = 15.00m,
            Provider = "Anthropic",
            DisplayName = "Claude 4.5 Sonnet",
            ContextWindow = 200000
        },
        ["claude-4-5-haiku"] = new()
        {
            ModelId = "claude-4-5-haiku",
            InputPricePerMillion = 1.00m,
            OutputPricePerMillion = 5.00m,
            Provider = "Anthropic",
            DisplayName = "Claude 4.5 Haiku",
            ContextWindow = 200000
        },

        // Claude 3.5 Series
        ["claude-3-5-sonnet-20241022"] = new()
        {
            ModelId = "claude-3-5-sonnet-20241022",
            InputPricePerMillion = 3.00m,
            OutputPricePerMillion = 15.00m,
            Provider = "Anthropic",
            DisplayName = "Claude 3.5 Sonnet",
            ContextWindow = 200000
        },
        ["claude-3-5-haiku-20241022"] = new()
        {
            ModelId = "claude-3-5-haiku-20241022",
            InputPricePerMillion = 0.80m,
            OutputPricePerMillion = 4.00m,
            Provider = "Anthropic",
            DisplayName = "Claude 3.5 Haiku",
            ContextWindow = 200000
        },

        // Claude 3 Series (Legacy)
        ["claude-3-opus-20240229"] = new()
        {
            ModelId = "claude-3-opus-20240229",
            InputPricePerMillion = 15.00m,
            OutputPricePerMillion = 75.00m,
            Provider = "Anthropic",
            DisplayName = "Claude 3 Opus",
            ContextWindow = 200000
        },
        ["claude-3-sonnet-20240229"] = new()
        {
            ModelId = "claude-3-sonnet-20240229",
            InputPricePerMillion = 3.00m,
            OutputPricePerMillion = 15.00m,
            Provider = "Anthropic",
            DisplayName = "Claude 3 Sonnet",
            ContextWindow = 200000
        },
        ["claude-3-haiku-20240307"] = new()
        {
            ModelId = "claude-3-haiku-20240307",
            InputPricePerMillion = 0.25m,
            OutputPricePerMillion = 1.25m,
            Provider = "Anthropic",
            DisplayName = "Claude 3 Haiku",
            ContextWindow = 200000
        }
    };

    /// <summary>
    /// Gets pricing for Google Gemini models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> Google { get; } = new Dictionary<string, ModelPricing>(StringComparer.OrdinalIgnoreCase)
    {
        // Gemini 2.5 Series
        ["gemini-2.5-pro"] = new()
        {
            ModelId = "gemini-2.5-pro",
            InputPricePerMillion = 1.25m,
            OutputPricePerMillion = 10.00m,
            Provider = "Google",
            DisplayName = "Gemini 2.5 Pro",
            ContextWindow = 1000000
        },
        ["gemini-2.5-flash"] = new()
        {
            ModelId = "gemini-2.5-flash",
            InputPricePerMillion = 0.15m,
            OutputPricePerMillion = 0.60m,
            Provider = "Google",
            DisplayName = "Gemini 2.5 Flash",
            ContextWindow = 1000000
        },

        // Gemini 2.0 Series
        ["gemini-2.0-flash"] = new()
        {
            ModelId = "gemini-2.0-flash",
            InputPricePerMillion = 0.10m,
            OutputPricePerMillion = 0.40m,
            Provider = "Google",
            DisplayName = "Gemini 2.0 Flash",
            ContextWindow = 1000000
        },
        ["gemini-2.0-flash-lite"] = new()
        {
            ModelId = "gemini-2.0-flash-lite",
            InputPricePerMillion = 0.075m,
            OutputPricePerMillion = 0.30m,
            Provider = "Google",
            DisplayName = "Gemini 2.0 Flash-Lite",
            ContextWindow = 1000000
        },

        // Gemini 1.5 Series (Legacy)
        ["gemini-1.5-pro"] = new()
        {
            ModelId = "gemini-1.5-pro",
            InputPricePerMillion = 1.25m,
            OutputPricePerMillion = 5.00m,
            Provider = "Google",
            DisplayName = "Gemini 1.5 Pro",
            ContextWindow = 2000000
        },
        ["gemini-1.5-flash"] = new()
        {
            ModelId = "gemini-1.5-flash",
            InputPricePerMillion = 0.075m,
            OutputPricePerMillion = 0.30m,
            Provider = "Google",
            DisplayName = "Gemini 1.5 Flash",
            ContextWindow = 1000000
        }
    };

    /// <summary>
    /// Gets pricing for xAI Grok models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> XAI { get; } = new Dictionary<string, ModelPricing>(StringComparer.OrdinalIgnoreCase)
    {
        // Grok 4 Series
        ["grok-4"] = new()
        {
            ModelId = "grok-4",
            InputPricePerMillion = 3.00m,
            OutputPricePerMillion = 15.00m,
            Provider = "xAI",
            DisplayName = "Grok 4",
            ContextWindow = 131072
        },
        ["grok-4-fast"] = new()
        {
            ModelId = "grok-4-fast",
            InputPricePerMillion = 0.20m,
            OutputPricePerMillion = 0.50m,
            Provider = "xAI",
            DisplayName = "Grok 4 Fast",
            ContextWindow = 131072
        },

        // Grok 3 Series
        ["grok-3"] = new()
        {
            ModelId = "grok-3",
            InputPricePerMillion = 3.00m,
            OutputPricePerMillion = 15.00m,
            Provider = "xAI",
            DisplayName = "Grok 3",
            ContextWindow = 131072
        },
        ["grok-3-mini"] = new()
        {
            ModelId = "grok-3-mini",
            InputPricePerMillion = 0.30m,
            OutputPricePerMillion = 0.50m,
            Provider = "xAI",
            DisplayName = "Grok 3 Mini",
            ContextWindow = 131072
        }
    };

    /// <summary>
    /// Gets pricing for Azure OpenAI models.
    /// Note: Azure pricing is generally the same as OpenAI for equivalent models.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> Azure { get; } = new Dictionary<string, ModelPricing>(StringComparer.OrdinalIgnoreCase)
    {
        ["azure-gpt-4o"] = new()
        {
            ModelId = "azure-gpt-4o",
            InputPricePerMillion = 2.50m,
            OutputPricePerMillion = 10.00m,
            Provider = "Azure",
            DisplayName = "Azure GPT-4o",
            ContextWindow = 128000
        },
        ["azure-gpt-4o-mini"] = new()
        {
            ModelId = "azure-gpt-4o-mini",
            InputPricePerMillion = 0.15m,
            OutputPricePerMillion = 0.60m,
            Provider = "Azure",
            DisplayName = "Azure GPT-4o mini",
            ContextWindow = 128000
        },
        ["azure-gpt-4-turbo"] = new()
        {
            ModelId = "azure-gpt-4-turbo",
            InputPricePerMillion = 10.00m,
            OutputPricePerMillion = 30.00m,
            Provider = "Azure",
            DisplayName = "Azure GPT-4 Turbo",
            ContextWindow = 128000
        },
        ["azure-gpt-4"] = new()
        {
            ModelId = "azure-gpt-4",
            InputPricePerMillion = 30.00m,
            OutputPricePerMillion = 60.00m,
            Provider = "Azure",
            DisplayName = "Azure GPT-4",
            ContextWindow = 8192
        },
        ["azure-gpt-35-turbo"] = new()
        {
            ModelId = "azure-gpt-35-turbo",
            InputPricePerMillion = 0.50m,
            OutputPricePerMillion = 1.50m,
            Provider = "Azure",
            DisplayName = "Azure GPT-3.5 Turbo",
            ContextWindow = 16385
        }
    };

    /// <summary>
    /// Gets all built-in pricing data.
    /// </summary>
    public static IReadOnlyDictionary<string, ModelPricing> All { get; } =
        OpenAI
            .Concat(Anthropic)
            .Concat(Google)
            .Concat(XAI)
            .Concat(Azure)
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets all providers with their pricing dictionaries.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, ModelPricing>> ByProvider { get; } =
        new Dictionary<string, IReadOnlyDictionary<string, ModelPricing>>(StringComparer.OrdinalIgnoreCase)
        {
            ["OpenAI"] = OpenAI,
            ["Anthropic"] = Anthropic,
            ["Google"] = Google,
            ["xAI"] = XAI,
            ["Azure"] = Azure
        };

    /// <summary>
    /// Tries to find pricing for a model by ID or alias.
    /// </summary>
    public static ModelPricing? FindPricing(string modelId)
    {
        if (All.TryGetValue(modelId, out var pricing))
        {
            return pricing;
        }

        // Try to match by prefix/pattern
        var normalized = modelId.ToLowerInvariant();

        // OpenAI patterns
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

        if (normalized.StartsWith("o1-mini", StringComparison.Ordinal))
        {
            return OpenAI["o1-mini"];
        }

        if (normalized.StartsWith("o1", StringComparison.Ordinal))
        {
            return OpenAI["o1"];
        }

        if (normalized.StartsWith("o3-mini", StringComparison.Ordinal))
        {
            return OpenAI["o3-mini"];
        }

        if (normalized.StartsWith("o3", StringComparison.Ordinal))
        {
            return OpenAI["o3"];
        }

        // Anthropic patterns
        if (normalized.Contains("claude-4-5-opus", StringComparison.Ordinal) || normalized.Contains("claude-4.5-opus", StringComparison.Ordinal))
        {
            return Anthropic["claude-4-5-opus"];
        }

        if (normalized.Contains("claude-4-5-sonnet", StringComparison.Ordinal) || normalized.Contains("claude-4.5-sonnet", StringComparison.Ordinal))
        {
            return Anthropic["claude-4-5-sonnet"];
        }

        if (normalized.Contains("claude-4-5-haiku", StringComparison.Ordinal) || normalized.Contains("claude-4.5-haiku", StringComparison.Ordinal))
        {
            return Anthropic["claude-4-5-haiku"];
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

        // Google patterns
        if (normalized.Contains("gemini-2.5-pro", StringComparison.Ordinal) || normalized.Contains("gemini-2-5-pro", StringComparison.Ordinal))
        {
            return Google["gemini-2.5-pro"];
        }

        if (normalized.Contains("gemini-2.5-flash", StringComparison.Ordinal) || normalized.Contains("gemini-2-5-flash", StringComparison.Ordinal))
        {
            return Google["gemini-2.5-flash"];
        }

        if (normalized.Contains("gemini-2.0-flash-lite", StringComparison.Ordinal) || normalized.Contains("gemini-2-0-flash-lite", StringComparison.Ordinal))
        {
            return Google["gemini-2.0-flash-lite"];
        }

        if (normalized.Contains("gemini-2.0-flash", StringComparison.Ordinal) || normalized.Contains("gemini-2-0-flash", StringComparison.Ordinal))
        {
            return Google["gemini-2.0-flash"];
        }

        if (normalized.Contains("gemini-1.5-pro", StringComparison.Ordinal) || normalized.Contains("gemini-1-5-pro", StringComparison.Ordinal))
        {
            return Google["gemini-1.5-pro"];
        }

        if (normalized.Contains("gemini-1.5-flash", StringComparison.Ordinal) || normalized.Contains("gemini-1-5-flash", StringComparison.Ordinal))
        {
            return Google["gemini-1.5-flash"];
        }

        // xAI/Grok patterns
        if (normalized.Contains("grok-4-fast", StringComparison.Ordinal) || normalized.Contains("grok-4.1", StringComparison.Ordinal))
        {
            return XAI["grok-4-fast"];
        }

        if (normalized.Contains("grok-4", StringComparison.Ordinal))
        {
            return XAI["grok-4"];
        }

        if (normalized.Contains("grok-3-mini", StringComparison.Ordinal))
        {
            return XAI["grok-3-mini"];
        }

        if (normalized.Contains("grok-3", StringComparison.Ordinal))
        {
            return XAI["grok-3"];
        }

        return null;
    }

    /// <summary>
    /// Gets all models for a specific provider.
    /// </summary>
    public static IEnumerable<ModelPricing> GetByProvider(string providerName)
    {
        if (ByProvider.TryGetValue(providerName, out var provider))
        {
            return provider.Values;
        }

        return [];
    }

    /// <summary>
    /// Gets all available provider names.
    /// </summary>
    public static IEnumerable<string> GetProviderNames() => ByProvider.Keys;
}
