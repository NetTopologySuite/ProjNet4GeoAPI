// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Rebuilds immutable info-backed model objects while replacing their top-level authority metadata.
/// </summary>
internal static class InfoAuthorityCloneHelper
{
    /// <summary>
    /// Creates a deep clone of the supplied info-backed model object with replacement authority metadata.
    /// </summary>
    /// <param name="info">Object to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned object of the same runtime type.</returns>
    internal static Info CloneWithAuthority(Info info, string authority, long authorityCode)
    {
        info = ArgumentGuard.ThrowIfNull(info, nameof(info));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));

        return info switch
        {
            AngularUnit angularUnit => CloneAngularUnit(angularUnit, authority, authorityCode),
            LinearUnit linearUnit => CloneLinearUnit(linearUnit, authority, authorityCode),
            Unit unit => CloneUnit(unit, authority, authorityCode),
            ParametricUnit parametricUnit => CloneParametricUnit(parametricUnit, authority, authorityCode),
            TimeUnit timeUnit => CloneTimeUnit(timeUnit, authority, authorityCode),
            Ellipsoid ellipsoid => CloneEllipsoid(ellipsoid, authority, authorityCode),
            PrimeMeridian primeMeridian => ClonePrimeMeridian(primeMeridian, authority, authorityCode),
            Projection projection => CloneProjection(projection, authority, authorityCode),
            HorizontalDatum horizontalDatum => CloneHorizontalDatum(horizontalDatum, authority, authorityCode),
            VerticalDatum verticalDatum => CloneVerticalDatum(verticalDatum, authority, authorityCode),
            EngineeringDatum engineeringDatum => CloneEngineeringDatum(engineeringDatum, authority, authorityCode),
            ParametricDatum parametricDatum => CloneParametricDatum(parametricDatum, authority, authorityCode),
            TemporalDatum temporalDatum => CloneTemporalDatum(temporalDatum, authority, authorityCode),
            GeographicCoordinateSystem geographicCoordinateSystem => CloneGeographicCoordinateSystem(geographicCoordinateSystem, authority, authorityCode),
            ProjectedCoordinateSystem projectedCoordinateSystem => CloneProjectedCoordinateSystem(projectedCoordinateSystem, authority, authorityCode),
            GeocentricCoordinateSystem geocentricCoordinateSystem => CloneGeocentricCoordinateSystem(geocentricCoordinateSystem, authority, authorityCode),
            VerticalCoordinateSystem verticalCoordinateSystem => CloneVerticalCoordinateSystem(verticalCoordinateSystem, authority, authorityCode),
            CompoundCoordinateSystem compoundCoordinateSystem => CloneCompoundCoordinateSystem(compoundCoordinateSystem, authority, authorityCode),
            BoundCoordinateSystem boundCoordinateSystem => CloneBoundCoordinateSystem(boundCoordinateSystem, authority, authorityCode),
            FittedCoordinateSystem fittedCoordinateSystem => CloneFittedCoordinateSystem(fittedCoordinateSystem, authority, authorityCode),
            EngineeringCoordinateSystem engineeringCoordinateSystem => CloneEngineeringCoordinateSystem(engineeringCoordinateSystem, authority, authorityCode),
            ParametricCoordinateSystem parametricCoordinateSystem => CloneParametricCoordinateSystem(parametricCoordinateSystem, authority, authorityCode),
            TemporalCoordinateSystem temporalCoordinateSystem => CloneTemporalCoordinateSystem(temporalCoordinateSystem, authority, authorityCode),
            CoordinateOperation coordinateOperation => CloneCoordinateOperation(coordinateOperation, authority, authorityCode),
            ConcatenatedOperation concatenatedOperation => CloneConcatenatedOperation(concatenatedOperation, authority, authorityCode),
            _ => throw new NotSupportedException($"WithAuthority is not supported for info type '{info.GetType().FullName}'."),
        };
    }

    /// <summary>
    /// Creates a deep clone of the supplied angular unit with replacement authority metadata.
    /// </summary>
    /// <param name="angularUnit">Unit to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned angular unit with the requested authority metadata.</returns>
    internal static AngularUnit CloneWithAuthority(AngularUnit angularUnit, string authority, long authorityCode)
    {
        angularUnit = ArgumentGuard.ThrowIfNull(angularUnit, nameof(angularUnit));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneAngularUnit(angularUnit, authority, authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied linear unit with replacement authority metadata.
    /// </summary>
    /// <param name="linearUnit">Unit to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned linear unit with the requested authority metadata.</returns>
    internal static LinearUnit CloneWithAuthority(LinearUnit linearUnit, string authority, long authorityCode)
    {
        linearUnit = ArgumentGuard.ThrowIfNull(linearUnit, nameof(linearUnit));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneLinearUnit(linearUnit, authority, authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied parametric unit with replacement authority metadata.
    /// </summary>
    /// <param name="parametricUnit">Unit to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned parametric unit with the requested authority metadata.</returns>
    internal static ParametricUnit CloneWithAuthority(ParametricUnit parametricUnit, string authority, long authorityCode)
    {
        parametricUnit = ArgumentGuard.ThrowIfNull(parametricUnit, nameof(parametricUnit));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneParametricUnit(parametricUnit, authority, authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied time unit with replacement authority metadata.
    /// </summary>
    /// <param name="timeUnit">Unit to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned time unit with the requested authority metadata.</returns>
    internal static TimeUnit CloneWithAuthority(TimeUnit timeUnit, string authority, long authorityCode)
    {
        timeUnit = ArgumentGuard.ThrowIfNull(timeUnit, nameof(timeUnit));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneTimeUnit(timeUnit, authority, authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied ellipsoid with replacement authority metadata.
    /// </summary>
    /// <param name="ellipsoid">Ellipsoid to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned ellipsoid with the requested authority metadata.</returns>
    internal static Ellipsoid CloneWithAuthority(Ellipsoid ellipsoid, string authority, long authorityCode)
    {
        ellipsoid = ArgumentGuard.ThrowIfNull(ellipsoid, nameof(ellipsoid));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneEllipsoid(ellipsoid, authority, authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied prime meridian with replacement authority metadata.
    /// </summary>
    /// <param name="primeMeridian">Prime meridian to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned prime meridian with the requested authority metadata.</returns>
    internal static PrimeMeridian CloneWithAuthority(PrimeMeridian primeMeridian, string authority, long authorityCode)
    {
        primeMeridian = ArgumentGuard.ThrowIfNull(primeMeridian, nameof(primeMeridian));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return ClonePrimeMeridian(primeMeridian, authority, authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied horizontal datum with replacement WGS84 conversion parameters.
    /// </summary>
    /// <param name="horizontalDatum">Datum to clone.</param>
    /// <param name="wgs84Parameters">Replacement WGS84 conversion parameters, or <see langword="null"/> to clear them.</param>
    /// <returns>A cloned datum with the requested WGS84 conversion parameters.</returns>
    internal static HorizontalDatum CloneWithWgs84Parameters(HorizontalDatum horizontalDatum, Wgs84ConversionInfo? wgs84Parameters)
    {
        horizontalDatum = ArgumentGuard.ThrowIfNull(horizontalDatum, nameof(horizontalDatum));
        return CloneHorizontalDatum(horizontalDatum, wgs84Parameters);
    }

    /// <summary>
    /// Creates a deep clone of the supplied info-backed model object with a replacement name.
    /// </summary>
    /// <param name="info">Object to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned object of the same runtime type.</returns>
    internal static Info CloneWithName(Info info, string name)
    {
        info = ArgumentGuard.ThrowIfNull(info, nameof(info));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));

        return info switch
        {
            AngularUnit angularUnit => CloneAngularUnit(angularUnit, name: name),
            LinearUnit linearUnit => CloneLinearUnit(linearUnit, name: name),
            Unit unit => CloneUnit(unit, name: name),
            ParametricUnit parametricUnit => CloneParametricUnit(parametricUnit, name: name),
            TimeUnit timeUnit => CloneTimeUnit(timeUnit, name: name),
            Ellipsoid ellipsoid => CloneEllipsoid(ellipsoid, name: name),
            PrimeMeridian primeMeridian => ClonePrimeMeridian(primeMeridian, name: name),
            Projection projection => CloneProjection(projection, name: name),
            HorizontalDatum horizontalDatum => CloneHorizontalDatum(horizontalDatum, name: name),
            VerticalDatum verticalDatum => CloneVerticalDatum(verticalDatum, name: name),
            EngineeringDatum engineeringDatum => CloneEngineeringDatum(engineeringDatum, name: name),
            ParametricDatum parametricDatum => CloneParametricDatum(parametricDatum, name: name),
            TemporalDatum temporalDatum => CloneTemporalDatum(temporalDatum, name: name),
            GeographicCoordinateSystem geographicCoordinateSystem => CloneGeographicCoordinateSystem(geographicCoordinateSystem, name: name),
            ProjectedCoordinateSystem projectedCoordinateSystem => CloneProjectedCoordinateSystem(projectedCoordinateSystem, name: name),
            GeocentricCoordinateSystem geocentricCoordinateSystem => CloneGeocentricCoordinateSystem(geocentricCoordinateSystem, name: name),
            VerticalCoordinateSystem verticalCoordinateSystem => CloneVerticalCoordinateSystem(verticalCoordinateSystem, name: name),
            CompoundCoordinateSystem compoundCoordinateSystem => CloneCompoundCoordinateSystem(compoundCoordinateSystem, name: name),
            BoundCoordinateSystem boundCoordinateSystem => CloneBoundCoordinateSystem(boundCoordinateSystem, name: name),
            FittedCoordinateSystem fittedCoordinateSystem => CloneFittedCoordinateSystem(fittedCoordinateSystem, name: name),
            EngineeringCoordinateSystem engineeringCoordinateSystem => CloneEngineeringCoordinateSystem(engineeringCoordinateSystem, name: name),
            ParametricCoordinateSystem parametricCoordinateSystem => CloneParametricCoordinateSystem(parametricCoordinateSystem, name: name),
            TemporalCoordinateSystem temporalCoordinateSystem => CloneTemporalCoordinateSystem(temporalCoordinateSystem, name: name),
            CoordinateOperation coordinateOperation => CloneCoordinateOperation(coordinateOperation, name: name),
            ConcatenatedOperation concatenatedOperation => CloneConcatenatedOperation(concatenatedOperation, name: name),
            _ => throw new NotSupportedException($"WithName is not supported for info type '{info.GetType().FullName}'."),
        };
    }

    /// <summary>
    /// Creates a deep clone of the supplied angular unit with a replacement name.
    /// </summary>
    /// <param name="angularUnit">Unit to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned angular unit with the requested name.</returns>
    internal static AngularUnit CloneWithName(AngularUnit angularUnit, string name)
    {
        angularUnit = ArgumentGuard.ThrowIfNull(angularUnit, nameof(angularUnit));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneAngularUnit(angularUnit, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied linear unit with a replacement name.
    /// </summary>
    /// <param name="linearUnit">Unit to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned linear unit with the requested name.</returns>
    internal static LinearUnit CloneWithName(LinearUnit linearUnit, string name)
    {
        linearUnit = ArgumentGuard.ThrowIfNull(linearUnit, nameof(linearUnit));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneLinearUnit(linearUnit, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied parametric unit with a replacement name.
    /// </summary>
    /// <param name="parametricUnit">Unit to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned parametric unit with the requested name.</returns>
    internal static ParametricUnit CloneWithName(ParametricUnit parametricUnit, string name)
    {
        parametricUnit = ArgumentGuard.ThrowIfNull(parametricUnit, nameof(parametricUnit));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneParametricUnit(parametricUnit, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied time unit with a replacement name.
    /// </summary>
    /// <param name="timeUnit">Unit to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned time unit with the requested name.</returns>
    internal static TimeUnit CloneWithName(TimeUnit timeUnit, string name)
    {
        timeUnit = ArgumentGuard.ThrowIfNull(timeUnit, nameof(timeUnit));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneTimeUnit(timeUnit, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied ellipsoid with a replacement name.
    /// </summary>
    /// <param name="ellipsoid">Ellipsoid to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned ellipsoid with the requested name.</returns>
    internal static Ellipsoid CloneWithName(Ellipsoid ellipsoid, string name)
    {
        ellipsoid = ArgumentGuard.ThrowIfNull(ellipsoid, nameof(ellipsoid));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneEllipsoid(ellipsoid, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied prime meridian with a replacement name.
    /// </summary>
    /// <param name="primeMeridian">Prime meridian to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned prime meridian with the requested name.</returns>
    internal static PrimeMeridian CloneWithName(PrimeMeridian primeMeridian, string name)
    {
        primeMeridian = ArgumentGuard.ThrowIfNull(primeMeridian, nameof(primeMeridian));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return ClonePrimeMeridian(primeMeridian, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied datum with replacement retained datum-ensemble metadata.
    /// </summary>
    /// <param name="datum">Datum to clone.</param>
    /// <param name="ensemble">Replacement ensemble metadata, or <see langword="null"/> to clear it.</param>
    /// <returns>A cloned datum of the same runtime type when supported.</returns>
    internal static Datum CloneWithEnsemble(Datum datum, DatumEnsemble? ensemble)
    {
        datum = ArgumentGuard.ThrowIfNull(datum, nameof(datum));

        return datum switch
        {
            HorizontalDatum horizontalDatum => CloneHorizontalDatum(horizontalDatum, ensemble),
            VerticalDatum verticalDatum => CloneVerticalDatum(verticalDatum, ensemble),
            EngineeringDatum engineeringDatum when ensemble is null => CloneEngineeringDatum(engineeringDatum),
            ParametricDatum parametricDatum when ensemble is null => CloneParametricDatum(parametricDatum),
            TemporalDatum temporalDatum when ensemble is null => CloneTemporalDatum(temporalDatum),
            _ => throw new NotSupportedException($"Datum ensembles are not supported for datum type '{datum.GetType().FullName}'."),
        };
    }

    private static AngularUnit CloneAngularUnit(AngularUnit angularUnit, string? authority = null, long? authorityCode = null, string? name = null)
    {
        return new AngularUnit(
            angularUnit.RadiansPerUnit,
            name ?? angularUnit.Name,
            authority ?? angularUnit.Authority,
            authorityCode ?? angularUnit.AuthorityCode,
            angularUnit.Alias,
            angularUnit.Abbreviation,
            angularUnit.Remarks);
    }

    private static LinearUnit CloneLinearUnit(LinearUnit linearUnit, string? authority = null, long? authorityCode = null, string? name = null)
    {
        return new LinearUnit(
            linearUnit.MetersPerUnit,
            name ?? linearUnit.Name,
            authority ?? linearUnit.Authority,
            authorityCode ?? linearUnit.AuthorityCode,
            linearUnit.Alias,
            linearUnit.Abbreviation,
            linearUnit.Remarks);
    }

    private static Unit CloneUnit(Unit unit, string? authority = null, long? authorityCode = null, string? name = null)
    {
        return new Unit(
            unit.ConversionFactor,
            name ?? unit.Name,
            authority ?? unit.Authority,
            authorityCode ?? unit.AuthorityCode,
            unit.Alias,
            unit.Abbreviation,
            unit.Remarks);
    }

    private static ParametricUnit CloneParametricUnit(ParametricUnit parametricUnit, string? authority = null, long? authorityCode = null, string? name = null)
    {
        return new ParametricUnit(
            parametricUnit.ConversionFactor,
            name ?? parametricUnit.Name,
            authority ?? parametricUnit.Authority,
            authorityCode ?? parametricUnit.AuthorityCode,
            parametricUnit.Alias,
            parametricUnit.Abbreviation,
            parametricUnit.Remarks);
    }

    private static TimeUnit CloneTimeUnit(TimeUnit timeUnit, string? authority = null, long? authorityCode = null, string? name = null)
    {
        return new TimeUnit(
            timeUnit.ConversionFactor,
            name ?? timeUnit.Name,
            authority ?? timeUnit.Authority,
            authorityCode ?? timeUnit.AuthorityCode,
            timeUnit.Alias,
            timeUnit.Abbreviation,
            timeUnit.Remarks);
    }

    private static Ellipsoid CloneEllipsoid(Ellipsoid ellipsoid, string? authority = null, long? authorityCode = null, string? name = null)
    {
        return new Ellipsoid(
            ellipsoid.SemiMajorAxis,
            ellipsoid.SemiMinorAxis,
            ellipsoid.InverseFlattening,
            ellipsoid.IsIvfDefinitive,
            CloneLinearUnit(ellipsoid.AxisUnit),
            name ?? ellipsoid.Name,
            authority ?? ellipsoid.Authority,
            authorityCode ?? ellipsoid.AuthorityCode,
            ellipsoid.Alias,
            ellipsoid.Abbreviation,
            ellipsoid.Remarks);
    }

    private static PrimeMeridian ClonePrimeMeridian(PrimeMeridian primeMeridian, string? authority = null, long? authorityCode = null, string? name = null)
    {
        return new PrimeMeridian(
            primeMeridian.Longitude,
            CloneAngularUnit(primeMeridian.AngularUnit),
            name ?? primeMeridian.Name,
            authority ?? primeMeridian.Authority,
            authorityCode ?? primeMeridian.AuthorityCode,
            primeMeridian.Alias,
            primeMeridian.Abbreviation,
            primeMeridian.Remarks);
    }

    private static Projection CloneProjection(IProjection projection, string? authority = null, long? authorityCode = null, string? name = null)
    {
        return new Projection(
            projection.ClassName,
            CloneProjectionParameters(projection),
            name ?? projection.Name,
            authority ?? projection.Authority,
            authorityCode ?? projection.AuthorityCode,
            projection.Alias,
            projection.Remarks,
            projection.Abbreviation);
    }

    private static HorizontalDatum CloneHorizontalDatum(HorizontalDatum horizontalDatum, string? authority = null, long? authorityCode = null, string? name = null)
    {
        Ellipsoid ellipsoid = CloneEllipsoid(horizontalDatum.Ellipsoid);
        return new HorizontalDatum(
            ellipsoid,
            CloneOptionalWgs84ConversionInfo(horizontalDatum.Wgs84Parameters),
            horizontalDatum.DatumType,
            name ?? horizontalDatum.Name,
            authority ?? horizontalDatum.Authority,
            authorityCode ?? horizontalDatum.AuthorityCode,
            horizontalDatum.Alias,
            horizontalDatum.Remarks,
            horizontalDatum.Abbreviation,
            CloneDatumEnsemble(horizontalDatum.Ensemble, ellipsoid));
    }

    private static HorizontalDatum CloneHorizontalDatum(HorizontalDatum horizontalDatum, Wgs84ConversionInfo? wgs84Parameters)
    {
        Ellipsoid ellipsoid = CloneEllipsoid(horizontalDatum.Ellipsoid);
        return new HorizontalDatum(
            ellipsoid,
            CloneOptionalWgs84ConversionInfo(wgs84Parameters),
            horizontalDatum.DatumType,
            horizontalDatum.Name,
            horizontalDatum.Authority,
            horizontalDatum.AuthorityCode,
            horizontalDatum.Alias,
            horizontalDatum.Remarks,
            horizontalDatum.Abbreviation,
            CloneDatumEnsemble(horizontalDatum.Ensemble, ellipsoid));
    }

    private static HorizontalDatum CloneHorizontalDatum(HorizontalDatum horizontalDatum, DatumEnsemble? ensemble)
    {
        Ellipsoid ellipsoid = CloneEllipsoid(horizontalDatum.Ellipsoid);
        return new HorizontalDatum(
            ellipsoid,
            CloneOptionalWgs84ConversionInfo(horizontalDatum.Wgs84Parameters),
            horizontalDatum.DatumType,
            horizontalDatum.Name,
            horizontalDatum.Authority,
            horizontalDatum.AuthorityCode,
            horizontalDatum.Alias,
            horizontalDatum.Remarks,
            horizontalDatum.Abbreviation,
            CloneDatumEnsemble(ensemble, ellipsoid));
    }

    private static VerticalDatum CloneVerticalDatum(VerticalDatum verticalDatum, string? authority = null, long? authorityCode = null, string? name = null)
    {
        return new VerticalDatum(
            verticalDatum.DatumType,
            name ?? verticalDatum.Name,
            authority ?? verticalDatum.Authority,
            authorityCode ?? verticalDatum.AuthorityCode,
            verticalDatum.Alias,
            verticalDatum.Remarks,
            verticalDatum.Abbreviation,
            CloneDatumEnsemble(verticalDatum.Ensemble));
    }

    private static VerticalDatum CloneVerticalDatum(VerticalDatum verticalDatum, DatumEnsemble? ensemble)
    {
        return new VerticalDatum(
            verticalDatum.DatumType,
            verticalDatum.Name,
            verticalDatum.Authority,
            verticalDatum.AuthorityCode,
            verticalDatum.Alias,
            verticalDatum.Remarks,
            verticalDatum.Abbreviation,
            CloneDatumEnsemble(ensemble));
    }

    private static EngineeringDatum CloneEngineeringDatum(EngineeringDatum engineeringDatum, string? authority = null, long? authorityCode = null, string? name = null)
    {
        return new EngineeringDatum(
            name ?? engineeringDatum.Name,
            authority ?? engineeringDatum.Authority,
            authorityCode ?? engineeringDatum.AuthorityCode,
            engineeringDatum.Alias,
            engineeringDatum.Remarks,
            engineeringDatum.Abbreviation);
    }

    private static ParametricDatum CloneParametricDatum(ParametricDatum parametricDatum, string? authority = null, long? authorityCode = null, string? name = null)
    {
        return new ParametricDatum(
            name ?? parametricDatum.Name,
            authority ?? parametricDatum.Authority,
            authorityCode ?? parametricDatum.AuthorityCode,
            parametricDatum.Alias,
            parametricDatum.Remarks,
            parametricDatum.Abbreviation);
    }

    private static TemporalDatum CloneTemporalDatum(TemporalDatum temporalDatum, string? authority = null, long? authorityCode = null, string? name = null)
    {
        return new TemporalDatum(
            temporalDatum.TimeOrigin,
            name ?? temporalDatum.Name,
            authority ?? temporalDatum.Authority,
            authorityCode ?? temporalDatum.AuthorityCode,
            temporalDatum.Alias,
            temporalDatum.Remarks,
            temporalDatum.Abbreviation);
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

    private static CoordinateOperation CloneCoordinateOperation(
        CoordinateOperation coordinateOperation,
        string? authority = null,
        long? authorityCode = null,
        string? name = null)
    {
        return new CoordinateOperation(
            coordinateOperation.MethodName,
            CloneParameters(coordinateOperation.Parameters),
            CloneCoordinateSystem(coordinateOperation.SourceCoordinateSystem),
            CloneCoordinateSystem(coordinateOperation.TargetCoordinateSystem),
            name ?? coordinateOperation.Name,
            authority ?? coordinateOperation.Authority,
            authorityCode ?? coordinateOperation.AuthorityCode,
            coordinateOperation.Alias,
            coordinateOperation.Abbreviation,
            coordinateOperation.Remarks);
    }

    private static ConcatenatedOperation CloneConcatenatedOperation(
        ConcatenatedOperation concatenatedOperation,
        string? authority = null,
        long? authorityCode = null,
        string? name = null)
    {
        return new ConcatenatedOperation(
            CloneCoordinateOperations(concatenatedOperation.Steps),
            CloneCoordinateSystem(concatenatedOperation.SourceCoordinateSystem),
            CloneCoordinateSystem(concatenatedOperation.TargetCoordinateSystem),
            name ?? concatenatedOperation.Name,
            authority ?? concatenatedOperation.Authority,
            authorityCode ?? concatenatedOperation.AuthorityCode,
            concatenatedOperation.Alias,
            concatenatedOperation.Abbreviation,
            concatenatedOperation.Remarks);
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

    private static IUnit CloneUnit(IUnit unit)
    {
        return unit switch
        {
            AngularUnit angularUnit => CloneAngularUnit(angularUnit),
            LinearUnit linearUnit => CloneLinearUnit(linearUnit),
            Unit genericUnit => CloneUnit(genericUnit),
            ParametricUnit parametricUnit => CloneParametricUnit(parametricUnit),
            TimeUnit timeUnit => CloneTimeUnit(timeUnit),
            _ => throw new NotSupportedException($"Unit cloning is not supported for type '{unit.GetType().FullName}'."),
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

    private static List<ProjectionParameter> CloneProjectionParameters(IProjection projection)
    {
        var clone = new List<ProjectionParameter>(projection.NumParameters);
        for (int i = 0; i < projection.NumParameters; i++)
        {
            ProjectionParameter parameter = projection.GetParameter(i);
            clone.Add(new ProjectionParameter(parameter.Name, parameter.Value));
        }

        return clone;
    }

    private static List<Parameter> CloneParameters(IReadOnlyList<Parameter> parameters)
    {
        var clone = new List<Parameter>(parameters.Count);
        for (int i = 0; i < parameters.Count; i++)
        {
            clone.Add(new Parameter(parameters[i].Name, parameters[i].Value));
        }

        return clone;
    }

    private static List<CoordinateOperation> CloneCoordinateOperations(IReadOnlyList<CoordinateOperation> steps)
    {
        var clone = new List<CoordinateOperation>(steps.Count);
        for (int i = 0; i < steps.Count; i++)
        {
            clone.Add(CloneCoordinateOperation(steps[i]));
        }

        return clone;
    }

    private static DatumEnsemble? CloneDatumEnsemble(DatumEnsemble? ensemble, Ellipsoid? ellipsoidOverride = null)
    {
        if (ensemble is null)
        {
            return null;
        }

        return new DatumEnsemble(
            ensemble.Name,
            CloneDatumEnsembleMembers(ensemble.Members),
            ensemble.Accuracy,
            ellipsoidOverride ?? (ensemble.Ellipsoid is null ? null : CloneEllipsoid(ensemble.Ellipsoid)),
            ensemble.Authority,
            ensemble.AuthorityCode);
    }

    private static List<DatumEnsembleMember> CloneDatumEnsembleMembers(IReadOnlyList<DatumEnsembleMember> members)
    {
        var clone = new List<DatumEnsembleMember>(members.Count);
        for (int i = 0; i < members.Count; i++)
        {
            DatumEnsembleMember member = members[i];
            clone.Add(new DatumEnsembleMember(member.Name, member.Authority, member.AuthorityCode));
        }

        return clone;
    }

    private static BoundTransformation CloneBoundTransformation(BoundTransformation transformation)
    {
        if (transformation.Wgs84Parameters is not null)
        {
            return new BoundTransformation(
                transformation.MethodName,
                CloneWgs84ConversionInfo(transformation.Wgs84Parameters));
        }

        return new BoundTransformation(
            transformation.MethodName,
            ArgumentGuard.ThrowIfNull(transformation.ParameterFileName, nameof(transformation.ParameterFileName)));
    }

    private static VerticalBoundGridTransformation? CloneVerticalBoundGridTransformation(VerticalBoundGridTransformation? transformation)
    {
        if (transformation is null)
        {
            return null;
        }

        return new VerticalBoundGridTransformation(
            transformation.MethodName,
            transformation.ParameterFileName,
            CloneCompoundCoordinateSystem(transformation.HubCoordinateSystem));
    }

    private static List<Wgs84ConversionInfo> CloneWgs84ConversionInfoList(List<Wgs84ConversionInfo> conversions)
    {
        var clone = new List<Wgs84ConversionInfo>(conversions.Count);
        for (int i = 0; i < conversions.Count; i++)
        {
            clone.Add(CloneWgs84ConversionInfo(conversions[i]));
        }

        return clone;
    }

    private static Wgs84ConversionInfo CloneWgs84ConversionInfo(Wgs84ConversionInfo conversionInfo)
    {
        return new Wgs84ConversionInfo(
            conversionInfo.Dx,
            conversionInfo.Dy,
            conversionInfo.Dz,
            conversionInfo.Ex,
            conversionInfo.Ey,
            conversionInfo.Ez,
            conversionInfo.Ppm,
            conversionInfo.AreaOfUse);
    }

    private static Wgs84ConversionInfo? CloneOptionalWgs84ConversionInfo(Wgs84ConversionInfo? conversionInfo)
        => conversionInfo is null ? null : CloneWgs84ConversionInfo(conversionInfo);
}
