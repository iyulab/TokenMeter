using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace TokenMeter.Tests;

/// <summary>
/// Capability tiers: a vendor's lines (Haiku, Sonnet, Opus, Fable ...) sit on one five-step scale, a model has a line and a
/// version, and "the latest model of a tier" is a rule over the catalog. The tests hold the rule, the lookup that applies
/// it, and the catalog data the rule reads.
/// </summary>
public class CapabilityTierTests
{
    // A fixed day: whether a model is deprecated depends on the day, and a test must not change with the clock.
    private static readonly DateOnly AsOf = new(2026, 11, 24);

    private static ResolveOptions Options(TierFallback fallback = TierFallback.None, IReadOnlyCollection<string>? available = null) =>
        new() { Fallback = fallback, Available = available, AsOf = AsOf };

    // ── Resolve ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Anthropic", CapabilityTier.Entry, "claude-haiku-5-5")]
    [InlineData("Anthropic", CapabilityTier.Balanced, "claude-sonnet-5-5")]
    [InlineData("Anthropic", CapabilityTier.Advanced, "claude-opus-5-5")]
    [InlineData("Anthropic", CapabilityTier.Frontier, "claude-fable-5-2")]
    [InlineData("OpenAI", CapabilityTier.Entry, "gpt-6-luna")]
    [InlineData("OpenAI", CapabilityTier.Balanced, "gpt-6.1-terra")]
    [InlineData("OpenAI", CapabilityTier.Advanced, "gpt-6.2-sol")]
    [InlineData("OpenAI", CapabilityTier.Frontier, "gpt-6.2-astra")]
    [InlineData("Google", CapabilityTier.Entry, "gemini-4-hydrogen")]
    [InlineData("Google", CapabilityTier.Frontier, "gemini-4-carbon")]
    public void Resolve_ReturnsTheLatestModelOfTheTier(string provider, CapabilityTier tier, string expected)
    {
        var resolution = ModelCatalog.Resolve(provider, tier, Options());

        Assert.NotNull(resolution);
        Assert.Equal(expected, resolution.Model.ModelId);
        Assert.Equal(expected, resolution.Id);
        Assert.Equal(tier, resolution.Tier);
        Assert.False(resolution.Substituted);
    }

    [Theory]
    [InlineData("Anthropic", CapabilityTier.Basic)]
    [InlineData("OpenAI", CapabilityTier.Basic)]
    [InlineData("Google", CapabilityTier.Balanced)] // Gemini 4 Neon is not placed on a rung yet
    [InlineData("Google", CapabilityTier.Advanced)] // nor Gemini 4 Argon
    public void Resolve_ARungWithoutAModel_IsNullNotTheNeighbour(string provider, CapabilityTier tier) =>
        Assert.Null(ModelCatalog.Resolve(provider, tier, Options()));

    [Fact]
    public void Resolve_LowerFallback_TakesTheNearestRungBelow_AndSaysSo()
    {
        var resolution = ModelCatalog.Resolve("Anthropic", CapabilityTier.Basic, Options(TierFallback.Lower));

        Assert.NotNull(resolution);
        Assert.Equal("claude-haiku-5-5", resolution.Model.ModelId);
        Assert.Equal(CapabilityTier.Entry, resolution.Tier);
        Assert.True(resolution.Substituted);
    }

    [Fact]
    public void Resolve_HigherFallback_TakesTheNearestRungAbove_AndSaysSo()
    {
        var resolution = ModelCatalog.Resolve("Anthropic", CapabilityTier.Basic, Options(TierFallback.Higher));

        Assert.NotNull(resolution);
        Assert.Equal("claude-sonnet-5-5", resolution.Model.ModelId);
        Assert.Equal(CapabilityTier.Balanced, resolution.Tier);
        Assert.True(resolution.Substituted);
    }

    [Fact]
    public void Resolve_Fallback_IsNotUsedWhenTheRungHoldsAModel()
    {
        var resolution = ModelCatalog.Resolve("Anthropic", CapabilityTier.Balanced, Options(TierFallback.Lower));

        Assert.NotNull(resolution);
        Assert.Equal("claude-sonnet-5-5", resolution.Model.ModelId);
        Assert.False(resolution.Substituted);
    }

