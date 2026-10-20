using System.Reflection;
using System.Text.Json;

namespace TokenMeter.Tests;

/// <summary>
/// A tool fee carries the unit the vendor counts: each call the response reports, or once per request that used the tool.
/// </summary>
public class ToolFeeTests
{
    private static readonly string[] Units = ["call", "request"];

    private static readonly TokenCounts Usage = new() { InputTokens = 10_000, OutputTokens = 1_000 };

    private static TokenCounts WithSearches(string tool, int calls) =>
        Usage with { ToolCalls = new Dictionary<string, int> { [tool] = calls } };

    [Fact]
    public void Gemini_2_5_search_grounding_is_one_fee_per_grounded_request_however_many_queries_ran()
    {
        var model = ModelCatalog.FindModel("gemini-2.5-flash", AliasMatchType.Exact)!;

        // $35 / 1,000 grounded prompts: three queries in one request are one prompt.
        Assert.Equal(model.CalculateCost(Usage) + 0.035m, model.CalculateCost(WithSearches("google_search", 3)));
        Assert.Equal(model.CalculateCost(Usage) + 0.035m, model.CalculateCost(WithSearches("google_search", 1)));
        Assert.Equal(model.CalculateCost(Usage), model.CalculateCost(WithSearches("google_search", 0)));
    }

    [Fact]
    public void Gemini_3_search_grounding_is_one_fee_per_query()
    {
        var model = ModelCatalog.FindModel("gemini-3.8-flash", AliasMatchType.Exact)!;

        // $14 / 1,000, «charged for each individual search query performed».
        Assert.Equal(model.CalculateCost(Usage) + 0.042m, model.CalculateCost(WithSearches("google_search", 3)));
    }

    [Fact]
    public void Gemini_2_5_maps_grounding_is_one_fee_per_grounded_request()
    {
        var model = ModelCatalog.FindModel("gemini-2.5-pro", AliasMatchType.Exact)!;

        // $25 / 1,000 grounded prompts.
        Assert.Equal(model.CalculateCost(Usage) + 0.025m, model.CalculateCost(WithSearches("google_maps", 2)));
    }

    [Fact]
    public void Gemini_3_maps_grounding_is_one_fee_per_query()
    {
        var model = ModelCatalog.FindModel("gemini-4-argon", AliasMatchType.Exact)!;

        // $14 / 1,000 search queries.
        Assert.Equal(model.CalculateCost(Usage) + 0.028m, model.CalculateCost(WithSearches("google_maps", 2)));
    }

    [Fact]
    public void Gemini_2_5_grounding_fees_are_per_request_and_every_other_tool_fee_is_per_call()
    {
        var fees = ModelCatalog.GetProviderNames()
            .SelectMany(ModelCatalog.GetByProvider)
            .SelectMany(m => (m.ToolCallPrices ?? []).Select(f => (m.ModelId, Fee: f)))
            .ToList();

        Assert.NotEmpty(fees);
        foreach (var (modelId, fee) in fees)
        {
            var perRequest = fee.Tool is "google_search" or "google_maps" && modelId.StartsWith("gemini-2.5-", StringComparison.Ordinal);
            Assert.True(fee.Unit == (perRequest ? ToolFeeUnit.Request : ToolFeeUnit.Call), $"{modelId} {fee.Tool}: {fee.Unit}");
        }
        Assert.Equal(6, fees.Count(f => f.Fee.Unit == ToolFeeUnit.Request));
    }

    [Fact]
    public void Every_tool_fee_in_the_catalog_files_is_loaded()
    {
        // The loader leaves out a fee whose unit does not parse (the cost of that tool is then unknown, not mispriced) —
        // so every fee written in the files must arrive.
        var written = new Dictionary<string, int>(StringComparer.Ordinal);
        var asm = typeof(ModelCatalog).Assembly;
        foreach (var name in asm.GetManifestResourceNames().Where(n => n.EndsWith(".json", StringComparison.Ordinal)))
        {
            using var stream = asm.GetManifestResourceStream(name)!;
            using var doc = JsonDocument.Parse(stream);
            if (!doc.RootElement.TryGetProperty("models", out var models))
                continue;
            foreach (var model in models.EnumerateArray())
            {
                if (!model.TryGetProperty("toolCallPricesPerThousand", out var fees))
                    continue;
                foreach (var fee in fees.EnumerateObject())
                {
                    if (fee.Value.ValueKind == JsonValueKind.Object)
                        Assert.Contains(fee.Value.GetProperty("unit").GetString(), Units);
                    else
                        Assert.Equal(JsonValueKind.Number, fee.Value.ValueKind);
                }
                written[model.GetProperty("modelId").GetString()!] = fees.EnumerateObject().Count();
            }
        }

        Assert.NotEmpty(written);
        foreach (var (modelId, count) in written)
            Assert.Equal(count, ModelCatalog.FindModel(modelId, AliasMatchType.Exact)!.ToolCallPrices!.Count);
    }

    [Fact]
    public void A_fee_built_without_a_unit_is_per_call()
    {
        var model = new ModelInfo
        {
            ModelId = "custom",
            InputPricePerMillion = 1m,
            OutputPricePerMillion = 2m,
            ToolCallPrices = [new ToolCallPrice { Tool = "web_search", PricePerThousand = 10m }],
        };

        Assert.Equal(ToolFeeUnit.Call, model.ToolCallPrices[0].Unit);
        Assert.Equal(model.CalculateCost(Usage) + 0.02m, model.CalculateCost(WithSearches("web_search", 2)));
    }
}
