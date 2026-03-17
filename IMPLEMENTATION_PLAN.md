# Plan: Modernize `src\ProjNet4GeoAPI` toward current `PROJ` capabilities

## Problem statement
The current .NET implementation in `src\ProjNet4GeoAPI` is stable (baseline tests pass), but it is conceptually behind current `PROJ`:
- Focused on legacy WKT1/classic transformation flows.
- No `proj.db`-like metadata model for CRS/operation discovery.
- No modern grid/resource management approach.
- Legacy API structures and remaining `NotImplementedException` paths.

## Target outcome
A modern, evolvable .NET projection library aligned with current `PROJ` concepts:
- data-driven CRS and operation selection,
- modern format support (at least WKT2, optionally PROJJSON),
- reproducible verification against reference cases,
- incremental migration without a big-bang rewrite.

## Non-negotiable architecture rules
- Implementation must be fully in modern managed C#.
- No native wrapper, no P/Invoke/interop bridge to C++ `PROJ`.
- C++ `PROJ` is reference-only for behavior and test parity.
- Final deliverable must have no runtime SQLite dependency and no native dependency path.
- If source data comes from SQLite-based reference inputs, materialize it at build time into managed assets (files/resources/generated C#).

## Non-negotiable language and quality rules
- All identifiers and generated documentation must be in English.
- C# quality rules must follow StyleCop 6.2.0.
- Maximum analyzer strictness must be enabled.
- Warnings must be fixed (immediately or in an explicit planned step).
- Warning suppression is not a default solution and requires explicit approval.
- All unit tests must be on xUnit v3.2.2.
- The existing public API must remain fully present and functionally correct after modernization.
- Members may be marked with `[Obsolete]` when better alternatives exist, but obsolete members must continue to work correctly.
- New APIs may be added to expose modern C# patterns, as long as additions are non-breaking to existing consumers.
- Documentation must be concise: as short as possible, as detailed as necessary.
- Examples are allowed only when they add clear practical value.

## Legacy behavior compatibility boundary
- Behavioral compatibility is required for all documented and currently validated legacy API scenarios.
- If a legacy behavior is considered a defect, changing it requires an explicit decision record, release note entry, and targeted compatibility tests for the agreed replacement behavior.
- Compatibility exceptions must be explicit, approved, and traceable.

## Target framework policy
- Required shipping target: `netstandard2.0`.
- Candidate additional targets: `netstandard2.1`, `net48`, `net481`, `net8.0+`.
- Enable additional TFMs only when they provide concrete API/performance/dependency benefits.
- Use `#if` for target-specific improvements while keeping shared behavior consistent.
- Do not add targets that do not contain meaningful technical differences.

## Repository and workflow guardrails
- Only `src` may be modified.
- `spec` is read-only reference input.
- Create meaningful checkpoint commits in `src`.
- `git worktree` is allowed for parallel execution.

## Verified baseline
- `src\ProjNet4GeoAPI` .NET tests passed (baseline confirmed earlier).
- `spec\PROJ` was rebuilt with `vcpkg` and validated (`ctest` 54/54 passed).

## Execution progress snapshot
- Wave A completed:
  - plan copied into repository (`IMPLEMENTATION_PLAN.md`);
  - PROJ gap analysis artifact added (`docs\modernization\proj-gap-analysis.md`);
  - public API baseline and verification test implemented.
- Wave B completed:
  - test suite migrated to xUnit v3.2.2 attributes and runner setup;
  - CI/test tooling updated for xUnit v3 execution;
  - analyzer/style quality baseline hardened and documented.
- Next step progress:
  - `test-parity-matrix` completed (`docs\modernization\test-parity-matrix.md`);
  - `target-architecture` completed (`docs\modernization\target-architecture.md`);
  - `managed-data-packaging` baseline completed (`docs\modernization\managed-data-packaging.md`);
  - `format-support-upgrade` baseline completed (`docs\modernization\format-support-upgrade.md`);
  - `projection-kernel-alignment` baseline completed (`docs\modernization\projection-kernel-alignment.md`);
  - `registry-data-layer` baseline completed (`docs\modernization\registry-data-layer.md`) with additive public lookup APIs;
  - `operation-resolution-engine` baseline completed (`docs\modernization\operation-resolution-engine.md`) with candidate resolver entrypoint and identity fast-path;
  - `grid-resource-management` baseline completed (`docs\modernization\grid-resource-management.md`) with deterministic local-first resolution;
  - `verification-suite` baseline completed (`docs\modernization\verification-suite.md`) with EPSG reference-point checks and compatibility assertions;
  - `mutation-testing-stryker` baseline completed (`docs\modernization\mutation-testing-stryker.md`) with tooling and workflow integration;
  - `benchmarkdotnet-parity` baseline completed (`docs\modernization\benchmarkdotnet-parity.md`) with reproducible parity scenarios;
  - `migration-docs-release` baseline completed (`docs\modernization\migration-docs-release.md`) with concise migration and release guidance.

## Work packages (Todos)

### 1) `proj-gap-analysis`
**Goal:** Produce a concrete delta list between current `.NET` and current `PROJ`.
- Build a feature matrix (CRS types, operations, grids, formats, API flows).
- Prioritize by value/complexity (Must/Should/Could).
- Define Release 1 vs Release 2 scope.

### 2) `test-parity-matrix`
**Goal:** Align test coverage between C++ `PROJ` and .NET implementation.
- Map `PROJ` tests to existing .NET tests by feature.
- Add missing high-priority .NET tests.
- Define tolerances for cross-implementation comparisons.

### 3) `xunit-v3-migration`
**Goal:** Unify test infrastructure on xUnit v3.2.2 early.
- Migrate NUnit tests to xUnit v3.
- Update fixtures, shared test utilities, and data-driven patterns.
- Stabilize CI execution on xUnit v3 before feature expansion.

### 4) `public-api-baseline`
**Goal:** Freeze and govern public API changes before implementation work.
- Generate a public API baseline (types, signatures, visibility).
- Enforce API diff checks on pull requests.
- Document allowed API change classes (compatible/obsolete/breaking).
- Lock explicit compatibility contract: no removal of existing public members during modernization scope.
- Define obsolete policy: mark with guidance, keep behavior fully functional, and test it.
- Define extension policy for new modern C# APIs (additive and non-breaking only).
- Keep an explicit legacy API behavior matrix (member -> expected behavior -> test coverage).
- Define defect-compatibility exception workflow (decision record, approval, migration note, regression tests).

### 5) `target-architecture`
**Goal:** Finalize architecture and migration strategy.
- Define module boundaries (parser, registry/data, resolver, transform engine, grid/resource).
- Define compatibility-first API strategy.
- Standardize error/diagnostics behavior (no silent fallback behavior).
- Lock the managed-only architecture decision.

### 6) `platform-modernization`
**Goal:** Modernize technical platform before larger feature work.
- Apply the final TFM strategy (`netstandard2.0` required).
- Modernize build/test pipeline and CI matrix.
- Introduce analyzer baseline and deprecation policy.

### 7) `quality-gates-stylecop`
**Goal:** Enforce quality and language gates before feature development.
- Standardize StyleCop 6.2.0 configuration.
- Enforce maximum analyzer level in CI.
- Enforce warning policy (no unapproved suppression).
- Verify English-only naming/documentation on new content.

### 8) `registry-data-layer`
**Goal:** Introduce a data-driven CRS/operation metadata layer.
- Define authority/registry abstractions without runtime SQLite/native dependencies.
- Build versioned authority import pipeline.
- Provide lookup APIs for CRS, datum, operations, and area of use.

### 9) `format-support-upgrade`
**Goal:** Modernize input/output formats.
- Add WKT2 parsing support incrementally.
- Plan export paths (WKT2 and optional PROJJSON).
- Preserve compatibility with legacy WKT1 paths.

### 10) `operation-resolution-engine`
**Goal:** Move transformation selection from static wiring to data-driven resolution.
- Implement CRS->CRS candidate ranking (accuracy, area of use, grid availability).
- Support chained/pipeline operations.
- Reconnect legacy transformation factory to the new resolver.

### 11) `grid-resource-management`
**Goal:** Add deterministic grid/resource management.
- Design local grid resolver and optional network acquisition model.
- Define caching and missing-grid behavior deterministically.
- Add test assets for representative grid scenarios.

### 12) `projection-kernel-alignment`
**Goal:** Align projection kernel with prioritized `PROJ` methods.
- Implement prioritized missing methods from the gap analysis.
- Add numerical stability and inverse checks.
- Track performance against baseline scenarios.

### 13) `verification-suite`
**Goal:** Establish robust anti-regression verification.
- Derive golden-master tests from acceptable reference cases.
- Cross-validate with EPSG reference points.
- Enforce CI quality gates for correctness and stability.
- Add API compatibility regression tests for legacy members, including obsolete-marked members.

### 14) `migration-docs-release`
**Goal:** Keep adoption and release process controlled.
- Provide migration guidance for API users.
- Define versioning and breaking-change communication.
- Document data/grid update operations reproducibly.
- Keep docs concise and example-light by default.
- For every obsolete member, provide replacement guidance and support horizon in release/migration documentation.

### 15) `mutation-testing-stryker`
**Goal:** Use mutation testing to close test effectiveness gaps.
- Integrate `dotnet-stryker` into quality workflow.
- Run mutation tests on critical modules and analyze survivors.
- Add focused unit tests to eliminate weak spots.

### 16) `benchmarkdotnet-parity`
**Goal:** Add performance benchmarking based on `PROJ` benchmark intent.
- Analyze `spec\PROJ\test\benchmark` scenarios.
- Implement equivalent Benchmark.NET suites in `src`.
- Use before/after results to guide optimizations.

### 17) `managed-data-packaging`
**Goal:** Provide reference data without runtime SQLite/native dependencies.
- Define build-time data materialization pipeline.
- Choose managed output format (JSON/binary/resource/codegen).
- Add optional generator if required for transform/compression.
- Ensure deterministic outputs for identical inputs.

### 18) `tfm-target-policy`
**Goal:** Define minimal and justified target framework matrix.
- Lock `netstandard2.0` as mandatory output.
- Activate additional TFMs only with measurable value.
- Document concrete per-TFM differences.
- Prevent unnecessary builds.

## Measurable gates and acceptance criteria
- Public API breaks without approved design record: **0**.
- API diff report required for every merge touching public surface: **100%** coverage.
- Existing public API member retention across modernization scope: **100%**.
- Existing public API behavioral compatibility tests for supported scenarios: **100%** pass rate.
- Obsolete-marked legacy member behavioral compatibility tests: **100%** pass rate.
- Obsolete-marked public members with explicit replacement guidance and support horizon: **100%**.
- CI test pass rate on required matrix: **100%**.
- New/modified code warning count at merge: **0**.
- Repository-wide warning count before release branch: **0**.
- Mutation score target for critical modules: **>= 80%** (ratchet upward after baseline).
- Surviving critical-path mutants accepted without explicit issue: **0**.
- Performance regression budget for key benchmark scenarios: **<= 5%** unless approved.
- Benchmark reproducibility check (same commit, same environment, repeated run variance tracked): required.

## Deterministic data pipeline policy
- Pin every reference-data input by source version.
- Store and validate checksums for all raw inputs.
- Generator output must be deterministic (byte-for-byte for same input/tool version).
- Tool version for data generation must be pinned and tracked.
- Generated artifacts must include metadata manifest (source version, checksum set, generator version).

## Versioned baseline artifacts
- Version the public API snapshot artifact and keep it in the repository as merge evidence.
- Version the legacy API compatibility matrix (including obsolete member coverage status).
- Version benchmark environment profile and scenario set (runtime, OS, hardware summary, benchmark configuration).
- Require artifact updates when related scope changes (API surface, compatibility expectations, benchmark scenarios).

## Parallel execution plan (`/fleet` + `git worktree`)
- Parallelize only where file overlap is low.
- Keep one integration branch and short-lived task branches.
- Keep `spec` read-only in all agents/worktrees.

### Wave A (parallel, recommended now)
- `proj-gap-analysis`
- `public-api-baseline`
- `tfm-target-policy`

### Wave B (sequential baseline setup)
- `xunit-v3-migration`
- `quality-gates-stylecop`
- `platform-modernization`

### Wave C (dependency-safe split)
- C1: `target-architecture` (sequential prerequisite)
- C2: `managed-data-packaging` (starts only after C1 and quality gates)
- C3 (parallel after C2): `registry-data-layer` and `format-support-upgrade` with explicit file ownership split

### Wave D (mixed integration)
- `operation-resolution-engine` and `grid-resource-management` can start partially in parallel, then integrate together.
- `projection-kernel-alignment` starts after prerequisites are satisfied.

### Wave E (endgame hardening)
- `verification-suite`
- `mutation-testing-stryker`
- `benchmarkdotnet-parity`
- `migration-docs-release`

## Merge policy by wave (required checklist)

### For every wave merge
- Full CI for touched targets is green.
- API diff report attached (or explicitly not applicable).
- Analyzer/StyleCop/warning policy satisfied.
- No undocumented suppression introduced.
- Wave-specific evidence attached (tests/mutation/benchmark/docs as applicable).
- Required baseline artifacts are updated when scope changed.

### Wave-specific evidence
- Wave A: feature matrix + API baseline + compatibility contract + TFM decision record.
- Wave B: xUnit v3 migration evidence + strict analyzer/StyleCop gate in CI.
- Wave C: architecture decision record + deterministic data pipeline proof.
- Wave D: integration test evidence for resolver/grid/kernel interactions.
- Wave E: mutation report, benchmark report, API compatibility report (including obsolete members), concise migration docs.

## Dependencies
- `xunit-v3-migration`, `test-parity-matrix`, and `public-api-baseline` are gatekeepers before feature implementation.
- `test-parity-matrix` depends on `xunit-v3-migration`.
- `target-architecture` depends on `proj-gap-analysis`, `xunit-v3-migration`, `test-parity-matrix`, `public-api-baseline`, and `tfm-target-policy`.
- `platform-modernization` depends on `tfm-target-policy`.
- `quality-gates-stylecop` is a gatekeeper for all implementation packages.
- `managed-data-packaging` depends on `target-architecture` and `quality-gates-stylecop`.
- `registry-data-layer` depends on `target-architecture`, `quality-gates-stylecop`, and `managed-data-packaging`.
- `format-support-upgrade` depends on `target-architecture`, `public-api-baseline`, and `quality-gates-stylecop`.
- `operation-resolution-engine` depends on `registry-data-layer`, `format-support-upgrade`, and `quality-gates-stylecop`.
- `grid-resource-management` depends on `registry-data-layer`, `public-api-baseline`, and `quality-gates-stylecop`.
- `projection-kernel-alignment` depends on `proj-gap-analysis`, `test-parity-matrix`, `target-architecture`, `public-api-baseline`, and `quality-gates-stylecop`.
- `verification-suite` depends on `xunit-v3-migration`, `public-api-baseline`, `operation-resolution-engine`, `grid-resource-management`, and `projection-kernel-alignment`.
- `mutation-testing-stryker` depends on `verification-suite`.
- `benchmarkdotnet-parity` depends on `verification-suite` and `mutation-testing-stryker`.
- `migration-docs-release` depends on `verification-suite`, `mutation-testing-stryker`, and `benchmarkdotnet-parity`.

## Risks and open decisions
- Full parity with `PROJ` remains multi-phase; strict scope boundaries are required.
- Data licensing/distribution constraints for reference datasets and grids must be clarified early.
- Full legacy API behavior preservation while modernizing internals increases implementation complexity and test load.

## Definition of done (per phase)
- Architecture/decision records accepted.
- Code/tests/CI gates green.
- Measurable improvement demonstrated (coverage/accuracy/performance).
- Concise user-facing documentation updated where needed.
