# Changelog

All notable changes to this project are documented in this file. Versions follow
[Semantic Versioning](https://semver.org/); while the major version is 0, a minor release may contain
breaking changes, and each one is marked **Breaking** with a migration note.

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
  `grok-4`, `grok-4-0709`, `grok-3`, `grok-3-mini`, `grok-4-fast*` and `grok-4-1-fast*` as Grok 4.3
  ($1.25 / $2.50; the old rows said $3 / $15 for `grok-4` and `grok-3`), and `grok-code-fast-1` as
  Grok Build 0.1. DeepSeek bills `deepseek-v4-flash` as V4.1 Flash. Those rows are gone; looking the ids
  up returns the serving model. `grok-4-fast-thinking` and `grok-4.1-fast-thinking` no longer exist and
  were removed. Ids that can no longer be called at all keep their last row.
- Context windows corrected to the vendor pages: Mistral Medium 3.5 and Small 4 (256K), Codestral (128K),
  Devstral (256K), Magistral (128K), Sonar Deep Research (128K). `devstral-latest` now resolves to Devstral 2.

## [0.7.5] - 2026-09-15

This file starts at 0.7.5. Changes in earlier releases were not recorded here; the commit history is
the record for them.
