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

## [0.7.5] - 2026-09-15

This file starts at 0.7.5. Changes in earlier releases were not recorded here; the commit history is
the record for them.
