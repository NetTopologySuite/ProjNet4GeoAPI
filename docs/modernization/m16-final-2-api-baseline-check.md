# Milestone 16 — final-2 API Baseline Check

## Validation commands
- Normal mode:
  - `dotnet test test\ProjNet.Tests\ProjNET.Tests.csproj -c Release --no-build --nologo --filter "FullyQualifiedName~PublicApiBaselineTests"`
- Update mode:
  - `PROJNET_UPDATE_PUBLIC_API_BASELINE=1 dotnet test test\ProjNet.Tests\ProjNET.Tests.csproj -c Release --no-build --nologo --filter "FullyQualifiedName~PublicApiBaselineTests"`

## Results
- Normal mode: `1 total`, `1 passed`, `0 failed`.
- Update mode: `1 total`, `1 passed`, `0 failed`.
- `src\ProjNet\PublicAPI.Shipped.txt`: unchanged after update-mode run.

## Conclusion
- No unexpected public API drift detected.
- Current shipped API baseline is consistent with the compiled assembly surface.
