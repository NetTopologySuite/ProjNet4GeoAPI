# Milestone 15 — cov-1 Coverage Baseline

## Validation commands
- `dotnet tool restore`
- `dotnet test ProjNet4GeoAPI.sln -c Release --collect:"Code Coverage" --logger "trx;LogFileName=m15-cov-1.trx" --results-directory "test\ProjNet.Tests\TestResults\m15-cov-1"`
- `dotnet tool run dotnet-coverage merge "test\ProjNet.Tests\TestResults\m15-cov-1\<guid>\*.coverage" -f cobertura -o "docs\modernization\m15-cov-1-baseline.cobertura.xml"`

## Tooling notes
- `Code Coverage` collector is available and produces `.coverage` artifacts in this environment.
- `XPlat Code Coverage` is not available here; baseline collection therefore uses the built-in collector plus `dotnet-coverage` conversion to Cobertura XML.
- `dotnet-tools.json` now includes `dotnet-coverage` (while keeping `dotnet-stryker` and `nbgv`) so the coverage conversion is reproducible.

## Full-suite test result (baseline run)
- Tests: `3741 total`, `3223 passed`, `0 failed`, `518 skipped`.
- Coverage overall line rate (Cobertura): `93.69%`.
- Artifacts:
  - TRX: `test\ProjNet.Tests\TestResults\m15-cov-1\m15-cov-1.trx`
  - Coverage input: `test\ProjNet.Tests\TestResults\m15-cov-1\<guid>\*.coverage`
  - Cobertura output: `docs\modernization\m15-cov-1-baseline.cobertura.xml`

## Changed-file baseline coverage (M12 + M14 focus)
| File | Classes matched | Lines | Hits | Line coverage |
|---|---:|---:|---:|---:|
| `MathTransform.cs` | 1 | 194 | 78 | 40.21% |
| `AffineTransform.cs` | 1 | 142 | 113 | 79.58% |
| `HelmertMathTransform.cs` | 4 | 517 | 451 | 87.23% |
| `HornerMathTransform.cs` | 1 | 343 | 307 | 89.50% |
| `GeoTiffGridLoader.cs` | 8 | 515 | 452 | 87.77% |
| `DeformationMathTransform.cs` | 5 | 814 | 612 | 75.18% |

## Initial gap shortlist for follow-up milestones
- `MathTransform.cs`: low coverage on helper paths and dimensional overload combinations (`Transform(Span<XYZ>)`, list/derivative/domain helper paths), which aligns with planned `cov-2` and `cov-4` additions.
- `GeoTiffGridLoader.cs`: weaker coverage around metadata/scaling interpretation (`ResolveHorizontalShiftScaleToDegree`, `ResolveAngularScaleToDegree`) and page/sample edge handling.
- `DeformationMathTransform.cs`: gaps around ellipsoid resolution and interpolation edge normalization (`TryResolveEllipsoid`, `TryNormalizeInterpolationCell`, interpolation boundary behavior).
- `AffineTransform.cs`: constructor/inversion edge paths and `TransformAffine` branch coverage can be increased with explicit matrix-case tests.
- `HelmertMathTransform.cs` and `HornerMathTransform.cs`: mostly high coverage, but inverse/identity and parse edge paths still have meaningful uncovered branches.
