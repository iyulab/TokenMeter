using System.Text.Json;

namespace TokenMeter.Tests;

/// <summary>
/// Tokens a vendor prices by modality — audio input and audio cache reads on Gemini Flash models, image output on Gemini
/// image models — priced at that modality's rate inside the one request cost.
/// </summary>
public class ModalityPriceTests
{
    [Fact]
    public void AudioInput_IsPricedAtTheAudioRate_TheRestAtTheTextRate()
    {
        var flash = ModelCatalog.FindModel("gemini-2.5-flash", AliasMatchType.Exact);
        Assert.NotNull(flash);

        // 1M input of which 400K audio: 600K x 0.30 + 400K x 1.00 = 0.58; 1M cache reads of which 500K audio:
        // 500K x 0.03 + 500K x 0.10 = 0.065.
        var usage = new TokenCounts
        {
            InputTokens = 1_000_000,
            InputTokensByModality = new Dictionary<TokenModality, int> { [TokenModality.Audio] = 400_000, [TokenModality.Text] = 600_000 },
            CacheReadTokens = 1_000_000,
            CacheReadTokensByModality = new Dictionary<TokenModality, int> { [TokenModality.Audio] = 500_000 },
        };

        Assert.Equal(0.58m + 0.065m, flash.CalculateCost(usage));
        // Without the breakdown the same request is priced at the text rate — the under-count the breakdown removes.
        Assert.Equal(0.30m + 0.03m, flash.CalculateCost(usage with { InputTokensByModality = null, CacheReadTokensByModality = null }));
    }

    [Fact]
    public void ImageOutput_IsPricedAtTheImageRate()
    {
        var image = ModelCatalog.FindModel("gemini-3.1-flash-image", AliasMatchType.Exact);
        Assert.NotNull(image);
        Assert.Equal(ModelType.ImageGeneration, image.ModelType);

        // 1,120 image tokens (one 1K image) + 200 text/thinking tokens: 1120 x 60 + 200 x 3 per 1M.
        var usage = new TokenCounts
        {
            OutputTokens = 1_320,
            OutputTokensByModality = new Dictionary<TokenModality, int> { [TokenModality.Image] = 1_120 },
        };

        Assert.Equal(1_120 * 60m / 1_000_000m + 200 * 3m / 1_000_000m, image.CalculateCost(usage));
    }

    [Fact]
    public void ModalityPrice_SitsUnderTheServiceTierMultiplier()
    {
        var model = new ModelInfo
        {
            ModelId = "m",
            InputPricePerMillion = 1m,
            OutputPricePerMillion = 2m,
            ModalityPrices = [new ModalityPrice { Modality = TokenModality.Audio, Use = TokenUse.Input, PricePerMillion = 3m }],
            ServiceTierMultipliers = [new ServiceTierMultiplier { Tier = ServiceTier.Batch, Multiplier = 0.5m }],
        };
        var usage = new TokenCounts
        {
            InputTokens = 1_000_000,
            InputTokensByModality = new Dictionary<TokenModality, int> { [TokenModality.Audio] = 1_000_000 },
        };

        Assert.Equal(3m, model.CalculateCost(usage));
        Assert.Equal(1.5m, model.CalculateCost(usage, new CostContext { ServiceTier = ServiceTier.Batch }));
    }

    [Fact]
    public void AModalityTheModelDoesNotPriceApart_StaysAtTheBaseRate()
    {
        var pro = ModelCatalog.FindModel("gemini-2.5-pro", AliasMatchType.Exact);
        Assert.NotNull(pro);
        // 100K tokens, under the 200K tier: 100K x 1.25.
        var usage = new TokenCounts
        {
            InputTokens = 100_000,
            InputTokensByModality = new Dictionary<TokenModality, int> { [TokenModality.Audio] = 100_000 },
        };

        Assert.Equal(0.125m, pro.CalculateCost(usage));
    }

    [Fact]
    public void ABreakdownLargerThanItsTotal_IsRejected()
    {
        var flash = ModelCatalog.FindModel("gemini-2.5-flash", AliasMatchType.Exact)!;

        Assert.Throws<ArgumentException>(() => flash.CalculateCost(new TokenCounts
        {
            OutputTokens = 10,
            OutputTokensByModality = new Dictionary<TokenModality, int> { [TokenModality.Image] = 11 },
        }));
        Assert.Throws<ArgumentOutOfRangeException>(() => flash.CalculateCost(new TokenCounts
        {
            InputTokens = 10,
            InputTokensByModality = new Dictionary<TokenModality, int> { [TokenModality.Audio] = -1 },
        }));
    }

    [Fact]
    public void TtsAndImageIds_ResolveToTheirOwnRows_NotTheChatModelTheyContain()
    {
        // Before these rows existed, "gemini-3.8-flash-tts" matched gemini-3.8-flash's contains alias and was priced as
        // chat output ($3.75 instead of $9.00 audio).
        Assert.Equal(9.00m, ModelCatalog.FindModel("gemini-3.8-flash-tts")?.OutputPricePerMillion);
        Assert.Equal(ModelType.TextToSpeech, ModelCatalog.FindModel("gemini-3.8-flash-tts")?.ModelType);
        Assert.Equal("gemini-3.1-flash-lite-image", ModelCatalog.FindModel("gemini-3.1-flash-lite-image")?.ModelId);
        Assert.Equal(18.00m, ModelCatalog.FindModel("gemini-3.8-flash-tts")!.AsOf(new DateOnly(2027, 1, 1)).OutputPricePerMillion);
    }

