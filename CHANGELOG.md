# Changelog

All notable changes to this project are documented in this file.

The format is based on Keep a Changelog and this project follows Semantic Versioning.

## [3.0.0] - Unreleased

### Added

- Added managed EPSG catalog runtime and supporting generated data access layers for coordinate reference and operation resolution.
- Added EPSG WKT ZIP-based generator pipeline as the primary managed data source.
- Added `net8.0` target for `ProjNET` in addition to `netstandard2.0` and `netstandard2.1`.
- Added source-generated regex paths (conditional on .NET 8) for selected hot regex call sites.
- Added structured modernization audit/finalization artifacts under `docs/modernization/` for parity, generator, style, license, test, and documentation waves.
- Added `LICENSES/` folder and `NOTICE.md` for consolidated attribution and licensing context.
- Added broad projection and transformation runtime verification coverage, including direct proj2proj parity fixtures.
- Added additive span-based public API overloads for key transformation and WKT parsing workflows:
  - `MathTransform.Transform(ReadOnlySpan<double>, Span<double>)`
  - `MathTransform.GetCodomainConvexHull(ReadOnlySpan<double>)`
  - `MathTransform.GetDomainFlags(ReadOnlySpan<double>)`
  - `CoordinateSystemWktReader.Parse(ReadOnlySpan<char>)`
  - `Wgs84ConversionInfo.WriteAffineTransform(Span<double>)`
- Added benchmark scenarios aligned to relevant PROJ `bench_proj_trans` patterns, including deterministic noise-based runs and additional CRS pair coverage.
- Added 4D `+proj=axisswap` runtime support (including sign-aware time ordinate handling) in pipeline execution paths.
- Added focused axisswap coverage with new `AxisSwapMathTransformTests` and `AxisOrderHelperTests`, plus expanded pipeline validation scenarios for `+axis` / `+order` combinations.

### Changed

- Reworked EPSG generator output to reduce eager runtime initialization and lookup overhead:
  - removed generator dependency on `proj.db`,
  - replaced large eager arrays with on-demand switch-based lookup paths where applicable,
  - split generated catalog into focused partial files.
- Migrated the test stack fully to xUnit v3 and removed NUnit compatibility usage.
- Renamed phase-prefixed test files/classes to descriptive names that reflect tested behavior.
- Performed structural cleanup:
  - one top-level type per file in targeted areas,
  - Roman numeral class-name suffixes replaced with numeric suffixes,
  - shared projection constants consolidated.
- Modernized coding style for C# 12 consistency:
  - expanded expression-bodied members where appropriate,
  - converted applicable `using (...)` scopes to `using var`,
  - expanded target-typed `new` and collection-expression usage where safe.
- Updated XML documentation across public API surfaces (projections, transformations, coordinate systems, and services/IO) and removed stale external URL references in targeted doc blocks.
- Updated README to current project status, feature scope, and compatibility/build guidance.
- Replaced legacy Java-style stream tokenization for WKT parsing with a buffered span-based tokenizer (`WktTokenizer`) and integrated it across WKT readers.
- Unified versioning with Nerdbank.GitVersioning (`version.json`) and removed CI-specific legacy `Nts*` version computation paths.
- Moved `InternalsVisibleTo` declaration from source-level assembly attributes to MSBuild project configuration.
- Normalized historical block comments in handwritten source/test files to consistent line comments.
- Renamed opaque `SpecialtyProjectionBatch*` tests into descriptive projection-family-focused test classes.
- Optimized selected hot internal paths using `stackalloc`, `ReadOnlySpan<T>/Span<T>`, and `ArrayPool<T>` to reduce transient allocations.
- Improved GIE builtins conversion fallback handling by normalizing cs2cs-style operation tokens for runtime conversion attempts and prioritizing detailed transform skip reasons.

### Fixed

- Corrected outdated and inconsistent file attribution headers by adopting SPDX-style per-file headers based on provenance categories.
- Removed dead/commented legacy code found during structural cleanup.
- Fixed multiple legacy naming inconsistencies in projection class families and their registry references.
- Fixed pooled-buffer lifecycle coverage by adding explicit success/failure-path tests for `GeoTiffGridLoader` pool rental/return behavior.

### Deprecated

- Legacy uppercase and snake_case `MapProjection` aliases remain as compatibility members but should be replaced with PascalCase names in new code.

### Notes

- Package version line is aligned to `3.0.0` via shared build props (`src/Directory.Build.props`).
- `PackageValidationBaselineVersion` remains `2.1.0` until `3.0.0` is published.
- Current validation snapshot (Phase 3): full tests `3834 total / 3003 passed / 0 failed / 831 skipped`; GIE builtins `2465 total / 1664 passed / 0 failed / 801 skipped`.