    [Fact]
    public void Resolve_Fallback_SkipsEveryEmptyRungUntilOneHoldsAModel()
    {
        // Google has only the two ends placed: the rungs between them are empty.
        var lower = ModelCatalog.Resolve("Google", CapabilityTier.Advanced, Options(TierFallback.Lower));
        var higher = ModelCatalog.Resolve("Google", CapabilityTier.Balanced, Options(TierFallback.Higher));

        Assert.Equal("gemini-4-hydrogen", lower?.Model.ModelId);
        Assert.True(lower?.Substituted);
        Assert.Equal("gemini-4-carbon", higher?.Model.ModelId);
        Assert.True(higher?.Substituted);
    }

    [Fact]
    public void Resolve_NeverReturnsARestrictedModel()
    {
        var frontier = ModelCatalog.Resolve("Anthropic", CapabilityTier.Frontier, Options());

        Assert.NotNull(frontier);
        Assert.DoesNotContain("mythos", frontier.Model.ModelId);
        Assert.All(ModelCatalog.Anthropic.Values.Where(m => m.Line == "mythos"),
            m => Assert.Equal(ModelAvailability.Restricted, m.Availability));
    }

    [Fact]
    public void Resolve_Alias_ReadsProviderAndRung_CaseInsensitively()
    {
        Assert.Equal("claude-sonnet-5-5", ModelCatalog.Resolve("anthropic:balanced", Options())?.Model.ModelId);
        Assert.Equal("gpt-6.2-astra", ModelCatalog.Resolve("OpenAI:FRONTIER", Options())?.Model.ModelId);
    }

    [Fact]
    public void Resolve_Alias_AlsoTakesALineName_AndReportsItsRung()
    {
        var resolution = ModelCatalog.Resolve("openai:sol", Options());

        Assert.NotNull(resolution);
        Assert.Equal("gpt-6.2-sol", resolution.Model.ModelId);
        Assert.Equal(CapabilityTier.Advanced, resolution.Tier);
        Assert.Equal("openai:sol", resolution.Request);
    }

    [Theory]
    [InlineData("balanced")]
    [InlineData(":balanced")]
    [InlineData("anthropic:")]
    [InlineData("")]
    public void Resolve_Alias_AMalformedAlias_Throws(string alias) =>
        Assert.Throws<ArgumentException>(() => ModelCatalog.Resolve(alias, Options()));

    [Theory]
    [InlineData("nobody:balanced")]
    [InlineData("anthropic:turbo")]
    public void Resolve_Alias_AWellFormedAliasTheCatalogDoesNotKnow_IsNull(string alias) =>
        Assert.Null(ModelCatalog.Resolve(alias, Options()));

    [Fact]
    public void Resolve_WithAvailableIds_ChoosesTheLatestAmongThose()
    {
        // The account serves the older Sonnet and the current Opus only.
        IReadOnlyCollection<string> live = ["claude-sonnet-4-6", "claude-opus-5-5"];

        Assert.Equal("claude-sonnet-4-6", ModelCatalog.Resolve("Anthropic", CapabilityTier.Balanced, Options(available: live))?.Model.ModelId);
        Assert.Equal("claude-opus-5-5", ModelCatalog.Resolve("Anthropic", CapabilityTier.Advanced, Options(available: live))?.Model.ModelId);
        Assert.Null(ModelCatalog.Resolve("Anthropic", CapabilityTier.Entry, Options(available: live)));
    }

    [Fact]
    public void Resolve_WithAvailableIds_ReportsTheLiveIdForASnapshotOfTheModel()
    {
        IReadOnlyCollection<string> live = ["claude-sonnet-4-6-20260101"];

        var resolution = ModelCatalog.Resolve("Anthropic", CapabilityTier.Balanced, Options(available: live));

        Assert.NotNull(resolution);
        Assert.Equal("claude-sonnet-4-6", resolution.Model.ModelId); // the catalog row: its prices and limits
        Assert.Equal("claude-sonnet-4-6-20260101", resolution.Id);   // the id the account serves: what to send
    }

