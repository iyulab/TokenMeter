using System.Text.Json;

namespace TokenMeter.Tests;

/// <summary>
/// Model lifecycle dates, per-lifetime cache-write prices and announced price changes — what a consumer needs to move
/// an alias before a model retires and to price a call at the rate in effect when it is made.
/// </summary>
public class LifecycleAndScheduleTests
{
    #region Lifecycle

    [Fact]
    public void GetLifecycleStatus_FollowsTheDates()
    {
        var model = new ModelInfo
        {
            ModelId = "m",
            DeprecationDate = new DateOnly(2026, 9, 30),
            RetirementDate = new DateOnly(2026, 11, 30),
        };

        Assert.Equal(ModelLifecycleStatus.Active, model.GetLifecycleStatus(new DateOnly(2026, 9, 29)));
        Assert.Equal(ModelLifecycleStatus.Deprecated, model.GetLifecycleStatus(new DateOnly(2026, 9, 30)));
        Assert.Equal(ModelLifecycleStatus.Deprecated, model.GetLifecycleStatus(new DateOnly(2026, 11, 29)));
        Assert.Equal(ModelLifecycleStatus.Retired, model.GetLifecycleStatus(new DateOnly(2026, 11, 30)));
    }

    [Fact]
    public void GetLifecycleStatus_NoDates_IsActive()
        => Assert.Equal(ModelLifecycleStatus.Active, new ModelInfo { ModelId = "m" }.GetLifecycleStatus(new DateOnly(2030, 1, 1)));

    [Fact]
    public void Catalog_CarriesTheVendorsDeprecation_AndTheReplacementResolves()
    {
        var sonnet45 = ModelCatalog.FindModel("claude-sonnet-4-5-20250929");
        Assert.NotNull(sonnet45);
        Assert.Equal(new DateOnly(2026, 9, 30), sonnet45.DeprecationDate);
        Assert.Equal(new DateOnly(2026, 11, 30), sonnet45.RetirementDate);
        Assert.Equal(ModelLifecycleStatus.Deprecated, sonnet45.GetLifecycleStatus(new DateOnly(2026, 10, 9)));
        Assert.Equal("claude-sonnet-5-5", ModelCatalog.FindModel(sonnet45.ReplacementModelId)?.ModelId);

        var gpt4 = ModelCatalog.FindModel("gpt-4");
        Assert.NotNull(gpt4);
        Assert.Equal(ModelLifecycleStatus.Retired, gpt4.GetLifecycleStatus(new DateOnly(2026, 10, 23)));
    }

    [Fact]
    public void EveryLifecycleEntry_IsConsistent_AndItsReplacementIsInTheCatalog()
    {
        var problems = new List<string>();
        foreach (var model in AllModels())
        {
            if (model.RetirementDate is { } retired && model.DeprecationDate is { } deprecated && retired < deprecated)
                problems.Add($"{model.ModelId}: retires {retired} before it is deprecated {deprecated}");
            // A retirement date without a deprecation date is a planned end of life: Google publishes a shutdown date for a
            // generally available model from its release (gemini-3.1-flash-lite: May 7, 2027) and gives no announcement
            // date for older shutdowns (gemini-1.5-*).
            if (model.ReplacementModelId is { } replacement && ModelCatalog.FindModel(replacement, AliasMatchType.Prefix) is null)
                problems.Add($"{model.ModelId}: replacement '{replacement}' is not in the catalog");
        }

        Assert.Empty(problems);
    }

    [Fact]
    public void Catalog_CarriesGooglesShutdowns_AndAutoRoutedReplacements()
    {
        // ai.google.dev deprecations page and changelog, 2026-10-10.
        var flash35 = ModelCatalog.FindModel("gemini-3.5-flash", AliasMatchType.Exact);
        Assert.NotNull(flash35);
        Assert.Equal(new DateOnly(2026, 10, 8), flash35.DeprecationDate);
        Assert.Null(flash35.RetirementDate);
        Assert.Equal("gemini-3.6-flash", flash35.ReplacementModelId);
        Assert.Equal(ModelLifecycleStatus.Deprecated, flash35.GetLifecycleStatus(new DateOnly(2026, 10, 10)));

        var flash20 = ModelCatalog.FindModel("gemini-2.0-flash", AliasMatchType.Exact);
        Assert.NotNull(flash20);
        Assert.Equal(ModelLifecycleStatus.Deprecated, flash20.GetLifecycleStatus(new DateOnly(2026, 5, 31)));
        Assert.Equal(ModelLifecycleStatus.Retired, flash20.GetLifecycleStatus(new DateOnly(2026, 6, 1)));

        var flashLite31 = ModelCatalog.FindModel("gemini-3.1-flash-lite", AliasMatchType.Exact);
        Assert.NotNull(flashLite31);
        Assert.Equal(ModelLifecycleStatus.Active, flashLite31.GetLifecycleStatus(new DateOnly(2027, 5, 6)));
        Assert.Equal(ModelLifecycleStatus.Retired, flashLite31.GetLifecycleStatus(new DateOnly(2027, 5, 7)));
        Assert.Equal("gemini-3.5-flash-lite", flashLite31.ReplacementModelId);

        Assert.Equal(ModelLifecycleStatus.Retired,
            ModelCatalog.FindModel("gemini-1.5-pro", AliasMatchType.Exact)!.GetLifecycleStatus(new DateOnly(2025, 9, 29)));
        Assert.Equal("gemini-3.8-flash", ModelCatalog.FindModel("gemini-3.7-flash", AliasMatchType.Exact)!.ReplacementModelId);
    }

