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

### 3.5. Recording a Tiered Rate (since v0.7.0)

Some providers charge more than their representative rate under a specific condition — a
long-context surcharge past a prompt-length threshold, or a peak-hour surcharge during specific
UTC windows. If you find one during a refresh, don't just overwrite the representative fields —
add a `pricingTiers` entry alongside them so both rates stay visible:

```json
{
  "modelId": "example-model",
  "inputPricePerMillion": 2.00,
  "outputPricePerMillion": 6.00,
  "pricingTiers": [
    {
      "axis": "ContextLength",
      "minContextLengthTokens": 200000,
      "inputPricePerMillion": 4.00,
      "outputPricePerMillion": 12.00
    }
  ]
}
```

- `axis` is `"ContextLength"` (gate on `minContextLengthTokens`) or `"TimeOfDay"` (gate on
  `windowStartUtc`/`windowEndUtc`, `"HH:mm"` UTC). A provider with more than one time window (e.g.
  two disjoint peak periods) gets one tier entry per window, all at the same price — not a single
  entry with a list of windows.
- The top-level `inputPricePerMillion`/etc. stay the **representative** (usually lowest/off-peak)
  rate; `CalculateCost(inputTokens, outputTokens)` with no context keeps using them unchanged.
  `CalculateCost(inputTokens, outputTokens, PricingTierContext)` applies a tier when the caller
  supplies the matching context (`ContextLengthTokens`/`CallTimeUtc`).
- A test enforces that no tier is priced below the representative rate
  (`ModelsWithPricingTiers_TierRatesAreNeverCheaperThanRepresentative`) — verify your tier's price
  against the vendor page before adding it, not just against the representative rate.

### 3.6. Recording a Third-Party-Sourced Price (since v0.7.2)

