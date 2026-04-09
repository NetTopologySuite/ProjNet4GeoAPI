// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Linq;
using ProjNet.IO.CoordinateSystems;

/// <summary>
/// Provides the milestone-40 WKT2 keyword coverage matrix.
/// </summary>
internal static class Wkt2KeywordCoverageMatrix
{
    private const string WktReader = nameof(CoordinateSystemWktReader);
    private const string RootDispatchReference = WktReader + ".Parse, " + WktReader + ".TryParseNativeWkt2";
    private const string GeodeticReference = WktReader + ".ReadWkt2GeodeticCoordinateReferenceSystem";
    private const string CoordinateSystemReference = WktReader + ".ReadWkt2CoordinateSystemDefinition";
    private const string AxisReference = WktReader + ".ReadWkt2Axis, " + WktReader + ".ParseWkt2AxisOrientation";
    private const string HorizontalDatumReference = WktReader + ".ReadWkt2HorizontalDatum";
    private const string EllipsoidReference = WktReader + ".ReadWkt2Ellipsoid";
    private const string PrimeMeridianReference = WktReader + ".ReadWkt2PrimeMeridian";
    private const string AngularUnitReference = WktReader + ".ReadWkt2AngularUnit";
    private const string LinearUnitReference = WktReader + ".ReadWkt2LinearUnit";
    private const string MetadataSkipReference = WktReader + ".ShouldSkipWkt2MetadataNode, " + WktReader + ".SkipKeywordNode";
    private const string ProjectedReference = WktReader + ".ReadWkt2ProjectedCoordinateSystem";
    private const string BaseGeographicReference = WktReader + ".ReadWkt2BaseGeographicCoordinateSystem";
    private const string ConversionReference = WktReader + ".ReadWkt2Conversion, " + WktReader + ".ReadWkt2ProjectionMethod, " + WktReader + ".ReadWkt2ProjectionParameter, " + WktReader + ".NormalizeWkt2ProjectionParameterName";
    private const string VerticalReference = WktReader + ".ReadWkt2VerticalCoordinateSystem, " + WktReader + ".ReadWkt2VerticalDatum";
    private const string CompoundReference = WktReader + ".ReadWkt2CompoundCoordinateSystem";
    private const string BoundReference = WktReader + ".ReadWkt2BoundCoordinateSystem, " + WktReader + ".ReadWkt2AbridgedTransformationDefinition, " + WktReader + ".ApplyWkt2HorizontalBoundCoordinateSystemToSource, " + WktReader + ".ApplyWkt2VerticalBoundCoordinateSystemToSource";
    private const string IdentifierReference = WktReader + ".ReadWkt2Axis, " + WktReader + ".ReadWkt2HorizontalDatum, " + WktReader + ".ReadWkt2Ellipsoid, " + WktReader + ".ReadWkt2PrimeMeridian, " + WktReader + ".ReadWkt2ProjectedCoordinateSystem, " + WktReader + ".ReadWkt2VerticalCoordinateSystem, " + WktReader + ".ReadWkt2CompoundCoordinateSystem, " + WktReader + ".ReadWkt2AbridgedTransformationDefinition, " + WktReader + ".ReadIdentifierWithUnknownCode";
    private const string DefaultUnsupportedReference = WktReader + ".ReadWkt2GeodeticCoordinateReferenceSystem, " + WktReader + ".ReadWkt2ProjectedCoordinateSystem, " + WktReader + ".ReadWkt2VerticalCoordinateSystem, " + WktReader + ".ReadWkt2BoundCoordinateSystemComponent, " + WktReader + ".ParseNormalizedWkt";
    private const string UnsupportedTopLevelReference = WktReader + ".Parse, " + WktReader + ".TryParseNativeWkt2, " + WktReader + ".ParseNormalizedWkt";
    private const string NormalizationReference = WktReader + ".NormalizeWkt, " + WktReader + ".ParseNormalizedWkt";

    /// <summary>
    /// Gets the current WKT2 coverage rows for all tracked milestone-40 keywords.
    /// </summary>
    internal static IReadOnlyList<Wkt2KeywordCoverageRow> Rows { get; } = CreateRows();

