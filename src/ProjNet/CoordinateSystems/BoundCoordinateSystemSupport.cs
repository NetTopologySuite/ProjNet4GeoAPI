// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Shared BoundCRS parsing and runtime-normalization helpers.
/// </summary>
internal static class BoundCoordinateSystemSupport
{
    /// <summary>
    /// Creates normalized bound-transformation metadata for the currently supported BoundCRS subset.
    /// </summary>
    /// <param name="methodName">Transformation method name.</param>
    /// <param name="wgs84Parameters">Bursa-Wolf parameters when the method is parameter based.</param>
    /// <param name="parameterFileName">Parameter file reference when the method is grid based.</param>
    /// <returns>The normalized bound transformation.</returns>
    internal static BoundTransformation CreateBoundTransformation(string methodName, Wgs84ConversionInfo? wgs84Parameters, string? parameterFileName)
    {
        if (IsCoordinateFrameRotationMethod(methodName))
        {
            if (wgs84Parameters is null)
            {
                throw new NotSupportedException("BOUNDCRS coordinate-frame rotations require Bursa-Wolf parameters.");
            }

            if (!string.IsNullOrWhiteSpace(parameterFileName))
            {
                throw new NotSupportedException("BOUNDCRS Bursa-Wolf-style abridged transformations do not support PARAMETERFILE.");
            }

            return new BoundTransformation(methodName, new Wgs84ConversionInfo(
                wgs84Parameters.Dx,
                wgs84Parameters.Dy,
                wgs84Parameters.Dz,
                -wgs84Parameters.Ex,
                -wgs84Parameters.Ey,
                -wgs84Parameters.Ez,
                wgs84Parameters.Ppm,
                wgs84Parameters.AreaOfUse));
        }

        if (IsGeocentricTranslationsMethod(methodName) || IsPositionVectorMethod(methodName))
        {
            if (wgs84Parameters is null)
            {
                throw new NotSupportedException("BOUNDCRS Bursa-Wolf-style abridged transformations require numeric parameters.");
            }

            if (!string.IsNullOrWhiteSpace(parameterFileName))
            {
                throw new NotSupportedException("BOUNDCRS Bursa-Wolf-style abridged transformations do not support PARAMETERFILE.");
            }

            return new BoundTransformation(methodName, CloneWgs84Parameters(wgs84Parameters));
        }

        if (IsGeographic3DToGravityRelatedHeightMethod(methodName))
        {
            if (string.IsNullOrWhiteSpace(parameterFileName))
            {
                throw new NotSupportedException("BOUNDCRS vertical abridged transformations require a PARAMETERFILE.");
            }

            return new BoundTransformation(methodName, ArgumentGuard.ThrowIfNull(parameterFileName, nameof(parameterFileName)));
        }

        throw new NotSupportedException($"BOUNDCRS abridged transformation method '{methodName}' is not supported.");
    }

    /// <summary>
    /// Assigns a normalized BoundCRS transformation parameter to a Bursa-Wolf container.
    /// </summary>
    /// <param name="parameterName">Parameter name or normalized token.</param>
    /// <param name="value">Parameter value.</param>
    /// <param name="parameters">Target parameter container.</param>
    internal static void AssignTransformationParameter(string parameterName, double value, Wgs84ConversionInfo parameters)
    {
        parameters = ArgumentGuard.ThrowIfNull(parameters, nameof(parameters));

        string normalizedParameterName = parameterName.Trim().ToUpperInvariant();
        switch (normalizedParameterName)
        {
            case "X-AXIS TRANSLATION":
            case "DX":
                parameters.Dx = value;
                break;
            case "Y-AXIS TRANSLATION":
            case "DY":
                parameters.Dy = value;
                break;
            case "Z-AXIS TRANSLATION":
            case "DZ":
                parameters.Dz = value;
                break;
            case "X-AXIS ROTATION":
            case "EX":
                parameters.Ex = value;
                break;
            case "Y-AXIS ROTATION":
            case "EY":
                parameters.Ey = value;
                break;
            case "Z-AXIS ROTATION":
            case "EZ":
                parameters.Ez = value;
                break;
            case "SCALE DIFFERENCE":
            case "PPM":
                parameters.Ppm = value;
                break;
            default:
                throw new NotSupportedException($"BOUNDCRS transformation parameter '{parameterName}' is not supported.");
        }
    }

