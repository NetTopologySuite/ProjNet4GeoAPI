# GitHub issue status

This document tracks the **42 currently open issues** in `NetTopologySuite/ProjNet4GeoAPI`
against the state of this modernization repository.

## Status meanings

- **Verified fixed** - behavior exists here and is covered by automated tests.
- **Partially addressed** - core plumbing exists, but the original issue is only partly solved,
  still needs documentation, or still lacks a reproducer-specific regression.
- **Open / future work** - not yet addressed in this repo.
- **Meta / out of scope** - licensing, release management, community, or non-ProjNET feature
  requests.

**Snapshot:** 2026-04-11  
**Counts:** 9 verified fixed, 16 partially addressed, 8 open / future work, 9 meta / out of scope.

## Verified fixed

| Issue | Title | Current state | Verification |
| --- | --- | --- | --- |
| #46 | Transform between the same coordinate system | Identity transforms are emitted for identical and equivalent CRS pairs instead of building a lossy transform chain. | `test\ProjNet.Tests\OperationResolutionEngineTests.cs::CreateFromCoordinateSystemsWithSameProjectedCoordinateSystemUsesIdentityTransform`; `test\ProjNet.Tests\OperationResolutionEngineTests.cs::CreateFromCoordinateSystemsWithEquivalentGeographicCoordinateSystemsUsesIdentityTransform` |
| #51 | Projection with NAD83 is not the same as ESRI | The reported NAD83 WKT scenario is now covered directly and stays within the expected tolerance. | `test\ProjNet.Tests\GitHub\GitHubIssueRegressionTests.cs::Nad83WktToWgs84MatchesExpectedCoordinate` |
| #56 | Include all supported EPSG SRIDs by default. | The generated managed EPSG catalog is the default provider and is instantiated exhaustively in tests. | `test\ProjNet.Tests\Generated\EpsgCatalogCoverageTests.cs::ManagedProviderShouldInstantiateEveryCatalogCoordinateReference` |
| #65 | WKT Parser Error | The reported Xian 1980 WKT now parses successfully as a projected CRS. | `test\ProjNet.Tests\GitHub\GitHubIssueWktRegressionTests.cs::Xian1980ProjectedCoordinateSystemParsesSuccessfully` |
| #78 | Support for Lambert Conformal Conic 1SP projection | The registry now exposes the expected Lambert 1SP aliases and the projection path is exercised in transformation tests. | `test\ProjNet.Tests\CoordinateTransformTests.cs::TestLamberTangentialConformalConicProjectionRegistryAndTransformation` |
| #83 | Adapt to new WKT for coordinate spatial reference | WKT2 CRS parsing is implemented across geographic, projected, vertical, compound, bound, engineering, temporal, and parametric cases. | `test\ProjNet.Tests\IO\CoordinateSystems\CoordinateSystemWktReaderWkt2Tests.cs::CreateFromWkt_ParsesSupportedWkt2CrsEquivalentToCatalogReference`; `test\ProjNet.Tests\IO\CoordinateSystems\CoordinateSystemWktReaderWkt2Tests.cs::CreateFromWkt_ParsesSupportedWkt2ProjectedCrsEquivalentToCatalogReference`; `test\ProjNet.Tests\IO\CoordinateSystems\CoordinateSystemWktReaderWkt2Tests.cs::CreateFromWkt_ParsesSupportedWkt2VerticalCrsEquivalentToCatalogReference`; `test\ProjNet.Tests\IO\CoordinateSystems\CoordinateSystemWktReaderWkt2Tests.cs::CreateFromWkt_ParsesSupportedWkt2CompoundCrsEquivalentToCatalogReference` |
| #86 | Getting an error saying PROJCRS not recognized | `PROJCRS` is recognized by the WKT2 reader and is exercised through catalog-equivalent projected CRS fixtures. | `test\ProjNet.Tests\IO\CoordinateSystems\CoordinateSystemWktReaderWkt2Tests.cs::CreateFromWkt_ParsesSupportedWkt2ProjectedCrsEquivalentToCatalogReference` |
| #92 | Add support for EPSG 5515 S-JTSK/05 / Modified Krovak | Krovak support is present and exercised through forward and inverse transformation coverage. | `test\ProjNet.Tests\CoordinateTransformTests.cs::TestKrovakGreenwichProjection`; `test\ProjNet.Tests\CoordinateTransformTests.cs::TestKrovakFerroProjection` |
| #106 | Handle WKT Extension Tag | WKT1 `EXTENSION[...]` nodes are now skipped safely in the parser, including the previously broken DATUM case. | `test\ProjNet.Tests\GitHub\GitHubIssueWktRegressionTests.cs::DatumLevelExtensionsParseAsProjectedCoordinateSystems`; `test\ProjNet.Tests\GitHub\GitHubIssueWktRegressionTests.cs::ProjcsLevelExtensionParsesAsProjectedCoordinateSystem`; `test\ProjNet.Tests\GitHub\GitHubIssueWktRegressionTests.cs::CompoundCoordinateSystemWithExtensionParsesSuccessfully` |

