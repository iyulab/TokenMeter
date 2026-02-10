using System.Text.Json.Serialization;

namespace TokenMeter;

internal sealed class ProviderPricingJson
{
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "";

    [JsonPropertyName("models")]
    public List<ModelPricingJson> Models { get; set; } = [];
}

internal sealed class ModelPricingJson
{
    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = "";

    [JsonPropertyName("inputPricePerMillion")]
    public decimal InputPricePerMillion { get; set; }

    [JsonPropertyName("outputPricePerMillion")]
    public decimal OutputPricePerMillion { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("contextWindow")]
    public int? ContextWindow { get; set; }

    [JsonPropertyName("aliases")]
    public List<AliasJson>? Aliases { get; set; }
}

internal sealed class AliasJson
{
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "exact";
}
