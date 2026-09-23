# TokenMeter

[![NuGet](https://img.shields.io/nuget/v/TokenMeter.svg)](https://www.nuget.org/packages/TokenMeter)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)

LLM model metadata catalog and cost calculator for .NET.

Provides context windows, pricing, capability flags (vision, audio, reasoning, tool calling, prompt caching), and thinking/reasoning format metadata for 12+ providers including OpenAI, Anthropic, Google, xAI, Mistral, DeepSeek, and more.

## Packages

| Package | Description |
|---------|-------------|
| `TokenMeter` | Full model catalog + cost calculation |

## Installation

```bash
dotnet add package TokenMeter
```

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

```csharp
// Basic cost (input + output tokens)
var cost = model?.CalculateCost(inputTokens: 500_000, outputTokens: 200_000);

// Cost including prompt cache tokens
var costWithCache = model?.CalculateCost(
    inputTokens: 100_000,
    outputTokens: 50_000,
    cacheReadTokens: 400_000,
    cacheWriteTokens: 50_000);

// Via CostCalculator (DI-friendly) — same overloads, plus a tiered one (see Tiered Pricing below)
ICostCalculator calc = CostCalculator.Default();
var price = calc.CalculateCost("gpt-4o", inputTokens: 1_000, outputTokens: 500);
```

### Tiered Pricing

Some providers charge more than the representative rate under a specific condition — a
long-context surcharge past a prompt-length threshold (xAI), or a peak-hour surcharge during
specific UTC windows (DeepSeek). `ModelInfo.PricingTiers` carries those bands; passing a
`PricingTierContext` picks the right one automatically. Omitting the context, or a model with no
tiers, reproduces the representative-rate calculation exactly — existing callers are unaffected.

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

// By model type (the built-in catalog currently contains Chat models only)
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
| `ImageInputPrice` | Per-image input cost |
| `AudioInputPricePerSecond` | Audio input cost per second |
| `PricingTiers` | Non-representative price bands (context-length or time-of-day) — see [Tiered Pricing](#tiered-pricing) |

> **Note — these fields hold the representative rate**: the price outside of any
> [pricing tier](#tiered-pricing) a model carries. Cache-write cost falls back to the input rate
> when a provider does not price it separately, which matches how automatic prompt caching is
> normally billed.

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
Console.WriteLine(ModelCatalog.LastUpdated);        // 2026-08-01
Console.WriteLine(ModelCatalog.DataAgeDays);        // days since last update
Console.WriteLine(ModelCatalog.IsDataStale());      // true if > 90 days old
```

`LastUpdated` is derived from the `lastUpdated` field each bundled provider file declares, and
reports the **most recent** of them. Providers are refreshed independently, so an individual
provider's data can be considerably older than this value.

> **Note — what the signal does and does not tell you**: it reports when this catalog was last
> refreshed, not whether a provider has changed its prices since. A vendor can cut a rate the day
> after a refresh, and `IsDataStale()` will still answer `false` while the bundled figure is wrong.
> Treat catalog pricing as a good default for estimation and budgeting, and read authoritative
> figures from your provider's billing data when they have to be exact.

## Price Source

```csharp
var model = ModelCatalog.FindModel("qwen-max")!;
Console.WriteLine(model.PriceSource); // ThirdParty
```

`ModelInfo.PriceSource` reports how a model's price fields were obtained:

- `Official` (default) — verified directly against the vendor's own published rate card.
- `ThirdParty` — the vendor's own rate card could not be read directly (a client-side-rendered
  pricing page, a docs-only landing page, or no direct per-token price published at all), so the
  figure was cross-referenced from a third-party aggregator or another vendor's official rate card
  instead. Currently applies to **Amazon Nova**, **Azure**, **Meta Llama**, and **Qwen** — see
  `docs/pricing-update-guide.md` Version History for the source used per provider. Treat these as
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