    [Fact]
    public void Resolve_WithAvailableIds_ADifferentModelWithTheSamePrefixIsNotTheSameModel()
    {
        // A word, a longer number, a one-digit suffix and a bare separator are not a snapshot date.
        IReadOnlyCollection<string> live = ["claude-sonnet-4-6-turbo", "claude-sonnet-4-60", "claude-sonnet-4-6-2", "claude-sonnet-4-6-"];

        Assert.Null(ModelCatalog.Resolve("Anthropic", CapabilityTier.Balanced, Options(available: live)));
    }

    [Fact]
    public void Resolve_ADeprecatedModelIsNotTheLatest_OnceItsDateHasCome()
    {
        // claude-sonnet-4-5 is deprecated from 2026-09-30: before that day it is a live candidate, after it is not.
        IReadOnlyCollection<string> live = ["claude-sonnet-4-5"];

        Assert.Equal("claude-sonnet-4-5",
            ModelCatalog.Resolve("Anthropic", CapabilityTier.Balanced, new ResolveOptions { Available = live, AsOf = new DateOnly(2026, 9, 1) })?.Model.ModelId);
        Assert.Null(ModelCatalog.Resolve("Anthropic", CapabilityTier.Balanced, new ResolveOptions { Available = live, AsOf = new DateOnly(2026, 10, 1) }));
    }

    // ── List ──────────────────────────────────────────────────────────────────

    [Fact]
    public void List_LatestPerTier_HoldsOneModelPerRung_LowestFirst()
    {
        var anthropic = ModelCatalog.List(new ModelQuery { Scope = ModelListScope.LatestPerTier, Provider = "Anthropic", AsOf = AsOf });

        Assert.Equal(["claude-haiku-5-5", "claude-sonnet-5-5", "claude-opus-5-5", "claude-fable-5-2"],
            anthropic.Select(m => m.ModelId));
        Assert.Equal([CapabilityTier.Entry, CapabilityTier.Balanced, CapabilityTier.Advanced, CapabilityTier.Frontier],
            anthropic.Select(m => m.Tier!.Value));
    }

    [Fact]
    public void List_LatestPerTier_AcrossProviders_HoldsEachProvidersRungs()
    {
        var all = ModelCatalog.List(new ModelQuery { Scope = ModelListScope.LatestPerTier, AsOf = AsOf });

        foreach (var provider in all.GroupBy(m => m.Provider))
            Assert.Equal(provider.Count(), provider.Select(m => m.Tier).Distinct().Count());
        Assert.Contains(all, m => m.ModelId == "gpt-6.2-sol");
        Assert.Contains(all, m => m.ModelId == "gemini-4-carbon");
        Assert.DoesNotContain(all, m => m.Tier is null);
    }

    [Fact]
    public void List_All_AddsTheOlderVersionsOfALine_OfTheSameLifecycleState()
    {
        var sonnet = ModelCatalog.List(new ModelQuery { Scope = ModelListScope.All, Provider = "Anthropic", AsOf = AsOf })
            .Where(m => m.Line == "sonnet").Select(m => m.ModelId).ToList();

        Assert.Contains("claude-sonnet-5-5", sonnet);
        Assert.Contains("claude-sonnet-5", sonnet);
        Assert.Contains("claude-sonnet-4-6", sonnet);
        Assert.DoesNotContain("claude-sonnet-4-5", sonnet); // deprecated from 2026-09-30
        // newest first within a line
        Assert.True(sonnet.IndexOf("claude-sonnet-5-5") < sonnet.IndexOf("claude-sonnet-5"));
        Assert.True(sonnet.IndexOf("claude-sonnet-5") < sonnet.IndexOf("claude-sonnet-4-6"));
    }

    [Fact]
    public void List_All_ShowsDeprecatedAndRetiredModelsOnlyWhenAsked()
    {
        IEnumerable<string> Ids(bool deprecated, bool retired) =>
            ModelCatalog.List(new ModelQuery
            {
                Scope = ModelListScope.All, Provider = "Anthropic", AsOf = AsOf,
                IncludeDeprecated = deprecated, IncludeRetired = retired,
            }).Select(m => m.ModelId).ToList();

        Assert.DoesNotContain("claude-sonnet-4-5", Ids(false, false));
        Assert.Contains("claude-sonnet-4-5", Ids(true, false));
        Assert.DoesNotContain("claude-opus-4-1", Ids(true, false)); // retired from 2026-08-05
        Assert.Contains("claude-opus-4-1", Ids(true, true));
    }

