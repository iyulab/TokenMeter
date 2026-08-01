# LLM Pricing Update Guide

This document describes how to keep TokenMeter's model pricing data current.

## Update Cadence

- **Recommended**: Once a month, or whenever a major model is released.
- **Urgent**: Immediately when a provider announces a pricing policy change.

## Official Pricing Sources

| Provider | URL |
|----------|-----|
| OpenAI | https://developers.openai.com/api/docs/pricing |
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
  "lastUpdated": "2026-08-01",
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

### 4. Bump `lastUpdated` in Every File You Touched

Each provider JSON carries its own date at the top:

```json
{
  "provider": "NewProvider",
  "lastUpdated": "2026-08-01",
  "models": [ ... ]
}
```

Set it to the date you verified that provider against its vendor rate card. There is no date to
maintain in code — `ModelCatalog.LastUpdated` is derived from these values (the most recent one
wins), and a provider left undated makes the catalog report itself as stale rather than fresh.
The test suite fails if any provider file is missing the field or the date does not parse.

### 5. Update README.md

`README.md` documents the schema rather than per-model rates, so a routine refresh usually leaves
it untouched. Update it when the refresh changes something a reader would see: the `LastUpdated`
value shown in the Data Freshness example, the provider list, or any example that names a model
whose figures moved.

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
| 2026-08-01 | 0.7.0 | Provider audit against published rate cards. What had gone stale was mostly the model list, not the prices: added Claude Opus 5 and Claude Mythos 5, Gemini 3.6 Flash and Gemini 3.5 Flash-Lite, DeepSeek V4 Flash and V4 Pro. Corrected two evergreen Mistral ids that had kept a superseded version's rate (`mistral-medium-latest` → Medium 3.5, `mistral-small-latest` → Small 4). Only the providers re-verified in this pass had their `lastUpdated` advanced; the rest keep their older date on purpose. Sources: platform.claude.com, ai.google.dev, api-docs.deepseek.com, mistral.ai/pricing/api |
| 2026-08-01 | 0.6.3 | Reported: GPT-5.6 Luna and Terra repriced downward (Luna −80%, Terra −20%); Sol unchanged. Found while re-verifying the family: GPT-5.6 now charges a separate cache-write rate, and the reasoning series (o1, o3, o3-mini, o4-mini) plus Grok 4.x carried cached-read prices with prompt caching reported as unsupported. Freshness moved into the data — each provider file declares `lastUpdated` and `ModelCatalog.LastUpdated` is derived from it, replacing the hand-maintained constant. Sources: developers.openai.com/api/docs/pricing, docs.x.ai |
| 2026-07-21 | 0.6.2 | Bi-weekly refresh: +GPT-5.6 Sol / Terra / Luna, GPT-5.5 (+Pro), GPT-5.4 Pro, Gemini 3.5 Flash, Grok 4.5. |
| 2026-07-15 | 0.6.1 | Corrected OpenAI cached-input prices for the gpt-5 and gpt-4.1 families. Resolves the cache-read ratio flag raised in 0.4.1 — the reasoning tiers are 0.1x of input, re-verified against the vendor rate card on 2026-08-01. |
| 2026-07-07 | 0.6.0 | Strict opt-in model lookup — bounded fuzziness plus match diagnostics. |
| 2026-07-07 | 0.5.0 | Removed dead output-modality schema fields (breaking). |
| 2026-07-06 | 0.4.1 | Bi-weekly refresh: +Anthropic Fable 5 / Opus 4.8 / Opus 4.7 / Sonnet 5, +xAI Grok 4.3 / 4.20 (all price+context source-verified). Doc class-name fixes (ModelCatalog/ModelInfoLoader). Quality-review hardening: `CalculateCost` (ModelInfo + CostCalculator) now rejects negative token counts (`ArgumentOutOfRangeException`); capability-field regression guard tests. Deferred (price captured, context window unverified → next cycle): OpenAI GPT-5.5 ($5/$30), Google Gemini 3.5 Flash ($1.50/$9). Flag (resolved in 0.6.1): OpenAI GPT-5.4 family cache-read is 0.1x, not 0.5x. Sources: platform.claude.com, developers.openai.com, ai.google.dev, docs.x.ai |
| 2026-03-09 | 0.4.0 | Longest-match alias algorithm, ModelInfoLoader validation, improved gpt-4 alias accuracy |
| 2026-02-10 | 0.3.0 | Refactored to JSON-based pricing system, 12 providers supported |
| 2026-01-28 | 0.1.0 | Initial pricing data (OpenAI, Anthropic, Google, xAI, Azure) |

---

Last Updated: 2026-08-01