    /// <summary>
    /// Determines whether the coordinate system tree contains a BoundCRS wrapper.
    /// </summary>
    /// <param name="coordinateSystem">Coordinate system to inspect.</param>
    /// <returns><see langword="true"/> when a bound wrapper is present; otherwise <see langword="false"/>.</returns>
    internal static bool ContainsBoundCoordinateSystem(CoordinateSystem coordinateSystem)
    {
        return coordinateSystem switch
        {
            BoundCoordinateSystem => true,
            CompoundCoordinateSystem compoundCoordinateSystem => ContainsBoundCoordinateSystem(compoundCoordinateSystem.HeadCoordinateSystem)
                || ContainsBoundCoordinateSystem(compoundCoordinateSystem.TailCoordinateSystem),
            _ => false,
        };
    }

    /// <summary>
    /// Rewrites BoundCRS wrappers into the legacy runtime-compatible coordinate-system shapes.
    /// </summary>
    /// <param name="coordinateSystem">Coordinate system to normalize.</param>
    /// <returns>A runtime-compatible coordinate system.</returns>
    internal static CoordinateSystem NormalizeCoordinateSystemForRuntime(CoordinateSystem coordinateSystem)
    {
        coordinateSystem = ArgumentGuard.ThrowIfNull(coordinateSystem, nameof(coordinateSystem));

        return coordinateSystem switch
        {
            BoundCoordinateSystem boundCoordinateSystem => NormalizeBoundCoordinateSystemForRuntime(boundCoordinateSystem),
            CompoundCoordinateSystem compoundCoordinateSystem => NormalizeCompoundCoordinateSystemForRuntime(compoundCoordinateSystem),
            _ => coordinateSystem,
        };
    }

    /// <summary>
    /// Attempts to discover the horizontal datum represented by the coordinate system tree.
    /// </summary>
    /// <param name="coordinateSystem">Coordinate system to inspect.</param>
    /// <param name="horizontalDatum">Resolved horizontal datum when available.</param>
    /// <returns><see langword="true"/> when a horizontal datum was found; otherwise <see langword="false"/>.</returns>
    internal static bool TryGetHorizontalDatum(CoordinateSystem coordinateSystem, out HorizontalDatum? horizontalDatum)
    {
        switch (coordinateSystem)
        {
            case BoundCoordinateSystem boundCoordinateSystem:
                return TryGetHorizontalDatum(boundCoordinateSystem.SourceCoordinateSystem, out horizontalDatum);
            case GeographicCoordinateSystem geographicCoordinateSystem:
                horizontalDatum = geographicCoordinateSystem.HorizontalDatum;
                return true;
            case ProjectedCoordinateSystem projectedCoordinateSystem:
                horizontalDatum = projectedCoordinateSystem.HorizontalDatum;
                return true;
            case GeocentricCoordinateSystem geocentricCoordinateSystem:
                horizontalDatum = geocentricCoordinateSystem.HorizontalDatum;
                return true;
            case CompoundCoordinateSystem compoundCoordinateSystem when compoundCoordinateSystem.TailCoordinateSystem is VerticalCoordinateSystem:
                return TryGetHorizontalDatum(compoundCoordinateSystem.HeadCoordinateSystem, out horizontalDatum);
            default:
                horizontalDatum = null;
                return false;
        }
    }

    /// <summary>
    /// Returns the canonical WKT keyword label for diagnostic messages.
    /// </summary>
    /// <param name="coordinateSystem">Coordinate system to classify.</param>
    /// <returns>The best matching keyword label.</returns>
    internal static string GetCoordinateSystemKeyword(CoordinateSystem coordinateSystem)
    {
        return coordinateSystem switch
        {
            BoundCoordinateSystem => "BOUNDCRS",
            GeographicCoordinateSystem => "GEOGCRS",
            ProjectedCoordinateSystem => "PROJCRS",
            GeocentricCoordinateSystem => "GEODCRS",
            VerticalCoordinateSystem => "VERTCRS",
            CompoundCoordinateSystem => "COMPOUNDCRS",
            FittedCoordinateSystem => "FITTED_CS",
            _ => coordinateSystem.GetType().Name,
        };
    }

