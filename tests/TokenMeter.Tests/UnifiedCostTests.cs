namespace TokenMeter.Tests;

/// <summary>
/// One request's cost in one call: the rates of the day, the tier its whole prompt falls in, cache reads and per-lifetime
/// cache writes — the way a ledger that records cost per request needs it.
/// </summary>
public class UnifiedCostTests
{
    private static readonly ModelInfo Tiered = new()
    {
        ModelId = "tiered",
        InputPricePerMillion = 1m,
        OutputPricePerMillion = 5m,
        CacheReadPricePerMillion = 0.1m,
        CacheWritePricePerMillion = 1.25m,
        CacheWritePrices = [new CacheWritePrice { Ttl = TimeSpan.FromMinutes(5), PricePerMillion = 1.25m }, new CacheWritePrice { Ttl = TimeSpan.FromHours(1), PricePerMillion = 2m }],
        PricingTiers =
        [
            new PricingTier
            {
                Axis = PricingTierAxis.ContextLength,
                MinContextLengthTokens = 100_001,
                InputPricePerMillion = 2m,
                OutputPricePerMillion = 10m,
                CacheReadPricePerMillion = 0.2m,
                CacheWritePricePerMillion = 2.5m,
                CacheWritePrices = [new CacheWritePrice { Ttl = TimeSpan.FromHours(1), PricePerMillion = 4m }],
            },
        ],
    };

    [Fact]
    public void Below_the_threshold_every_part_is_priced_at_the_base_rates()
    {
        var usage = new TokenCounts
        {
            InputTokens = 10_000,
            CacheReadTokens = 20_000,
            CacheWrites = [new CacheWriteTokenCount(TimeSpan.FromMinutes(5), 30_000), new CacheWriteTokenCount(TimeSpan.FromHours(1), 40_000)],
            OutputTokens = 1_000,
        };

        var cost = Tiered.CalculateCost(usage);

        // 0.01·1 + 0.02·0.1 + 0.03·1.25 + 0.04·2 + 0.001·5
        Assert.Equal(0.01m + 0.002m + 0.0375m + 0.08m + 0.005m, cost);
    }

    [Fact]
    public void A_prompt_over_the_threshold_counting_cache_tokens_moves_the_whole_request_to_the_tier()
    {
        // 10k uncached + 50k read + 50k written = 110k prompt: over 100k only because cache tokens count.
        var usage = new TokenCounts
        {
            InputTokens = 10_000,
            CacheReadTokens = 50_000,
            CacheWrites = [new CacheWriteTokenCount(TimeSpan.FromHours(1), 30_000), new CacheWriteTokenCount(null, 20_000)],
            OutputTokens = 1_000,
        };

        var cost = Tiered.CalculateCost(usage);

        // tier: 0.01·2 + 0.05·0.2 + 0.03·4 (1h) + 0.02·2.5 (default lifetime) + 0.001·10
        Assert.Equal(0.02m + 0.01m + 0.12m + 0.05m + 0.01m, cost);
        Assert.Equal(110_000, usage.PromptTokens);
    }

    [Fact]
    public void A_lifetime_the_tier_does_not_list_falls_back_to_the_tier_default_write_price()
    {
        var usage = new TokenCounts
        {
            InputTokens = 200_000,
            CacheWrites = [new CacheWriteTokenCount(TimeSpan.FromMinutes(5), 1_000_000)],
        };

        Assert.Equal(0.2m * 2m + 1m * 2.5m, Tiered.CalculateCost(usage));
    }

    [Fact]
    public void The_date_selects_the_rates_in_effect_that_day()
    {
        var model = ModelCatalog.FindModel("gemini-3.8-flash", AliasMatchType.Exact)!;
        var usage = new TokenCounts { InputTokens = 1_000_000, OutputTokens = 1_000_000 };

        var before = model.CalculateCost(usage, new CostContext { Date = new DateOnly(2026, 12, 31) });
        var after = model.CalculateCost(usage, new CostContext { Date = new DateOnly(2027, 1, 1) });

        Assert.Equal(0.75m + 3.75m, before);
        Assert.Equal(1.50m + 7.50m, after);
    }

