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
Console.WriteLine(model?.SupportsReasoning);       // True (via ReasoningMode)
Console.WriteLine(model?.ReasoningMode);           // Optional
Console.WriteLine(model?.ThinkingFormat);          // Block
Console.WriteLine(model?.PromptCachingMode);       // Explicit
Console.WriteLine(model?.ToolCallingFormat);       // Anthropic
Console.WriteLine(model?.SupportsMcpToolUse);      // True
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

// Via CostCalculator (DI-friendly)
ICostCalculator calc = CostCalculator.Default();
var price = calc.CalculateCost("gpt-4o", inputTokens: 1_000, outputTokens: 500);
```

### Browsing the Catalog

```csharp
// By provider
foreach (var m in ModelCatalog.Anthropic.Values)
    Console.WriteLine($"{m.ModelId}: ctx={m.ContextWindow}, ${m.InputPricePerMillion}/M");

// By model type
var embeddingModels = ModelCatalog.GetByType(ModelType.Embedding);
var chatModels      = ModelCatalog.GetByType(ModelType.Chat);

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
| `AudioOutputPricePerSecond` | Audio output cost per second |

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
| OpenAI | GPT-5.x, GPT-4.1, GPT-4o, o1, o3, o4-mini series |
| Anthropic | Claude 5, 4.x, 3.x families (Fable, Opus, Sonnet, Haiku) |
| Google | Gemini 3.x, 2.5, 2.0, 1.5 families |
| xAI | Grok 4.x, 3.x series |
| Azure | Azure OpenAI equivalents |
| Mistral | Large, Medium, Small, Magistral, Pixtral |
| DeepSeek | R1 (reasoning), V3, Coder |
| Amazon Nova | Premier, Pro, Lite, Micro |
| Cohere | Command A, R+, R, R7B |
| Meta Llama | Maverick, Scout |
| Perplexity | Sonar Pro, Deep Research, Reasoning |
| Qwen | Max, Plus, Turbo |

## Data Freshness

```csharp
Console.WriteLine(ModelCatalog.LastUpdated);        // 2026-07-06
Console.WriteLine(ModelCatalog.DataAgeDays);        // days since last update
Console.WriteLine(ModelCatalog.IsDataStale());      // true if > 90 days old
```

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
