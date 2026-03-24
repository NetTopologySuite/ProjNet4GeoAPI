# Milestone 2b Review

## Scope
- Added `net8.0` target for `ProjNET` while preserving `netstandard2.0`/`netstandard2.1`.
- Applied conditional source-generated regex patterns for static regex hotspots.
- Converted source and benchmark `using (...)` blocks to scope-based `using var` where appropriate.
- Performed expression-bodied modernization across coordinate-system, transformation, IO/service, and selected simple method surfaces.
- Adopted collection expressions for empty-array cases with explicit target typing.
- Expanded target-typed `new()` usage in explicit-type contexts.
- Evaluated and adopted primary constructors only for simple immutable parameter-capture nested classes.

## Correctness Review
- Conversions were limited to syntax/style and verified to avoid behavior changes.
- `GeneratedRegex` was guarded by `#if NET8_0_OR_GREATER` with compiled fallback for netstandard targets.
- A regression introduced during `target-typed new()` (interface-typed initialization in `GridResourceResolver`) was immediately detected by build validation and fixed before final commits.

## Outcome
- Milestone 2b style objectives are fully implemented with all targeted steps completed and committed.