    /// <summary>
    /// Normalizes a parsed vertical BoundCRS hub to the canonical WGS84 lon/lat/up runtime form.
    /// </summary>
    /// <param name="parsedHubCoordinateSystem">Parsed hub coordinate system.</param>
    /// <returns>The runtime-compatible hub compound coordinate system.</returns>
    internal static CompoundCoordinateSystem CreateRuntimeCompatibleVerticalBoundHubCoordinateSystem(CompoundCoordinateSystem parsedHubCoordinateSystem)
    {
        if (parsedHubCoordinateSystem.TailCoordinateSystem is not VerticalCoordinateSystem parsedVerticalCoordinateSystem)
        {
            throw new NotSupportedException("BOUNDCRS vertical targets must be ellipsoidal 3D geographic CRS definitions.");
        }

        var runtimeVerticalCoordinateSystem = new VerticalCoordinateSystem(
            CloneLinearUnit(parsedVerticalCoordinateSystem.LinearUnit),
            new VerticalDatum(
                DatumType.VD_Ellipsoidal,
                parsedVerticalCoordinateSystem.VerticalDatum.Name,
                parsedVerticalCoordinateSystem.Authority,
                parsedVerticalCoordinateSystem.AuthorityCode,
                parsedVerticalCoordinateSystem.Alias,
                parsedVerticalCoordinateSystem.Remarks,
                parsedVerticalCoordinateSystem.Abbreviation),
            new AxisInfo(parsedVerticalCoordinateSystem.GetAxis(0)),
            parsedVerticalCoordinateSystem.Name,
            parsedVerticalCoordinateSystem.Authority,
            parsedVerticalCoordinateSystem.AuthorityCode,
            parsedVerticalCoordinateSystem.Alias,
            parsedVerticalCoordinateSystem.Abbreviation,
            parsedVerticalCoordinateSystem.Remarks);
        CopyDefaultEnvelope(parsedVerticalCoordinateSystem, runtimeVerticalCoordinateSystem);

        var runtimeHubCoordinateSystem = new CompoundCoordinateSystem(
            GeographicCoordinateSystem.WGS84,
            runtimeVerticalCoordinateSystem,
            parsedHubCoordinateSystem.Name,
            parsedHubCoordinateSystem.Authority,
            parsedHubCoordinateSystem.AuthorityCode,
            parsedHubCoordinateSystem.Alias,
            parsedHubCoordinateSystem.Abbreviation,
            parsedHubCoordinateSystem.Remarks);
        CopyDefaultEnvelope(parsedHubCoordinateSystem, runtimeHubCoordinateSystem);
        return runtimeHubCoordinateSystem;
    }

    /// <summary>
    /// Compares parameter-file references using the repository runtime's path-normalization rules.
    /// </summary>
    /// <param name="left">First parameter-file reference.</param>
    /// <param name="right">Second parameter-file reference.</param>
    /// <returns><see langword="true"/> when both references resolve to the same normalized token; otherwise <see langword="false"/>.</returns>
    internal static bool AreEquivalentParameterFileReferences(string left, string right)
    {
        StringComparison comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return NormalizeParameterFileReference(left).Equals(NormalizeParameterFileReference(right), comparison);
    }

    /// <summary>
    /// Normalizes a parameter-file reference for runtime comparison.
    /// </summary>
    /// <param name="parameterFileName">Parameter-file reference to normalize.</param>
    /// <returns>The normalized reference.</returns>
    internal static string NormalizeParameterFileReference(string parameterFileName)
    {
        string normalized = parameterFileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        if (!Path.IsPathRooted(normalized))
        {
            return normalized;
        }

        try
        {
            return Path.GetFullPath(normalized);
        }
        catch (ArgumentException)
        {
            return normalized;
        }
        catch (NotSupportedException)
        {
            return normalized;
        }
    }

    private static CoordinateSystem NormalizeBoundCoordinateSystemForRuntime(BoundCoordinateSystem boundCoordinateSystem)
    {
        CoordinateSystem runtimeSource = CloneCoordinateSystem(NormalizeCoordinateSystemForRuntime(boundCoordinateSystem.SourceCoordinateSystem));
        CoordinateSystem runtimeTarget = NormalizeCoordinateSystemForRuntime(boundCoordinateSystem.TargetCoordinateSystem);
        return ApplyBoundTransformationToSource(runtimeSource, runtimeTarget, boundCoordinateSystem.Transformation);
    }

