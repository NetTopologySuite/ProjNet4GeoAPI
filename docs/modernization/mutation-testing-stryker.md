# Mutation Testing (Stryker) Baseline

## Implemented in this step
- Added local .NET tool manifest with `dotnet-stryker` for reproducible mutation runs.
- Added repository-level `stryker-config.json` with:
  - target solution/project/test-project wiring,
  - scoped mutation targets for critical modernization paths,
  - deterministic `coverage-analysis: off` baseline to avoid coverage-capture instability with the current test stack,
  - mutation thresholds (`high: 80`, `low: 70`, `break: 60`),
  - HTML/JSON reporting.
- Added dedicated GitHub Actions workflow (`.github/workflows/mutation-tests.yml`) for on-demand mutation execution.

## Usage
- Restore tools:
  - `dotnet tool restore`
- Run mutation test:
  - `dotnet dotnet-stryker --config-file stryker-config.json --output artifacts/stryker`

## Notes
- Mutation testing is intentionally separated from the default CI path to keep regular PR feedback fast.
- Thresholds are set for ratcheting and can be tightened as coverage quality improves.