    [Fact]
    public void GeminiFlashModels_CarryTheirAudioRates()
    {
        // ai.google.dev pricing page (Standard), 2026-10-10: (audio input, audio cache read).
        var expected = new Dictionary<string, (decimal, decimal)>
        {
            ["gemini-2.5-flash"] = (1.00m, 0.10m), ["gemini-2.5-flash-lite"] = (0.30m, 0.03m),
            ["gemini-3-flash-preview"] = (1.00m, 0.10m), ["gemini-3.1-flash-lite"] = (0.50m, 0.05m),
        };
        foreach (var (id, (input, read)) in expected)
        {
            var prices = ModelCatalog.FindModel(id, AliasMatchType.Exact)?.ModalityPrices ?? [];
            Assert.Equal(input, prices.Single(p => p is { Modality: TokenModality.Audio, Use: TokenUse.Input }).PricePerMillion);
            Assert.Equal(read, prices.Single(p => p is { Modality: TokenModality.Audio, Use: TokenUse.CacheRead }).PricePerMillion);
        }
    }

    [Fact]
    public void EveryModalityPriceInTheJson_Parses()
    {
        var problems = new List<string>();
        var asm = typeof(ModelCatalog).Assembly;
        foreach (var file in asm.GetManifestResourceNames().Where(n => n.EndsWith(".json", StringComparison.Ordinal)))
        {
            using var stream = asm.GetManifestResourceStream(file)!;
            using var doc = JsonDocument.Parse(stream);
            foreach (var model in doc.RootElement.GetProperty("models").EnumerateArray())
            {
                if (!model.TryGetProperty("modalityPrices", out var uses)) continue;
                var id = model.GetProperty("modelId").GetString()!;
                var declared = uses.EnumerateObject().Sum(u => u.Value.EnumerateObject().Count());
                var loaded = ModelCatalog.FindModel(id, AliasMatchType.Exact)?.ModalityPrices?.Count ?? 0;
                if (declared != loaded)
                    problems.Add($"{file} {id}: {declared} declared, {loaded} loaded");
            }
        }

        Assert.Empty(problems);
    }
}

/// <summary>
/// A speech, transcription, search or image product whose id contains a chat model's name is not priced as that chat model.
/// </summary>
public class VariantIdMatchingTests
{
    [Theory]
    [InlineData("gpt-4o-audio-preview")]
    [InlineData("gpt-4o-realtime-preview")]
    [InlineData("gpt-4o-search-preview")]
    [InlineData("gpt-5.5-audio")]
    [InlineData("gemini-2.5-flash-live-preview")]
    public void AVariantId_DoesNotResolveToTheChatModelItContains(string id)
        => Assert.Null(ModelCatalog.FindModel(id));

    [Theory]
    [InlineData("gpt-4o-2024-08-06", "gpt-4o")]
    [InlineData("gpt-4o-mini", "gpt-4o-mini")]
    [InlineData("gpt-4o-mini-tts", "gpt-4o-mini-tts")]
    [InlineData("gpt-4o-transcribe", "gpt-4o-transcribe")]
    [InlineData("gemini-3.8-flash-tts", "gemini-3.8-flash-tts")]
    public void SnapshotsAndRowsOfTheirOwn_StillResolve(string id, string expected)
        => Assert.Equal(expected, ModelCatalog.FindModel(id)?.ModelId);
}

/// <summary>OpenAI's realtime, audio, speech, transcription and search models carry their own prices.</summary>
public class OpenAIVariantRowTests
{
    [Fact]
    public void Realtime_PricesAudioAndImageApartFromText()
    {
        var realtime = ModelCatalog.FindModel("gpt-realtime-2.1", AliasMatchType.Exact);
        Assert.NotNull(realtime);

        // 1M input: 600K text x 4 + 400K audio x 32; 100K output audio x 64 + 100K text x 24 (developers.openai.com pricing).
        var usage = new TokenCounts
        {
            InputTokens = 1_000_000,
            InputTokensByModality = new Dictionary<TokenModality, int> { [TokenModality.Audio] = 400_000 },
            OutputTokens = 200_000,
            OutputTokensByModality = new Dictionary<TokenModality, int> { [TokenModality.Audio] = 100_000 },
        };

        Assert.Equal(2.4m + 12.8m + 6.4m + 2.4m, realtime.CalculateCost(usage));
    }

    [Theory]
    [InlineData("gpt-4o-mini-tts", ModelType.TextToSpeech, 0.60, 12.00)]
    [InlineData("gpt-4o-transcribe", ModelType.SpeechToText, 2.50, 10.00)]
    [InlineData("gpt-4o-mini-transcribe", ModelType.SpeechToText, 1.25, 5.00)]
    [InlineData("gpt-audio", ModelType.Chat, 2.50, 10.00)]
    public void SpeechTranscriptionAndSearchModels_HaveTheirOwnRates(string id, ModelType type, double input, double output)
    {
        var model = ModelCatalog.FindModel(id, AliasMatchType.Exact);
        Assert.NotNull(model);
        Assert.Equal(type, model.ModelType);
        Assert.Equal((decimal)input, model.InputPricePerMillion);
        Assert.Equal((decimal)output, model.OutputPricePerMillion);
    }
}