    private static CompoundCoordinateSystem NormalizeCompoundCoordinateSystemForRuntime(CompoundCoordinateSystem compoundCoordinateSystem)
    {
        CoordinateSystem normalizedHead = NormalizeCoordinateSystemForRuntime(compoundCoordinateSystem.HeadCoordinateSystem);
        CoordinateSystem normalizedTail = NormalizeCoordinateSystemForRuntime(compoundCoordinateSystem.TailCoordinateSystem);

        if (ReferenceEquals(normalizedHead, compoundCoordinateSystem.HeadCoordinateSystem)
            && ReferenceEquals(normalizedTail, compoundCoordinateSystem.TailCoordinateSystem))
        {
            return compoundCoordinateSystem;
        }

        var normalizedCompound = new CompoundCoordinateSystem(
            normalizedHead,
            normalizedTail,
            compoundCoordinateSystem.Name,
            compoundCoordinateSystem.Authority,
            compoundCoordinateSystem.AuthorityCode,
            compoundCoordinateSystem.Alias,
            compoundCoordinateSystem.Abbreviation,
            compoundCoordinateSystem.Remarks);
        CopyDefaultEnvelope(compoundCoordinateSystem, normalizedCompound);
        return normalizedCompound;
    }

    private static CoordinateSystem ApplyBoundTransformationToSource(
        CoordinateSystem sourceCoordinateSystem,
        CoordinateSystem targetCoordinateSystem,
        BoundTransformation transformation)
    {
        if (transformation.UsesWgs84Parameters)
        {
            return ApplyHorizontalBoundCoordinateSystemToSource(
                sourceCoordinateSystem,
                targetCoordinateSystem,
                ArgumentGuard.ThrowIfNull(transformation.Wgs84Parameters, nameof(transformation.Wgs84Parameters)));
        }

        if (sourceCoordinateSystem is VerticalCoordinateSystem verticalCoordinateSystem)
        {
            return ApplyVerticalBoundCoordinateSystemToSource(
                verticalCoordinateSystem,
                targetCoordinateSystem,
                transformation.MethodName,
                ArgumentGuard.ThrowIfNull(transformation.ParameterFileName, nameof(transformation.ParameterFileName)));
        }

        throw new NotSupportedException(
            $"BOUNDCRS source coordinate system type '{GetCoordinateSystemKeyword(sourceCoordinateSystem)}' is not supported.");
    }

    private static CoordinateSystem ApplyHorizontalBoundCoordinateSystemToSource(
        CoordinateSystem sourceCoordinateSystem,
        CoordinateSystem targetCoordinateSystem,
        Wgs84ConversionInfo wgs84Parameters)
    {
        if (!TryGetHorizontalDatum(sourceCoordinateSystem, out HorizontalDatum? sourceDatum))
        {
            throw new NotSupportedException(
                $"BOUNDCRS source coordinate system type '{GetCoordinateSystemKeyword(sourceCoordinateSystem)}' is not supported.");
        }

        if (!TryGetHorizontalDatum(targetCoordinateSystem, out HorizontalDatum? targetDatum))
        {
            throw new NotSupportedException("BOUNDCRS targets other than WGS 84 are not supported.");
        }

        sourceDatum = ArgumentGuard.ThrowIfNull(sourceDatum, nameof(sourceDatum));
        targetDatum = ArgumentGuard.ThrowIfNull(targetDatum, nameof(targetDatum));

        if (!targetDatum.EqualParams(HorizontalDatum.WGS84))
        {
            throw new NotSupportedException("BOUNDCRS targets other than WGS 84 are not supported.");
        }

        if (sourceDatum.Wgs84Parameters is not null && !sourceDatum.Wgs84Parameters.Equals(wgs84Parameters))
        {
            throw new NotSupportedException("BOUNDCRS source CRS already defines a conflicting WGS 84 transformation.");
        }

        sourceDatum.Wgs84Parameters ??= CloneWgs84Parameters(wgs84Parameters);
        return sourceCoordinateSystem;
    }

