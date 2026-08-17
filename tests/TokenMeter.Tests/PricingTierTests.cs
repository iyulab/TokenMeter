namespace TokenMeter.Tests;

public class PricingTierTests
{
    #region No context — reproduces the representative rate exactly

    [Fact]
    public void CalculateCost_NoContext_MatchesUntieredOverload()
    {
        var model = ModelCatalog.FindModel("grok-4.6");
        Assert.NotNull(model);
        Assert.NotNull(model.PricingTiers);

        var tiered = model.CalculateCost(1_000_000, 500_000, new PricingTierContext());
        var untiered = model.CalculateCost(1_000_000, 500_000);

        Assert.Equal(untiered, tiered);
    }

    [Fact]
    public void CalculateCost_ModelWithoutTiers_ContextIgnored()
    {
        var model = new ModelInfo
        {
            ModelId = "no-tiers",
            InputPricePerMillion = 1.0m,
            OutputPricePerMillion = 2.0m
        };

        var cost = model.CalculateCost(1_000_000, 1_000_000,
            new PricingTierContext { ContextLengthTokens = 500_000_000 });

        Assert.Equal(1.0m + 2.0m, cost);
    }

    #endregion

    #region ContextLength axis (xAI — long-context surcharge)

    [Fact]
    public void CalculateCost_ContextBelowThreshold_UsesRepresentativeRate()
    {
        var model = ModelCatalog.FindModel("grok-4.6");
        Assert.NotNull(model);

        var cost = model.CalculateCost(1_000_000, 1_000_000,
            new PricingTierContext { ContextLengthTokens = 199_999 });

        // Representative: $2.00 in + $6.00 out per 1M
        Assert.Equal(2.00m + 6.00m, cost);
    }

    [Fact]
    public void CalculateCost_ContextAtOrAboveThreshold_UsesTierRate()
    {
        var model = ModelCatalog.FindModel("grok-4.6");
        Assert.NotNull(model);

        var atThreshold = model.CalculateCost(1_000_000, 1_000_000,
            new PricingTierContext { ContextLengthTokens = 200_000 });
        var aboveThreshold = model.CalculateCost(1_000_000, 1_000_000,
            new PricingTierContext { ContextLengthTokens = 5_000_000 });

        // Tier: $4.00 in + $12.00 out per 1M — double the representative rate
        Assert.Equal(4.00m + 12.00m, atThreshold);
        Assert.Equal(4.00m + 12.00m, aboveThreshold);
    }

    [Fact]
    public void CalculateCost_ContextLengthTier_CacheReadFallsBackToRepresentative()
    {
        // The catalog does not carry a distinct cache-read rate for the long-context tier —
        // it must fall back to the model's representative CacheReadPricePerMillion, not to
        // the tier's (absent) one.
        var model = ModelCatalog.FindModel("grok-4.6");
        Assert.NotNull(model);
        var tier = Assert.Single(model.PricingTiers!, t => t.Axis == PricingTierAxis.ContextLength);

        Assert.Null(tier.CacheReadPricePerMillion);
    }

    #endregion

    #region TimeOfDay axis (DeepSeek — peak-hour surcharge, two disjoint windows)

    [Theory]
    [InlineData("02:30")] // inside 01:00–04:00 window
    [InlineData("07:00")] // inside 06:00–10:00 window
    public void CalculateCost_WithinPeakWindow_UsesTierRate(string timeOfDay)
    {
        var model = ModelCatalog.FindModel("deepseek-v4-pro");
        Assert.NotNull(model);

        var cost = model.CalculateCost(1_000_000, 1_000_000,
            new PricingTierContext { CallTimeUtc = TimeOnly.Parse(timeOfDay, System.Globalization.CultureInfo.InvariantCulture) });

        // Peak: $1.32 in + $3.96 out per 1M — double the off-peak representative rate
        Assert.Equal(1.32m + 3.96m, cost);
    }

    [Theory]
    [InlineData("00:30")]
    [InlineData("05:00")]
    [InlineData("12:00")]
    [InlineData("23:59")]
    public void CalculateCost_OutsidePeakWindows_UsesRepresentativeRate(string timeOfDay)
    {
        var model = ModelCatalog.FindModel("deepseek-v4-pro");
        Assert.NotNull(model);

        var cost = model.CalculateCost(1_000_000, 1_000_000,
            new PricingTierContext { CallTimeUtc = TimeOnly.Parse(timeOfDay, System.Globalization.CultureInfo.InvariantCulture) });

        // Off-peak: $0.66 in + $1.98 out per 1M
        Assert.Equal(0.66m + 1.98m, cost);
    }