    [Fact]
    public void List_RestrictedModelsAreListedOnlyByAllAndOnlyWhenAsked()
    {
        var withoutRestricted = ModelCatalog.List(new ModelQuery { Scope = ModelListScope.All, Provider = "Anthropic", AsOf = AsOf });
        var withRestricted = ModelCatalog.List(new ModelQuery { Scope = ModelListScope.All, Provider = "Anthropic", AsOf = AsOf, IncludeRestricted = true });
        var latest = ModelCatalog.List(new ModelQuery { Scope = ModelListScope.LatestPerTier, Provider = "Anthropic", AsOf = AsOf, IncludeRestricted = true });

        Assert.DoesNotContain(withoutRestricted, m => m.Line == "mythos");
        Assert.Contains(withRestricted, m => m.Line == "mythos");
        Assert.DoesNotContain(latest, m => m.Line == "mythos"); // a model few can call is never "the latest of a tier"
    }

    [Fact]
    public void List_LatestPerTier_NeverTakesARestrictedModel_EvenWhenItIsTheOnlyOneTheAccountServes()
    {
        // Fable is not served here, so the frontier rung would be Mythos if a restricted model could be the latest.
        var listed = ModelCatalog.List(new ModelQuery
        {
            Scope = ModelListScope.LatestPerTier, Provider = "Anthropic", AsOf = AsOf, IncludeRestricted = true,
            Available = ["claude-mythos-5-1", "claude-haiku-5-5"],
        });

        Assert.Equal(["claude-haiku-5-5"], listed.Select(m => m.ModelId));
        Assert.Null(ModelCatalog.Resolve("Anthropic", CapabilityTier.Frontier,
            new ResolveOptions { AsOf = AsOf, Available = ["claude-mythos-5-1"] }));
    }

    [Fact]
    public void List_All_HidesPreviewModelsUnlessAsked()
    {
        var google = (bool preview) => ModelCatalog.List(new ModelQuery
        {
            Scope = ModelListScope.All, Provider = "Google", AsOf = AsOf, IncludePreview = preview, IncludeDeprecated = true, IncludeRetired = true,
        }).Select(m => m.ModelId).ToList();

        Assert.DoesNotContain("gemini-3.1-pro-preview", google(false));
        Assert.Contains("gemini-3.1-pro-preview", google(true));
    }

    [Fact]
    public void List_All_KeepsUnclassifiedModels_ButLatestPerTierDoesNot()
    {
        var all = ModelCatalog.List(new ModelQuery { Scope = ModelListScope.All, Provider = "OpenAI", AsOf = AsOf }).ToList();
        var latest = ModelCatalog.List(new ModelQuery { Scope = ModelListScope.LatestPerTier, Provider = "OpenAI", AsOf = AsOf }).ToList();

        Assert.Contains(all, m => m.ModelId == "gpt-5.5" && m.Tier is null);
        Assert.DoesNotContain(latest, m => m.ModelId == "gpt-5.5");
    }

    [Fact]
    public void List_FiltersByModelType_ChatByDefault()
    {
        var chat = ModelCatalog.List(new ModelQuery { Scope = ModelListScope.All, Provider = "OpenAI", AsOf = AsOf });
        var any = ModelCatalog.List(new ModelQuery { Scope = ModelListScope.All, Provider = "OpenAI", AsOf = AsOf, Type = null });

        Assert.All(chat, m => Assert.Equal(ModelType.Chat, m.ModelType));
        Assert.True(any.Count > chat.Count);
    }

    [Fact]
    public void List_WithAvailableIds_KeepsOnlyTheModelsTheAccountServes()
    {
        var listed = ModelCatalog.List(new ModelQuery
        {
            Scope = ModelListScope.LatestPerTier, Provider = "Anthropic", AsOf = AsOf,
            Available = ["claude-sonnet-4-6", "claude-opus-5-5", "claude-opus-4-8"],
        });

        Assert.Equal(["claude-sonnet-4-6", "claude-opus-5-5"], listed.Select(m => m.ModelId));
    }

