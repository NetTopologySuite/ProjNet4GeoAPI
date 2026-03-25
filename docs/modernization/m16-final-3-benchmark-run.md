# Milestone 16 — final-3 Benchmark Validation

## Validation commands
- `dotnet build src\ProjNet.Benchmark\ProjNet.Benchmark.csproj -c Release --nologo`
- `dotnet run --project src\ProjNet.Benchmark\ProjNet.Benchmark.csproj -c Release -- --list flat`

## Results
- Benchmark project build succeeded (`net8`, 2 warnings, 0 errors).
- Benchmark entrypoint started successfully and listed benchmark cases after executing startup validation.
- Listed benchmark methods include all expected `PerformanceTests` and `ProjParityBenchmarks` entries (including noise and additional CRS-pair scenarios).

## Notes
- BenchmarkDotNet does not expose a `--validate` command-line option; using `--list flat` is a safe non-benchmarking execution mode that still exercises startup flow (`PerformanceTests.Validate()` and `ProjParityBenchmarks.Validate()`) before listing runnable benchmarks.