    [Fact]
    public void Gemini_pro_prompts_over_200k_pay_the_long_context_rates()
    {
        var model = ModelCatalog.FindModel("gemini-2.5-pro", AliasMatchType.Exact)!;

        var shortPrompt = model.CalculateCost(new TokenCounts { InputTokens = 200_000, OutputTokens = 1_000_000 });
        var longPrompt = model.CalculateCost(new TokenCounts { InputTokens = 200_001, OutputTokens = 1_000_000 });

        Assert.Equal(0.2m * 1.25m + 10m, shortPrompt);
        Assert.Equal(0.200001m * 2.50m + 15m, longPrompt);
    }

    [Fact]
    public void Haiku_5_5_long_prompts_price_cache_writes_at_the_tier_rates()
    {
        var model = ModelCatalog.FindModel("claude-haiku-5-5", AliasMatchType.Exact)!;
        var usage = new TokenCounts
        {
            InputTokens = 100_000,
            CacheWrites = [new CacheWriteTokenCount(TimeSpan.FromHours(1), 1_000_000)],
        };

        Assert.Equal(0.1m * 0.50m + 1m * 1.00m, model.CalculateCost(usage));
    }

    [Fact]
    public void The_calculator_routes_to_the_same_calculation()
    {
        var calculator = CostCalculator.CustomOnly();
        calculator.RegisterModel(Tiered);
        var usage = new TokenCounts { InputTokens = 1_000, OutputTokens = 1_000 };

        Assert.Equal(Tiered.CalculateCost(usage), calculator.CalculateCost("tiered", usage));
    }

    [Fact]
    public void A_bounded_calculator_does_not_price_a_self_hosted_name_as_a_public_model()
    {
        // "gemini-2.5-pro-local" contains a public alias; full fuzzy matching prices it as the public model.
        Assert.NotNull(CostCalculator.Default().GetModel("my-gemini-2.5-pro-local"));
        Assert.Null(CostCalculator.Default(AliasMatchType.Exact).GetModel("my-gemini-2.5-pro-local"));

        var bounded = CostCalculator.Default(AliasMatchType.Exact);
        bounded.RegisterModel(new ModelInfo { ModelId = "my-gemini-2.5-pro-local", InputPricePerMillion = 0m, OutputPricePerMillion = 0m });
        Assert.Equal(0m, bounded.CalculateCost("my-gemini-2.5-pro-local", new TokenCounts { InputTokens = 10, OutputTokens = 10 }));
    }

    [Fact]
    public void Negative_counts_are_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Tiered.CalculateCost(new TokenCounts { CacheWrites = [new CacheWriteTokenCount(null, -1)] }));
    }

    [Fact]
    public void CostContext_At_splits_an_instant_into_its_UTC_date_and_time()
    {
        var context = CostContext.At(new DateTimeOffset(2027, 1, 1, 8, 30, 0, TimeSpan.FromHours(9)));

        Assert.Equal(new DateOnly(2026, 12, 31), context.Date);
        Assert.Equal(new TimeOnly(23, 30), context.CallTimeUtc);
    }
}

public class ProviderFreshnessTests
{
    [Fact]
    public void Each_provider_reports_its_own_date_and_the_catalog_the_newest()
    {
        var dates = ModelCatalog.GetProviderNames().Select(ModelCatalog.GetLastUpdated).ToList();

        Assert.All(dates, d => Assert.NotNull(d));
        Assert.Equal(ModelCatalog.LastUpdated, dates.Max());
        Assert.Equal(new DateOnly(2026, 10, 10), ModelCatalog.GetLastUpdated("Google"));
        Assert.Null(ModelCatalog.GetLastUpdated("no-such-provider"));
    }
}
