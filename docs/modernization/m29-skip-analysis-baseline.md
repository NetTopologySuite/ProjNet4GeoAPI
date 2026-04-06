# Milestone 29 - skip-analysis-baseline

## Validation command
- `dotnet test --project test\ProjNet.Tests\ProjNET.Tests.csproj`
- Captured output: `C:\Users\M37AA~1.KAR\AppData\Local\Temp\m29-skip-analysis-test-output.txt`

## Full-suite result
- Tests: `4872 total`, `4219 passed`, `653 skipped`, `0 failed`.
- Skip total was derived from categorized skip reasons because the captured MTP summary line for `übersprungen` was written with mojibake in the temporary output file.

## Harness scope
- `GieBuiltinsTheoryTests` contributes the builtins-wave skip baseline through four member-data sources:
  - `GetBuiltinsCases()` -> `builtins.gie`
  - `GetMoreBuiltinsCases()` -> `more_builtins.gie`
  - `GetDhdnEtrs89Cases()` -> `DHDN_ETRS89.gie`
  - `GetRemainingGieCases()` -> 17 additional fixture files (`4D-API_cs2cs-style.gie`, `adams_hemi.gie`, `adams_ws1.gie`, `adams_ws2.gie`, `axisswap.gie`, `defmodel.gie`, `deformation.gie`, `ellipsoid.gie`, `GDA.gie`, `geotiff_grids.gie`, `gridshift.gie`, `guyou.gie`, `nkg.gie`, `peirce_q.gie`, `spilhaus.gie`, `tinshift.gie`, `unitconvert.gie`).
- `ProjReferenceTests` contributes the additional `forward-delta-exceeds` skips through the edge-case and roundtrip parity checks.
- External integration tests contribute the database-connection skips.

## Skip categories
| Category | Count | Sources | Notes |
|---|---:|---|---|
| `higher-fidelity` | 612 | `GieBuiltinsTheoryTests` x612 | Result exists, but the numeric delta to the PROJ reference exceeds tolerance. |
| `first-wave-domain` | 15 | `GieBuiltinsTheoryTests` x15 | Current harness catches ArgumentException and skips unsupported edge-domain cases. |
| `runtime-features-missing` | 9 | `GieBuiltinsTheoryTests` x9 | The harness reports runtime features that are intentionally not wired into the current builtins wave. |
| `outside-domain` | 6 | `GieBuiltinsTheoryTests` x6 | Transform returned NaN or an unusable coordinate outside the supported domain. |
| `no-applicable-case` | 5 | `GieBuiltinsTheoryTests` x5 | Fixture parsing produced no applicable local case for the current row. |
| `no-db-connection` | 2 | `ExternalIntegrationTests` x2 | External database-backed integration tests are skipped without a connection string. |
| `push-pop-projection` | 2 | `GieBuiltinsTheoryTests` x2 | The builtins harness explicitly treats push/pop as out of wave instead of mapping them as pipeline operations. |
| `forward-delta-exceeds` | 2 | `ProjReferenceTests` x2 | ProjReference roundtrip prerequisites skip when the forward projection diverges too far from the PROJ reference. |

## Higher-fidelity delta magnitude buckets
| Delta bucket | Count |
|---|---:|
| `<1e-2` | 6 |
| `1e-2..1` | 11 |
| `1..1e2` | 14 |
| `1e2..1e4` | 14 |
| `1e4..1e6` | 57 |
| `>=1e6` | 510 |

## Top higher-fidelity projections
| Projection | Count |
|---|---:|
| `peirce_q` | 244 |
| `spilhaus` | 72 |
| `airocean` | 69 |
| `eqc` | 25 |
| `eqearth` | 15 |
| `bonne` | 12 |
| `gnom` | 12 |
| `eck4` | 10 |
| `cass` | 10 |
| `aea` | 9 |
| `pipeline` | 9 |
| `cea` | 9 |
| `euler` | 8 |
| `eqdc` | 8 |
| `alsk` | 8 |
| `fouc` | 8 |
| `etmerc` | 7 |
| `aeqd` | 5 |
| `longlat` | 5 |
| `fahey` | 4 |

## Domain-related projections
| Projection | Count |
|---|---:|
| `calcofi` | 8 |
| `bipc` | 6 |
| `eck3` | 4 |
| `gnom` | 2 |
| `peirce_q` | 2 |
| `airy` | 1 |

## Quick-win candidates
| Planned step | Target | Indicative skips | Rationale |
|---|---|---:|---|
| `skip-fix-eqc-mapping` | `eqc` | 25 | High skip count with low algorithmic complexity suggests a harness parameter-mapping issue rather than a kernel defect. |
| `skip-fix-pushpop-mapping` | `push/pop` | 2 | These are explicit harness omissions and should be removable without changing projection formulas. |
| `skip-fix-runtime-features` | `builtins runtime features` | 9 | The skip reason already points at missing harness wiring instead of projection math. |
| `skip-fix-no-applicable` | `fixture generation` | 5 | These skips come from case production rather than transform execution. |
| `accuracy-authalic-fix` | `cea/aea/eqearth` | 36 | A shared authalic-latitude chain could eliminate multiple low-complexity higher-fidelity skips in one place. |

## Immediate conclusions
- The current skip profile is dominated by `higher-fidelity` cases (`612/653`), so milestone 29 should first remove harness/configuration skips before algorithm work starts.
- `eqc`, `push/pop`, generic runtime-feature skips, and no-applicable-case generation are the smallest likely wins because their reasons point at harness behavior rather than projection formulas.
- The concentrated projection hotspots (`peirce_q`, `spilhaus`, `airocean`) justify their own high-complexity milestone instead of being mixed into the quick-win wave.
