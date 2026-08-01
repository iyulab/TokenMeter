using System.Globalization;
using System.Text.Json;

namespace TokenMeter.Tests;

/// <summary>
/// The freshness signal is only trustworthy if it cannot drift away from the data it
/// describes. It used to be a hand-maintained constant in a source file, updated by
/// convention; these cases enforce that it is now derived from the bundled data, and that
/// every provider carries the date a reader would need to judge it.
/// </summary>
public class CatalogFreshnessTests
{
    private const string ResourcePrefix = "TokenMeter.Pricing.";

    private static IEnumerable<(string Resource, JsonDocument Json)> PricingResources()
    {
        var assembly = typeof(ModelCatalog).Assembly;

        foreach (var name in assembly.GetManifestResourceNames()
                     .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                              && n.EndsWith(".json", StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            yield return (name, JsonDocument.Parse(stream));
        }
    }

    private static DateOnly? DeclaredDate(JsonDocument json)
    {
        if (!json.RootElement.TryGetProperty("lastUpdated", out var element)) return null;
        if (element.ValueKind != JsonValueKind.String) return null;

        return DateOnly.TryParse(element.GetString(), CultureInfo.InvariantCulture, out var date)
            ? date
            : null;
    }

    [Fact]
    public void EveryProvider_DeclaresAParseableLastUpdatedDate()
    {
        var undated = new List<string>();

        foreach (var (resource, json) in PricingResources())
        {
            using (json)
            {
                if (DeclaredDate(json) is null) undated.Add(resource);
            }
        }

        Assert.True(undated.Count == 0,
            "Every provider file must declare a \"lastUpdated\" date in yyyy-MM-dd form, so that a " +
            "data refresh and the freshness it advertises cannot come apart. Missing or malformed in: " +
            string.Join(", ", undated));
    }

    [Fact]
    public void CatalogLastUpdated_IsDerivedFromTheData_NotDeclaredSeparately()
    {
        DateOnly? newest = null;

        foreach (var (_, json) in PricingResources())
        {
            using (json)
            {
                if (DeclaredDate(json) is { } date && (newest is null || date > newest))
                    newest = date;
            }
        }

        Assert.NotNull(newest);
        Assert.Equal(newest, ModelCatalog.LastUpdated);
    }

    [Fact]
    public void CatalogLastUpdated_IsDated_SoFreshnessIsNotReportedByDefault()
    {
        // An undated catalog falls back to DateOnly.MinValue, which reports as stale rather
        // than fresh. Reaching that state in a shipped build would mean the data lost its dates.
        Assert.NotEqual(default, ModelCatalog.LastUpdated);
        Assert.False(ModelCatalog.IsDataStale(int.MaxValue));
    }
}