    #endregion

    #region Cache storage

    [Fact]
    public void CalculateCacheStorageCost_IsTokensTimesHoursTimesTheRate()
    {
        var pro = ModelCatalog.FindModel("gemini-2.5-pro", AliasMatchType.Exact);
        Assert.NotNull(pro);
        Assert.Equal(4.50m, pro.CacheStoragePricePerMillionPerHour);

        // 1M tokens for an hour, 200K tokens for 90 minutes (linear in the time, no rounding).
        Assert.Equal(4.50m, pro.CalculateCacheStorageCost(1_000_000, TimeSpan.FromHours(1)));
        Assert.Equal(1.35m, pro.CalculateCacheStorageCost(200_000, TimeSpan.FromMinutes(90)));
        Assert.Equal(0m, pro.CalculateCacheStorageCost(1_000_000, TimeSpan.Zero));
    }

    [Fact]
    public void CalculateCacheStorageCost_FollowsTheAnnouncedRate_OnItsDate()
    {
        var flash = ModelCatalog.FindModel("gemini-3.8-flash", AliasMatchType.Exact);
        Assert.NotNull(flash);

        Assert.Equal(0.50m, flash.CalculateCacheStorageCost(1_000_000, TimeSpan.FromHours(1), new DateOnly(2026, 12, 31)));
        Assert.Equal(1.00m, flash.CalculateCacheStorageCost(1_000_000, TimeSpan.FromHours(1), new DateOnly(2027, 1, 1)));
        Assert.Equal(1.00m, flash.AsOf(new DateOnly(2027, 1, 1)).CacheStoragePricePerMillionPerHour);
    }

