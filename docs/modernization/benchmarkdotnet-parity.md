# Benchmark.NET Parity Baseline

## Implemented in this step
- Extended `src/ProjNet.Benchmark` with `ProjParityBenchmarks` for PROJ-aligned scenario groups:
  - EPSG:4326 -> EPSG:3857 (batched / one-by-one),
  - EPSG:4326 -> EPSG:32632 (batched),
  - EPSG:3857 -> EPSG:4326 (batched, via round-trip input preparation).
- Added deterministic benchmark data generation (`Random` seed fixed) and finite-value validation.
- Updated benchmark runner to use `BenchmarkSwitcher`, so both legacy and parity benchmark classes can be selected via CLI filters.

## Usage
- List available benchmarks:
  - `dotnet run -c Release --project src\\ProjNet.Benchmark\\ProjNet.Benchmark.csproj -- --list flat`
- Run parity benchmarks only:
  - `dotnet run -c Release --project src\\ProjNet.Benchmark\\ProjNet.Benchmark.csproj -- --filter *ProjParityBenchmarks*`

## Notes
- This baseline focuses on reproducible scenario coverage and comparison shape, not on absolute score targets.
