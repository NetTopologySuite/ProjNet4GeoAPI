# Operation Resolution Engine Baseline

## Implemented in this step
- Introduced internal resolver entrypoint `CoordinateOperationResolver` and routed
  `CoordinateTransformationFactory.CreateFromCoordinateSystems(...)` through it.
- Added candidate-based selection baseline with:
  - identity candidate for equivalent source/target coordinate systems,
  - existing direct factory path as fallback candidate.
- Added internal `IdentityMathTransform` for deterministic no-op operations.
- Added regression tests in `OperationResolutionEngineTests` for:
  - same projected source/target,
  - equivalent geographic source/target loaded from WKT.

## Compatibility
- Existing public API surface is unchanged.
- Existing transformation paths are preserved via the direct resolver candidate.
- Behavior change is additive: equivalent CRS pairs now resolve to explicit identity transforms.

## Next increment
- Extend candidate model with metadata-driven ranking (accuracy, area-of-use, grid availability).
- Add chained operation candidates from registry/resource layers when available.
