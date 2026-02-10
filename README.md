# TokenMeter

[![NuGet](https://img.shields.io/nuget/v/TokenMeter.svg)](https://www.nuget.org/packages/TokenMeter)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)

Token counting, cost calculation, and usage tracking for LLM applications.

## Features

- **Accurate Token Counting** - Uses Microsoft.ML.Tokenizers for precise token counting (cl100k_base, p50k_base)
- **Multi-Provider Support** - Built-in pricing for 12 providers (OpenAI, Anthropic, Google, xAI, Azure, Mistral, DeepSeek, Amazon Nova, Cohere, Meta Llama, Perplexity, Qwen)
- **Usage Tracking** - Session-based tracking with statistics and cost aggregation
- **Thread-Safe** - All components are designed for concurrent access
- **Extensible** - Register custom pricing for any model or provider

## Installation

```bash
dotnet add package TokenMeter
```

## Quick Start

### Token Counting

```csharp
using TokenMeter;

// Create a token counter (defaults to cl100k_base encoding)
var counter = TokenCounter.Default();

// Count tokens in text
var tokens = counter.CountTokens("Hello, how are you today?");
Console.WriteLine($"Tokens: {tokens}"); // Output: Tokens: 7

// Use model-specific counters
var gpt4Counter = TokenCounter.ForGpt4();
var gpt35Counter = TokenCounter.ForGpt35Turbo();
```

### Cost Calculation

```csharp
using TokenMeter;

// Create cost calculator with built-in pricing
var calculator = new CostCalculator();

// Calculate cost for a request
var cost = calculator.CalculateCost("gpt-4o", inputTokens: 1000, outputTokens: 500);
Console.WriteLine($"Cost: ${cost:F6}"); // Cost: $0.007500

// Get pricing info for a model
var pricing = calculator.GetPricing("claude-4-5-sonnet");
Console.WriteLine($"Input: ${pricing.InputPricePerMillion}/M tokens");
Console.WriteLine($"Output: ${pricing.OutputPricePerMillion}/M tokens");
```

### Usage Tracking

```csharp
using TokenMeter;

// Create tracker with cost calculator for automatic cost calculation
var costCalculator = new CostCalculator();
var tracker = new UsageTracker(costCalculator);

// Record usage for each API call
tracker.Record("gpt-4o-mini", inputTokens: 500, outputTokens: 200);
tracker.Record("gemini-2.0-flash", inputTokens: 800, outputTokens: 350);

// Get session statistics
var stats = tracker.GetSessionStatistics();
Console.WriteLine($"Requests: {stats.RequestCount}");
Console.WriteLine($"Total Input: {stats.TotalInputTokens}");
Console.WriteLine($"Total Output: {stats.TotalOutputTokens}");
Console.WriteLine($"Total Cost: ${stats.TotalCost:F4}");
```

## Supported Providers

| Provider | Models | Official Pricing |
|----------|--------|------------------|
| OpenAI | GPT-4o, GPT-4.1, o3, o4-mini, etc. | [openai.com/api/pricing](https://openai.com/api/pricing/) |
| Anthropic | Claude Opus 4.6, Claude Opus 4.1, Claude 4.5, Claude 3.7, Claude 3.5, Claude 3 | [docs.anthropic.com](https://docs.anthropic.com/en/docs/about-claude/pricing) |
| Google | Gemini 3, Gemini 2.5, Gemini 2.0, Gemini 1.5 | [ai.google.dev](https://ai.google.dev/gemini-api/docs/pricing) |
| xAI | Grok 4.1, Grok 4, Grok 3 | [docs.x.ai](https://docs.x.ai/docs/models) |
| Azure | Azure OpenAI models | [azure.microsoft.com](https://azure.microsoft.com/pricing/details/cognitive-services/openai-service/) |
| Mistral | Mistral Large, Medium, Small, Codestral, Devstral | [mistral.ai](https://mistral.ai/technology/) |
| DeepSeek | DeepSeek V3, R1, Coder | [deepseek.com](https://platform.deepseek.com/api-docs/pricing) |
| Amazon Nova | Nova Premier, Pro, Lite, Micro | [aws.amazon.com](https://aws.amazon.com/bedrock/pricing/) |
| Cohere | Command A, Command R+, Command R, Command R7B | [cohere.com](https://cohere.com/pricing) |
| Meta Llama | Llama 4 Maverick, Scout | [llama.meta.com](https://llama.meta.com/) |
| Perplexity | Sonar Pro, Sonar Deep Research, Sonar | [perplexity.ai](https://docs.perplexity.ai/guides/pricing) |
| Qwen | Qwen Max, Plus | [aliyun.com](https://help.aliyun.com/zh/model-studio/) |

## Model Pricing (February 2026)

### OpenAI

| Model | Input (per 1M) | Output (per 1M) | Context |
|-------|---------------|-----------------|---------|
| GPT-4.1 | $2.00 | $8.00 | 1M |
| GPT-4.1 Mini | $0.40 | $1.60 | 1M |
| GPT-4.1 Nano | $0.10 | $0.40 | 1M |
| GPT-4o | $2.50 | $10.00 | 128K |
| GPT-4o-mini | $0.15 | $0.60 | 128K |
| o3-pro | $20.00 | $80.00 | 200K |
| o3 | $2.00 | $8.00 | 200K |
| o3-mini | $1.10 | $4.40 | 200K |
| o4-mini | $1.10 | $4.40 | 200K |
| o1 | $15.00 | $60.00 | 200K |

### Anthropic (Claude)

| Model | Input (per 1M) | Output (per 1M) | Context |
|-------|---------------|-----------------|---------|
| Claude Opus 4.6 | $5.00 | $25.00 | 200K |
| Claude Opus 4.1 | $15.00 | $75.00 | 200K |
| Claude 4.5 Opus | $5.00 | $25.00 | 200K |
| Claude Opus 4 | $15.00 | $75.00 | 200K |
| Claude 4.5 Sonnet | $3.00 | $15.00 | 200K |
| Claude 4.5 Haiku | $1.00 | $5.00 | 200K |
| Claude Sonnet 4 | $3.00 | $15.00 | 200K |
| Claude Sonnet 3.7 | $3.00 | $15.00 | 200K |
| Claude 3.5 Sonnet | $3.00 | $15.00 | 200K |
| Claude 3.5 Haiku | $0.80 | $4.00 | 200K |
| Claude 3 Opus | $15.00 | $75.00 | 200K |
| Claude 3 Sonnet | $3.00 | $15.00 | 200K |
| Claude 3 Haiku | $0.25 | $1.25 | 200K |

### Google (Gemini)

| Model | Input (per 1M) | Output (per 1M) | Context |
|-------|---------------|-----------------|---------|
| Gemini 3 Pro Preview | $2.00 | $12.00 | 1M |
| Gemini 3 Flash Preview | $0.50 | $3.00 | 1M |
| Gemini 2.5 Pro | $1.25 | $10.00 | 1M |
| Gemini 2.5 Flash | $0.30 | $2.50 | 1M |
| Gemini 2.5 Flash-Lite | $0.10 | $0.40 | 1M |
| Gemini 2.0 Flash | $0.10 | $0.40 | 1M |
| Gemini 2.0 Flash-Lite | $0.075 | $0.30 | 1M |
| Gemini 1.5 Pro | $1.25 | $5.00 | 2M |
| Gemini 1.5 Flash | $0.075 | $0.30 | 1M |

### xAI (Grok)

| Model | Input (per 1M) | Output (per 1M) | Context |
|-------|---------------|-----------------|---------|
| Grok 4.1 Fast Thinking | $0.20 | $0.50 | 2M |
| Grok 4.1 Fast | $0.20 | $0.50 | 2M |
| Grok 4 Fast Thinking | $0.20 | $0.50 | 2M |
| Grok 4 Fast | $0.20 | $0.50 | 2M |
| Grok 4 | $3.00 | $15.00 | 256K |
| Grok Code Fast | $0.20 | $1.50 | 256K |
| Grok 3 | $3.00 | $15.00 | 131K |
| Grok 3 Mini | $0.30 | $0.50 | 131K |

### Azure OpenAI

| Model | Input (per 1M) | Output (per 1M) | Context |
|-------|---------------|-----------------|---------|
| Azure GPT-4o | $2.50 | $10.00 | 128K |
| Azure GPT-4o-mini | $0.15 | $0.60 | 128K |
| Azure GPT-4.1 | $2.00 | $8.00 | 1M |
| Azure GPT-4.1 Mini | $0.40 | $1.60 | 1M |
| Azure GPT-4.1 Nano | $0.10 | $0.40 | 1M |
| Azure o3-pro | $20.00 | $80.00 | 200K |
| Azure o4-mini | $1.10 | $4.40 | 200K |

### Mistral

| Model | Input (per 1M) | Output (per 1M) | Context |
|-------|---------------|-----------------|---------|
| Mistral Large | $2.00 | $6.00 | 128K |
| Mistral Medium 3 | $0.40 | $2.00 | 128K |
| Mistral Small | $0.20 | $0.60 | 128K |
| Codestral | $0.30 | $0.90 | 256K |
| Devstral Small | $0.10 | $0.30 | 128K |
| Pixtral Large | $2.00 | $6.00 | 128K |

### DeepSeek

| Model | Input (per 1M) | Output (per 1M) | Context |
|-------|---------------|-----------------|---------|
| DeepSeek V3 | $0.28 | $0.42 | 128K |
| DeepSeek R1 | $0.28 | $0.42 | 128K |
| DeepSeek Coder | $0.14 | $0.28 | 128K |

### Amazon Nova

| Model | Input (per 1M) | Output (per 1M) | Context |
|-------|---------------|-----------------|---------|
| Nova Premier | $2.50 | $12.50 | 1M |
| Nova Pro | $0.80 | $3.20 | 300K |
| Nova Lite | $0.06 | $0.24 | 300K |
| Nova Micro | $0.035 | $0.14 | 128K |

### Cohere

| Model | Input (per 1M) | Output (per 1M) | Context |
|-------|---------------|-----------------|---------|
| Command A | $2.50 | $10.00 | 256K |
| Command R+ | $2.50 | $10.00 | 128K |
| Command R | $0.50 | $1.50 | 128K |
| Command R7B | $0.0375 | $0.15 | 128K |
| Command Light | $0.30 | $0.60 | 4K |

### Meta Llama

| Model | Input (per 1M) | Output (per 1M) | Context |
|-------|---------------|-----------------|---------|
| Llama 4 Maverick | $0.22 | $0.85 | 1M |
| Llama 4 Scout | $0.15 | $0.50 | 10M |

### Perplexity

| Model | Input (per 1M) | Output (per 1M) | Context |
|-------|---------------|-----------------|---------|
| Sonar Pro | $3.00 | $15.00 | 200K |
| Sonar Deep Research | $2.00 | $8.00 | 200K |
| Sonar | $1.00 | $1.00 | 128K |

### Qwen

| Model | Input (per 1M) | Output (per 1M) | Context |
|-------|---------------|-----------------|---------|
| Qwen Max | $1.20 | $6.00 | 128K |
| Qwen Plus | $0.20 | $1.00 | 128K |

> **Note**: Prices change frequently. See [docs/pricing-update-guide.md](docs/pricing-update-guide.md) for update instructions.

## Custom Pricing

Register custom pricing for local models or other providers:

```csharp
var calculator = new CostCalculator();

// Register custom model pricing
calculator.RegisterPricing(new ModelPricing
{
    ModelId = "llama-3.2-70b",
    InputPricePerMillion = 0.50m,
    OutputPricePerMillion = 0.75m,
    Provider = "Local",
    DisplayName = "Llama 3.2 70B",
    ContextWindow = 128000
});

// Use as normal
var cost = calculator.CalculateCost("llama-3.2-70b", 1000, 500);
```

## Query by Provider

```csharp
// Get all models for a provider
var googleModels = ModelPricingData.GetByProvider("Google");
foreach (var model in googleModels)
{
    Console.WriteLine($"{model.DisplayName}: ${model.InputPricePerMillion}/${model.OutputPricePerMillion}");
}

// List all providers
var providers = ModelPricingData.GetProviderNames();
// ["OpenAI", "Anthropic", "Google", "xAI", "Azure", "Mistral", "DeepSeek", ...]

// Check last update date
Console.WriteLine($"Pricing last updated: {ModelPricingData.LastUpdated}");
```

## API Reference

### ITokenCounter

```csharp
public interface ITokenCounter
{
    int CountTokens(string text);
    int CountTokens(IEnumerable<string> texts);
    string ModelName { get; }
}
```

### ICostCalculator

```csharp
public interface ICostCalculator
{
    decimal? CalculateCost(string modelId, int inputTokens, int outputTokens);
    ModelPricing? GetPricing(string modelId);
    void RegisterPricing(ModelPricing pricing);
    IEnumerable<string> GetRegisteredModels();
}
```

### IUsageTracker

```csharp
public interface IUsageTracker
{
    void Record(UsageRecord record);
    UsageRecord Record(string? modelId, int inputTokens, int outputTokens, string? sessionId = null);
    UsageStatistics GetSessionStatistics();
    UsageStatistics GetStatistics(DateTimeOffset startTime, DateTimeOffset endTime);
    UsageStatistics GetTodayStatistics();
    IReadOnlyList<UsageRecord> GetRecords();
    void Clear();
    string SessionId { get; }
    string StartNewSession();
}
```

### ModelPricing

```csharp
public record ModelPricing
{
    public required string ModelId { get; init; }
    public required decimal InputPricePerMillion { get; init; }
    public required decimal OutputPricePerMillion { get; init; }
    public string? Provider { get; init; }
    public string? DisplayName { get; init; }
    public int? ContextWindow { get; init; }

    public decimal CalculateCost(int inputTokens, int outputTokens);
}
```

## Advanced Usage

### Multi-Session Tracking

```csharp
var tracker = new UsageTracker(new CostCalculator());

// First session
tracker.Record("gpt-4o", 1000, 500);
var session1Stats = tracker.GetSessionStatistics();

// Start new session
tracker.StartNewSession();
tracker.Record("gpt-4o-mini", 2000, 800);
var session2Stats = tracker.GetSessionStatistics();

// Get all-time statistics
var todayStats = tracker.GetTodayStatistics();
```

### Token Counting for Chat Messages

```csharp
var counter = TokenCounter.Default();

// Estimate tokens for a chat conversation
var messages = new[]
{
    "You are a helpful assistant.",          // System
    "What is the capital of France?",        // User
    "The capital of France is Paris."        // Assistant
};

var totalTokens = counter.CountTokens(messages);
// Add overhead for message formatting (~4 tokens per message)
var estimatedTokens = totalTokens + (messages.Length * 4);
```

### Cost Comparison

```csharp
var calculator = new CostCalculator();
var inputTokens = 10000;
var outputTokens = 2000;

Console.WriteLine("Cost comparison for 10K input / 2K output tokens:");

foreach (var provider in ModelPricingData.GetProviderNames())
{
    Console.WriteLine($"\n{provider}:");
    foreach (var model in ModelPricingData.GetByProvider(provider).Take(3))
    {
        var cost = model.CalculateCost(inputTokens, outputTokens);
        Console.WriteLine($"  {model.DisplayName}: ${cost:F4}");
    }
}
```

## Requirements

- .NET 10.0
- Microsoft.ML.Tokenizers 2.0.0+

## Related Projects

- [ironhive-cli](https://github.com/iyulab/ironhive-cli) - CLI agent using TokenMeter
- [ToolCallParser](https://github.com/iyulab/ToolCallParser) - Multi-provider tool call parsing

## Documentation

- [Pricing Update Guide](docs/pricing-update-guide.md) - How to update model pricing

## License

MIT License - see [LICENSE](LICENSE) for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Updating Pricing

See [docs/pricing-update-guide.md](docs/pricing-update-guide.md) for detailed instructions on updating model pricing.

---

Made with care by [iyulab](https://github.com/iyulab)
