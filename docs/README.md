# ProjNET documentation index

This folder collects the user-facing and project-level reference material that
complements the top-level `README.md`.

## Start here

| Path | Purpose |
| --- | --- |
| [`concepts.md`](concepts.md) | Core terminology for CRS, datum, projections, transformation pipelines, formats, and the managed EPSG catalog. |
| [`cookbook.md`](cookbook.md) | Practical recipes for the most common ProjNET workflows. |
| [`grids.md`](grids.md) | Detailed guidance for NTv2, GTX, and GeoTIFF grid configuration. |
| [`projection-coverage.md`](projection-coverage.md) | Audited projection and PROJ alias coverage matrix. |
| [`benchmarks/`](benchmarks/) | Benchmark history and performance-focused documentation. |
| [`grid-fixture-notes/`](grid-fixture-notes/) | Provenance notes for vendored grid fixtures and related artifacts. |

## Suggested reading order

1. Start with [`concepts.md`](concepts.md) if you are new to CRS terminology.
2. Move to [`cookbook.md`](cookbook.md) for concrete API examples.
3. Read [`grids.md`](grids.md) before enabling grid-backed transformations in production.
4. Use [`projection-coverage.md`](projection-coverage.md) when you need detailed parity or alias information.
