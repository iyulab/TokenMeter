# Changelog

All notable changes to this project are documented in this file. Versions follow
[Semantic Versioning](https://semver.org/); while the major version is 0, a minor release may contain
breaking changes, and each one is marked **Breaking** with a migration note.

## [0.7.8] - unreleased

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
- Azure GPT-6 Sol and Luna (Azure: "in processing for publishing"). Amazon Nova 2 Pro and Omni (priced in the AWS Price
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
