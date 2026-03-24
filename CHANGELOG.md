# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog and this project follows Semantic Versioning.

## [3.0.0] - Unreleased

### Added

- Added managed-data runtime support for coordinate system definitions and modernization artifacts under `docs/modernization`.
- Added targeted mutation-focused tests for `IdentityMathTransform` to strengthen regression safety.
- Added explicit release validation commands in `README.md` for API baseline, benchmark, and parity checks.

### Changed

- Updated package version line to `3.0.0` via shared build props (`src/Directory.Build.props`).
- Updated `CoordinateSystemFactory.CreateFromWkt` parameter metadata from `WKT` to `wkt`.
- Migrated test suite usage to xUnit v3 APIs and removed NUnit compatibility dependencies.
- Normalized `MapProjection` constants to PascalCase and retained compatibility aliases.

### Deprecated

- Legacy uppercase and snake_case `MapProjection` aliases are now compatibility members and should be replaced with PascalCase names.

### Notes

- `PackageValidationBaselineVersion` is set to `2.1.0` until `3.0.0` is published, so package validation restore remains stable during release preparation.
- Mutation testing reruns currently report survivors in `IdentityMathTransform` with `coveredBy=[]` despite targeted tests; this is tracked as a tooling correlation blocker in Milestone 11.