    [Fact]
    public void ModelQuery_ScopeIsRequired_SoNoDefaultHidesOldVersions()
    {
        var scope = typeof(ModelQuery).GetProperty(nameof(ModelQuery.Scope))!;

        Assert.NotNull(scope.GetCustomAttribute<RequiredMemberAttribute>());
    }

    [Fact]
    public void FindModel_DoesNotResolveATierOrALineName()
    {
        // The price lookup stays on a concrete id: a fuzzy "latest" would price a newer model as an older one.
        Assert.Null(ModelCatalog.FindModel("claude-sonnet", AliasMatchType.Exact));
        Assert.Null(ModelCatalog.FindModel("balanced", AliasMatchType.Exact));
    }

    // ── Names ─────────────────────────────────────────────────────────────────

    [Fact]
    public void TheRungNames_AreTheFiveOfTheLmSeries_InOrder()
    {
        Assert.Equal(["entry", "basic", "balanced", "advanced", "frontier"],
            Enum.GetValues<CapabilityTier>().OrderBy(t => (int)t).Select(t => t.Alias()));
    }

    [Theory]
    [InlineData("entry", CapabilityTier.Entry)]
    [InlineData("Balanced", CapabilityTier.Balanced)]
    [InlineData("FRONTIER", CapabilityTier.Frontier)]
    public void TryParse_ReadsAnAlias(string text, CapabilityTier expected)
    {
        Assert.True(CapabilityTierExtensions.TryParse(text, out var tier));
        Assert.Equal(expected, tier);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sonnet")]
    [InlineData("1")] // a number is not an alias
    public void TryParse_RefusesAnythingElse(string text) =>
        Assert.False(CapabilityTierExtensions.TryParse(text, out _));

    // ── The catalog data the rule reads ───────────────────────────────────────

    private static IEnumerable<string> ProvidersWithLines() =>
        ModelCatalog.GetProviderNames().Where(p => ModelCatalog.GetLines(p).Count > 0);

    [Fact]
    public void Lines_ExistForTheThreeVendorsWhoseTiersAreKnown()
    {
        Assert.Equal(["Anthropic", "Google", "OpenAI"], ProvidersWithLines().Order(StringComparer.Ordinal));
    }

    [Fact]
    public void EveryLineOfAProvidersTable_HasAtLeastOneRow()
    {
        foreach (var provider in ProvidersWithLines())
            foreach (var line in ModelCatalog.GetLines(provider).Keys)
                Assert.Contains(ModelCatalog.GetByProvider(provider), m => m.Line == line);
    }

    [Fact]
    public void ARowWithALine_HasAVersion_AndARowWithoutOneHasNoLine()
    {
        foreach (var model in ModelCatalog.All.Values)
        {
            Assert.Equal(model.Line is not null, model.Version is not null);
            Assert.Equal(model.Line is not null && ModelCatalog.GetLines(model.Provider!).TryGetValue(model.Line, out _), model.Tier is not null);
        }
    }

    [Fact]
    public void EachRung_HasAtMostOneGeneralLine_PerProvider()
    {
        // "The latest model of a rung" compares versions inside one line. Two general lines on a rung would make the
        // answer depend on which version number is bigger - a thing two vendors' lines do not mean.
        foreach (var provider in ProvidersWithLines())
        {
            var general = ModelCatalog.GetByProvider(provider)
                .Where(m => m.Tier is not null && m.Availability == ModelAvailability.General)
                .GroupBy(m => m.Tier!.Value, m => m.Line!);

            foreach (var rung in general)
                Assert.True(rung.Distinct().Count() == 1,
                    $"{provider} {rung.Key.Alias()} holds general lines {string.Join(", ", rung.Distinct())}");
        }
    }

    private static readonly Dictionary<string, Regex> IdPatterns = new()
    {
        // claude-<line>-<major>[-<minor>] and claude-<major>[-<minor>]-<line>-<yyyymmdd>
        ["Anthropic"] = new(@"^claude-(?:(?<line>haiku|sonnet|opus|fable|mythos)-(?<major>\d+)(?:-(?<minor>\d{1,2}))?|(?<major>\d)(?:-(?<minor>\d))?-(?<line>haiku|sonnet|opus)-\d{8})$",
            RegexOptions.CultureInvariant),
        ["OpenAI"] = new(@"^gpt-(?<major>\d+)(?:\.(?<minor>\d+))?-(?<line>luna|terra|sol|astra)$", RegexOptions.CultureInvariant),
        ["Google"] = new(@"^gemini-(?<major>\d+)-(?<line>hydrogen|neon|carbon|argon)$", RegexOptions.CultureInvariant),
    };

    [Fact]
    public void LineAndVersion_WrittenInTheData_EqualWhatTheIdSays_WhereTheIdSaysIt()
    {
        // The data is written by hand and never derived from the id (an id says nothing for 45 OpenAI rows). Where an id does
        // carry a line and a version, the two must agree, so a typo in either shows here.
        foreach (var (provider, pattern) in IdPatterns)
        {
            var matched = 0;
            foreach (var model in ModelCatalog.GetByProvider(provider).Where(m => m.ModelType == ModelType.Chat))
            {
                var match = pattern.Match(model.ModelId);
                if (!match.Success)
                {
                    Assert.True(model.Line is null, $"{model.ModelId}: has line '{model.Line}' but its id does not say one");
                    continue;
                }

                matched++;
                var expected = new Version(
                    int.Parse(match.Groups["major"].Value, System.Globalization.CultureInfo.InvariantCulture),
                    match.Groups["minor"].Success ? int.Parse(match.Groups["minor"].Value, System.Globalization.CultureInfo.InvariantCulture) : 0);
                Assert.Equal(match.Groups["line"].Value, model.Line);
                Assert.Equal(expected, model.Version);
            }

            Assert.True(matched >= 4, $"{provider}: the pattern matched only {matched} ids - the pattern, not the data, is wrong");
        }
    }

    [Fact]
    public void OnlyTheProvidersWhoseTiersAreKnown_CarryLines()
    {
        // Only the three providers above carry lines; the rest stay unclassified until a vendor's tiers are known.
        Assert.All(ModelCatalog.All.Values.Where(m => !IdPatterns.ContainsKey(m.Provider!)), m => Assert.Null(m.Line));
    }

    [Fact]
    public void AReplacementInTheSameLine_IsANewerVersion()
    {
        foreach (var model in ModelCatalog.All.Values.Where(m => m.ReplacementModelId is not null && m.Line is not null))
        {
            if (!ModelCatalog.All.TryGetValue(model.ReplacementModelId!, out var replacement) || replacement.Line != model.Line)
                continue;
            Assert.True(replacement.Version > model.Version, $"{model.ModelId} is replaced by the older {replacement.ModelId}");
        }
    }

    [Fact]
    public void TheLatestOfEachRung_IsTheSnapshot_AndAMovedAliasNeedsAChangelogLine()
    {
        // An alias is a promise that follows the catalog: a catalog refresh that moves "balanced" to another model changes
        // what every caller of the alias gets. This test fails when that happens, so the move is made on purpose: update
        // tier-latest.snapshot and add a CHANGELOG line that says which alias moved and why.
        var text = new StringBuilder();
        foreach (var model in ModelCatalog.List(new ModelQuery { Scope = ModelListScope.LatestPerTier, AsOf = AsOf }))
            text.Append(model.Provider).Append('\t').Append(model.Tier!.Value.Alias()).Append('\t').Append(model.ModelId).Append('\n');

        var path = Path.Combine(AppContext.BaseDirectory, "tier-latest.snapshot");
        var expected = File.ReadAllText(path).Replace("\r\n", "\n");

        Assert.True(expected == text.ToString(),
            "The latest model of a rung changed. Update tests/TokenMeter.Tests/tier-latest.snapshot and add a CHANGELOG line." +
            $"\n--- snapshot\n{expected}\n--- catalog\n{text}");
    }
}