Some providers' own pricing pages cannot be read directly — client-side rendering, a docs-only
landing page with no rate table, or no direct per-token price published at all (a model only
reachable through third-party hosts). When that happens, don't leave the entry stale indefinitely
waiting on a page that may never become fetchable: cross-reference the figure from a reputable
third-party aggregator (or, when the model is a rebrand of another vendor's own model under a
documented price-parity policy, from that vendor's own official file) and mark it explicitly:

```json
{
  "modelId": "example-model",
  "inputPricePerMillion": 0.78,
  "outputPricePerMillion": 3.90,
  "priceSource": "ThirdParty"
}
```

- Omit `priceSource` (or set it to `"Official"`, the default) once you've verified a price directly
  against the vendor's own page — flip it back the moment that becomes possible again.
  `ModelInfo.PriceSource` surfaces this to consumers (see README.md "Price Source").
- Record which aggregator/vendor page you cross-referenced in the commit message and the Version
  History entry below, the same as any other source.
- A test (`ModelPricingValidationTests.ThirdPartySourcedProviders_AllModelsFlagged`) pins which
  providers currently carry this flag — update it if you add or remove a provider from the list.

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
| 2026-09-23 | 0.7.6 | Added Claude Opus 5.5 ($4 / $20, cache write $5, cache read $0.20 — 0.05x input; 1M / 128K; thinking always on). Its id previously fell through to the `claude-opus-5` contains alias and was priced as Opus 5. Sonnet 5 re-checked: $2 / $10 is now the standard price (the scheduled increase will not occur) — catalog already matched. Source: platform.claude.com pricing |
| 2026-08-31 | 0.7.3 | HD-40(f) — audited capability metadata (non-price fields) on the same 4 `ThirdParty`-flagged providers just re-verified for pricing in 0.7.2, cross-referencing each vendor's own FAQ/spec via a third-party host page. Amazon Nova: `amazon-nova-lite` was missing `supportsImageInput`/`supportsVideoInput` (it is multimodal, not text-only as the entry implied); `amazon-nova-2-lite` was missing all three of `supportsImageInput`/`supportsVideoInput`/`supportsDocumentInput` (accepts text, images, video, and PDFs). Azure: `azure-o4-mini` had no `promptCachingMode`/`cacheReadPricePerMillion` at all, unlike every other Azure reasoning-tier model — an omission, since it mirrors OpenAI's o4-mini which does support automatic caching; backfilled from this repo's own `openai.json`. Meta Llama: both `llama-4-maverick` and `llama-4-scout` were missing `supportsImageInput` (both are text+image multimodal) and `supportsStructuredOutput` (both support `response_format` JSON-schema output). Qwen: `qwen-max` and `qwen-plus` both carried a flat, stale 128K `contextWindow` — real values are 262,144 (`qwen-max`) and 1,000,000 (`qwen-plus`), understating both by a wide margin; also missing `supportsStructuredOutput` and `maxOutputTokens`. Sources: openrouter.ai/amazon/nova-{lite,2-lite,pro,premier,micro}-v1, this repo's openai.json, openrouter.ai/meta-llama/llama-4-{maverick,scout}, openrouter.ai/qwen/{qwen3-max,qwen-plus} |
| 2026-08-31 | 0.7.2 | Owner-approved resolution of the 4 providers 0.7.1 left stale (Amazon Nova, Azure, Meta Llama, Qwen — official pages unfetchable): added `ModelInfo.PriceSource` (`Official`/`ThirdParty`) and flagged all 4 providers' models `ThirdParty`. Cross-referenced via OpenRouter (a third-party aggregator) for Amazon Nova (`amazon/nova-*-v1` — confirmed all 5 models' input/output rates unchanged from the prior entry, so no price correction needed there) and for Qwen/Meta Llama (found real drift: `qwen-max`→`qwen3-max` $1.20/$6.00 → $0.78/$3.90, `qwen-plus`→`qwen3-plus` $0.20/$1.00 → $0.26/$0.78, `llama-4-maverick` $0.22/$0.85 → $0.20/$0.696, `llama-4-scout` $0.15/$0.50 → $0.10/$0.30). Azure's input/output rates were cross-checked against this repo's own OpenAI file instead (Azure OpenAI mirrors OpenAI's list price under Microsoft's documented price-parity policy) and found correct — but its `cacheReadPricePerMillion` values turned out to be a uniform "50% of input" guess rather than each model's actual published discount tier (OpenAI's real ratios range 10%–50% by generation), overstating the cached-read cost 2x–5x on `azure-gpt-5`/`azure-gpt-4.1`/`azure-gpt-4.1-mini`/`azure-gpt-4.1-nano` — corrected to match OpenAI's real per-model rates (same defect class as the 0.6.1 cache-ratio fix, this time in Azure's file rather than OpenAI's). Sources: openrouter.ai/amazon/nova-{premier,pro,lite,micro,2-lite}-v1, openrouter.ai/qwen/qwen3-max, openrouter.ai/qwen/qwen-plus, openrouter.ai/meta-llama/llama-4-{maverick,scout}, this repo's openai.json (`lastUpdated: 2026-08-16`) |
| 2026-08-30 | 0.7.1 | Waiver-driven refresh attempt on the 5 providers stale since 2026-05-19 (Amazon Nova, Azure, Cohere, Meta Llama, Qwen — flagged by `check-catalog-staleness.ps1`, waiver expires 2026-10-01). Only Cohere's official pricing page was directly fetchable: `command-r` was $0.15/$0.60, official page now shows $0.50/$1.50 (3x re-fetched, consistent) — corrected. `command-a`/`command-r7b` no longer appear on the page at all (not confirmed deprecated — TokenMeter has no schema field for that yet, tracked as item (e) in the umbrella's TokenMeter backlog) — left unchanged rather than guessed at removal. Amazon Nova (aws.amazon.com/bedrock/pricing, aws.amazon.com/nova/pricing) and Azure (azure.microsoft.com/.../openai-service) render their tables client-side — fetched content is placeholder dashes only. Meta Llama's official page (llama.meta.com) does not publish direct API token pricing at all (Meta doesn't host it; our catalog's numbers must originate from a specific inference host, unidentified). Qwen's page (help.aliyun.com/zh/model-studio) is a docs landing page, not the pricing table. These 4 remain stale pending either a different data source or manual verification. Sources: cohere.com/pricing |
| 2026-08-17 | 0.7.0 | Added `PricingTiers` — non-representative price bands for context-length and time-of-day surcharges (see §3.5 above). xAI grok-4.6/4.5/4.3/4.20/build-0.1 get a ≥200K context-length tier; DeepSeek V4 Pro/Flash get two peak-hour time-of-day tiers (01:00–04:00 and 06:00–10:00 UTC). Both verified against official documentation, not estimated. `CalculateCost` gained a `PricingTierContext`-aware overload on both `ModelInfo` and `ICostCalculator`; the no-context path is unchanged. Sources: api-docs.deepseek.com/quick_start/pricing, docs.x.ai/docs/models |
| 2026-08-16 | 0.6.5 | Bi-weekly refresh: DeepSeek's peak/off-peak split reflected (off-peak used as the single representative price at the time — superseded by the 0.7.0 tiers above), +xAI grok-4.6/grok-build-0.1, +Google gemini-3.7-flash, corrected gemini-3.6-flash (was carrying a post-introductory rate ahead of its effective date). Anthropic/Mistral/OpenAI/Perplexity re-verified, no changes needed. |
| 2026-08-01 | 0.6.4 | Provider audit against published rate cards. What had gone stale was mostly the model list, not the prices: added Claude Opus 5 and Claude Mythos 5, Gemini 3.6 Flash and Gemini 3.5 Flash-Lite, DeepSeek V4 Flash and V4 Pro. Corrected two evergreen Mistral ids that had kept a superseded version's rate (`mistral-medium-latest` → Medium 3.5, `mistral-small-latest` → Small 4). Only the providers re-verified in this pass had their `lastUpdated` advanced; the rest keep their older date on purpose. Sources: platform.claude.com, ai.google.dev, api-docs.deepseek.com, mistral.ai/pricing/api |
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

Last Updated: 2026-08-31
