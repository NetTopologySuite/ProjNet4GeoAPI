# WKT parsing benchmark history

This snapshot records the main WKT parser performance checkpoints that were used
to drive the M92-M99 optimization work and the later Iteration 11 verification.

- **Benchmark command**: `dotnet run -c Release --project src\ProjNet.Benchmark -- --filter "*WktParsingBenchmarks*" "*WktBulkParsingBenchmarks*"`
- **Primary scenarios tracked here**:
  - `ParseAllCatalogWkt1`
  - `ParseAllCatalogWkt2`
- **Interpretation**:
  - Lower runtime is better.
  - Lower allocation is better.
  - The tree-parser migration temporarily caused a severe regression, which was
    then recovered over several optimization milestones.

## Snapshot table

| Snapshot | Context | WKT1 runtime | WKT1 alloc | WKT2 runtime | WKT2 alloc |
|---|---|---:|---:|---:|---:|
| `c33f984` | Pre-migration baseline | 71.73 ms | 82.47 MB | 95.11 ms | 120.05 MB |
| `e5ac2ea` | Initial post-migration regression | 315.20 ms | 514.41 MB | 142.80 ms | 187.18 MB |
| M92 | First major recovery pass | 84.63 ms | 109.47 MB | 104.79 ms | 128.59 MB |
| M93 | Source-backed node recovery | 73.55 ms | 66.61 MB | 90.99 ms | 84.32 MB |
| M94 | Post-cleanup verification | 74.15 ms | 66.61 MB | 98.08 ms | 80.76 MB |
| M98 | Later parser verification | 87.72 ms | 62.26 MB | 110.61 ms | 67.11 MB |
| M99 | Guard-cleanup benchmark snapshot | 51.20 ms | 62.26 MB | 62.51 ms | 67.11 MB |
| 2026-04-15 | Iteration 11 reference | 61.79 ms | 62.26 MB | 74.53 ms | 66.74 MB |

## Summary

- Relative to the original baseline, the current parser remains faster in bulk:
  - `ParseAllCatalogWkt1`: `71.73 ms -> 61.79 ms` (`-13.9%`)
  - `ParseAllCatalogWkt2`: `95.11 ms -> 74.53 ms` (`-21.6%`)
- Relative to the worst post-migration state, the current parser recovered most
  of the lost runtime and nearly all excess allocation pressure.
- `M99` remains the fastest recorded runtime snapshot so far, but the later
  Iteration 11 state retains the same low-allocation profile while staying
  comfortably ahead of the original baseline.

## Notes

- The current reference was captured after the full `CODE_REVIEW_5` remediation
  work reached a green build and full test run.
- More detailed ad-hoc benchmark notes and raw logs were used during development,
  but this file is the committed repository snapshot for the main WKT parser
  checkpoints referenced by the review and follow-up discussions.