## Partially addressed

| Issue | Title | Current state | Verification / evidence |
| --- | --- | --- | --- |
| #14 | CoordinateTransformationFactory CreateFromCoordinateSystems south not supported | South-oriented Transverse Mercator support exists in the projection registry, but there is still no dedicated reproducer-level regression for this exact issue. | `docs\projection-coverage.md` |
| #29 | Implement loading multiple predefined transformations for GeographicCoordinateSystem | The operation resolver already prefers catalogued operations and can choose metadata-backed paths, but the original multi-candidate issue should still be revalidated against its exact reproducer. | `test\ProjNet.Tests\OperationResolutionEngineTests.cs::CreateFromCoordinateSystemsWithProjectedPairHavingDirectMetadataPrefersMetadataCandidate` |
| #45 | Transform coordinates using nadgrids? | NTv2, GeoTIFF, and GTX grid-shift support exists, but the user-facing guidance and issue-specific end-to-end scenario are still incomplete. | `test\ProjNet.Tests\HorizontalGridShiftRuntimeTests.cs::HgridshiftWithNtv2GridAppliesExpectedShift`; `test\ProjNet.Tests\GeoTiffGridRuntimeTests.cs::HgridshiftWithGeoTiffGridAppliesExpectedShift`; `test\ProjNet.Tests\VerticalGridShiftRuntimeTests.cs::VgridshiftWithGtxGridAppliesExpectedVerticalShift` |
| #47 | Equality of ICoordinateSystem instances | `EqualParams` is stable, but full value semantics (`Equals`, `GetHashCode`, singleton/reference semantics) have not been redesigned yet. | Existing equality coverage is broad but not issue-specific. |
| #50 | API issues to consider fixing for 3.0 | The planned v3 immutability and migration work directly targets this issue family, but M66 is still pending. | Planned milestone: `M66` in `plan.md` |
| #67 | Inaccurate Transformations - OSGB 1936 / British National Grid to WGS84 | The reported sample now has a regression test and stays within a practical tolerance, but grid-grade OSTN precision still depends on external grid data. | `test\ProjNet.Tests\GitHub\GitHubIssueRegressionTests.cs::Osgb36ToWgs84TransformationMatchesExpectedCoordinate` |
| #79 | Add EPSG 4979 Support? | EPSG:4979 and ellipsoidal 3D WKT2 CRS parse successfully and are present in the catalog, but full height-operation coverage remains limited. | `test\ProjNet.Tests\IO\CoordinateSystems\CoordinateSystemWktReaderWkt2Tests.cs::CreateFromWkt_ParsesTopLevelEllipsoidal3dGeographicCrsAsOperationalCompound`; `test\ProjNet.Tests\Generated\EpsgCatalogCoverageTests.cs::ManagedProviderShouldInstantiateEveryCatalogCoordinateReference` |
| #85 | How can i transforme a point from EPSG:2154 to EPSG:3857 ? | The direct EPSG-to-EPSG workflow exists through `CoordinateSystemServices`, but there is no dedicated 2154->3857 sample or issue regression yet. | General service-path coverage exists in `test\ProjNet.Tests\GitHub\GitHubIssueRegressionTests.cs::AmersfoortToWgs84TransformationProducesCoordinateInTheNetherlands` and other transformation tests. |
| #89 | Migrate to Proj.6 | The library has moved beyond the old WGS84-pivot-only model and now resolves direct operation/catalog paths, but parity with modern PROJ is still not complete. | `test\ProjNet.Tests\OperationResolutionEngineTests.cs`; `test\ProjNet.Tests\GitHub\GitHubIssueRegressionTests.cs::Nad83WktToWgs84MatchesExpectedCoordinate`; `test\ProjNet.Tests\GitHub\GitHubIssueRegressionTests.cs::Osgb36ToWgs84TransformationMatchesExpectedCoordinate` |
| #94 | ProjNET only supports [] objects | Tokenizer and reader infrastructure support non-square bracket handling, but this issue still deserves a direct WKT round-bracket regression if it resurfaces. | `test\ProjNet.Tests\WKT\WktTokenizerTests.cs::ReadCloserWithMismatchedBracketThrows` |
| #98 | Add support of modern coordinate systems that includes height | Vertical, compound, and bound CRS support is implemented, but not every height-related transformation scenario has dedicated regression coverage yet. | `test\ProjNet.Tests\IO\CoordinateSystems\CoordinateSystemWktReaderWkt2Tests.cs::CreateFromWkt_ParsesSupportedWkt2VerticalCrsEquivalentToCatalogReference`; `test\ProjNet.Tests\IO\CoordinateSystems\CoordinateSystemWktReaderWkt2Tests.cs::CreateFromWkt_ParsesSupportedWkt2CompoundCrsEquivalentToCatalogReference`; `test\ProjNet.Tests\Generated\EpsgCatalogCoverageTests.cs::ManagedProviderShouldInstantiateEveryCompoundCoordinateReferenceWithExpectedStructure` |
| #113 | Latitude & Longitude order confusing | Axis-order handling exists, but the public documentation and ergonomics are still easy to misunderstand. | `test\ProjNet.Tests\AxisOrderHelperTests.cs`; `test\ProjNet.Tests\OperationResolutionEngineTests.cs::CreateFromCoordinateSystemsWithEquivalentGeographicCoordinateSystemsUsesIdentityTransform` |
| #115 | WGS84 coordinates transformation to Cartesian coordinates | Geocentric/cartesian transformation support already exists, but the repository still lacks a dedicated how-to example for the issue. | `test\ProjNet.Tests\CoordinateTransformTests.cs::TestGeocentric` |
| #118 | Is it the ICoordinateTransformation thread safe ? | The codebase is moving toward a stronger answer here, but the actual thread-safety documentation work is still planned for M65 after the v3 immutability work. | Planned milestones: `M65` and `M66` in `plan.md` |
| #126 | Project suggested by Claude.ai in a response but resulted in ~ 150m offset due to incomplete solution | The catalog path is now smoke-tested and produces a plausible Dutch result, but high-precision validation for the exact original complaint still needs stronger reference data. | `test\ProjNet.Tests\GitHub\GitHubIssueRegressionTests.cs::AmersfoortToWgs84TransformationProducesCoordinateInTheNetherlands` |
| #134 | Is grid-based transformation support planned? | Yes in engine terms: NTv2, GeoTIFF, GTX, and XYZ-grid paths exist. What is still missing is polished documentation and more end-user guidance. | `test\ProjNet.Tests\HorizontalGridShiftRuntimeTests.cs`; `test\ProjNet.Tests\GeoTiffGridRuntimeTests.cs`; `test\ProjNet.Tests\VerticalGridShiftRuntimeTests.cs`; `test\ProjNet.Tests\XyzGridShiftRuntimeTests.cs` |

