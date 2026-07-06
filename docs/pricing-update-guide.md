# LLM Pricing Update Guide

This document describes how to keep TokenMeter's model pricing data current.

## Update Cadence

- **Recommended**: Once a month, or whenever a major model is released.
- **Urgent**: Immediately when a provider announces a pricing policy change.

## Official Pricing Sources

| Provider | URL |
|----------|-----|
| OpenAI | https://openai.com/api/pricing/ |
| Anthropic | https://docs.anthropic.com/en/docs/about-claude/pricing |
| Google | https://ai.google.dev/gemini-api/docs/pricing |
| xAI | https://docs.x.ai/docs/models |
| Azure | https://azure.microsoft.com/en-us/pricing/details/cognitive-services/openai-service/ |
| Mistral | https://mistral.ai/technology/ |
| DeepSeek | https://platform.deepseek.com/api-docs/pricing |
| Amazon Nova | https://aws.amazon.com/bedrock/pricing/ |
| Cohere | https://cohere.com/pricing |
| Meta Llama | https://llama.meta.com/ |
| Perplexity | https://docs.perplexity.ai/guides/pricing |
| Qwen | https://help.aliyun.com/zh/model-studio/ |

## Update Procedure

### 1. Collect Pricing Information

Check each provider's official pricing page for the latest rates.

### 2. Edit JSON Files

Edit the corresponding provider JSON file in `src/TokenMeter/Pricing/`.

**Updating an existing model's price:**

```json
{
  "modelId": "gpt-4o",
  "inputPricePerMillion": 2.50,
  "outputPricePerMillion": 10.00,
  "displayName": "GPT-4o",
  "contextWindow": 128000,
  "aliases": [
    { "pattern": "gpt-4o", "type": "prefix" }
  ]
}
```

**Adding a new model:**

Append a new entry to the `models` array in the appropriate JSON file.

> **Note**: Since v0.4.0, prefix/contains matching uses a **longest-match** rule.
> When multiple aliases of the same type match, the longest pattern wins.
> This means the order of model entries in JSON does not affect matching results.

**Alias types:**

| Type | Behavior | Example |
|------|----------|---------|
| `exact` | Matches the full string only | `"gpt-4o"` → matches only `gpt-4o` |
| `prefix` | Matches any string starting with the pattern (longest match wins) | `"gpt-4o"` → matches `gpt-4o`, `gpt-4o-2024-08-06` |
| `contains` | Matches any string containing the pattern (longest match wins) | `"claude-3.5-sonnet"` → matches if found anywhere in the string |

**Alias design guidelines:**

- For base models that differ only by date snapshot (e.g., `gpt-4`), use `exact` type with individual date aliases.
- `prefix` works well for model families (e.g., `gpt-4o` → `gpt-4o-2024-08-06`).
- Verify that a short prefix won't collide with another model family (e.g., a `gpt-4` prefix also matches `gpt-4o`).

### 3. Add a New Provider

Create a new JSON file in `src/TokenMeter/Pricing/`:

```json
{
  "provider": "NewProvider",
  "models": [
    {
      "modelId": "model-id",
      "inputPricePerMillion": 1.00,
      "outputPricePerMillion": 2.00,
      "displayName": "Model Name",
      "contextWindow": 128000,
      "aliases": [
        { "pattern": "model", "type": "prefix" }
      ]
    }
  ]
}
```

New JSON files are automatically included as embedded resources via the `Pricing\*.json` wildcard in `TokenMeter.csproj`.

**Validation**: `ModelInfoLoader` throws `InvalidOperationException` only when an embedded JSON resource fails to deserialize (malformed or empty file). It does **not** enforce non-empty provider names or non-empty model lists at load time — those data-integrity checks live in the test suite (`ModelPricingValidationTests`, `PricingBugFixTests`). Always run tests after adding a new JSON file.

Provider models are always reachable at runtime via `ModelCatalog.GetProvider("NewProvider")`
(string-keyed, returns an empty dictionary for unknown names) — no per-provider code is required.

Optionally, to expose a **typed convenience property** for a frequently used provider, add the
following to `ModelCatalog.cs` (it simply delegates to `GetProvider`):

```csharp
public static IReadOnlyDictionary<string, ModelInfo> NewProvider => GetProvider("NewProvider");
```

### 4. Update LastUpdated Date

Update the `LastUpdated` date in `ModelCatalog.cs` to reflect the current date.

### 5. Update README.md

Update the pricing tables in `README.md` to reflect the latest data.

### 6. Run Tests

```bash
dotnet test tests/TokenMeter.Tests/TokenMeter.Tests.csproj
```

### 7. Commit Changes

```bash
git add .
git commit -m "chore: update model pricing (YYYY-MM-DD)

- [Change summary]

Sources:
- [Referenced URLs]"
```

## Price Change Notifications (Optional)

1. **OpenAI**: Subscribe to RSS at https://status.openai.com/
2. **Anthropic**: Subscribe to RSS at https://status.anthropic.com/
3. **Google**: Configure Cloud notifications
4. **xAI**: Follow @xaboratory on X (Twitter)

## Reference: Price Comparison Sites

- https://pricepertoken.com/ — Real-time token price comparison
- https://artificialanalysis.ai/ — LLM benchmarks and pricing
- https://llmpricecheck.com/ — Pricing calculator

## Version History

| Date | Version | Changes |
|------|---------|---------|
| 2026-07-06 | 0.4.1 | Bi-weekly refresh: +Anthropic Fable 5 / Opus 4.8 / Opus 4.7 / Sonnet 5, +xAI Grok 4.3 / 4.20 (all price+context source-verified). Doc class-name fixes (ModelCatalog/ModelInfoLoader). Quality-review hardening: `CalculateCost` (ModelInfo + CostCalculator) now rejects negative token counts (`ArgumentOutOfRangeException`); capability-field regression guard tests. Deferred (price captured, context window unverified → next cycle): OpenAI GPT-5.5 ($5/$30), Google Gemini 3.5 Flash ($1.50/$9). Flag: OpenAI GPT-5.4 family cache-read may be 0.1x not 0.5x (verify on source). Sources: platform.claude.com, developers.openai.com, ai.google.dev, docs.x.ai |
| 2026-03-09 | 0.4.0 | Longest-match alias algorithm, ModelInfoLoader validation, improved gpt-4 alias accuracy |
| 2026-02-10 | 0.3.0 | Refactored to JSON-based pricing system, 12 providers supported |
| 2026-01-28 | 0.1.0 | Initial pricing data (OpenAI, Anthropic, Google, xAI, Azure) |

---

Last Updated: 2026-07-06
