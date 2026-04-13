// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Rebuilds immutable info-backed coordinate system objects while replacing top-level metadata.
/// </summary>
internal static partial class InfoAuthorityCloneHelper
{
    /// <summary>
    /// Creates a deep clone of the supplied geographic coordinate system with replacement authority metadata.
    /// </summary>
    /// <param name="geographicCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned geographic coordinate system with the requested authority metadata.</returns>
    internal static GeographicCoordinateSystem CloneWithAuthority(GeographicCoordinateSystem geographicCoordinateSystem, string authority, long authorityCode)
    {
        geographicCoordinateSystem = ArgumentGuard.ThrowIfNull(geographicCoordinateSystem, nameof(geographicCoordinateSystem));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneGeographicCoordinateSystem(geographicCoordinateSystem, authority: authority, authorityCode: authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied projected coordinate system with replacement authority metadata.
    /// </summary>
    /// <param name="projectedCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned projected coordinate system with the requested authority metadata.</returns>
    internal static ProjectedCoordinateSystem CloneWithAuthority(ProjectedCoordinateSystem projectedCoordinateSystem, string authority, long authorityCode)
    {
        projectedCoordinateSystem = ArgumentGuard.ThrowIfNull(projectedCoordinateSystem, nameof(projectedCoordinateSystem));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneProjectedCoordinateSystem(projectedCoordinateSystem, authority: authority, authorityCode: authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied geocentric coordinate system with replacement authority metadata.
    /// </summary>
    /// <param name="geocentricCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned geocentric coordinate system with the requested authority metadata.</returns>
    internal static GeocentricCoordinateSystem CloneWithAuthority(GeocentricCoordinateSystem geocentricCoordinateSystem, string authority, long authorityCode)
    {
        geocentricCoordinateSystem = ArgumentGuard.ThrowIfNull(geocentricCoordinateSystem, nameof(geocentricCoordinateSystem));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneGeocentricCoordinateSystem(geocentricCoordinateSystem, authority: authority, authorityCode: authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied vertical coordinate system with replacement authority metadata.
    /// </summary>
    /// <param name="verticalCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned vertical coordinate system with the requested authority metadata.</returns>
    internal static VerticalCoordinateSystem CloneWithAuthority(VerticalCoordinateSystem verticalCoordinateSystem, string authority, long authorityCode)
    {
        verticalCoordinateSystem = ArgumentGuard.ThrowIfNull(verticalCoordinateSystem, nameof(verticalCoordinateSystem));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneVerticalCoordinateSystem(verticalCoordinateSystem, authority: authority, authorityCode: authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied compound coordinate system with replacement authority metadata.
    /// </summary>
    /// <param name="compoundCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned compound coordinate system with the requested authority metadata.</returns>
    internal static CompoundCoordinateSystem CloneWithAuthority(CompoundCoordinateSystem compoundCoordinateSystem, string authority, long authorityCode)
    {
        compoundCoordinateSystem = ArgumentGuard.ThrowIfNull(compoundCoordinateSystem, nameof(compoundCoordinateSystem));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneCompoundCoordinateSystem(compoundCoordinateSystem, authority: authority, authorityCode: authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied bound coordinate system with replacement authority metadata.
    /// </summary>
    /// <param name="boundCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned bound coordinate system with the requested authority metadata.</returns>
    internal static BoundCoordinateSystem CloneWithAuthority(BoundCoordinateSystem boundCoordinateSystem, string authority, long authorityCode)
    {
        boundCoordinateSystem = ArgumentGuard.ThrowIfNull(boundCoordinateSystem, nameof(boundCoordinateSystem));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneBoundCoordinateSystem(boundCoordinateSystem, authority: authority, authorityCode: authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied fitted coordinate system with replacement authority metadata.
    /// </summary>
    /// <param name="fittedCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned fitted coordinate system with the requested authority metadata.</returns>
    internal static FittedCoordinateSystem CloneWithAuthority(FittedCoordinateSystem fittedCoordinateSystem, string authority, long authorityCode)
    {
        fittedCoordinateSystem = ArgumentGuard.ThrowIfNull(fittedCoordinateSystem, nameof(fittedCoordinateSystem));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneFittedCoordinateSystem(fittedCoordinateSystem, authority: authority, authorityCode: authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied engineering coordinate system with replacement authority metadata.
    /// </summary>
    /// <param name="engineeringCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned engineering coordinate system with the requested authority metadata.</returns>
    internal static EngineeringCoordinateSystem CloneWithAuthority(EngineeringCoordinateSystem engineeringCoordinateSystem, string authority, long authorityCode)
    {
        engineeringCoordinateSystem = ArgumentGuard.ThrowIfNull(engineeringCoordinateSystem, nameof(engineeringCoordinateSystem));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneEngineeringCoordinateSystem(engineeringCoordinateSystem, authority: authority, authorityCode: authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied parametric coordinate system with replacement authority metadata.
    /// </summary>
    /// <param name="parametricCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned parametric coordinate system with the requested authority metadata.</returns>
    internal static ParametricCoordinateSystem CloneWithAuthority(ParametricCoordinateSystem parametricCoordinateSystem, string authority, long authorityCode)
    {
        parametricCoordinateSystem = ArgumentGuard.ThrowIfNull(parametricCoordinateSystem, nameof(parametricCoordinateSystem));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneParametricCoordinateSystem(parametricCoordinateSystem, authority: authority, authorityCode: authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied temporal coordinate system with replacement authority metadata.
    /// </summary>
    /// <param name="temporalCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned temporal coordinate system with the requested authority metadata.</returns>
    internal static TemporalCoordinateSystem CloneWithAuthority(TemporalCoordinateSystem temporalCoordinateSystem, string authority, long authorityCode)
    {
        temporalCoordinateSystem = ArgumentGuard.ThrowIfNull(temporalCoordinateSystem, nameof(temporalCoordinateSystem));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneTemporalCoordinateSystem(temporalCoordinateSystem, authority: authority, authorityCode: authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied geographic coordinate system with a replacement name.
    /// </summary>
    /// <param name="geographicCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned geographic coordinate system with the requested name.</returns>
    internal static GeographicCoordinateSystem CloneWithName(GeographicCoordinateSystem geographicCoordinateSystem, string name)
    {
        geographicCoordinateSystem = ArgumentGuard.ThrowIfNull(geographicCoordinateSystem, nameof(geographicCoordinateSystem));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneGeographicCoordinateSystem(geographicCoordinateSystem, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied projected coordinate system with a replacement name.
    /// </summary>
    /// <param name="projectedCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned projected coordinate system with the requested name.</returns>
    internal static ProjectedCoordinateSystem CloneWithName(ProjectedCoordinateSystem projectedCoordinateSystem, string name)
    {
        projectedCoordinateSystem = ArgumentGuard.ThrowIfNull(projectedCoordinateSystem, nameof(projectedCoordinateSystem));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneProjectedCoordinateSystem(projectedCoordinateSystem, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied geocentric coordinate system with a replacement name.
    /// </summary>
    /// <param name="geocentricCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned geocentric coordinate system with the requested name.</returns>
    internal static GeocentricCoordinateSystem CloneWithName(GeocentricCoordinateSystem geocentricCoordinateSystem, string name)
    {
        geocentricCoordinateSystem = ArgumentGuard.ThrowIfNull(geocentricCoordinateSystem, nameof(geocentricCoordinateSystem));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneGeocentricCoordinateSystem(geocentricCoordinateSystem, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied vertical coordinate system with a replacement name.
    /// </summary>
    /// <param name="verticalCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned vertical coordinate system with the requested name.</returns>
    internal static VerticalCoordinateSystem CloneWithName(VerticalCoordinateSystem verticalCoordinateSystem, string name)
    {
        verticalCoordinateSystem = ArgumentGuard.ThrowIfNull(verticalCoordinateSystem, nameof(verticalCoordinateSystem));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneVerticalCoordinateSystem(verticalCoordinateSystem, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied compound coordinate system with a replacement name.
    /// </summary>
    /// <param name="compoundCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned compound coordinate system with the requested name.</returns>
    internal static CompoundCoordinateSystem CloneWithName(CompoundCoordinateSystem compoundCoordinateSystem, string name)
    {
        compoundCoordinateSystem = ArgumentGuard.ThrowIfNull(compoundCoordinateSystem, nameof(compoundCoordinateSystem));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneCompoundCoordinateSystem(compoundCoordinateSystem, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied bound coordinate system with a replacement name.
    /// </summary>
    /// <param name="boundCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned bound coordinate system with the requested name.</returns>
    internal static BoundCoordinateSystem CloneWithName(BoundCoordinateSystem boundCoordinateSystem, string name)
    {
        boundCoordinateSystem = ArgumentGuard.ThrowIfNull(boundCoordinateSystem, nameof(boundCoordinateSystem));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneBoundCoordinateSystem(boundCoordinateSystem, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied fitted coordinate system with a replacement name.
    /// </summary>
    /// <param name="fittedCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned fitted coordinate system with the requested name.</returns>
    internal static FittedCoordinateSystem CloneWithName(FittedCoordinateSystem fittedCoordinateSystem, string name)
    {
        fittedCoordinateSystem = ArgumentGuard.ThrowIfNull(fittedCoordinateSystem, nameof(fittedCoordinateSystem));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneFittedCoordinateSystem(fittedCoordinateSystem, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied engineering coordinate system with a replacement name.
    /// </summary>
    /// <param name="engineeringCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned engineering coordinate system with the requested name.</returns>
    internal static EngineeringCoordinateSystem CloneWithName(EngineeringCoordinateSystem engineeringCoordinateSystem, string name)
    {
        engineeringCoordinateSystem = ArgumentGuard.ThrowIfNull(engineeringCoordinateSystem, nameof(engineeringCoordinateSystem));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneEngineeringCoordinateSystem(engineeringCoordinateSystem, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied parametric coordinate system with a replacement name.
    /// </summary>
    /// <param name="parametricCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned parametric coordinate system with the requested name.</returns>
    internal static ParametricCoordinateSystem CloneWithName(ParametricCoordinateSystem parametricCoordinateSystem, string name)
    {
        parametricCoordinateSystem = ArgumentGuard.ThrowIfNull(parametricCoordinateSystem, nameof(parametricCoordinateSystem));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneParametricCoordinateSystem(parametricCoordinateSystem, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied temporal coordinate system with a replacement name.
    /// </summary>
    /// <param name="temporalCoordinateSystem">Coordinate system to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned temporal coordinate system with the requested name.</returns>
    internal static TemporalCoordinateSystem CloneWithName(TemporalCoordinateSystem temporalCoordinateSystem, string name)
    {
        temporalCoordinateSystem = ArgumentGuard.ThrowIfNull(temporalCoordinateSystem, nameof(temporalCoordinateSystem));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneTemporalCoordinateSystem(temporalCoordinateSystem, name: name);
    }

    private static GeographicCoordinateSystem CloneGeographicCoordinateSystem(
        GeographicCoordinateSystem geographicCoordinateSystem,
        string? authority = null,
        long? authorityCode = null,
        string? name = null)
    {
        HorizontalDatum horizontalDatum = CloneHorizontalDatum(geographicCoordinateSystem.HorizontalDatum);
        return CloneGeographicCoordinateSystem(geographicCoordinateSystem, horizontalDatum, authority, authorityCode, name);
    }

    private static GeographicCoordinateSystem CloneGeographicCoordinateSystem(
        GeographicCoordinateSystem geographicCoordinateSystem,
        HorizontalDatum horizontalDatum,
        string? authority = null,
        long? authorityCode = null,
        string? name = null)
    {
        var clone = new GeographicCoordinateSystem(
            CloneAngularUnit(geographicCoordinateSystem.AngularUnit),
            horizontalDatum,
            ClonePrimeMeridian(geographicCoordinateSystem.PrimeMeridian),
            CloneAxisInfo(geographicCoordinateSystem),
            name ?? geographicCoordinateSystem.Name,
            authority ?? geographicCoordinateSystem.Authority,
            authorityCode ?? geographicCoordinateSystem.AuthorityCode,
            geographicCoordinateSystem.Alias,
            geographicCoordinateSystem.Abbreviation,
            geographicCoordinateSystem.Remarks,
            geographicCoordinateSystem.DefaultEnvelope,
            CloneWgs84ConversionInfoList(geographicCoordinateSystem.WGS84ConversionInfo));

        return clone;
    }

    private static ProjectedCoordinateSystem CloneProjectedCoordinateSystem(
        ProjectedCoordinateSystem projectedCoordinateSystem,
        string? authority = null,
        long? authorityCode = null,
        string? name = null)
    {
        HorizontalDatum horizontalDatum = CloneHorizontalDatum(projectedCoordinateSystem.HorizontalDatum);
        GeographicCoordinateSystem geographicCoordinateSystem = CloneGeographicCoordinateSystem(projectedCoordinateSystem.GeographicCoordinateSystem, horizontalDatum);

        return new ProjectedCoordinateSystem(
            horizontalDatum,
            geographicCoordinateSystem,
            CloneLinearUnit(projectedCoordinateSystem.LinearUnit),
            CloneProjection(projectedCoordinateSystem.Projection),
            CloneAxisInfo(projectedCoordinateSystem),
            name ?? projectedCoordinateSystem.Name,
            authority ?? projectedCoordinateSystem.Authority,
            authorityCode ?? projectedCoordinateSystem.AuthorityCode,
            projectedCoordinateSystem.Alias,
            projectedCoordinateSystem.Remarks,
            projectedCoordinateSystem.Abbreviation,
            projectedCoordinateSystem.DefaultEnvelope);
    }

    private static GeocentricCoordinateSystem CloneGeocentricCoordinateSystem(
        GeocentricCoordinateSystem geocentricCoordinateSystem,
        string? authority = null,
        long? authorityCode = null,
        string? name = null)
    {
        HorizontalDatum horizontalDatum = CloneHorizontalDatum(geocentricCoordinateSystem.HorizontalDatum);
        return new GeocentricCoordinateSystem(
            horizontalDatum,
            CloneLinearUnit(geocentricCoordinateSystem.LinearUnit),
            ClonePrimeMeridian(geocentricCoordinateSystem.PrimeMeridian),
            CloneAxisInfo(geocentricCoordinateSystem),
            name ?? geocentricCoordinateSystem.Name,
            authority ?? geocentricCoordinateSystem.Authority,
            authorityCode ?? geocentricCoordinateSystem.AuthorityCode,
            geocentricCoordinateSystem.Alias,
            geocentricCoordinateSystem.Remarks,
            geocentricCoordinateSystem.Abbreviation,
            geocentricCoordinateSystem.DefaultEnvelope);
    }

    private static VerticalCoordinateSystem CloneVerticalCoordinateSystem(
        VerticalCoordinateSystem verticalCoordinateSystem,
        string? authority = null,
        long? authorityCode = null,
        string? name = null)
    {
        var clone = new VerticalCoordinateSystem(
            CloneLinearUnit(verticalCoordinateSystem.LinearUnit),
            CloneVerticalDatum(verticalCoordinateSystem.VerticalDatum),
            CloneAxisInfo(verticalCoordinateSystem),
            name ?? verticalCoordinateSystem.Name,
            authority ?? verticalCoordinateSystem.Authority,
            authorityCode ?? verticalCoordinateSystem.AuthorityCode,
            verticalCoordinateSystem.Alias,
            verticalCoordinateSystem.Abbreviation,
            verticalCoordinateSystem.Remarks,
            verticalCoordinateSystem.DefaultEnvelope,
            CloneVerticalBoundGridTransformation(verticalCoordinateSystem.BoundGridTransformation));

        return clone;
    }

    private static CompoundCoordinateSystem CloneCompoundCoordinateSystem(
        CompoundCoordinateSystem compoundCoordinateSystem,
        string? authority = null,
        long? authorityCode = null,
        string? name = null)
    {
        return new CompoundCoordinateSystem(
            CloneCoordinateSystem(compoundCoordinateSystem.HeadCoordinateSystem),
            CloneCoordinateSystem(compoundCoordinateSystem.TailCoordinateSystem),
            name ?? compoundCoordinateSystem.Name,
            authority ?? compoundCoordinateSystem.Authority,
            authorityCode ?? compoundCoordinateSystem.AuthorityCode,
            compoundCoordinateSystem.Alias,
            compoundCoordinateSystem.Abbreviation,
            compoundCoordinateSystem.Remarks,
            CloneAxisInfo(compoundCoordinateSystem),
            compoundCoordinateSystem.DefaultEnvelope);
    }

    private static BoundCoordinateSystem CloneBoundCoordinateSystem(
        BoundCoordinateSystem boundCoordinateSystem,
        string? authority = null,
        long? authorityCode = null,
        string? name = null)
    {
        return new BoundCoordinateSystem(
            CloneCoordinateSystem(boundCoordinateSystem.SourceCoordinateSystem),
            CloneCoordinateSystem(boundCoordinateSystem.TargetCoordinateSystem),
            CloneBoundTransformation(boundCoordinateSystem.Transformation),
            name ?? boundCoordinateSystem.Name,
            authority ?? boundCoordinateSystem.Authority,
            authorityCode ?? boundCoordinateSystem.AuthorityCode,
            boundCoordinateSystem.Alias,
            boundCoordinateSystem.Abbreviation,
            boundCoordinateSystem.Remarks);
    }

    private static FittedCoordinateSystem CloneFittedCoordinateSystem(
        FittedCoordinateSystem fittedCoordinateSystem,
        string? authority = null,
        long? authorityCode = null,
        string? name = null)
    {
        return new FittedCoordinateSystem(
            CloneCoordinateSystem(fittedCoordinateSystem.BaseCoordinateSystem),
            fittedCoordinateSystem.ToBaseTransform,
            name ?? fittedCoordinateSystem.Name,
            authority ?? fittedCoordinateSystem.Authority,
            authorityCode ?? fittedCoordinateSystem.AuthorityCode,
            fittedCoordinateSystem.Alias,
            fittedCoordinateSystem.Remarks,
            fittedCoordinateSystem.Abbreviation,
            CloneAxisInfo(fittedCoordinateSystem));
    }

    private static EngineeringCoordinateSystem CloneEngineeringCoordinateSystem(
        EngineeringCoordinateSystem engineeringCoordinateSystem,
        string? authority = null,
        long? authorityCode = null,
        string? name = null)
    {
        return new EngineeringCoordinateSystem(
            CloneEngineeringDatum(engineeringCoordinateSystem.EngineeringDatum),
            engineeringCoordinateSystem.CoordinateSystemType,
            CloneAxisInfo(engineeringCoordinateSystem),
            CloneUnits(engineeringCoordinateSystem.AxisUnits),
            name ?? engineeringCoordinateSystem.Name,
            authority ?? engineeringCoordinateSystem.Authority,
            authorityCode ?? engineeringCoordinateSystem.AuthorityCode,
            engineeringCoordinateSystem.Alias,
            engineeringCoordinateSystem.Abbreviation,
            engineeringCoordinateSystem.Remarks);
    }

    private static ParametricCoordinateSystem CloneParametricCoordinateSystem(
        ParametricCoordinateSystem parametricCoordinateSystem,
        string? authority = null,
        long? authorityCode = null,
        string? name = null)
    {
        return new ParametricCoordinateSystem(
            CloneParametricUnit(parametricCoordinateSystem.ParametricUnit),
            CloneParametricDatum(parametricCoordinateSystem.ParametricDatum),
            new AxisInfo(parametricCoordinateSystem.GetAxis(0)),
            name ?? parametricCoordinateSystem.Name,
            authority ?? parametricCoordinateSystem.Authority,
            authorityCode ?? parametricCoordinateSystem.AuthorityCode,
            parametricCoordinateSystem.Alias,
            parametricCoordinateSystem.Abbreviation,
            parametricCoordinateSystem.Remarks);
    }

    private static TemporalCoordinateSystem CloneTemporalCoordinateSystem(
        TemporalCoordinateSystem temporalCoordinateSystem,
        string? authority = null,
        long? authorityCode = null,
        string? name = null)
    {
        return new TemporalCoordinateSystem(
            CloneTimeUnit(temporalCoordinateSystem.TimeUnit),
            CloneTemporalDatum(temporalCoordinateSystem.TemporalDatum),
            new AxisInfo(temporalCoordinateSystem.GetAxis(0)),
            name ?? temporalCoordinateSystem.Name,
            authority ?? temporalCoordinateSystem.Authority,
            authorityCode ?? temporalCoordinateSystem.AuthorityCode,
            temporalCoordinateSystem.Alias,
            temporalCoordinateSystem.Abbreviation,
            temporalCoordinateSystem.Remarks);
    }

    private static CoordinateSystem CloneCoordinateSystem(CoordinateSystem coordinateSystem)
    {
        return coordinateSystem switch
        {
            GeographicCoordinateSystem geographicCoordinateSystem => CloneGeographicCoordinateSystem(geographicCoordinateSystem),
            ProjectedCoordinateSystem projectedCoordinateSystem => CloneProjectedCoordinateSystem(projectedCoordinateSystem),
            GeocentricCoordinateSystem geocentricCoordinateSystem => CloneGeocentricCoordinateSystem(geocentricCoordinateSystem),
            VerticalCoordinateSystem verticalCoordinateSystem => CloneVerticalCoordinateSystem(verticalCoordinateSystem),
            CompoundCoordinateSystem compoundCoordinateSystem => CloneCompoundCoordinateSystem(compoundCoordinateSystem),
            BoundCoordinateSystem boundCoordinateSystem => CloneBoundCoordinateSystem(boundCoordinateSystem),
            FittedCoordinateSystem fittedCoordinateSystem => CloneFittedCoordinateSystem(fittedCoordinateSystem),
            EngineeringCoordinateSystem engineeringCoordinateSystem => CloneEngineeringCoordinateSystem(engineeringCoordinateSystem),
            ParametricCoordinateSystem parametricCoordinateSystem => CloneParametricCoordinateSystem(parametricCoordinateSystem),
            TemporalCoordinateSystem temporalCoordinateSystem => CloneTemporalCoordinateSystem(temporalCoordinateSystem),
            _ => throw new NotSupportedException($"Coordinate system cloning is not supported for type '{coordinateSystem.GetType().FullName}'."),
        };
    }

    private static List<IUnit> CloneUnits(IReadOnlyList<IUnit> units)
    {
        var clone = new List<IUnit>(units.Count);
        for (int i = 0; i < units.Count; i++)
        {
            clone.Add(CloneUnit(units[i]));
        }

        return clone;
    }

    private static List<AxisInfo> CloneAxisInfo(CoordinateSystem coordinateSystem)
    {
        var clone = new List<AxisInfo>(coordinateSystem.Dimension);
        for (int i = 0; i < coordinateSystem.Dimension; i++)
        {
            clone.Add(new AxisInfo(coordinateSystem.GetAxis(i)));
        }

        return clone;
    }
}
