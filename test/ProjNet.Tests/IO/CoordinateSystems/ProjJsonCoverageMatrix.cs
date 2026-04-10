// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Linq;
using ProjNet.IO.CoordinateSystems;

/// <summary>
/// Provides the milestone-40 PROJJSON coverage matrix across reader and writer surfaces.
/// </summary>
internal static class ProjJsonCoverageMatrix
{
    private const string Reader = nameof(ProjJsonReader);
    private const string Writer = nameof(ProjJsonWriter);
    private const string ReaderRootReference = Reader + ".Parse, " + Reader + ".ReadInfo";
    private const string ReaderGeographicReference = Reader + ".ReadGeographicCoordinateSystem";
    private const string ReaderGeodeticReference = Reader + ".ReadGeodeticCoordinateSystem, " + Reader + ".ReadGeocentricCoordinateSystem";
    private const string ReaderProjectedReference = Reader + ".ReadProjectedCoordinateSystem";
    private const string ReaderDerivedReference = Reader + ".ReadDerivedGeodeticCoordinateSystem, " + Reader + ".ReadDerivedProjectedCoordinateSystem, " + Reader + ".CreateDerivedCoordinateSystem";
    private const string ReaderBoundReference = Reader + ".ReadBoundCoordinateSystem, " + Reader + ".ReadBoundTransformation, " + Reader + ".ReadCoordinateSystemElement";
    private const string ReaderVerticalReference = Reader + ".ReadVerticalCoordinateSystem";
    private const string ReaderCompoundReference = Reader + ".ReadCompoundCoordinateSystem";
    private const string ReaderConversionReference = Reader + ".ReadConversion, " + Reader + ".NormalizeProjectionParameterName";
    private const string ReaderCoordinateSystemReference = Reader + ".ReadCoordinateSystemDefinition, " + Reader + ".ReadAxis, " + Reader + ".ParseAxisOrientation";
    private const string ReaderDatumReference = Reader + ".ReadHorizontalDatumOrEnsemble, " + Reader + ".ReadVerticalDatumOrEnsemble, " + Reader + ".ReadHorizontalDatum, " + Reader + ".ReadVerticalDatum, " + Reader + ".ReadHorizontalDatumEnsemble, " + Reader + ".ReadVerticalDatumEnsemble, " + Reader + ".ReadDatumEnsemble, " + Reader + ".ReadDatumEnsembleMember, " + Reader + ".ReadEllipsoid";
    private const string ReaderUnitReference = Reader + ".ReadAngularUnit, " + Reader + ".ReadLinearUnit, " + Reader + ".IsAngularUnit, " + Reader + ".IsLinearUnit";
    private const string ReaderIdentifierReference = Reader + ".ReadIdentifier, " + Reader + ".ReadSingleIdentifier";
    private const string ReaderEnsembleReference = Reader + ".ReadHorizontalDatumOrEnsemble, " + Reader + ".ReadVerticalDatumOrEnsemble, " + Reader + ".ReadHorizontalDatumEnsemble, " + Reader + ".ReadVerticalDatumEnsemble, " + Reader + ".ReadDatumEnsemble, " + Reader + ".ReadDatumEnsembleMember";
    private const string WriterRootReference = Writer + ".ToJson, " + Writer + ".WriteTo, " + Writer + ".WriteCoordinateSystem";
    private const string WriterGeographicReference = Writer + ".WriteGeographicCoordinateSystem";
    private const string WriterGeocentricReference = Writer + ".WriteGeocentricCoordinateSystem";
    private const string WriterProjectedReference = Writer + ".WriteProjectedCoordinateSystem";
    private const string WriterDerivedReference = Writer + ".WriteDerivedCoordinateSystem, " + Writer + ".WriteDerivedAffineConversion, " + Writer + ".WriteDerivedAffineParameter";
    private const string WriterVerticalReference = Writer + ".WriteVerticalCoordinateSystem";
    private const string WriterCompoundReference = Writer + ".WriteCompoundCoordinateSystem, " + Writer + ".WriteCompoundComponents, " + Writer + ".WriteCompoundComponent";
    private const string WriterAxisReference = Writer + ".WriteCoordinateSystemDefinition, " + Writer + ".WriteAxis, " + Writer + ".GetAxisDirection";
    private const string WriterDatumReference = Writer + ".WriteHorizontalDatumProperty, " + Writer + ".WriteVerticalDatumProperty, " + Writer + ".WriteHorizontalDatum, " + Writer + ".WriteDatumEnsemble, " + Writer + ".WriteDatumEnsembleMember, " + Writer + ".WriteEllipsoid, " + Writer + ".WritePrimeMeridian, " + Writer + ".WriteVerticalDatum";
    private const string WriterConversionReference = Writer + ".WriteConversion, " + Writer + ".WriteMethod, " + Writer + ".WriteProjectionParameter, " + Writer + ".WriteUnit, " + Writer + ".WriteAngularUnit, " + Writer + ".WriteLinearUnit, " + Writer + ".WriteScaleUnit";
    private const string WriterBoundReference = Writer + ".WriteBoundCoordinateSystem, " + Writer + ".WriteBoundCoordinateSystemComponent, " + Writer + ".WriteBoundTransformation, " + Writer + ".WriteBoundTransformationParameters, " + Writer + ".TryWriteLegacyBoundCoordinateSystem";
    private const string WriterIdentifierReference = Writer + ".WriteIdentifier";

