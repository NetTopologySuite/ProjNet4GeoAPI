# Milestone 14 Review — Internal Span/stackalloc/ArrayPool Optimization

## Scope reviewed
- `perf-1` through `perf-7`
- Internal allocation and parsing optimizations in:
  - `MathTransform`
  - `AffineTransform`
  - `HelmertMathTransform`
  - `HornerMathTransform`
  - `GeoTiffGridLoader`
  - `DeformationMathTransform`

## Verification performed
- Release build + focused runtime tests after each optimization step.
- Full Release build + full test suite for milestone validation.
- Manual review of stack/pooled buffer lifetimes:
  - `stackalloc` spans remain method-local and do not escape.
  - pooled buffers in `GeoTiffGridLoader` are always returned via `try/finally`.

## Findings
- `MathTransform` and `AffineTransform` hot paths now use tighter scratch-buffer patterns with reduced transient allocations.
- `HelmertMathTransform` and `HornerMathTransform` moved CSV numeric parsing from `Split(...)` + temporary arrays to span-based parsing.
- `GeoTiffGridLoader` now uses pooled temporary sample-read buffers and copies only final-sized arrays into persistent grid storage.
- `DeformationMathTransform` endian readers no longer allocate temporary `byte[4]/byte[8]` buffers per value.

## Conclusion
Milestone 14 optimization changes are complete, correctness-safe, and validation-clean with no observed regressions.
