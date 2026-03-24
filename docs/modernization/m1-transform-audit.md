# Milestone 1 Step 2: Transformation Coverage Audit (`parity-2-transform-audit`)

## Scope

This audit compares transformation coverage between:

- C++ reference: `spec\PROJ\src\transformations\*.cpp`
- .NET runtime: `src\ProjNet\CoordinateSystems\Transformations\*.cs`
- pipeline dispatch surface: `ProjPipelineMathTransformFactory.TryCreateStepTransform(...)`

Special focus (explicit request): `affine`, `cart`/`geocent`, `push`/`pop`, `molobadekas`, `geogoffset`.

## C++ reference inventory

C++ transformation source files discovered: **12**

1. `affine.cpp`
2. `defmodel.cpp`
3. `deformation.cpp`
4. `gridshift.cpp`
5. `helmert.cpp`
6. `hgridshift.cpp`
7. `horner.cpp`
8. `molodensky.cpp`
9. `tinshift.cpp`
10. `vertoffset.cpp`
11. `vgridshift.cpp`
12. `xyzgridshift.cpp`

## .NET implementation inventory

Transformation implementation classes present include (non-exhaustive list for C++ parity scope):

- `AffineTransform`
- `DefModelMathTransform`
- `DeformationMathTransform`
- `HelmertMathTransform`
- `MolodenskyMathTransform`
- `HornerMathTransform`
- `TinShiftMathTransform`
- `VertOffsetMathTransform`
- `Ntv2HGridShiftMathTransform`
- `GeoTiffHGridShiftMathTransform`
- `GtxVGridShiftMathTransform`
- `GeoTiffVGridShiftMathTransform`
- `GeoTiffXyzGridShiftMathTransform`
- `GeocentricTransform`

## Direct mapping results (C++ transformations -> .NET runtime)

| C++ transformation | .NET implementation | Pipeline dispatch status |
| --- | --- | --- |
| `affine` | `AffineTransform` + WKT path (`MathTransformWktReader`) | **Not** dispatched as `+proj=affine` in `ProjPipelineMathTransformFactory` |
| `defmodel` | `DefModelMathTransform.TryCreate(...)` | dispatched (`projCode.Equals("defmodel")`) |
| `deformation` | `DeformationMathTransform.TryCreate(...)` | dispatched (`projCode.Equals("deformation")`) |
| `gridshift` | horizontal grid shift path | dispatched (`projCode.Equals("gridshift")`) |
| `hgridshift` | `Ntv2HGridShiftMathTransform` / `GeoTiffHGridShiftMathTransform` | dispatched (`projCode.Equals("hgridshift")`) |
| `helmert` | `HelmertMathTransform.TryCreate(...)` | dispatched (`projCode.Equals("helmert")`) |
| `horner` | `HornerMathTransform.TryCreate(...)` | dispatched (`projCode.Equals("horner")`) |
| `molodensky` | `MolodenskyMathTransform.TryCreate(...)` | dispatched (`projCode.Equals("molodensky")`) |
| `tinshift` | `TinShiftMathTransform.TryCreate(...)` | dispatched (`projCode.Equals("tinshift")`) |
| `vertoffset` | `VertOffsetMathTransform.TryCreate(...)` | dispatched (`projCode.Equals("vertoffset")`) |
| `vgridshift` | `GtxVGridShiftMathTransform` / `GeoTiffVGridShiftMathTransform` | dispatched (`projCode.Equals("vgridshift")`) |
| `xyzgridshift` | `GeoTiffXyzGridShiftMathTransform` | dispatched (`projCode.Equals("xyzgridshift")`) |

## Special-focus operations analysis

### `affine`

- C++: declared in `affine.cpp` (`PROJ_HEAD(affine, ...)`).
- .NET: `AffineTransform` exists and is used in WKT math transform reading.
- Pipeline dispatch: **missing** direct `+proj=affine` branch in `TryCreateStepTransform`.
- Classification: **implemented runtime class, missing direct pipeline token dispatch**.

### `cart` / `geocent`

- C++: geodetic/cartesian conversions are exposed as `cart` and `geocent`.
- .NET: `GeocentricTransform` exists; geocentric conversion is used in coordinate operation composition via `CoordinateTransformationFactory.CreateCoordinateOperation(GeocentricCoordinateSystem)`.
- Pipeline dispatch: **missing** direct `+proj=cart` and `+proj=geocent` branches.
- Classification: **implemented through factory/resolver/runtime composition; missing direct pipeline token dispatch**.

### `push` / `pop`

- C++: pipeline stack operations.
- .NET: no dedicated `push`/`pop` dispatch in `ProjPipelineMathTransformFactory`.
- GIE harness (`GieBuiltinsTheoryTests`) filters unsupported runtime operations through `TryIsRuntimeOperationSupported`, so unsupported tokens are skipped from execution lanes.
- Classification: **missing direct runtime operation support**.

### `molobadekas`

- C++: exposed as its own `PROJ_HEAD` in `helmert.cpp`.
- .NET: Helmert implementation exists (`HelmertMathTransform`) and EPSG generated data references Molodensky-Badekas method names, but there is no dedicated `+proj=molobadekas` dispatch token branch.
- Classification: **partially represented by Helmert ecosystem, missing explicit pipeline token dispatch**.

### `geogoffset`

- C++: declared together with `affine` in `affine.cpp`.
- .NET: no dedicated `+proj=geogoffset` dispatch branch found.
- Classification: **missing direct runtime operation support**.

## Overall result for Milestone 1 Step 2

For the C++ transformation file set (`spec\PROJ\src\transformations\*.cpp`), the .NET runtime has implementation coverage for the main transformation families. Gaps are concentrated in **direct pipeline token dispatch parity** for:

- `affine`
- `cart`
- `geocent`
- `push`
- `pop`
- `molobadekas`
- `geogoffset`

This means transformation capability exists in several cases through other runtime paths (factory/WKT/composed transforms), but `+proj=` step-token parity is not complete yet for the listed operations.

