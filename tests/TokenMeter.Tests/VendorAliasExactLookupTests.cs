using System.Text.Json;
using System.Text.RegularExpressions;

namespace TokenMeter.Tests;

/// <summary>
/// A caller that refuses fuzzy matches (a gateway pricing from the name the vendor's API takes) must still find every name the
/// vendor documents: the dated snapshot and the vendor's own undated alias. These cases pin that, row by row and as a rule over
/// the bundled data, so a new dated row cannot arrive with its undated alias reachable only through the Contains pass.
/// </summary>
public partial class VendorAliasExactLookupTests
{
    private const string ResourcePrefix = "TokenMeter.Pricing.";

    [Theory]
    [InlineData("claude-sonnet-4-5", "claude-sonnet-4-5")]
    [InlineData("claude-sonnet-4-5-20250929", "claude-sonnet-4-5")]
    [InlineData("claude-haiku-4-5", "claude-haiku-4-5")]
    [InlineData("claude-haiku-4-5-20251001", "claude-haiku-4-5")]
    [InlineData("claude-opus-4-5", "claude-opus-4-5")]
    [InlineData("claude-opus-4-5-20251101", "claude-opus-4-5")]
    [InlineData("claude-opus-4-1", "claude-opus-4-1")]
    [InlineData("claude-opus-4-0", "claude-opus-4-0")]
    [InlineData("claude-sonnet-4-0", "claude-sonnet-4")]
    [InlineData("claude-3-7-sonnet-latest", "claude-3-7-sonnet-20250219")]
    [InlineData("claude-3-5-sonnet-latest", "claude-3-5-sonnet-20241022")]
    [InlineData("claude-3-5-haiku-latest", "claude-3-5-haiku-20241022")]
    [InlineData("claude-3-opus-latest", "claude-3-opus-20240229")]
    [InlineData("gpt-4o", "gpt-4o")]
    public void VendorModelName_ResolvesExactly(string vendorName, string expectedModelId)
    {
        var match = ModelCatalog.FindModelMatch(vendorName, AliasMatchType.Exact);

        Assert.NotNull(match);
        Assert.Equal(AliasMatchType.Exact, match.MatchKind);
        Assert.Equal(expectedModelId, match.Model.ModelId);
    }

    [Theory]
    [InlineData("claude-4-5-sonnet", "claude-sonnet-4-5")]
    [InlineData("claude-4-5-haiku", "claude-haiku-4-5")]
    [InlineData("claude-4-5-opus", "claude-opus-4-5")]
    public void FormerRowId_StillResolvesExactly(string formerId, string modelId)
    {
        var match = ModelCatalog.FindModelMatch(formerId, AliasMatchType.Exact);

        Assert.NotNull(match);
        Assert.Equal(modelId, match.Model.ModelId);
    }

    [Theory]
    [InlineData("text-embedding-3-small", "OpenAI", 0.02)]
    [InlineData("text-embedding-3-large", "OpenAI", 0.13)]
    [InlineData("text-embedding-ada-002", "OpenAI", 0.10)]
    [InlineData("gemini-embedding-2", "Google", 0.20)]
    public void EmbeddingModel_IsPricedOnInputTokens(string modelId, string provider, double inputPrice)
    {
        var match = ModelCatalog.FindModelMatch(modelId, AliasMatchType.Exact);

        Assert.NotNull(match);
        var model = match.Model;
        Assert.Equal(provider, model.Provider);
        Assert.Equal(ModelType.Embedding, model.ModelType);
        Assert.Equal((decimal)inputPrice, model.InputPricePerMillion);

        // One million input tokens and no output cost exactly the input price.
        Assert.Equal((decimal)inputPrice, model.CalculateCost(1_000_000, 0));
    }

    [Fact]
    public void EmbeddingModels_AreListedByType()
    {
        var embeddings = ModelCatalog.GetByType(ModelType.Embedding).Select(m => m.ModelId).ToList();

        Assert.Contains("text-embedding-3-small", embeddings);
        Assert.Contains("gemini-embedding-2", embeddings);
    }

    /// <summary>
    /// Rule: a row reachable by a dated exact alias (<c>name-20250929</c>, <c>name-2024-08-06</c>) is also reachable exactly by
    /// the undated name, or by Anthropic's <c>name-0</c> form for its first 4.x models. A row that keeps the undated name only
    /// as a Contains alias fails here.
    /// </summary>
    [Fact]
    public void EveryDatedExactName_HasItsUndatedNameResolvingExactlyToTheSameRow()
    {
        var gaps = new List<string>();

        foreach (var (modelId, datedName) in DatedExactNames())
        {
            var undated = DatedSuffix().Replace(datedName, "");
            var hit = ModelCatalog.FindModelMatch(undated, AliasMatchType.Exact)
                   ?? ModelCatalog.FindModelMatch(undated + "-0", AliasMatchType.Exact);

            if (hit is null || hit.Model.ModelId != ModelCatalog.FindModel(modelId, AliasMatchType.Exact)!.ModelId)
            {
                gaps.Add($"{datedName} (row {modelId}): '{undated}' -> {hit?.Model.ModelId ?? "null"}");
            }
        }

        Assert.True(gaps.Count == 0, "Undated names not resolving exactly:\n" + string.Join("\n", gaps));
    }

    [Fact]
    public void DatedExactNames_AreFoundInTheBundledData()
    {
        // Positive control: the rule above would pass vacuously on an empty enumeration.
        Assert.Contains(DatedExactNames(), n => n.DatedName == "claude-sonnet-4-5-20250929");
    }

    private static List<(string ModelId, string DatedName)> DatedExactNames()
    {
        var names = new List<(string, string)>();
        var assembly = typeof(ModelCatalog).Assembly;

        foreach (var resource in assembly.GetManifestResourceNames()
                     .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                              && n.EndsWith(".json", StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var json = JsonDocument.Parse(stream);

            foreach (var model in json.RootElement.GetProperty("models").EnumerateArray())
            {
                var modelId = model.GetProperty("modelId").GetString()!;
                // Aliases only: a row whose own id is dated (Claude 3) is a model the vendor named only by date.
                var candidates = new List<string>();
                if (model.TryGetProperty("aliases", out var aliases))
                {
                    candidates.AddRange(aliases.EnumerateArray()
                        .Where(a => a.GetProperty("type").GetString() == "exact")
                        .Select(a => a.GetProperty("pattern").GetString()!));
                }

                names.AddRange(candidates.Where(c => DatedSuffix().IsMatch(c)).Select(c => (modelId, c)));
            }
        }

        return names;
    }

    [GeneratedRegex(@"-(\d{8}|\d{4}-\d{2}-\d{2})$")]
    private static partial Regex DatedSuffix();
}
