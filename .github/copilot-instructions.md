# Copilot instructions

## Repository context

- Build and test from the repository root.
- Build with `dotnet build .\ProjNet4GeoAPI.sln -c Release --tl:off -v minimal`.
- Test with `dotnet test --project .\test\ProjNet.Tests\ProjNET.Tests.csproj`.
- The main CI workflow is `.github/workflows/full-ci.yml`; benchmark monitoring lives in `.github/workflows/benchmarks.yml`; CodeQL uses `.github/workflows/codeql.yml`; mutation testing uses `.github/workflows/mutation-tests.yml`.
- Coverage generation mirrors CI after a Release build: run `dotnet tool restore`, then `dotnet build .\ProjNet4GeoAPI.sln -c Release --tl:off -v minimal`, then `dotnet dotnet-coverage collect --output .\coverage-out\coverage.cobertura.xml --output-format cobertura -- dotnet test --project .\test\ProjNet.Tests\ProjNET.Tests.csproj -c Release --no-build`.
- Curated benchmark runs mirror CI after a Release build: use `.\.github\scripts\Invoke-CuratedBenchmarks.ps1 -Mode Smoke -NoBuild` or `.\.github\scripts\Invoke-CuratedBenchmarks.ps1 -Mode Full -NoBuild`; keep `Get-CuratedBenchmarkConfiguration.ps1` as the single source of truth and validate converted datasets with `Convert-BenchmarkReports.ps1` plus `Assert-CuratedBenchmarkDataset.ps1`.
- The library ships `netstandard2.0`, `netstandard2.1`, and `net8.0`.
- Public API changes must update `src/ProjNet/PublicAPI.Shipped.txt` intentionally.
- In touched C# code, prefer `is null` / `is not null` checks and avoid broad warning suppressions.

