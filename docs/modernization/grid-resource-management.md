# Grid Resource Management Baseline

## Implemented in this step
- Added internal managed grid resolver in `ProjNet.Resources`:
  - `GridResourceResolver`
  - `GridResourceResolverOptions`
  - `GridResourceResolutionMode`
  - `IGridResourceFetchClient`
- Implemented deterministic resolution order:
  1. in-memory resolved path cache,
  2. local directories (in configured order),
  3. optional network fetch to a local cache directory (only in `LocalThenNetwork` mode).
- Added tests in `GridResourceResolverTests` for:
  - local-only resolution,
  - strict no-network behavior in local-only mode,
  - network fetch + cached reuse behavior.

## Compatibility and constraints
- Managed C# only, no SQLite runtime dependency, no native dependency.
- Internal-only baseline (no public API breakage).
- Deterministic missing-grid behavior: `TryResolve` returns `false` when a grid cannot be resolved.

## Next increment
- Resolver is now wired into projected `EPSG` metadata candidate selection for grid-dependent operations.
- Missing required grid resources now fail deterministically with `DataUnavailable:` in strict mode (`PROJNET_GRID_REQUIRED=true`).
- Add checksum/manifest verification for cached grid artifacts.
