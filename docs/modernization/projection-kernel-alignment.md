# Projection Kernel Alignment Baseline

## Implemented in this step
- Expanded projection alias coverage in `ProjectionsRegistry` for commonly used modern names:
  - `Mercator (variant A)`
  - `Mercator (variant B)`
  - `Web_Mercator`
  - `Transverse_Mercator_South_Oriented`
  - `Lambert_Conformal_Conic_1SP`
  - `Lambert_Conformal_Conic_2SP_Belgium`
  - `Lambert_Conic_Conformal_(1SP)`
- Added kernel alignment tests in `test\ProjNet.Tests\ProjectionKernelAlignmentTests.cs`.

## Validation
- Alias tests pass under xUnit v3.
- Full solution tests remain green after alias additions.

## Compatibility
- Public API unchanged.
- Behavior is additive: previously unsupported alias names now resolve to existing projection implementations.