    /// <summary>
    /// Gets the current milestone-40 PROJJSON coverage rows.
    /// </summary>
    internal static IReadOnlyList<ProjJsonCoverageRow> Rows { get; } = CreateRows();

    private static ProjJsonCoverageRow[] CreateRows() =>
        new ProjJsonCoverageRow[]
        {
            Row(
                "BoundCRS",
                ProjJsonCoverageStatus.Supported,
                $"{ReaderRootReference}, {ReaderBoundReference}",
                ProjJsonCoverageStatus.Supported,
                WriterBoundReference,
                "Reader and writer both dispatch BoundCRS objects into the first-class bound model, and the writer also bridges retained legacy bound metadata through the same BoundCRS path."),
            Row(
                "CompoundCRS",
                ProjJsonCoverageStatus.Supported,
                ReaderCompoundReference,
                ProjJsonCoverageStatus.Supported,
                WriterCompoundReference,
                "Compound CRS objects are parsed and emitted natively."),
            Row(
                "conversion",
                ProjJsonCoverageStatus.Supported,
                $"{ReaderConversionReference}, {ReaderDerivedReference}",
                ProjJsonCoverageStatus.Supported,
                $"{WriterConversionReference}, {WriterDerivedReference}",
                "Nested conversion objects are parsed and emitted natively for projected CRS and the supported affine derived CRS slice."),
            Row(
                "conversion.parameters[].unit",
                ProjJsonCoverageStatus.Ignored,
                ReaderConversionReference,
                ProjJsonCoverageStatus.Supported,
                WriterConversionReference,
                "Reader keeps parameter names and values but does not consume parameter unit objects, while the writer emits angular, linear, and scale units."),
            Row(
                "coordinate_system.axis",
                ProjJsonCoverageStatus.Supported,
                ReaderCoordinateSystemReference,
                ProjJsonCoverageStatus.Supported,
                WriterAxisReference,
                "Axis definitions are parsed and emitted natively."),
            Row(
                "coordinate_system.axis.unit object",
                ProjJsonCoverageStatus.Supported,
                $"{ReaderCoordinateSystemReference}, {ReaderUnitReference}",
                ProjJsonCoverageStatus.Supported,
                $"{WriterAxisReference}, {WriterConversionReference}",
                "Object-form angular and linear units are supported on both sides."),
            Row(
                "coordinate_system.axis.unit string shorthand",
                ProjJsonCoverageStatus.Supported,
                ReaderUnitReference,
                ProjJsonCoverageStatus.Unsupported,
                WriterAxisReference,
                "Reader accepts degree/metre shorthand strings, while the writer always emits structured unit objects."),
            Row(
                "CoordinateMetadata",
                ProjJsonCoverageStatus.Unsupported,
                ReaderRootReference,
                ProjJsonCoverageStatus.Unsupported,
                WriterRootReference,
                "No coordinate metadata type path exists yet."),
            Row(
                "datum.type = DynamicGeodeticReferenceFrame",
                ProjJsonCoverageStatus.Partial,
                ReaderDatumReference,
                ProjJsonCoverageStatus.Unsupported,
                WriterDatumReference,
                "Reader accepts the type token but does not retain dynamic metadata, and the writer only emits GeodeticReferenceFrame."),
            Row(
                "datum.type = DynamicVerticalReferenceFrame",
                ProjJsonCoverageStatus.Partial,
                ReaderDatumReference,
                ProjJsonCoverageStatus.Unsupported,
                WriterDatumReference,
                "Reader accepts the type token but does not retain dynamic metadata, and the writer only emits VerticalReferenceFrame."),
            Row(
                "datum.type = GeodeticReferenceFrame",
                ProjJsonCoverageStatus.Supported,
                ReaderDatumReference,
                ProjJsonCoverageStatus.Supported,
                WriterDatumReference,
                "Static geodetic reference frames are fully supported."),
            Row(
                "datum.type = VerticalReferenceFrame",
                ProjJsonCoverageStatus.Supported,
                ReaderDatumReference,
                ProjJsonCoverageStatus.Supported,
                WriterDatumReference,
                "Static vertical reference frames are fully supported."),
            Row(
                "datum_ensemble",
                ProjJsonCoverageStatus.Supported,
                ReaderEnsembleReference,
                ProjJsonCoverageStatus.Supported,
                WriterDatumReference,
                "Reader and writer retain datum_ensemble metadata for supported geodetic and vertical CRS definitions."),
            Row(
                "DerivedCRS/FittedCoordinateSystem",
                ProjJsonCoverageStatus.Supported,
                $"{ReaderRootReference}, {ReaderDerivedReference}",
                ProjJsonCoverageStatus.Supported,
                $"{WriterRootReference}, {WriterDerivedReference}",
                "Derived geographic and projected CRS now roundtrip onto FittedCoordinateSystem for the supported affine 2D slice."),
            Row(
                "EngineeringCRS",
                ProjJsonCoverageStatus.Unsupported,
                ReaderRootReference,
                ProjJsonCoverageStatus.Unsupported,
                WriterRootReference,
                "Engineering CRS objects are outside the current PROJJSON surface."),
            Row(
                "GeodeticCRS.cartesian",
                ProjJsonCoverageStatus.Supported,
                ReaderGeodeticReference,
                ProjJsonCoverageStatus.Supported,
                WriterGeocentricReference,
                "Cartesian geodetic CRS are handled as geocentric coordinate systems on both sides."),
            Row(
                "GeodeticCRS.ellipsoidal",
                ProjJsonCoverageStatus.Supported,
                ReaderGeodeticReference,
                ProjJsonCoverageStatus.Unsupported,
                WriterGeographicReference,
                "Reader accepts ellipsoidal GeodeticCRS, but the writer emits GeographicCRS instead of this type value."),
            Row(
                "GeographicCRS",
                ProjJsonCoverageStatus.Supported,
                ReaderGeographicReference,
                ProjJsonCoverageStatus.Supported,
                WriterGeographicReference,
                "Geographic CRS are fully supported on read and write."),
            Row(
                "id",
                ProjJsonCoverageStatus.Supported,
                ReaderIdentifierReference,
                ProjJsonCoverageStatus.Supported,
                WriterIdentifierReference,
                "Single identifier objects are parsed and emitted natively."),
            Row(
                "ids[]",
                ProjJsonCoverageStatus.Supported,
                ReaderIdentifierReference,
                ProjJsonCoverageStatus.Unsupported,
                WriterIdentifierReference,
                "Reader supports ids[] with EPSG preference, while the writer only emits a singular id."),
            Row(
                "ParametricCRS",
                ProjJsonCoverageStatus.Unsupported,
                ReaderRootReference,
                ProjJsonCoverageStatus.Unsupported,
                WriterRootReference,
                "Parametric CRS objects are outside the current PROJJSON surface."),
            Row(
                "prime_meridian",
                ProjJsonCoverageStatus.Supported,
                Reader + ".ReadGeographicCoordinateSystem, " + Reader + ".ReadGeodeticCoordinateSystem, " + Reader + ".ReadPrimeMeridian",
                ProjJsonCoverageStatus.Supported,
                Writer + ".WriteGeographicCoordinateSystem, " + Writer + ".WriteGeocentricCoordinateSystem, " + Writer + ".WritePrimeMeridian",
                "Prime meridians are parsed and emitted natively."),
            Row(
                "ProjectedCRS",
                ProjJsonCoverageStatus.Supported,
                ReaderProjectedReference,
                ProjJsonCoverageStatus.Supported,
                WriterProjectedReference,
                "Projected CRS are fully supported on read and write."),
            Row(
                "retained bound metadata on existing CRS",
                ProjJsonCoverageStatus.Unsupported,
                ReaderRootReference,
                ProjJsonCoverageStatus.Unsupported,
                WriterBoundReference,
                "The current PROJJSON surface cannot preserve retained WGS84 or bound-grid metadata on existing CRS objects."),
            Row(
                "Standalone operation objects",
                ProjJsonCoverageStatus.Unsupported,
                ReaderRootReference,
                ProjJsonCoverageStatus.Unsupported,
                WriterRootReference,
                "Standalone coordinate-operation or concatenated-operation objects are not exposed as top-level PROJJSON parse/write targets."),
            Row(
                "TimeCRS",
                ProjJsonCoverageStatus.Unsupported,
                ReaderRootReference,
                ProjJsonCoverageStatus.Unsupported,
                WriterRootReference,
                "Temporal CRS objects are outside the current PROJJSON surface."),
            Row(
                "unit.type = Unit",
                ProjJsonCoverageStatus.Supported,
                ReaderUnitReference,
                ProjJsonCoverageStatus.Unsupported,
                WriterConversionReference,
                "Reader accepts generic Unit objects for angular and linear units, while the writer emits concrete unit types."),
            Row(
                "usage metadata",
                ProjJsonCoverageStatus.Ignored,
                ReaderRootReference,
                ProjJsonCoverageStatus.Unsupported,
                WriterRootReference,
                "Reader ignores extra metadata properties such as scope, area, bbox, remarks, and usages; writer does not emit them."),
            Row(
                "VerticalCRS",
                ProjJsonCoverageStatus.Supported,
                ReaderVerticalReference,
                ProjJsonCoverageStatus.Supported,
                WriterVerticalReference,
                "Vertical CRS are fully supported on read and write."),
        }
        .OrderBy(row => row.Feature, StringComparer.Ordinal)
        .ToArray();

    private static ProjJsonCoverageRow Row(
        string feature,
        ProjJsonCoverageStatus readerStatus,
        string readerReference,
        ProjJsonCoverageStatus writerStatus,
        string writerReference,
        string notes) =>
        new(feature, readerStatus, readerReference, writerStatus, writerReference, notes);
}
