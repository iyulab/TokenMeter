# Changelog

All notable changes to this project are documented in this file. Versions follow
[Semantic Versioning](https://semver.org/); while the major version is 0, a minor release may contain
breaking changes, and each one is marked **Breaking** with a migration note.

## [0.10.0] - 2026-10-10

### Added
- **Explicit-cache storage cost.** `ModelInfo.CacheStoragePricePerMillionPerHour` and
  `CalculateCacheStorageCost(cachedTokens, storedFor, date)` price keeping tokens in an explicit prompt cache, apart from
  the requests that read it (linear in the time; service tiers and regions do not apply). `ScheduledPrice` carries a
  storage change. Filled for Gemini from Google's pricing page (2026-10-10): 3.8 Flash and 3.6 Flash $0.50 per 1M tokens
  per hour ($1.00 from 2027-01-01), 3.1 Pro Preview and 2.5 Pro $4.50, 3.5 Flash-Lite, 3.1 Flash-Lite, 3 Flash Preview,
  2.5 Flash and 2.5 Flash-Lite $1.00.
- **Per-token modality prices.** `ModelInfo.ModalityPrices` (`ModalityPrice`: `TokenModality` × `TokenUse` Input /
  CacheRead / Output) and `TokenCounts.InputTokensByModality` / `CacheReadTokensByModality` / `OutputTokensByModality` —
  breakdowns of the totals, as vendors report them. `CalculateCost(TokenCounts, CostContext)` prices the tokens of a
  modality the model prices apart at that rate (before service-tier and regional multipliers); a breakdown larger than its
  total throws. Filled from Google's pricing page (2026-10-10): audio input / audio cache read on Gemini 2.5 Flash
  (1.00 / 0.10), 2.5 Flash-Lite (0.30 / 0.03), 3 Flash Preview (1.00 / 0.10) and 3.1 Flash-Lite (0.50 / 0.05).
- **Gemini image and speech models.** `gemini-nano-banana-2.1`, `gemini-3.1-flash-image` (deprecated 2026-10-06),
  `gemini-3.1-flash-lite-image` and `gemini-3-pro-image` (`ImageGeneration`, image output 30 – 120 per 1M tokens), and
  `gemini-3.8-flash-tts`, `gemini-3.8-flash-lite-tts` (with their 2027-01-01 rates), `gemini-3.1-flash-tts-preview`,
  `gemini-2.5-flash-preview-tts`, `gemini-2.5-pro-preview-tts` (`TextToSpeech`, output = audio). These ids used to match
  the chat model whose name they contain and were priced as chat output.
- **Google model lifecycle.** From Google's deprecations page and changelog (2026-10-10): `gemini-3.5-flash` deprecated
  2026-10-08 for `gemini-3.6-flash`; `gemini-3.7-flash` → `gemini-3.8-flash` and `gemini-3-flash-preview` →
  `gemini-3.6-flash` as replacements; `gemini-3.1-flash-lite` shuts down no earlier than 2027-05-07 (`gemini-3.5-flash-lite`);
  `gemini-3-pro-preview`, `gemini-3.1-flash-lite-preview`, `gemini-2.0-flash(-lite)` and `gemini-1.5-pro/flash` carry
  their past shutdown dates and report `Retired`.

### Removed
- **Breaking: `ModelInfo.ImageInputPrice` and `AudioInputPricePerSecond`.** No catalog row set them and no calculation read
  them. Migration: per-token modality rates are in `ModalityPrices`; a per-image or per-second price of your own belongs in
  your own data.

## [0.9.0] - 2026-10-10

### Added
- **One cost calculation per request.** `ModelInfo.CalculateCost(TokenCounts, CostContext)` prices uncached input, cache
  reads, cache writes by lifetime (`CacheWriteTokenCount(Ttl, Tokens)`), output and server-side tool calls together, in
  this order: the rates of `CostContext.Date` (`AsOf`) → the long-context or time-of-day tier → the
  `CostContext.ServiceTier` multiplier → the `CostContext.Region` multiplier → per-call tool fees.
  `CostContext.At(instant)` sets the date and time of day from one timestamp. A service tier the model is not offered at,
  or a tool with no fee, gives `null` — an unknown price, not the standard one.
- **Service tiers, data residency and server tool fees.** `ModelInfo.ServiceTierMultipliers` (`ServiceTier.Batch` /
  `Flex` / `Fast`), `RegionalMultipliers` and `ToolCallPrices`, with `TokenCounts.ToolCalls` keyed by the vendor's tool
  name. Filled from the vendors' pricing pages (2026-10-10): OpenAI gpt-5 – gpt-6 families Batch/Flex 0.5x, Fast 2x
  (gpt-5.5 2.5x, gpt-5-mini 1.8x, none for the nano models), Pro models Batch 0.5x, `web_search` $10 and `file_search`
  $2.50 per 1,000 calls; Anthropic Batch 0.5x, fast mode 2x on Opus 5.5 / 5 / 4.8, `inference_geo: "us"` 1.1x on Claude
  4.6 and later, `web_search` $10 per 1,000; Gemini `google_search` $14 (3.x) and $35 (2.5) per 1,000 grounded prompts.
- **Tiers carry cache-write prices.** `PricingTier.CacheWritePricePerMillion` and `CacheWritePrices`; Claude Haiku 5.5's
  over-100K tier has its 5-minute and 1-hour write prices.
- **Gemini long-context tiers and cache-read prices.** Gemini 2.5 Pro (over 200K: input 2.50, output 15.00, cache read
  0.25) and 3.1 Pro Preview (4.00 / 18.00 / 0.40); cache-read prices for 2.5 Pro, 3.1 Pro Preview, 3 Flash Preview,
  2.5 Flash and 2.5 Flash-Lite (Google pricing page, 2026-10-10).
- **Per-provider freshness.** `ModelCatalog.GetLastUpdated(providerName)`.
- **A calculator that does not guess a self-hosted model.** `CostCalculator.Default(AliasMatchType maxFuzziness)` bounds
  how an unregistered id is matched against the catalog.

### Changed
- **Breaking: `ICostCalculator` has a new member**, `CalculateCost(string modelId, TokenCounts usage, CostContext context)`.
  Migration: an implementation of the interface adds it — delegating to `GetModel(modelId)?.CalculateCost(usage, context)`
  is the built-in behaviour.
- **A tier is decided by the request's prompt length.** `PricingTierContext.ContextLengthTokens` is documented as every
  input token, cache reads and writes included — the vendors' rule — not «prompt + expected completion». The calculation
  did not change; a caller that added the expected completion should pass the prompt length alone.

## [0.8.0] - 2026-10-09

### Added
- **Model lifecycle.** `ModelInfo.DeprecationDate`, `RetirementDate` and `ReplacementModelId`, with
  `GetLifecycleStatus(asOf)` → `Active` / `Deprecated` / `Retired` computed from the dates. Filled for Anthropic
  (10 models) and OpenAI (18) from the vendors' deprecation pages — e.g. `claude-sonnet-4-5` retires 2026-11-30 for
  `claude-sonnet-5-5`, `gpt-4`/`o1`/`o4-mini` on 2026-10-23.
- **Cache-write prices per cache lifetime.** `ModelInfo.CacheWritePrices` and `GetCacheWritePrice(ttl)`, and a
  `CalculateCost(..., cacheWriteTtl)` overload. Every Anthropic model carries its 5-minute and 1-hour write prices
  (1-hour = 2x input).
- **Announced price changes.** `ModelInfo.ScheduledPrices` and `AsOf(date)`, which returns the model with the rates in
  effect on a date. Gemini 3.8/3.7/3.6 Flash carry their 2027-01-01 rates (input 1.50, output 7.50, cache read 0.15).

### Fixed
- **Claude Sonnet 5.5 cache reads cost 0.10 per MTok**, not 0.20 (0.05x input, per Anthropic's pricing page).

### Documentation
- How reasoning tokens are billed and counted per vendor (Gemini reports them apart from candidate tokens).

## [0.7.10] - 2026-10-08

### Added
- **Claude Haiku 5.5** (`claude-haiku-5-5`, 1M context, 128K output): $0.10 / $0.50 per 1M tokens when the prompt is
  100K tokens or fewer, $0.50 / $2.50 above that (a `ContextLength` pricing tier from 100,001 tokens); cache read
  $0.01 ($0.05 above 100K), cache write $0.125. Served by the vendor and absent from the catalog until now.

## [0.7.9] - 2026-10-05

### Changed
- **The packages now carry the LICENSE text**, so an application that redistributes them can ship the MIT notice
  from the package itself.

### Fixed
- **Fuzzy matching no longer crosses a version token: an unknown newer id resolves to nothing instead of to the older
  model whose alias it contains.** `claude-sonnet-5-5` used to match the `claude-sonnet-5` row through its `contains`
  alias and was priced and described as that model. An alias that ends in a version number no longer matches an id that
  continues the number (`-5`, `.5`, a further digit); snapshot and deployment suffixes (`-20250929`, `-2025-08-07`,
  `-0309`, `-latest`, `-v1`) still match.

### Added
- **Claude Sonnet 5.5** ($2 / $10, cache read $0.20, adaptive thinking optional) and **GPT-6.1 Sol** ($2 / $10, cached
  input $0.10; above 272K input tokens $4 / $15 / $0.20) — both served by the vendors, neither in the catalog.
- **GPT-5.1, GPT-5.2, GPT-5.2 Pro, GPT-5 Pro, o1-pro and Gemini 3.1 Flash-Lite (GA)** from the vendors' price pages, and
  `grok-4.20-multi-agent-0309` as an exact alias of Grok 4.20 (same rates). All served today; before this release
  `gpt-5.1`/`gpt-5.2` were priced as GPT-5 by a fuzzy alias, and the Pro rows were absent.

## [0.7.8] - 2026-09-24

### Fixed
- **GPT-5.6 Sol cache writes are $5.00 per 1M** (1.25 × its $4 input). The row carried the key twice — the new rate
  was added above the old $6.25 instead of replacing it, and the loader kept the last one. GPT-5.6 Terra and Luna had
  the same duplicate with equal values. The catalog loader now rejects a key written twice
  (`AllowDuplicateProperties = false`), so this cannot recur silently.

- **OpenAI GPT-5.4, GPT-5.4 Pro, GPT-5.5, GPT-5.5 Pro and GPT-5.6 Sol/Terra/Luna now carry the long-context band.**
  Above 272K input tokens OpenAI bills the whole request at 2× input and cached input and 1.5× output; these rows had
  no band, so a long prompt was priced at the short-context rate. GPT-5.6 Sol stays at $4 / $20 — OpenAI's promotional
  price "at least through November 21, 2026" (it was $5 / $30 before 2026-08-21).

### Added
- **Azure GPT-5.4, GPT-5.4 Pro, GPT-5.4 mini, GPT-5.4 nano, GPT-5.5, GPT-5.6 Sol/Terra/Luna and GPT-6 Astra**, from the
  Azure Retail Prices API (Global Standard; Data Zone is 1.1×), flagged `Official`. Before, these ids fell under the
  `azure-gpt-5` prefix and were priced as GPT-5 ($1.25 / $10). Each has Azure's long-context band from 272K prompt
  tokens (Azure states the threshold for GPT-5.4; the others use the same 272K as OpenAI's list for these models).
  Cache writes on the long band are not expressible per tier and bill at the short-band rate.

### Not added (still unpublished by the vendor)
- Azure GPT-6 Sol and Luna (Azure: "in processing for publishing"). Azure GPT-5.5 Pro: Azure has no meter for it
  (Retail Prices API, 2026-09-24), so `azure-gpt-5.5-pro` resolves to the GPT-5.5 row. Amazon Nova 2 Pro and Omni (priced in the AWS Price
  List, but not announced as generally available on Bedrock). Perplexity Agent API models (no context window
  published per model; the Sonar API ends 2026-09-27, its rows stay for past usage).

## [0.7.7] - 2026-09-23

### Changed
- **Amazon Nova, Azure and Qwen prices now come from the vendors' own price lists**, and their rows are flagged
  `PriceSource.Official` (they were `ThirdParty`). Sources: the AWS Price List API (Bedrock offer), the Azure Retail
  Prices API (Foundry Models, Global Standard meters) and the Model Studio pricing page (International).
- **Qwen corrected.** `qwen-max` (now the legacy Qwen Max) is $1.60 / $6.40 with a 128K context; it was $0.78 /
  $3.90 and 262K. `qwen-plus` is $0.40 / $1.20 up to 256K prompt tokens and $1.20 / $3.60 above (it was $0.26 /
  $0.78). `qwen3-max*` ids no longer fall under `qwen-max`: they have their own row.
- **Meta Llama: Maverick output is $0.80** (was $0.696, from no traceable source). Meta hosts no token API; the rows
  follow DeepInfra's list price and stay flagged `ThirdParty`.
- **Cohere: Command A is flagged `ThirdParty`.** Cohere's pricing page no longer lists it.
- Azure `gpt-35-turbo` is $0.55 / $1.65 (regional meter, it has no Global Standard price).

### Added
- Qwen3 Max (with its 32K and 128K prompt-length bands), Qwen3.8 Max, Qwen3.7 Plus (256K band), Qwen3.8 Flash,
  Qwen Flash (256K band).
- Azure GPT-5 mini, GPT-5 nano, GPT-5 pro, GPT-5.1, GPT-5.2.
- Cohere `command-r-08-2024` ($0.15 / $0.60) and `command-r-plus-08-2024` ($2.50 / $10.00). The `command-r` and
  `command-r-plus` ids, retired on 2025-09-15, keep their last rows for past usage.
- Cache-read rates for every Amazon Nova model and for Azure GPT-4o, GPT-4.1 (all three sizes) and o4-mini.

## [0.7.6] - 2026-09-23

### Added
- **Claude Opus 5.5 (`claude-opus-5-5`) is in the catalog at its published rates** — $4 input / $20 output
  per MTok, $5 cache write, $0.20 cache read (0.05x input), 1M context, 128K output, reasoning always on.
  Before this, the id (and platform-prefixed forms such as `anthropic.claude-opus-5-5`) matched the
  `claude-opus-5` alias and was priced as Opus 5 ($5 / $25).
- **GPT-6 Sol (`gpt-6-sol`, $2 / $10) and GPT-6 Luna (`gpt-6-luna`, $0.10 / $0.50)**, each with the
  published long-context band (past 272K prompt tokens: 2x input and cache read, 1.5x output).
- **Grok 4.7 (`grok-4.7`, $2 / $6, cache read $0.50)** with its 200K-token band ($4 / $12).
- **DeepSeek V4.1 Flash (`deepseek-flash`, off-peak $0.15 / $0.60, cache read $0.003)** with its peak
  windows ($0.30 / $1.20).
- **Ministral 3 (3B / 8B / 14B) and GLM-5.3 on Mistral (`zai-glm-latest`, $1.40 / $4.40).**
- Cache-write rates for GPT-6 Astra/Sol/Luna and GPT-5.6, and cache-read rates on every xAI long-context band.

### Changed
- **Ids a vendor now serves as another model resolve to that model and its price.** xAI answers
  `grok-4`, `grok-4-0709`, `grok-4-latest`, every `grok-3*` id, `grok-4-fast` (`-reasoning` /
  `-non-reasoning`) and `grok-4-1-fast*` as Grok 4.3 ($1.25 / $2.50; the old rows said $3 / $15 for
  `grok-4` and `grok-3`), and `grok-code-fast-1` as Grok Build 0.1. DeepSeek bills `deepseek-v4-flash` as
  V4.1 Flash. Those rows are gone; looking the ids up returns the serving model. Ids that can no longer
  be called at all keep their last row (the catalog also prices past usage).
- **The dotted `grok-4.1-fast*` rows and `grok-4-fast-thinking` are gone and do not resolve.** xAI answers
  those names with not-found (its ids are hyphenated, `grok-4-1-fast-*`), so no model was ever priced
  under them.
- Context windows corrected to the vendor pages: Mistral Medium 3.5 and Small 4 (256K), Codestral (128K),
  Devstral (256K), Magistral (128K), Sonar Deep Research (128K). `devstral-latest` now resolves to Devstral 2.

## [0.7.5] - 2026-09-15

This file starts at 0.7.5. Changes in earlier releases were not recorded here; the commit history is
the record for them.