    private static Wkt2KeywordCoverageRow[] CreateRows() =>
        new Wkt2KeywordCoverageRow[]
    {
        Native("ABRIDGEDTRANSFORMATION", BoundReference, "Parsed by the retained BoundCRS transformation reader."),
        Ignored("ANCHOR", MetadataSkipReference, "Accepted as non-operational metadata and skipped."),
        Ignored("ANCHOREPOCH", MetadataSkipReference, "Accepted as non-operational metadata and skipped."),
        Native("ANGLEUNIT", AngularUnitReference, "Parsed directly at CRS, axis, and parameter sites."),
        Ignored("AREA", MetadataSkipReference, "Accepted as non-operational metadata and skipped."),
        Native("AXIS", AxisReference, "Parsed directly by the native axis reader."),
        Unsupported("AXISMAXVALUE", AxisReference, "No native axis-range branch exists."),
        Unsupported("AXISMINVALUE", AxisReference, "No native axis-range branch exists."),
        Unsupported("BASEENGCRS", UnsupportedTopLevelReference, "No engineering CRS reader path exists yet."),
        Native("BASEGEODCRS", $"{ProjectedReference}, {BaseGeographicReference}", "Handled as a native projected-base geodetic CRS block."),
        Native("BASEGEOGCRS", $"{ProjectedReference}, {BaseGeographicReference}", "Handled as a native projected-base geographic CRS block."),
        Unsupported("BASEPARAMCRS", UnsupportedTopLevelReference, "No parametric CRS reader path exists yet."),
        Unsupported("BASEPROJCRS", UnsupportedTopLevelReference, "No derived projected CRS reader path exists yet."),
        Unsupported("BASETIMECRS", UnsupportedTopLevelReference, "No temporal CRS reader path exists yet."),
        Unsupported("BASEVERTCRS", UnsupportedTopLevelReference, "No derived vertical CRS reader path exists yet."),
        Ignored("BBOX", MetadataSkipReference, "Accepted as non-operational metadata and skipped."),
        Unsupported("BEARING", PrimeMeridianReference, "The prime-meridian reader has no BEARING branch."),
        Native("BOUNDCRS", $"{RootDispatchReference}, {BoundReference}", "Handled natively for the currently supported BoundCRS subset."),
        Unsupported("CALENDAR", UnsupportedTopLevelReference, "Temporal datum and temporal CRS parsing are not implemented."),
        Unsupported("CITATION", DefaultUnsupportedReference, "The native reader does not classify CITATION as skippable metadata."),
        Native("COMPOUNDCRS", $"{RootDispatchReference}, {CompoundReference}", "Handled natively by the compound CRS reader."),
        Unsupported("CONCATENATEDOPERATION", UnsupportedTopLevelReference, "No standalone coordinate-operation reader path exists yet."),
        Native("CONVERSION", ConversionReference, "Handled natively inside projected CRS definitions."),
        Unsupported("COORDEPOCH", UnsupportedTopLevelReference, "Coordinate metadata parsing is not implemented."),
        Unsupported("COORDINATEMETADATA", UnsupportedTopLevelReference, "No coordinate metadata root reader path exists yet."),
        Unsupported("COORDINATEOPERATION", UnsupportedTopLevelReference, "No standalone coordinate-operation reader path exists yet."),
        Native("CS", CoordinateSystemReference, "Parsed directly by the native coordinate-system definition reader."),
        Native("DATUM", HorizontalDatumReference, "Parsed directly by the native horizontal-datum reader."),
        Ignored("DEFININGTRANSFORMATION", MetadataSkipReference, "Accepted as non-operational metadata and skipped."),
        Unsupported("DERIVEDPROJCRS", UnsupportedTopLevelReference, "No derived projected CRS reader path exists yet."),
        Unsupported("DERIVINGCONVERSION", UnsupportedTopLevelReference, "No derived CRS reader path consumes deriving conversions yet."),
        Ignored("DYNAMIC", MetadataSkipReference, "Accepted as non-operational metadata and skipped."),
        Unsupported("EDATUM", UnsupportedTopLevelReference, "No engineering datum reader path exists yet."),
        Native("ELLIPSOID", EllipsoidReference, "Parsed directly by the native ellipsoid reader."),
        Unsupported("ENGCRS", UnsupportedTopLevelReference, "No engineering CRS root reader path exists yet."),
        Unsupported("ENGINEERINGCRS", UnsupportedTopLevelReference, "No engineering CRS root reader path exists yet."),
        Unsupported("ENGINEERINGDATUM", UnsupportedTopLevelReference, "No engineering datum reader path exists yet."),
        Unsupported("ENSEMBLE", GeodeticReference, "Encountering ENSEMBLE triggers an explicit NotSupportedException."),
        Unsupported("ENSEMBLEACCURACY", GeodeticReference, "Blocked behind the unsupported ENSEMBLE path."),
        Unsupported("EPOCH", UnsupportedTopLevelReference, "Coordinate metadata parsing is not implemented."),
        Ignored("FRAMEEPOCH", MetadataSkipReference, "Tolerated transitively inside skipped DYNAMIC metadata."),
        Native("GEODCRS", $"{RootDispatchReference}, {GeodeticReference}", "Handled natively through the geodetic CRS reader."),
        Native("GEODETICCRS", $"{RootDispatchReference}, {GeodeticReference}", "Handled natively through the geodetic CRS reader."),
        Unsupported("GEODETICDATUM", DefaultUnsupportedReference, "The native reader expects DATUM rather than GEODETICDATUM."),
        Native("GEOGCRS", $"{RootDispatchReference}, {GeodeticReference}", "Handled natively through the geodetic CRS reader."),
        Unsupported("GEOGRAPHICCRS", UnsupportedTopLevelReference, "No direct GEOGRAPHICCRS branch or normalization map exists."),
        Ignored("GEOIDMODEL", MetadataSkipReference, "Accepted as non-operational metadata and skipped."),
        Native("ID", IdentifierReference, "Parsed directly at CRS, unit, datum, vertical datum, and BoundCRS operation sites."),
        Unsupported("INTERPOLATIONCRS", DefaultUnsupportedReference, "No coordinate-operation reader path consumes interpolation CRS blocks yet."),
        Native("LENGTHUNIT", LinearUnitReference, "Parsed directly at CRS, axis, parameter, and vertical sites."),
        Unsupported("MEMBER", GeodeticReference, "Blocked behind the unsupported ENSEMBLE path."),
        Ignored("MERIDIAN", MetadataSkipReference, "Currently tolerated as skippable metadata instead of being retained."),
        Native("METHOD", ConversionReference, "Parsed directly in conversion and abridged-transformation blocks."),
        Ignored("MODEL", MetadataSkipReference, "Tolerated transitively inside skipped DYNAMIC metadata."),
        Unsupported("OPERATIONACCURACY", BoundReference, "The abridged transformation reader does not consume operation-accuracy nodes."),
        Ignored("ORDER", MetadataSkipReference, "Accepted as non-operational metadata and skipped."),
        Native("PARAMETER", ConversionReference, "Parsed directly in projection and abridged-transformation blocks."),
        Native("PARAMETERFILE", BoundReference, "Parsed directly for supported vertical BoundCRS transformations."),
        Unsupported("PARAMETRICCRS", UnsupportedTopLevelReference, "No parametric CRS root reader path exists yet."),
        Unsupported("PARAMETRICDATUM", UnsupportedTopLevelReference, "No parametric datum reader path exists yet."),
        Unsupported("PARAMETRICUNIT", UnsupportedTopLevelReference, "No parametric unit reader path exists yet."),
        Unsupported("PDATUM", UnsupportedTopLevelReference, "No parametric datum reader path exists yet."),
        Unsupported("POINTMOTIONOPERATION", UnsupportedTopLevelReference, "No point-motion operation reader path exists yet."),
        Native("PRIMEM", PrimeMeridianReference, "Parsed directly by the native prime-meridian reader."),
        Unsupported("PRIMEMERIDIAN", DefaultUnsupportedReference, "The native reader expects PRIMEM rather than PRIMEMERIDIAN."),
        Native("PROJCRS", $"{RootDispatchReference}, {ProjectedReference}", "Handled natively through the projected CRS reader."),
        LegacyNormalized("PROJECTEDCRS", NormalizationReference, "Only handled through NormalizeWkt -> PROJCS fallback."),
        Unsupported("RANGEMEANING", AxisReference, "The native axis reader has no range-meaning branch."),
        Ignored("REMARK", MetadataSkipReference, "Accepted as non-operational metadata and skipped."),
        Native("SCALEUNIT", ConversionReference, "Parsed directly for WKT2 projection and BoundCRS parameter units."),
        Ignored("SCOPE", MetadataSkipReference, "Accepted as non-operational metadata and skipped."),
        Native("SOURCECRS", BoundReference, "Parsed directly inside supported BoundCRS definitions."),
        Unsupported("STEP", UnsupportedTopLevelReference, "No standalone concatenated-operation reader path exists yet."),
        Native("TARGETCRS", BoundReference, "Parsed directly inside supported BoundCRS definitions."),
        Unsupported("TDATUM", UnsupportedTopLevelReference, "No temporal datum reader path exists yet."),
        Unsupported("TEMPORALQUANTITY", UnsupportedTopLevelReference, "No temporal CRS reader path exists yet."),
        Unsupported("TIMECRS", UnsupportedTopLevelReference, "No temporal CRS root reader path exists yet."),
        Unsupported("TIMEDATUM", UnsupportedTopLevelReference, "No temporal datum reader path exists yet."),
        Ignored("TIMEEXTENT", MetadataSkipReference, "Tolerated transitively inside skipped USAGE metadata."),
        Unsupported("TIMEORIGIN", UnsupportedTopLevelReference, "No temporal datum reader path exists yet."),
        Unsupported("TIMEUNIT", UnsupportedTopLevelReference, "No temporal unit reader path exists yet."),
        Unsupported("TRF", UnsupportedTopLevelReference, "No dedicated terrestrial reference-frame reader path exists yet."),
        Unsupported("URI", DefaultUnsupportedReference, "The native reader does not classify URI as skippable metadata."),
        Ignored("USAGE", MetadataSkipReference, "Accepted as non-operational metadata and skipped."),
        Native("VDATUM", VerticalReference, "Parsed directly by the native vertical-datum reader."),
        Ignored("VELOCITYGRID", MetadataSkipReference, "Tolerated transitively inside skipped DYNAMIC metadata."),
        Ignored("VERSION", MetadataSkipReference, "Accepted as non-operational metadata and skipped."),
        Native("VERTCRS", $"{RootDispatchReference}, {VerticalReference}", "Handled natively through the vertical CRS reader."),
        Ignored("VERTICALEXTENT", MetadataSkipReference, "Tolerated transitively inside skipped USAGE metadata."),
        Unsupported("VERTICALCRS", UnsupportedTopLevelReference, "No direct VERTICALCRS branch or normalization map exists."),
        Unsupported("VERTICALDATUM", UnsupportedTopLevelReference, "The native reader expects VDATUM rather than VERTICALDATUM."),
        Unsupported("VRF", UnsupportedTopLevelReference, "No dedicated vertical reference-frame reader path exists yet."),
    }
        .OrderBy(row => row.Keyword, StringComparer.Ordinal)
        .ToArray();

    private static Wkt2KeywordCoverageRow Native(string keyword, string reference, string notes) =>
        new(keyword, Wkt2KeywordSupportStatus.Native, reference, notes);

    private static Wkt2KeywordCoverageRow LegacyNormalized(string keyword, string reference, string notes) =>
        new(keyword, Wkt2KeywordSupportStatus.LegacyNormalized, reference, notes);

    private static Wkt2KeywordCoverageRow Ignored(string keyword, string reference, string notes) =>
        new(keyword, Wkt2KeywordSupportStatus.Ignored, reference, notes);

    private static Wkt2KeywordCoverageRow Unsupported(string keyword, string reference, string notes) =>
        new(keyword, Wkt2KeywordSupportStatus.Unsupported, reference, notes);
}
