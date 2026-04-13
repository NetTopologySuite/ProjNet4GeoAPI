// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Rebuilds immutable info-backed model objects while replacing their top-level authority metadata.
/// Typed overloads back the concrete <c>With*</c> APIs, while the generic <see cref="Info"/> overloads remain
/// the fallback for callers that only hold a base-typed reference.
/// </summary>
internal static partial class InfoAuthorityCloneHelper
{
    /// <summary>
    /// Creates a deep clone of the supplied info-backed model object with replacement authority metadata.
    /// This generic entry point remains the fallback for base-typed <see cref="Info"/> callers.
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
    /// Creates a deep clone of the supplied horizontal datum with replacement authority metadata.
    /// </summary>
    /// <param name="horizontalDatum">Datum to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned horizontal datum with the requested authority metadata.</returns>
    internal static HorizontalDatum CloneWithAuthority(HorizontalDatum horizontalDatum, string authority, long authorityCode)
    {
        horizontalDatum = ArgumentGuard.ThrowIfNull(horizontalDatum, nameof(horizontalDatum));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneHorizontalDatum(horizontalDatum, authority, authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied vertical datum with replacement authority metadata.
    /// </summary>
    /// <param name="verticalDatum">Datum to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned vertical datum with the requested authority metadata.</returns>
    internal static VerticalDatum CloneWithAuthority(VerticalDatum verticalDatum, string authority, long authorityCode)
    {
        verticalDatum = ArgumentGuard.ThrowIfNull(verticalDatum, nameof(verticalDatum));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneVerticalDatum(verticalDatum, authority, authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied engineering datum with replacement authority metadata.
    /// </summary>
    /// <param name="engineeringDatum">Datum to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned engineering datum with the requested authority metadata.</returns>
    internal static EngineeringDatum CloneWithAuthority(EngineeringDatum engineeringDatum, string authority, long authorityCode)
    {
        engineeringDatum = ArgumentGuard.ThrowIfNull(engineeringDatum, nameof(engineeringDatum));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneEngineeringDatum(engineeringDatum, authority, authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied parametric datum with replacement authority metadata.
    /// </summary>
    /// <param name="parametricDatum">Datum to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned parametric datum with the requested authority metadata.</returns>
    internal static ParametricDatum CloneWithAuthority(ParametricDatum parametricDatum, string authority, long authorityCode)
    {
        parametricDatum = ArgumentGuard.ThrowIfNull(parametricDatum, nameof(parametricDatum));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneParametricDatum(parametricDatum, authority, authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied temporal datum with replacement authority metadata.
    /// </summary>
    /// <param name="temporalDatum">Datum to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned temporal datum with the requested authority metadata.</returns>
    internal static TemporalDatum CloneWithAuthority(TemporalDatum temporalDatum, string authority, long authorityCode)
    {
        temporalDatum = ArgumentGuard.ThrowIfNull(temporalDatum, nameof(temporalDatum));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneTemporalDatum(temporalDatum, authority, authorityCode);
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
    /// This generic entry point remains the fallback for base-typed <see cref="Info"/> callers.
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
    /// Creates a deep clone of the supplied horizontal datum with a replacement name.
    /// </summary>
    /// <param name="horizontalDatum">Datum to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned horizontal datum with the requested name.</returns>
    internal static HorizontalDatum CloneWithName(HorizontalDatum horizontalDatum, string name)
    {
        horizontalDatum = ArgumentGuard.ThrowIfNull(horizontalDatum, nameof(horizontalDatum));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneHorizontalDatum(horizontalDatum, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied vertical datum with a replacement name.
    /// </summary>
    /// <param name="verticalDatum">Datum to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned vertical datum with the requested name.</returns>
    internal static VerticalDatum CloneWithName(VerticalDatum verticalDatum, string name)
    {
        verticalDatum = ArgumentGuard.ThrowIfNull(verticalDatum, nameof(verticalDatum));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneVerticalDatum(verticalDatum, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied engineering datum with a replacement name.
    /// </summary>
    /// <param name="engineeringDatum">Datum to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned engineering datum with the requested name.</returns>
    internal static EngineeringDatum CloneWithName(EngineeringDatum engineeringDatum, string name)
    {
        engineeringDatum = ArgumentGuard.ThrowIfNull(engineeringDatum, nameof(engineeringDatum));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneEngineeringDatum(engineeringDatum, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied parametric datum with a replacement name.
    /// </summary>
    /// <param name="parametricDatum">Datum to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned parametric datum with the requested name.</returns>
    internal static ParametricDatum CloneWithName(ParametricDatum parametricDatum, string name)
    {
        parametricDatum = ArgumentGuard.ThrowIfNull(parametricDatum, nameof(parametricDatum));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneParametricDatum(parametricDatum, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied temporal datum with a replacement name.
    /// </summary>
    /// <param name="temporalDatum">Datum to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned temporal datum with the requested name.</returns>
    internal static TemporalDatum CloneWithName(TemporalDatum temporalDatum, string name)
    {
        temporalDatum = ArgumentGuard.ThrowIfNull(temporalDatum, nameof(temporalDatum));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneTemporalDatum(temporalDatum, name: name);
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

    /// <summary>
    /// Creates a deep clone of the supplied horizontal datum with replacement retained datum-ensemble metadata.
    /// </summary>
    /// <param name="horizontalDatum">Datum to clone.</param>
    /// <param name="ensemble">Replacement ensemble metadata, or <see langword="null"/> to clear it.</param>
    /// <returns>A cloned horizontal datum with the requested ensemble metadata.</returns>
    internal static HorizontalDatum CloneWithEnsemble(HorizontalDatum horizontalDatum, DatumEnsemble? ensemble)
    {
        horizontalDatum = ArgumentGuard.ThrowIfNull(horizontalDatum, nameof(horizontalDatum));
        return CloneHorizontalDatum(horizontalDatum, ensemble);
    }

    /// <summary>
    /// Creates a deep clone of the supplied vertical datum with replacement retained datum-ensemble metadata.
    /// </summary>
    /// <param name="verticalDatum">Datum to clone.</param>
    /// <param name="ensemble">Replacement ensemble metadata, or <see langword="null"/> to clear it.</param>
    /// <returns>A cloned vertical datum with the requested ensemble metadata.</returns>
    internal static VerticalDatum CloneWithEnsemble(VerticalDatum verticalDatum, DatumEnsemble? ensemble)
    {
        verticalDatum = ArgumentGuard.ThrowIfNull(verticalDatum, nameof(verticalDatum));
        return CloneVerticalDatum(verticalDatum, ensemble);
    }

    /// <summary>
    /// Creates a deep clone of the supplied engineering datum with replacement retained datum-ensemble metadata.
    /// </summary>
    /// <param name="engineeringDatum">Datum to clone.</param>
    /// <param name="ensemble">Replacement ensemble metadata, or <see langword="null"/> to keep the datum non-ensemble-backed.</param>
    /// <returns>A cloned engineering datum with the requested ensemble metadata.</returns>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="ensemble"/> is not <see langword="null"/>.</exception>
    internal static EngineeringDatum CloneWithEnsemble(EngineeringDatum engineeringDatum, DatumEnsemble? ensemble)
    {
        engineeringDatum = ArgumentGuard.ThrowIfNull(engineeringDatum, nameof(engineeringDatum));
        if (ensemble is not null)
        {
            throw new NotSupportedException($"Datum ensembles are not supported for datum type '{engineeringDatum.GetType().FullName}'.");
        }

        return CloneEngineeringDatum(engineeringDatum);
    }

    /// <summary>
    /// Creates a deep clone of the supplied parametric datum with replacement retained datum-ensemble metadata.
    /// </summary>
    /// <param name="parametricDatum">Datum to clone.</param>
    /// <param name="ensemble">Replacement ensemble metadata, or <see langword="null"/> to keep the datum non-ensemble-backed.</param>
    /// <returns>A cloned parametric datum with the requested ensemble metadata.</returns>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="ensemble"/> is not <see langword="null"/>.</exception>
    internal static ParametricDatum CloneWithEnsemble(ParametricDatum parametricDatum, DatumEnsemble? ensemble)
    {
        parametricDatum = ArgumentGuard.ThrowIfNull(parametricDatum, nameof(parametricDatum));
        if (ensemble is not null)
        {
            throw new NotSupportedException($"Datum ensembles are not supported for datum type '{parametricDatum.GetType().FullName}'.");
        }

        return CloneParametricDatum(parametricDatum);
    }

    /// <summary>
    /// Creates a deep clone of the supplied temporal datum with replacement retained datum-ensemble metadata.
    /// </summary>
    /// <param name="temporalDatum">Datum to clone.</param>
    /// <param name="ensemble">Replacement ensemble metadata, or <see langword="null"/> to keep the datum non-ensemble-backed.</param>
    /// <returns>A cloned temporal datum with the requested ensemble metadata.</returns>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="ensemble"/> is not <see langword="null"/>.</exception>
    internal static TemporalDatum CloneWithEnsemble(TemporalDatum temporalDatum, DatumEnsemble? ensemble)
    {
        temporalDatum = ArgumentGuard.ThrowIfNull(temporalDatum, nameof(temporalDatum));
        if (ensemble is not null)
        {
            throw new NotSupportedException($"Datum ensembles are not supported for datum type '{temporalDatum.GetType().FullName}'.");
        }

        return CloneTemporalDatum(temporalDatum);
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
