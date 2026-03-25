# Milestone 15 — cov-7 Coverage Delta

## Validation commands
- `dotnet test ProjNet4GeoAPI.sln -c Release --collect:"Code Coverage" --logger "trx;LogFileName=m15-cov-7.trx" --results-directory "test\ProjNet.Tests\TestResults\m15-cov-7"`
- `dotnet tool run dotnet-coverage merge "test\ProjNet.Tests\TestResults\m15-cov-7\<guid>\*.coverage" -f cobertura -o "docs\modernization\m15-cov-7-post.cobertura.xml"`

## Full-suite result
- Tests: `3768 total`, `3250 passed`, `0 failed`, `518 skipped`.
- TRX: `test\ProjNet.Tests\TestResults\m15-cov-7\m15-cov-7.trx`.
- Coverage artifacts:
  - Baseline: `docs\modernization\m15-cov-1-baseline.cobertura.xml`
  - Post-M15-wave: `docs\modernization\m15-cov-7-post.cobertura.xml`

## Overall line coverage
- Baseline (`cov-1`): `93.69%`
- Post (`cov-7`): `93.71%`
- Delta: `+0.02%`

## Changed-file delta (baseline → post)
| File | Baseline line coverage | Post line coverage | Delta |
|---|---:|---:|---:|
| `MathTransform.cs` | 40.21% | 42.78% | +2.58% |
| `AffineTransform.cs` | 79.58% | 79.58% | 0.00% |
| `HelmertMathTransform.cs` | 87.23% | 87.23% | 0.00% |
| `HornerMathTransform.cs` | 89.50% | 89.50% | 0.00% |
| `GeoTiffGridLoader.cs` | 87.77% | 87.67% | -0.10% |
| `DeformationMathTransform.cs` | 75.18% | 75.18% | 0.00% |

## Notes
- The `MathTransform.cs` increase reflects new span/boundary tests (`cov-2`, `cov-4`).
- `GeoTiffGridLoader.cs` changed line counts after testability overloads introduced in `cov-5`; the small percentage shift is denominator-sensitive rather than a functional regression.