    private static VerticalCoordinateSystem ApplyVerticalBoundCoordinateSystemToSource(
        VerticalCoordinateSystem sourceCoordinateSystem,
        CoordinateSystem targetCoordinateSystem,
        string methodName,
        string parameterFileName)
    {
        if (targetCoordinateSystem is not CompoundCoordinateSystem hubCoordinateSystem
            || hubCoordinateSystem.HeadCoordinateSystem is not GeographicCoordinateSystem hubHorizontal
            || hubCoordinateSystem.TailCoordinateSystem is not VerticalCoordinateSystem hubVertical)
        {
            throw new NotSupportedException("BOUNDCRS vertical targets must be ellipsoidal 3D geographic CRS definitions.");
        }

        if (!TryGetHorizontalDatum(hubHorizontal, out HorizontalDatum? targetDatum))
        {
            throw new NotSupportedException("BOUNDCRS vertical targets other than WGS 84 are not supported.");
        }

        targetDatum = ArgumentGuard.ThrowIfNull(targetDatum, nameof(targetDatum));
        if (!targetDatum.EqualParams(HorizontalDatum.WGS84))
        {
            throw new NotSupportedException("BOUNDCRS vertical targets other than WGS 84 are not supported.");
        }

        if (hubVertical.VerticalDatum.DatumType != DatumType.VD_Ellipsoidal)
        {
            throw new NotSupportedException("BOUNDCRS vertical targets must expose an ellipsoidal height axis.");
        }

        if (!IsGeographic3DToGravityRelatedHeightMethod(methodName))
        {
            throw new NotSupportedException($"BOUNDCRS abridged transformation method '{methodName}' is not supported.");
        }

        CompoundCoordinateSystem runtimeHubCoordinateSystem = CreateRuntimeCompatibleVerticalBoundHubCoordinateSystem(hubCoordinateSystem);

        if (sourceCoordinateSystem.BoundGridTransformation is not null
            && (!AreEquivalentParameterFileReferences(sourceCoordinateSystem.BoundGridTransformation.ParameterFileName, parameterFileName)
                || !sourceCoordinateSystem.BoundGridTransformation.HubCoordinateSystem.EqualParams(runtimeHubCoordinateSystem)))
        {
            throw new NotSupportedException("BOUNDCRS source CRS already defines a conflicting grid transformation.");
        }

        sourceCoordinateSystem.BoundGridTransformation ??= new VerticalBoundGridTransformation(methodName, parameterFileName, runtimeHubCoordinateSystem);
        return sourceCoordinateSystem;
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
            _ => throw new NotSupportedException(
                $"BOUNDCRS source coordinate system type '{GetCoordinateSystemKeyword(coordinateSystem)}' is not supported."),
        };
    }

    private static GeographicCoordinateSystem CloneGeographicCoordinateSystem(GeographicCoordinateSystem geographicCoordinateSystem)
    {
        HorizontalDatum horizontalDatum = CloneHorizontalDatum(geographicCoordinateSystem.HorizontalDatum);
        GeographicCoordinateSystem clone = CloneGeographicCoordinateSystem(geographicCoordinateSystem, horizontalDatum);
        clone.WGS84ConversionInfo = [];
        foreach (Wgs84ConversionInfo conversionInfo in geographicCoordinateSystem.WGS84ConversionInfo)
        {
            clone.WGS84ConversionInfo.Add(CloneWgs84Parameters(conversionInfo));
        }

        return clone;
    }

    private static GeographicCoordinateSystem CloneGeographicCoordinateSystem(GeographicCoordinateSystem geographicCoordinateSystem, HorizontalDatum horizontalDatum)
    {
        var clone = new GeographicCoordinateSystem(
            CloneAngularUnit(geographicCoordinateSystem.AngularUnit),
            horizontalDatum,
            ClonePrimeMeridian(geographicCoordinateSystem.PrimeMeridian),
            CloneAxisInfo(geographicCoordinateSystem),
            geographicCoordinateSystem.Name,
            geographicCoordinateSystem.Authority,
            geographicCoordinateSystem.AuthorityCode,
            geographicCoordinateSystem.Alias,
            geographicCoordinateSystem.Abbreviation,
            geographicCoordinateSystem.Remarks);
        CopyDefaultEnvelope(geographicCoordinateSystem, clone);
        return clone;
    }

    private static ProjectedCoordinateSystem CloneProjectedCoordinateSystem(ProjectedCoordinateSystem projectedCoordinateSystem)
    {
        HorizontalDatum horizontalDatum = CloneHorizontalDatum(projectedCoordinateSystem.HorizontalDatum);
        GeographicCoordinateSystem geographicCoordinateSystem = CloneGeographicCoordinateSystem(projectedCoordinateSystem.GeographicCoordinateSystem, horizontalDatum);

        var clone = new ProjectedCoordinateSystem(
            horizontalDatum,
            geographicCoordinateSystem,
            CloneLinearUnit(projectedCoordinateSystem.LinearUnit),
            CloneProjection(projectedCoordinateSystem.Projection),
            CloneAxisInfo(projectedCoordinateSystem),
            projectedCoordinateSystem.Name,
            projectedCoordinateSystem.Authority,
            projectedCoordinateSystem.AuthorityCode,
            projectedCoordinateSystem.Alias,
            projectedCoordinateSystem.Remarks,
            projectedCoordinateSystem.Abbreviation);
        CopyDefaultEnvelope(projectedCoordinateSystem, clone);
        return clone;
    }

    private static GeocentricCoordinateSystem CloneGeocentricCoordinateSystem(GeocentricCoordinateSystem geocentricCoordinateSystem)
    {
        var clone = new GeocentricCoordinateSystem(
            CloneHorizontalDatum(geocentricCoordinateSystem.HorizontalDatum),
            CloneLinearUnit(geocentricCoordinateSystem.LinearUnit),
            ClonePrimeMeridian(geocentricCoordinateSystem.PrimeMeridian),
            CloneAxisInfo(geocentricCoordinateSystem),
            geocentricCoordinateSystem.Name,
            geocentricCoordinateSystem.Authority,
            geocentricCoordinateSystem.AuthorityCode,
            geocentricCoordinateSystem.Alias,
            geocentricCoordinateSystem.Remarks,
            geocentricCoordinateSystem.Abbreviation);
        CopyDefaultEnvelope(geocentricCoordinateSystem, clone);
        return clone;
    }

    private static VerticalCoordinateSystem CloneVerticalCoordinateSystem(VerticalCoordinateSystem verticalCoordinateSystem)
    {
        var clone = new VerticalCoordinateSystem(
            CloneLinearUnit(verticalCoordinateSystem.LinearUnit),
            CloneVerticalDatum(verticalCoordinateSystem.VerticalDatum),
            new AxisInfo(verticalCoordinateSystem.GetAxis(0)),
            verticalCoordinateSystem.Name,
            verticalCoordinateSystem.Authority,
            verticalCoordinateSystem.AuthorityCode,
            verticalCoordinateSystem.Alias,
            verticalCoordinateSystem.Abbreviation,
            verticalCoordinateSystem.Remarks);
        CopyDefaultEnvelope(verticalCoordinateSystem, clone);

        if (verticalCoordinateSystem.BoundGridTransformation is not null)
        {
            clone.BoundGridTransformation = new VerticalBoundGridTransformation(
                verticalCoordinateSystem.BoundGridTransformation.MethodName,
                verticalCoordinateSystem.BoundGridTransformation.ParameterFileName,
                CloneCompoundCoordinateSystem(verticalCoordinateSystem.BoundGridTransformation.HubCoordinateSystem));
        }

        return clone;
    }

    private static CompoundCoordinateSystem CloneCompoundCoordinateSystem(CompoundCoordinateSystem compoundCoordinateSystem)
    {
        var clone = new CompoundCoordinateSystem(
            CloneCoordinateSystem(compoundCoordinateSystem.HeadCoordinateSystem),
            CloneCoordinateSystem(compoundCoordinateSystem.TailCoordinateSystem),
            compoundCoordinateSystem.Name,
            compoundCoordinateSystem.Authority,
            compoundCoordinateSystem.AuthorityCode,
            compoundCoordinateSystem.Alias,
            compoundCoordinateSystem.Abbreviation,
            compoundCoordinateSystem.Remarks);
        CopyDefaultEnvelope(compoundCoordinateSystem, clone);
        return clone;
    }

    private static HorizontalDatum CloneHorizontalDatum(HorizontalDatum horizontalDatum)
    {
        return new HorizontalDatum(
            CloneEllipsoid(horizontalDatum.Ellipsoid),
            horizontalDatum.Wgs84Parameters is null ? null : CloneWgs84Parameters(horizontalDatum.Wgs84Parameters),
            horizontalDatum.DatumType,
            horizontalDatum.Name,
            horizontalDatum.Authority,
            horizontalDatum.AuthorityCode,
            horizontalDatum.Alias,
            horizontalDatum.Remarks,
            horizontalDatum.Abbreviation);
    }

    private static VerticalDatum CloneVerticalDatum(VerticalDatum verticalDatum)
    {
        return new VerticalDatum(
            verticalDatum.DatumType,
            verticalDatum.Name,
            verticalDatum.Authority,
            verticalDatum.AuthorityCode,
            verticalDatum.Alias,
            verticalDatum.Remarks,
            verticalDatum.Abbreviation);
    }

    private static Ellipsoid CloneEllipsoid(Ellipsoid ellipsoid)
    {
        return new Ellipsoid(
            ellipsoid.SemiMajorAxis,
            ellipsoid.SemiMinorAxis,
            ellipsoid.InverseFlattening,
            ellipsoid.IsIvfDefinitive,
            CloneLinearUnit(ellipsoid.AxisUnit),
            ellipsoid.Name,
            ellipsoid.Authority,
            ellipsoid.AuthorityCode,
            ellipsoid.Alias,
            ellipsoid.Abbreviation,
            ellipsoid.Remarks);
    }

    private static PrimeMeridian ClonePrimeMeridian(PrimeMeridian primeMeridian)
    {
        return new PrimeMeridian(
            primeMeridian.Longitude,
            CloneAngularUnit(primeMeridian.AngularUnit),
            primeMeridian.Name,
            primeMeridian.Authority,
            primeMeridian.AuthorityCode,
            primeMeridian.Alias,
            primeMeridian.Abbreviation,
            primeMeridian.Remarks);
    }

    private static AngularUnit CloneAngularUnit(AngularUnit angularUnit)
    {
        return new AngularUnit(
            angularUnit.RadiansPerUnit,
            angularUnit.Name,
            angularUnit.Authority,
            angularUnit.AuthorityCode,
            angularUnit.Alias,
            angularUnit.Abbreviation,
            angularUnit.Remarks);
    }

    private static LinearUnit CloneLinearUnit(LinearUnit linearUnit)
    {
        return new LinearUnit(
            linearUnit.MetersPerUnit,
            linearUnit.Name,
            linearUnit.Authority,
            linearUnit.AuthorityCode,
            linearUnit.Alias,
            linearUnit.Abbreviation,
            linearUnit.Remarks);
    }

    private static Projection CloneProjection(IProjection projection)
    {
        var parameters = new List<ProjectionParameter>(projection.NumParameters);
        for (int i = 0; i < projection.NumParameters; i++)
        {
            ProjectionParameter parameter = projection.GetParameter(i);
            parameters.Add(new ProjectionParameter(parameter.Name, parameter.Value));
        }

        return new Projection(
            projection.ClassName,
            parameters,
            projection.Name,
            projection.Authority,
            projection.AuthorityCode,
            projection.Alias,
            projection.Remarks,
            projection.Abbreviation);
    }

    private static List<AxisInfo> CloneAxisInfo(CoordinateSystem coordinateSystem)
    {
        var axisInfo = new List<AxisInfo>(coordinateSystem.Dimension);
        for (int i = 0; i < coordinateSystem.Dimension; i++)
        {
            axisInfo.Add(new AxisInfo(coordinateSystem.GetAxis(i)));
        }

        return axisInfo;
    }

    private static void CopyDefaultEnvelope(CoordinateSystem source, CoordinateSystem target)
    {
        if (source.DefaultEnvelope.Length > 0)
        {
            target.DefaultEnvelope = (double[])source.DefaultEnvelope.Clone();
        }
    }

    private static Wgs84ConversionInfo CloneWgs84Parameters(Wgs84ConversionInfo parameters)
        => new(parameters.Dx, parameters.Dy, parameters.Dz, parameters.Ex, parameters.Ey, parameters.Ez, parameters.Ppm, parameters.AreaOfUse);

    private static bool IsGeocentricTranslationsMethod(string methodName)
        => methodName.StartsWith("Geocentric translations", StringComparison.OrdinalIgnoreCase);

    private static bool IsPositionVectorMethod(string methodName)
        => methodName.StartsWith("Position Vector transformation", StringComparison.OrdinalIgnoreCase);

    private static bool IsGeographic3DToGravityRelatedHeightMethod(string methodName)
        => methodName.Equals("Geographic3D to GravityRelatedHeight (EGM)", StringComparison.OrdinalIgnoreCase);

    private static bool IsCoordinateFrameRotationMethod(string methodName)
        => methodName.StartsWith("Coordinate Frame rotation", StringComparison.OrdinalIgnoreCase);
}