    [Fact]
    public void CalculateCacheStorageCost_NoStoragePrice_IsUnknown()
    {
        var claude = ModelCatalog.FindModel("claude-opus-4-8");
        Assert.NotNull(claude);
        Assert.Null(claude.CalculateCacheStorageCost(1_000_000, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void CalculateCacheStorageCost_RejectsNegativeInput()
    {
        var model = new ModelInfo { ModelId = "m", CacheStoragePricePerMillionPerHour = 1m };
        Assert.Throws<ArgumentOutOfRangeException>(() => model.CalculateCacheStorageCost(-1, TimeSpan.FromHours(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => model.CalculateCacheStorageCost(1, TimeSpan.FromHours(-1)));
    }

    [Fact]
    public void GeminiModels_WithExplicitCaching_CarryTheirStorageRate()
    {
        // ai.google.dev pricing page (Standard), 2026-10-10.
        var expected = new Dictionary<string, decimal>
        {
            ["gemini-3.8-flash"] = 0.50m, ["gemini-3.6-flash"] = 0.50m, ["gemini-3.5-flash-lite"] = 1.00m,
            ["gemini-3.1-flash-lite"] = 1.00m, ["gemini-3.1-pro-preview"] = 4.50m, ["gemini-3-flash-preview"] = 1.00m,
            ["gemini-2.5-pro"] = 4.50m, ["gemini-2.5-flash"] = 1.00m, ["gemini-2.5-flash-lite"] = 1.00m,
        };
        var actual = expected.Keys.ToDictionary(id => id, id => ModelCatalog.FindModel(id, AliasMatchType.Exact)?.CacheStoragePricePerMillionPerHour);
        Assert.Equal(expected.ToDictionary(kv => kv.Key, kv => (decimal?)kv.Value), actual);
    }

    #endregion

    #region Cache write per lifetime

    [Fact]
    public void AnthropicModels_PriceTheOneHourWriteAtTwiceInput()
    {
        var problems = AllModels()
            .Where(m => m.Provider == "Anthropic" && m.CacheWritePricePerMillion is not null)
            .Where(m => m.GetCacheWritePrice(TimeSpan.FromHours(1)) != m.InputPricePerMillion * 2
                     || m.GetCacheWritePrice(TimeSpan.FromMinutes(5)) != m.CacheWritePricePerMillion)
            .Select(m => m.ModelId)
            .ToList();

        Assert.Empty(problems);
    }

    [Fact]
    public void CalculateCost_WithTtl_PricesTheWriteAtThatLifetime()
    {
        var opus = ModelCatalog.FindModel("claude-opus-4-8");
        Assert.NotNull(opus);

        // 1M cache-write tokens: 5-minute 6.25, 1-hour 10.00 (input 5.00).
        Assert.Equal(6.25m, opus.CalculateCost(0, 0, 0, 1_000_000, TimeSpan.FromMinutes(5)));
        Assert.Equal(10.00m, opus.CalculateCost(0, 0, 0, 1_000_000, TimeSpan.FromHours(1)));
        Assert.Equal(opus.CalculateCost(1000, 2000, 3000, 4000), opus.CalculateCost(1000, 2000, 3000, 4000, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void GetCacheWritePrice_UnknownLifetime_FallsBackToTheDefaultWritePrice()
    {
        var model = new ModelInfo
        {
            ModelId = "m",
            CacheWritePricePerMillion = 1.25m,
            CacheWritePrices = [new CacheWritePrice { Ttl = TimeSpan.FromHours(1), PricePerMillion = 2m }],
        };

        Assert.Equal(1.25m, model.GetCacheWritePrice(TimeSpan.FromMinutes(30)));
        Assert.Equal(2m, model.GetCacheWritePrice(TimeSpan.FromHours(1)));
    }

    #endregion

    #region Announced price changes

    [Fact]
    public void AsOf_AppliesTheAnnouncedGeminiFlashRates_FromTheirEffectiveDate()
    {
        var flash = ModelCatalog.FindModel("gemini-3.8-flash");
        Assert.NotNull(flash);

        var before = flash.AsOf(new DateOnly(2026, 12, 31));
        Assert.Equal(0.75m, before.InputPricePerMillion);
        Assert.Equal(3.75m, before.OutputPricePerMillion);

        var after = flash.AsOf(new DateOnly(2027, 1, 1));
        Assert.Equal(1.50m, after.InputPricePerMillion);
        Assert.Equal(7.50m, after.OutputPricePerMillion);
        Assert.Equal(0.15m, after.CacheReadPricePerMillion);
        Assert.Equal(1.50m + 7.50m, after.CalculateCost(1_000_000, 1_000_000));
    }

    [Fact]
    public void AsOf_MovesPerLifetimeWritePrices_WithTheDefaultWritePrice()
    {
        var model = new ModelInfo
        {
            ModelId = "m",
            InputPricePerMillion = 1m,
            OutputPricePerMillion = 5m,
            CacheWritePricePerMillion = 1.25m,
            CacheWritePrices = [new CacheWritePrice { Ttl = TimeSpan.FromHours(1), PricePerMillion = 2m }],
            ScheduledPrices = [new ScheduledPrice { EffectiveFrom = new DateOnly(2027, 1, 1), InputPricePerMillion = 2m, CacheWritePricePerMillion = 2.5m }],
        };

        Assert.Same(model, model.AsOf(new DateOnly(2026, 12, 31)));
        var after = model.AsOf(new DateOnly(2027, 1, 1));
        Assert.Equal(2m, after.InputPricePerMillion);
        Assert.Equal(5m, after.OutputPricePerMillion);
        Assert.Equal(4m, after.GetCacheWritePrice(TimeSpan.FromHours(1)));
    }

    #endregion

    #region Data — every new entry in the embedded JSON loads

    [Fact]
    public void EveryLifecycleDate_TtlAndScheduledDate_InTheData_Loads()
    {
        var problems = new List<string>();
        var asm = typeof(ModelCatalog).Assembly;
        foreach (var name in asm.GetManifestResourceNames().Where(n => n.EndsWith(".json", StringComparison.Ordinal)))
        {
            using var stream = asm.GetManifestResourceStream(name)!;
            using var doc = JsonDocument.Parse(stream);
            foreach (var model in doc.RootElement.GetProperty("models").EnumerateArray())
            {
                var id = model.GetProperty("modelId").GetString()!;
                var loaded = ModelCatalog.FindModel(id, AliasMatchType.Exact);
                foreach (var (field, value) in new[] { ("deprecationDate", loaded?.DeprecationDate), ("retirementDate", loaded?.RetirementDate) })
                {
                    if (model.TryGetProperty(field, out _) && value is null)
                        problems.Add($"{id}: {field} does not parse");
                }
                if (model.TryGetProperty("cacheWritePrices", out var ttls) && ttls.GetArrayLength() != (loaded?.CacheWritePrices?.Count ?? 0))
                    problems.Add($"{id}: a cacheWritePrices ttl does not parse");
                if (model.TryGetProperty("scheduledPrices", out var scheduled) && scheduled.GetArrayLength() != (loaded?.ScheduledPrices?.Count ?? 0))
                    problems.Add($"{id}: a scheduledPrices effectiveFrom does not parse");
            }
        }

        Assert.Empty(problems);
    }

    #endregion

    private static IEnumerable<ModelInfo> AllModels()
        => ModelCatalog.GetProviderNames().SelectMany(ModelCatalog.GetByProvider);
}
