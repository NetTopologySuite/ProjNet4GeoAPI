# Milestone 15 — cov-6 Renamed Test Validation

## Validation commands
- `dotnet test ProjNet4GeoAPI.sln -c Release --no-build --nologo --logger "trx;LogFileName=m15-cov-6.trx" --results-directory "test\ProjNet.Tests\TestResults\m15-cov-6"`
- `rg "SpecialtyProjectionBatch|BatchA|BatchB|BatchC|BatchD" test\ProjNet.Tests`

## Full-suite result
- Tests: `3768 total`, `3250 passed`, `0 failed`, `518 skipped`.
- TRX artifact: `test\ProjNet.Tests\TestResults\m15-cov-6\m15-cov-6.trx`.

## Legacy-name verification
- Source check: no remaining `SpecialtyProjectionBatch*` / `BatchA..BatchD*` identifiers under `test\ProjNet.Tests`.
- TRX execution check: `LEGACY_CLASS_HITS=0` for class names matching legacy batch patterns.

## Renamed class execution evidence (TRX mapping)
- `GlobularAndMiscProjectionTests`: 52 executed results.
- `InterruptedAndSpecialMercatorProjectionTests`: 43 executed results.
- `ConicAndEqualAreaMiscProjectionTests`: 51 executed results.
- `SimpleConicAndImwProjectionTests`: 46 executed results.
- `ModifiedStereographicProjectionTests`: 35 executed results.
- `S2ProjectionTests`: 44 executed results.
- `SpaceObliqueMercatorProjectionTests`: 24 executed results.
- `PutninsAndVanDerGrintenProjectionTests`: 57 executed results.
- `AdamsGuyouPeirceProjectionTests`: 41 executed results.
- `SpilhausProjectionTests`: 16 executed results.
- `IcosahedralProjectionTests`: 37 executed results.
- `LeacUpsWebMercatorProjectionTests`: 28 executed results.
- `SchProjectionTests`: 15 executed results.

## Conclusion
- Renamed M13 projection test classes are actively executed in the current full suite.
- No legacy batch naming remains in either source identifiers or executed test class names.
