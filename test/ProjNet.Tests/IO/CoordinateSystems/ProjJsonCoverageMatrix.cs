// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Provides the milestone-40 PROJJSON coverage matrix across reader and writer surfaces.
/// </summary>
internal static class ProjJsonCoverageMatrix
{
    private const string ReaderRootReference = "ProjJsonReader.cs:59-87";
    private const string ReaderGeographicReference = "ProjJsonReader.cs:90-135";
    private const string ReaderGeodeticReference = "ProjJsonReader.cs:137-199";
    private const string ReaderProjectedReference = "ProjJsonReader.cs:201-243";
    private const string ReaderVerticalReference = "ProjJsonReader.cs:245-284";
    private const string ReaderCompoundReference = "ProjJsonReader.cs:286-321";
    private const string ReaderConversionReference = "ProjJsonReader.cs:323-346";
    private const string ReaderCoordinateSystemReference = "ProjJsonReader.cs:348-397";
    private const string ReaderDatumReference = "ProjJsonReader.cs:399-425";
    private const string ReaderUnitReference = "ProjJsonReader.cs:473-521";
    private const string ReaderIdentifierReference = "ProjJsonReader.cs:523-555";
    private const string ReaderEnsembleReference = "ProjJsonReader.cs:630-635";
    private const string WriterRootReference = "ProjJsonWriter.cs:50-70";
    private const string WriterGeographicReference = "ProjJsonWriter.cs:74-93";
    private const string WriterGeocentricReference = "ProjJsonWriter.cs:95-114";
    private const string WriterProjectedReference = "ProjJsonWriter.cs:116-139";
    private const string WriterVerticalReference = "ProjJsonWriter.cs:141-157";
    private const string WriterCompoundReference = "ProjJsonWriter.cs:159-172";
    private const string WriterAxisReference = "ProjJsonWriter.cs:174-197, 406-428";
    private const string WriterDatumReference = "ProjJsonWriter.cs:199-249";
    private const string WriterConversionReference = "ProjJsonWriter.cs:251-357";
    private const string WriterBoundReference = "ProjJsonWriter.cs:376-389";
    private const string WriterIdentifierReference = "ProjJsonWriter.cs:392-404";

    /// <summary>
    /// Gets the current milestone-40 PROJJSON coverage rows.
    /// </summary>
    internal static IReadOnlyList<ProjJsonCoverageRow> Rows { get; } = CreateRows();

    private static ProjJsonCoverageRow[] CreateRows() =>
        new ProjJsonCoverageRow[]
        {
            Row(
                "BoundCRS",
                ProjJsonCoverageStatus.Unsupported,
                ReaderRootReference,
                ProjJsonCoverageStatus.Unsupported,
                WriterBoundReference,
                "There is no BoundCRS type dispatch yet and the writer still throws when bound metadata would need preservation."),
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
                ReaderConversionReference,
                ProjJsonCoverageStatus.Supported,
                WriterConversionReference,
                "Nested conversion objects are parsed and emitted natively for projected CRS."),
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
                ProjJsonCoverageStatus.Unsupported,
                ReaderEnsembleReference,
                ProjJsonCoverageStatus.Unsupported,
                WriterRootReference,
                "Reader rejects datum_ensemble and the writer has no ensemble-capable model to emit."),
            Row(
                "DerivedCRS/FittedCoordinateSystem",
                ProjJsonCoverageStatus.Unsupported,
                ReaderRootReference,
                ProjJsonCoverageStatus.Unsupported,
                WriterRootReference,
                "No derived CRS PROJJSON path exists and FittedCoordinateSystem still falls into the writer default throw."),
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
                "ProjJsonReader.cs:96-98, 160-162, 461-470",
                ProjJsonCoverageStatus.Supported,
                "ProjJsonWriter.cs:85-86, 106-107, 231-239",
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
