// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

using System.Collections.Generic;

/// <summary>
/// Provides the milestone-40 reference keyword set for the WKT2 inventory work.
/// </summary>
/// <remarks>
/// The primary inventory rows are derived from the ISO 19162:2019 CRS/operation surface and
/// cross-checked against the vendored PROJ WKT2 grammar in
/// <c>spec\PROJ\src\wkt2_generated_parser.h</c> plus the related PROJ WKT2 unit fixtures.
/// Additional keywords capture alternate long-form spellings and reference-frame forms that
/// appear in the local PROJ reference material and still need explicit coverage classification
/// in the next inventory steps.
/// </remarks>
internal static class Wkt2KeywordInventoryReference
{
    /// <summary>
    /// Gets the primary milestone-40 inventory rows.
    /// </summary>
    internal static IReadOnlyList<string> InventoryKeywords { get; } =
    [
        "ABRIDGEDTRANSFORMATION",
        "ANCHOR",
        "ANCHOREPOCH",
        "ANGLEUNIT",
        "AREA",
        "AXIS",
        "AXISMAXVALUE",
        "AXISMINVALUE",
        "BASEENGCRS",
        "BASEGEODCRS",
        "BASEGEOGCRS",
        "BASEPARAMCRS",
        "BASEPROJCRS",
        "BASETIMECRS",
        "BASEVERTCRS",
        "BBOX",
        "BEARING",
        "BOUNDCRS",
        "CALENDAR",
        "CITATION",
        "COMPOUNDCRS",
        "CONCATENATEDOPERATION",
        "CONVERSION",
        "COORDEPOCH",
        "COORDINATEMETADATA",
        "COORDINATEOPERATION",
        "CS",
        "DATUM",
        "DEFININGTRANSFORMATION",
        "DERIVEDPROJCRS",
        "DERIVINGCONVERSION",
        "DYNAMIC",
        "EDATUM",
        "ELLIPSOID",
        "ENGCRS",
        "ENSEMBLE",
        "ENSEMBLEACCURACY",
        "EPOCH",
        "FRAMEEPOCH",
        "GEODCRS",
        "GEOGCRS",
        "GEOIDMODEL",
        "ID",
        "INTERPOLATIONCRS",
        "LENGTHUNIT",
        "MEMBER",
        "MERIDIAN",
        "METHOD",
        "MODEL",
        "OPERATIONACCURACY",
        "ORDER",
        "PARAMETER",
        "PARAMETERFILE",
        "PARAMETRICCRS",
        "PARAMETRICUNIT",
        "PDATUM",
        "POINTMOTIONOPERATION",
        "PRIMEM",
        "PROJCRS",
        "RANGEMEANING",
        "REMARK",
        "SCALEUNIT",
        "SCOPE",
        "SOURCECRS",
        "STEP",
        "TARGETCRS",
        "TDATUM",
        "TEMPORALQUANTITY",
        "TIMECRS",
        "TIMEEXTENT",
        "TIMEORIGIN",
        "TIMEUNIT",
        "URI",
        "USAGE",
        "VDATUM",
        "VELOCITYGRID",
        "VERSION",
        "VERTCRS",
        "VERTICALEXTENT",
    ];

    /// <summary>
    /// Gets additional spellings that occur in the local PROJ WKT2 references.
    /// </summary>
    internal static IReadOnlyList<string> AdditionalKeywords { get; } =
    [
        "ENGINEERINGCRS",
        "ENGINEERINGDATUM",
        "GEODETICCRS",
        "GEODETICDATUM",
        "GEOGRAPHICCRS",
        "PARAMETRICDATUM",
        "PRIMEMERIDIAN",
        "PROJECTEDCRS",
        "TIMEDATUM",
        "TRF",
        "VERTICALCRS",
        "VERTICALDATUM",
        "VRF",
    ];

    /// <summary>
    /// Gets the full milestone-40 WKT2 keyword universe tracked so far.
    /// </summary>
    internal static IReadOnlyList<string> AllKeywords { get; } =
    [
        .. InventoryKeywords,
        .. AdditionalKeywords,
    ];
}