## Open / future work

| Issue | Title | Current state |
| --- | --- | --- |
| #4 | Implementation for GeoAPI.CoordinateSystems.Transformation.IMathTransformFactory | Legacy GeoAPI surface; not part of the active modernization milestones. |
| #27 | CreateFromWkt() in a Xamarin.Android project | No current reproducer or mobile-targeted regression exists in this repo. |
| #41 | Transform generates Z coordinate | Still open; the transformation API still exposes dimension behavior that needs deliberate v3 design work. |
| #66 | UTM ITRF96 3-30  TO WGS94 (GOOGLE EARTH) TRANSFORMATION | No dedicated reproducer, fixture, or regression has been added yet. |
| #81 | Add ProjectedCoordinateSystem.ED50_UTM | Convenience API not implemented yet. |
| #93 | confusing documentation for axis orientation | Functionality exists, but the documentation request itself is still open. |
| #124 | WGS84（EPSG4326）坐标转CGCS2000 108E（EPSG：4545） | No dedicated CGCS2000 fixture, sample, or regression has been added yet. |
| #135 | Better transformation from EPSG:5683 to EPSG:3857 | Still open; the concrete high-accuracy operation chain for this path needs its own investigation and regression coverage. |

## Meta / out of scope

| Issue | Title | Reason |
| --- | --- | --- |
| #18 | Licencing | Licensing decision, not an engine/runtime modernization task. |
| #39 | Bearing and Reverse bearing | Geodesic/bearing feature request, outside the current projection/transformation scope. |
| #82 | Rotate a set of coordinates within a PCS | Geometry manipulation request, not a CRS/projection engine issue. |
| #87 | How convert proj4js source to this lib source? | Usage/migration question, not a tracked engine defect. |
| #95 | Release version ? | Release-management topic. |
| #96 | Sqlite/sqlserver server side vs client side evaluation | ORM/query-provider topic, not a ProjNET core engine issue. |
| #99 | Seeking help | Community / maintainer-call issue. |
| #107 | License change to MIT | Licensing decision, not a code modernization task. |
| #114 | Convert map view objects to svg | Rendering/export feature request outside the scope of this library. |