    [Fact]
    public void CalculateCost_WindowEnd_IsExclusive()
    {
        var model = ModelCatalog.FindModel("deepseek-v4-pro");
        Assert.NotNull(model);

        var atWindowEnd = model.CalculateCost(1_000_000, 1_000_000,
            new PricingTierContext { CallTimeUtc = new TimeOnly(4, 0) });

        Assert.Equal(0.66m + 1.98m, atWindowEnd);
    }

    #endregion

    #region ICostCalculator — DI-friendly path mirrors ModelInfo

    [Fact]
    public void CostCalculator_CalculateCost_WithContext_MatchesModelInfoOverload()
    {
        var calc = CostCalculator.Default();
        var model = calc.GetModel("grok-4.6");
        Assert.NotNull(model);

        var context = new PricingTierContext { ContextLengthTokens = 250_000 };
        var viaCalculator = calc.CalculateCost("grok-4.6", 1_000_000, 500_000, context);
        var viaModelInfo = model.CalculateCost(1_000_000, 500_000, context);

        Assert.Equal(viaModelInfo, viaCalculator);
    }

    [Fact]
    public void CostCalculator_CalculateCost_WithContext_UnknownModel_ReturnsNull()
    {
        var calc = CostCalculator.Default();

        var cost = calc.CalculateCost("no-such-model", 1_000, 1_000, new PricingTierContext());

        Assert.Null(cost);
    }

    #endregion

    #region Catalog integrity

    [Fact]
    public void ModelsWithPricingTiers_TierRatesAreNeverCheaperThanRepresentative()
    {
        foreach (var (key, info) in ModelCatalog.All)
        {
            if (info.PricingTiers is not { Count: > 0 } tiers) continue;

            foreach (var tier in tiers)
            {
                Assert.True(tier.InputPricePerMillion >= info.InputPricePerMillion,
                    $"{key}: tier input rate {tier.InputPricePerMillion} is cheaper than " +
                    $"representative rate {info.InputPricePerMillion}");
            }
        }
    }

    [Fact]
    public void ContextLengthTiers_HaveThresholdSet()
    {
        var tiers = ModelCatalog.All.Values
            .SelectMany(m => m.PricingTiers ?? [])
            .Where(t => t.Axis == PricingTierAxis.ContextLength);

        Assert.All(tiers, t => Assert.NotNull(t.MinContextLengthTokens));
    }

    [Fact]
    public void TimeOfDayTiers_HaveWindowSet()
    {
        var tiers = ModelCatalog.All.Values
            .SelectMany(m => m.PricingTiers ?? [])
            .Where(t => t.Axis == PricingTierAxis.TimeOfDay);

        Assert.All(tiers, t =>
        {
            Assert.NotNull(t.WindowStartUtc);
            Assert.NotNull(t.WindowEndUtc);
        });
    }

    [Fact]
    public void XAI_LongContextModels_HaveContextLengthTier()
    {
        foreach (var modelId in new[] { "grok-4.6", "grok-4.5", "grok-4.3", "grok-4.20", "grok-build-0.1" })
        {
            var model = ModelCatalog.FindModel(modelId, AliasMatchType.Exact);
            Assert.NotNull(model);
            Assert.True(model.PricingTiers is { Count: > 0 },
                $"{modelId} should carry a >=200K context-length tier");
        }
    }

    [Fact]
    public void DeepSeek_V4Models_HavePeakWindowTiers()
    {
        foreach (var modelId in new[] { "deepseek-v4-pro", "deepseek-v4-flash" })
        {
            var model = ModelCatalog.FindModel(modelId, AliasMatchType.Exact);
            Assert.NotNull(model);
            var tiers = model.PricingTiers;
            Assert.NotNull(tiers);
            Assert.Equal(2, tiers.Count); // two disjoint peak windows
            Assert.All(tiers, t => Assert.Equal(PricingTierAxis.TimeOfDay, t.Axis));
        }
    }

    #endregion
}
