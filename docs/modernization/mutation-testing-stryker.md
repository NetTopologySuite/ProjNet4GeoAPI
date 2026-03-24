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

## M11 blocker snapshot
- The xUnit v3 stack in this repository currently defaults to Microsoft Testing Platform behavior.
- To keep Stryker on the intended VSTest path, test projects are pinned with:
  - `<UseMicrosoftTestingPlatformRunner>false</UseMicrosoftTestingPlatformRunner>`
- Repeated focused reruns on `IdentityMathTransformMutationTests` still report:
  - `Killed: 0`, `Survived: 6` (full focused run),
  - `Killed: 0`, `Survived: 2` (narrow baseline run),
  - survivors all in `IdentityMathTransform` with `coveredBy=[]`.
- Manual mutant validation was executed by temporarily changing `IdentityMathTransform` constructor logic (`dimension < 2` -> `dimension > 2`) and running:
  - `dotnet test test/ProjNet.Tests/ProjNET.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~IdentityMathTransformMutationTests"`
  - Result: 3 focused mutation tests fail immediately, proving assertions are mutation-sensitive.
- A focused Stryker rerun on `IdentityMathTransform.cs` **without** `test-case-filter` reports:
  - `Number of tests found: 1230`
  - `2 total mutants will be tested`
  - `Killed: 2`, `Survived: 0`, mutation score `100.00%`
  - both equality mutants (`id=15032`, `id=15033`) are killed (killer test id resolves to `PublicApiBaselineTests.PublicApiMatchesBaseline`).
- Conclusion: the remaining false survivors are tied to `test-case-filter` behavior in this Stryker/xUnit v3 setup, not to missing test assertions. For M11 verification, use the no-filter focused run on the mutate scope.
