# TokenMeter

[![NuGet](https://img.shields.io/nuget/v/TokenMeter.svg)](https://www.nuget.org/packages/TokenMeter)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)

LLM model metadata catalog and cost calculator for .NET.

Provides context windows, pricing, capability flags (vision, audio, reasoning, tool calling, prompt caching), and thinking/reasoning format metadata for 12+ providers including OpenAI, Anthropic, Google, xAI, Mistral, DeepSeek, and more.

## Packages

| Package | Description |
|---------|-------------|
| `TokenMeter` | Bundled model metadata catalog (`ModelCatalog`, `ModelInfo`) + cost calculation (`CostCalculator`) — no dependencies, AOT/trim compatible |

## Installation

```bash
dotnet add package TokenMeter
```

## Features

Everything is available as soon as the package is referenced — the catalog is embedded and loads on first use; there is
no DI extension or setup call. Register `ICostCalculator` yourself if you want it injected.

| Feature | Entry point | How to use |
|---------|-------------|------------|
| Model lookup by id or alias (Bedrock/Vertex prefixes, date suffixes) | `ModelCatalog.FindModel(id)` | Default: full fuzzy matching |
| Bounded / confidence-aware lookup | `ModelCatalog.FindModel(id, AliasMatchType)`, `ModelCatalog.FindModelMatch(id)` → `ModelMatch.MatchKind` | Pass `AliasMatchType.Exact` or `Prefix` to limit fuzziness |
| Catalog browsing | `ModelCatalog.All`, `ByProvider`, `GetProvider(name)`, `GetByProvider`, `GetByType(ModelType)`, `GetProviderNames()`, typed properties (`ModelCatalog.OpenAI`, `.Anthropic`, …) | Static, always on |
| Model metadata (limits, modalities, capabilities, reasoning/thinking format, tool-calling wire format) | `ModelInfo` properties | See [Model Metadata](#model-metadata) |
| Full request cost (date rates, tiers, cache reads, cache writes by lifetime, modality prices, service tier, region, tool fees) | `ModelInfo.CalculateCost(TokenCounts, CostContext)`, `ICostCalculator.CalculateCost(modelId, TokenCounts, CostContext)` | `CostContext.At(callTimeUtc)`; set `ServiceTier` / `Region` with `with { … }` |
| Per-part cost overloads | `ModelInfo.CalculateCost(input, output[, cacheRead, cacheWrite[, cacheWriteTtl]])`, `CalculateCost(input, output, PricingTierContext)` | Always on |
| Long-context / time-of-day pricing tiers | `ModelInfo.PricingTiers` + `PricingTierContext` | Applied when a context is passed (or automatically via `TokenCounts`) |
| Vendor-announced future prices | `ModelInfo.AsOf(date)` (`ScheduledPrices`) | Call `AsOf`, or set `CostContext.Date` |
| Explicit-cache storage cost | `ModelInfo.CalculateCacheStorageCost(tokens, storedFor, date)` | Models with `CacheStoragePricePerMillionPerHour` |
| Lifecycle (deprecated / retired / successor) | `ModelInfo.GetLifecycleStatus(asOf)`, `ReplacementModelId` | Anthropic, OpenAI, Google |
| Custom / self-hosted models | `CostCalculator.Default()`, `Default(AliasMatchType)`, `CustomOnly()`, `RegisterModel(ModelInfo)` | Registered models win by exact id |
| Data freshness | `ModelCatalog.LastUpdated`, `GetLastUpdated(provider)`, `DataAgeDays`, `IsDataStale(maxAgeDays = 90)` | Always on |
| Price provenance | `ModelInfo.PriceSource` (`Official` / `ThirdParty`) | See [Price Source](#price-source) |

## Quick Start

### Model Lookup

```csharp
// Find a model by ID or alias
var model = ModelCatalog.FindModel("claude-sonnet-4-6");

Console.WriteLine(model?.ContextWindow);          // 1000000
Console.WriteLine(model?.ReasoningMode);           // Optional
Console.WriteLine(model?.ThinkingFormat);          // Block
Console.WriteLine(model?.PromptCachingMode);       // Explicit
Console.WriteLine(model?.ToolCallingFormat);       // Anthropic
Console.WriteLine(model?.SupportsMcpToolUse);      // True
```

> **Note — fuzzy matching**: `FindModel` resolves aliases in 4 passes (exact → alias exact →
> prefix → contains) to absorb cloud-specific ID variants (Bedrock/Vertex prefixes, date
> suffixes). Local/self-hosted deployment names that embed a public model name (e.g.
> `deepseek-r1-distill-qwen-7b`) can therefore match a catalog entry whose context window and
> pricing do not describe your deployment. For self-hosted models, take the effective context
> length from your deployment configuration (e.g. llama.cpp `n_ctx`), not from the catalog.
>
> Fuzzy passes never cross a version token: an alias ending in a version number (`claude-sonnet-5`) does not
> match an id that continues it (`claude-sonnet-5-5`) — an unknown newer model returns `null` rather than the older
> model's row. Snapshot and deployment suffixes (`-20250929`, `-latest`, `-v1`) still match. Nor do they cross into a
> variant: an id whose remainder names `audio`, `realtime`, `tts`, `transcribe`, `search`, `image`, `embedding`, `live`,
> `translate` or `moderation` (`gpt-4o-mini-tts`) is a different product and resolves only by a row of its own.
>
> The catalog loads with source-generated JSON metadata, so it works under Native AOT and trimming.

```csharp
// When correctness matters more than recall, bound the fuzziness:
ModelCatalog.FindModel("deepseek-r1-distill-qwen-7b", AliasMatchType.Exact);   // null — no false positive
ModelCatalog.FindModel("us.anthropic.claude-sonnet-4.6-v1");                   // matched via contains alias

// Or inspect which pass produced the match and apply confidence-based fallback:
var match = ModelCatalog.FindModelMatch("deepseek-r1-distill-qwen-7b");
Console.WriteLine(match?.MatchKind);   // Prefix — a fuzzy inference, not an exact hit
Console.WriteLine(match?.Model.ModelId);
```

### Cost Calculation

One request, every part priced the way the vendor prices it — the rates of the call's date, the long-context or
time-of-day tier the request falls in (decided by its whole prompt, cache reads and writes included, and applied to the
whole request), cache reads, and each cache write at the price for its lifetime:

```csharp
var usage = new TokenCounts
{
    InputTokens = 20_000,                                   // neither read from nor written to the cache
    CacheReadTokens = 150_000,
    CacheWrites = [new CacheWriteTokenCount(TimeSpan.FromHours(1), 30_000)],   // null lifetime = the vendor default
    OutputTokens = 2_000,                                   // reasoning included
};
var requestCost = model?.CalculateCost(usage, CostContext.At(callTimeUtc));
var viaCalculator = CostCalculator.Default().CalculateCost("claude-haiku-5-5", usage, CostContext.At(callTimeUtc));

// Batch / Flex / Fast and a data-residency region multiply every token price; server-side tool calls add their fee
var batchInUs = model?.CalculateCost(
    usage with { ToolCalls = new Dictionary<string, int> { ["web_search"] = 2 } },
    CostContext.At(callTimeUtc) with { ServiceTier = ServiceTier.Batch, Region = "us" });
```

The order is: rates of the date (`AsOf`) → long-context / time-of-day tier → service-tier multiplier → regional
multiplier → per-call tool fees. A service tier the model is not offered at, or a tool with no fee in the catalog, makes
the result `null` (unknown) rather than the standard price. Tool names are the vendors' own (`web_search`, `file_search`,
`google_search`); tokens a tool adds to the context are already in the token counts, and a vendor's monthly free
allowance is not applied.

The overloads below are the same calculation for one part at a time:

```csharp
// Basic cost (input + output tokens)
var cost = model?.CalculateCost(inputTokens: 500_000, outputTokens: 200_000);

// Cost including prompt cache tokens
var costWithCache = model?.CalculateCost(
    inputTokens: 100_000,
    outputTokens: 50_000,
    cacheReadTokens: 400_000,
    cacheWriteTokens: 50_000);

// Cache writes priced by lifetime (Anthropic: 5-minute vs 1-hour writes — CacheWritePrices)
var costOneHourCache = model?.CalculateCost(100_000, 50_000, 400_000, 50_000, cacheWriteTtl: TimeSpan.FromHours(1));

// The rates in effect on a date — applies vendor-announced price changes (ScheduledPrices)
var costNextYear = model?.AsOf(new DateOnly(2027, 1, 1)).CalculateCost(500_000, 200_000);

// Via CostCalculator (DI-friendly) — the basic and cache overloads, the tiered one (see Tiered Pricing below) and TokenCounts
ICostCalculator calc = CostCalculator.Default();
var price = calc.CalculateCost("gpt-4o", inputTokens: 1_000, outputTokens: 500);
```

### Tiered Pricing

Some providers charge more than the representative rate under a specific condition — a
long-context surcharge past a prompt-length threshold (xAI), or a peak-hour surcharge during
specific UTC windows (DeepSeek). `ModelInfo.PricingTiers` carries those bands; passing a
`PricingTierContext` picks the right one automatically. Omitting the context, or a model with no
tiers, reproduces the representative-rate calculation exactly — existing callers are unaffected.
`ContextLengthTokens` is the request's **prompt** length — every input token, cache reads and writes included — which is
how the vendors decide the tier; `CalculateCost(TokenCounts, …)` computes it for you. A tier can carry its own cache
read and write prices (Claude Haiku 5.5 over 100K, Gemini 2.5 Pro / 3.1 Pro Preview over 200K).

```csharp
var model = ModelCatalog.FindModel("grok-4.6");

// Below the tier's threshold — representative rate ($2.00 / $6.00 per 1M)
var normal = model?.CalculateCost(50_000, 10_000, new PricingTierContext { ContextLengthTokens = 50_000 });

// At/above 200K context — the long-context tier applies ($4.00 / $12.00 per 1M)
var longContext = model?.CalculateCost(50_000, 10_000, new PricingTierContext { ContextLengthTokens = 250_000 });

// DeepSeek: peak-hour tier, keyed off wall-clock UTC time instead of context length
var deepseek = ModelCatalog.FindModel("deepseek-v4-pro");
var peakCost = deepseek?.CalculateCost(50_000, 10_000,
    new PricingTierContext { CallTimeUtc = TimeOnly.FromDateTime(DateTime.UtcNow) });
```

### Browsing the Catalog

```csharp
// By provider — typed convenience property
foreach (var m in ModelCatalog.Anthropic.Values)
    Console.WriteLine($"{m.ModelId}: ctx={m.ContextWindow}, ${m.InputPricePerMillion}/M");

// By provider — string-keyed (when the name is only known at runtime)
var openai = ModelCatalog.GetProvider("OpenAI");   // dict, empty if unknown

// By model type (mostly Chat, plus Gemini ImageGeneration/TextToSpeech and OpenAI TextToSpeech/SpeechToText models)
var chatModels = ModelCatalog.GetByType(ModelType.Chat);

// All providers
var providers = ModelCatalog.GetProviderNames();
```

### Custom Models

```csharp
var calc = CostCalculator.Default();
calc.RegisterModel(new ModelInfo
{
    ModelId = "my-fine-tuned-model",
    Provider = "MyCompany",
    InputPricePerMillion = 2.00m,
    OutputPricePerMillion = 8.00m,
    ContextWindow = 128_000,
    SupportsToolCalling = true,
    ToolCallingFormat = ToolCallingFormat.OpenAI
});

var cost = calc.CalculateCost("my-fine-tuned-model", 10_000, 5_000);

// A self-hosted name that embeds a public one ("qwen3-8b-local") would match the public model fuzzily.
// Bound the matching so an unregistered name is unknown instead of priced as something else:
var strict = CostCalculator.Default(AliasMatchType.Exact);
```

## Model Metadata

`ModelInfo` provides the following metadata:

### Identity
| Property | Type | Description |
|----------|------|-------------|
| `ModelId` | `string` | Canonical model identifier for API calls |
| `Provider` | `string?` | Provider name (e.g., "OpenAI", "Anthropic") |
| `DisplayName` | `string?` | Human-readable name |
| `ModelType` | `ModelType` | Chat, Embedding, Reranker, ImageGeneration, TextToSpeech, SpeechToText |
| `IsInstructTuned` | `bool` | Instruction-tuned vs. base model |

### Limits
| Property | Type | Description |
|----------|------|-------------|
| `ContextWindow` | `int?` | Maximum input tokens |
| `MaxOutputTokens` | `int?` | Maximum generated tokens per response |

### Pricing (USD / 1M tokens)
| Property | Description |
|----------|-------------|
| `InputPricePerMillion` | Standard input token price |
| `OutputPricePerMillion` | Standard output token price |
| `CacheReadPricePerMillion` | Prompt cache hit price (often 90% discount) |
| `CacheWritePricePerMillion` | Prompt cache population price |
| `ModalityPrices` | Per-token prices for a modality the vendor prices apart (`Modality` × `Use` = Input / CacheRead / Output): Gemini Flash audio input and audio cache reads, Gemini image models' image output. Applied to `TokenCounts.InputTokensByModality` / `CacheReadTokensByModality` / `OutputTokensByModality` |
| `PricingTiers` | Non-representative price bands (context-length or time-of-day) — see [Tiered Pricing](#tiered-pricing) |
| `CacheWritePrices` | Cache-write price per cache lifetime (`Ttl` → price) where the vendor prices them apart — Anthropic 5-minute and 1-hour writes; `GetCacheWritePrice(ttl)` |
| `ServiceTierMultipliers` | Batch / Flex / Fast multipliers on every token price (OpenAI, Anthropic). A tier not listed is not offered |
| `RegionalMultipliers` | Data-residency surcharges on every token price (Anthropic `inference_geo: "us"` 1.1x on Claude 4.6+) |
| `ToolCallPrices` | Per-call fees for server-side tools (web search, file search, Gemini Search grounding) |
| `CacheStoragePricePerMillionPerHour` | Explicit-cache storage per 1M tokens per hour (Gemini context caching); `CalculateCacheStorageCost(tokens, storedFor, date)` prices a cache's lifetime apart from the requests that read it |
| `ScheduledPrices` | Vendor-announced price changes with an `EffectiveFrom` date; `AsOf(date)` returns the model with the rates in effect then. Past prices are not kept — record a call's cost when it is made |

> **Note — these fields hold the representative rate**: the price outside of any
> [pricing tier](#tiered-pricing) a model carries. Cache-write cost falls back to the input rate
> when a provider does not price it separately, which matches how automatic prompt caching is
> normally billed.
>
> **Reasoning tokens** are billed at the output rate by OpenAI, Anthropic and Google — pass them as output tokens.
> OpenAI (`output_tokens` includes `reasoning_tokens`) and Anthropic (thinking is part of `output_tokens`) already count
> them there; Gemini reports them apart (`thoughtsTokenCount`), so its output is `candidatesTokenCount + thoughtsTokenCount`.
> Prices are in the vendor's currency (USD).

### Lifecycle
| Property | Description |
|----------|-------------|
| `DeprecationDate` | When the vendor deprecated the model on its own API (`null` = not deprecated) |
| `RetirementDate` | When the vendor stops serving it — requests fail from then on |
| `ReplacementModelId` | The vendor's recommended successor, as the vendor names it (resolves through `ModelCatalog.FindModel`) |

`GetLifecycleStatus(asOf)` returns `Active` / `Deprecated` / `Retired` for a date, computed from the dates so an older
package still answers correctly. Filled for Anthropic, OpenAI and Google from their deprecation pages; partner clouds
(Bedrock, Vertex AI, Azure) keep their own schedules and are not covered. Google publishes a shutdown date for a generally
available model from its release (the earliest date it may be shut down), so a Google model can carry a `RetirementDate`
without a `DeprecationDate`; a model whose requests Google routes to its successor carries only `ReplacementModelId`.

### Input Modalities
| Property | Description |
|----------|-------------|
| `SupportsImageInput` | Accepts image data |
| `SupportsAudioInput` | Accepts audio data |
| `SupportsVideoInput` | Accepts video data |
| `SupportsDocumentInput` | Accepts PDF/document files natively |

### API Capabilities
| Property | Description |
|----------|-------------|
| `SupportsToolCalling` | Tool/function calling |
| `SupportsParallelToolCalling` | Multiple tools per turn |
| `SupportsStructuredOutput` | JSON Schema-enforced output |
| `SupportsJsonMode` | JSON-guided output (soft) |
| `SupportsStreaming` | SSE streaming |
| `SupportsStopSequences` | Stop sequences |
| `PromptCachingMode` | None / Explicit / Automatic |
| `SupportsMcpToolUse` | Native MCP tool support |

### Reasoning & Thinking
| Property | Description |
|----------|-------------|
| `ReasoningMode` | None / Optional / Always |
| `ThinkingFormat` | None / Block / InlineTag / SeparateField |
| `ThinkingTagPattern` | Tag pattern (e.g., `<think>...</think>`) for InlineTag format |
| `ThinkingFieldName` | Field name (e.g., `reasoning_content`) for SeparateField format |
| `SupportsInterleavedThinking` | Reasoning between tool calls |
| `MaxThinkingTokens` | Maximum reasoning budget |

### Tool Calling Wire Format
| Value | Description |
|-------|-------------|
| `ToolCallingFormat.OpenAI` | `tool_calls` / `tool` role (default) |
| `ToolCallingFormat.Anthropic` | `tool_use` / `tool_result` content blocks |
| `ToolCallingFormat.Gemini` | Google Gemini format |

## Supported Providers

| Provider | Models |
|----------|--------|
| OpenAI | GPT-6, GPT-5.x, GPT-4.1, GPT-4o, o1, o3, o4-mini series |
| Anthropic | Claude 5, 4.x, 3.x families (Fable, Mythos, Opus, Sonnet, Haiku) |
| Google | Gemini 3.x, 2.5, 2.0, 1.5 families |
| xAI | Grok 4.x and Grok Build (legacy Grok 3/4 ids resolve to the model xAI serves them as) |
| Azure | Azure OpenAI equivalents |
| Mistral | Large, Medium, Small, Ministral, Codestral, Devstral, Magistral, Pixtral, GLM-5.3 (hosted) |
| DeepSeek | V4.1 Flash, V4 Pro, and the retired R1 (reasoning), V3, Coder |
| Amazon Nova | Premier, Pro, Lite, Micro |
| Cohere | Command A, R+, R, R7B |
| Meta Llama | Maverick, Scout |
| Perplexity | Sonar Pro, Deep Research, Reasoning |
| Qwen | Max, Plus, Turbo |

## Data Freshness

```csharp
Console.WriteLine(ModelCatalog.LastUpdated);        // e.g. 2026-10-10
Console.WriteLine(ModelCatalog.DataAgeDays);        // days since last update
Console.WriteLine(ModelCatalog.IsDataStale());      // true if > 90 days old
```

`LastUpdated` is derived from the `lastUpdated` field each bundled provider file declares, and
reports the **most recent** of them. Providers are refreshed independently, so an individual
provider's data can be considerably older than this value — read that provider's own date with
`ModelCatalog.GetLastUpdated("Google")` (null for an unknown provider).

> **Note — what the signal does and does not tell you**: it reports when this catalog was last
> refreshed, not whether a provider has changed its prices since. A vendor can cut a rate the day
> after a refresh, and `IsDataStale()` will still answer `false` while the bundled figure is wrong.
> Treat catalog pricing as a good default for estimation and budgeting, and read authoritative
> figures from your provider's billing data when they have to be exact.

## Price Source

```csharp
var model = ModelCatalog.FindModel("llama-4-maverick")!;
Console.WriteLine(model.PriceSource); // ThirdParty
```

`ModelInfo.PriceSource` reports how a model's price fields were obtained:

- `Official` (default) — verified directly against the vendor's own published rate card.
- `ThirdParty` — the vendor's own rate card could not be read directly (a client-side-rendered
  pricing page, a docs-only landing page, or no direct per-token price published at all), so the
  figure was cross-referenced from a third-party aggregator or another vendor's official rate card
  instead. Currently applies to **Meta Llama** (Maverick, Scout) and **Cohere** Command A — see
  [docs/pricing-update-guide.md](docs/pricing-update-guide.md) Version History for the source used per provider. Treat these as
  estimates for cost-sensitive accounting.

## Migration from 0.3.x

The following APIs were removed in 0.4.0:

- `TokenMeter.Abstractions` package (removed entirely)
- `ITokenCounter`, `TokenCounter` — use your own tokenizer library
- `IUsageTracker`, `UsageTracker`, `UsageRecord`, `UsageStatistics` — implement in your application
- `ModelPricing` → replaced by `ModelInfo`
- `ModelPricingData` → replaced by `ModelCatalog`
- `ICostCalculator.GetPricing()` → `GetModel()`
- `ICostCalculator.RegisterPricing()` → `RegisterModel()`

## Requirements

- .NET 10.0 or later

## License

MIT
