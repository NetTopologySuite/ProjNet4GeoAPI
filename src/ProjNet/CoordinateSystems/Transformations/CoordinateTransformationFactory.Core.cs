// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProjNet.CoordinateSystems.Projections;
using ProjNet.Data;
using ProjNet.Data.Generated;
using ProjNet.Resources;

/// <summary>
/// Creates coordinate transformations.
/// </summary>
/// <remarks>
/// Core coordinate-system conversion routing and math-transform composition helpers.
/// </remarks>
public partial class CoordinateTransformationFactory
{
    private static CoordinateSystemRuntimeKind GetCoordinateSystemRuntimeKind(CoordinateSystem coordinateSystem)
    {
        if (coordinateSystem is ProjectedCoordinateSystem)
        {
            return CoordinateSystemRuntimeKind.Projected;
        }

        if (coordinateSystem is GeographicCoordinateSystem)
        {
            return CoordinateSystemRuntimeKind.Geographic;
        }

        if (coordinateSystem is GeocentricCoordinateSystem)
        {
            return CoordinateSystemRuntimeKind.Geocentric;
        }

        return coordinateSystem is FittedCoordinateSystem ? CoordinateSystemRuntimeKind.Fitted : CoordinateSystemRuntimeKind.Unknown;
    }

    private ICoordinateTransformation CreateFromCoordinateSystemsCore(CoordinateSystem sourceCS, CoordinateSystem targetCS)
    {
        if (TryCreateSimpleCoordinateSystemConversion(sourceCS, targetCS, out ICoordinateTransformation? simpleConversionCandidate))
        {
            return ArgumentGuard.ThrowIfNull(simpleConversionCandidate, nameof(simpleConversionCandidate));
        }

        CoordinateSystemRuntimeKind sourceKind = GetCoordinateSystemRuntimeKind(sourceCS);
        CoordinateSystemRuntimeKind targetKind = GetCoordinateSystemRuntimeKind(targetCS);

        // Fitted -> Any
        if (sourceKind == CoordinateSystemRuntimeKind.Fitted)
        {
            return Fitt2Any((FittedCoordinateSystem)sourceCS, targetCS);
        }

        // Any -> Fitted
        if (targetKind == CoordinateSystemRuntimeKind.Fitted)
        {
            return Any2Fitt(sourceCS, (FittedCoordinateSystem)targetCS);
        }

        // Encode the fixed source/target runtime-kind pair as XY so the switch can stay dense
        // without needing a larger tuple-based dispatch structure.
        int route = ((int)sourceKind * 10) + (int)targetKind;
        return route switch
        {
            // Projected -> Geographic
            12 => Proj2Geog((ProjectedCoordinateSystem)sourceCS, (GeographicCoordinateSystem)targetCS),

            // Geographic -> Projected
            21 => Geog2Proj((GeographicCoordinateSystem)sourceCS, (ProjectedCoordinateSystem)targetCS),

            // Geographic -> Geocentric
            23 => Geog2Geoc((GeographicCoordinateSystem)sourceCS, (GeocentricCoordinateSystem)targetCS),

            // Geocentric -> Geographic
            32 => Geoc2Geog((GeocentricCoordinateSystem)sourceCS, (GeographicCoordinateSystem)targetCS),

            // Projected -> Projected
            11 => Proj2Proj((ProjectedCoordinateSystem)sourceCS, (ProjectedCoordinateSystem)targetCS),

            // Geocentric -> Geocentric
            33 => CreateGeoc2Geoc((GeocentricCoordinateSystem)sourceCS, (GeocentricCoordinateSystem)targetCS)
                                ?? CreateTransform(
                                    sourceCS,
                                    targetCS,
                                    TransformType.Conversion,
                                    new IdentityMathTransform(Math.Max(sourceCS.Dimension, targetCS.Dimension))),

            // Geographic -> Geographic
            22 => CreateGeog2Geog((GeographicCoordinateSystem)sourceCS, (GeographicCoordinateSystem)targetCS),
            _ => throw new NotSupportedException("No support for transforming between the two specified coordinate systems"),
        };
    }

    private static bool TryCreateSimpleCoordinateSystemConversion(
        CoordinateSystem source,
        CoordinateSystem target,
        [NotNullWhen(true)] out ICoordinateTransformation? transformation)
    {
        transformation = null;

        if (source is null || target is null)
        {
            return false;
        }

        if (source.GetType() != target.GetType())
        {
            return false;
        }

        if (!HaveEquivalentDefinitionsIgnoringAxisAndUnits(source, target))
        {
            return false;
        }

        if (!TryCreateAxisSwapConversionTransform(source, target, out MathTransform? axisSwapTransformCandidate))
        {
            return false;
        }

        if (!TryCreateUnitConversionTransform(source, target, out MathTransform? unitConversionTransformCandidate))
        {
            return false;
        }

        MathTransform axisSwapTransform = ArgumentGuard.ThrowIfNull(axisSwapTransformCandidate, nameof(axisSwapTransformCandidate));
        MathTransform unitConversionTransform = ArgumentGuard.ThrowIfNull(unitConversionTransformCandidate, nameof(unitConversionTransformCandidate));
        var transforms = new List<MathTransform>(2);
        if (!unitConversionTransform.Identity())
        {
            transforms.Add(unitConversionTransform);
        }

        if (!axisSwapTransform.Identity())
        {
            transforms.Add(axisSwapTransform);
        }

        MathTransform mathTransform;
        if (transforms.Count == 0)
        {
            mathTransform = new IdentityMathTransform(Math.Max(source.Dimension, target.Dimension));
        }
        else if (transforms.Count == 1)
        {
            mathTransform = transforms[0];
        }
        else
        {
            mathTransform = new CompositeMathTransform(transforms);
        }

        transformation = CreateTransform(source, target, TransformType.Conversion, mathTransform);
        return true;
    }

    private static bool TryCreateAxisSwapConversionTransform(
        CoordinateSystem source,
        CoordinateSystem target,
        [NotNullWhen(true)] out MathTransform? transform)
    {
        if (AxisOrderHelper.TryCreateAxisSwapTransform(source, target, out transform))
        {
            return true;
        }

        if (HaveSameAxisOrientations(source, target))
        {
            transform = new IdentityMathTransform(Math.Max(source.Dimension, target.Dimension));
            return true;
        }

        transform = null;
        return false;
    }

    private static bool HaveSameAxisOrientations(CoordinateSystem source, CoordinateSystem target)
    {
        if (source.Dimension != target.Dimension)
        {
            return false;
        }

        for (int i = 0; i < source.Dimension; i++)
        {
            if (source.GetAxis(i).Orientation != target.GetAxis(i).Orientation)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryCreateUnitConversionTransform(
        CoordinateSystem source,
        CoordinateSystem target,
        [NotNullWhen(true)] out MathTransform? transform)
    {
        if (source is GeographicCoordinateSystem sourceGeographic && target is GeographicCoordinateSystem targetGeographic)
        {
            return TryCreateUnitConversionTransform(sourceGeographic, targetGeographic, out transform);
        }

        if (source is ProjectedCoordinateSystem sourceProjected && target is ProjectedCoordinateSystem targetProjected)
        {
            return TryCreateUnitConversionTransform(sourceProjected, targetProjected, out transform);
        }

        if (source is GeocentricCoordinateSystem sourceGeocentric && target is GeocentricCoordinateSystem targetGeocentric)
        {
            return TryCreateUnitConversionTransform(sourceGeocentric, targetGeocentric, out transform);
        }

        transform = null;
        return false;
    }

    private static bool TryCreateUnitConversionTransform(
        GeographicCoordinateSystem source,
        GeographicCoordinateSystem target,
        [NotNullWhen(true)] out MathTransform? transform)
    {
        double scale = source.AngularUnit.RadiansPerUnit / target.AngularUnit.RadiansPerUnit;
        transform = new UnitConvertMathTransform(source.Dimension, scale, scale);
        return true;
    }

    private static bool TryCreateUnitConversionTransform(
        ProjectedCoordinateSystem source,
        ProjectedCoordinateSystem target,
        [NotNullWhen(true)] out MathTransform? transform)
    {
        if (source.LinearUnit is null || target.LinearUnit is null)
        {
            transform = null;
            return false;
        }

        double scale = source.LinearUnit.MetersPerUnit / target.LinearUnit.MetersPerUnit;
        transform = new UnitConvertMathTransform(source.Dimension, scale, scale);
        return true;
    }

    private static bool TryCreateUnitConversionTransform(
        GeocentricCoordinateSystem source,
        GeocentricCoordinateSystem target,
        [NotNullWhen(true)] out MathTransform? transform)
    {
        if (source.LinearUnit is null || target.LinearUnit is null)
        {
            transform = null;
            return false;
        }

        double scale = source.LinearUnit.MetersPerUnit / target.LinearUnit.MetersPerUnit;
        transform = new UnitConvertMathTransform(source.Dimension, scale, scale);
        return true;
    }

    private static bool HaveEquivalentDefinitionsIgnoringAxisAndUnits(CoordinateSystem source, CoordinateSystem target)
    {
        if (source is GeographicCoordinateSystem sourceGeographic && target is GeographicCoordinateSystem targetGeographic)
        {
            return HaveEquivalentDefinitionsIgnoringAxisAndUnits(sourceGeographic, targetGeographic);
        }

        if (source is ProjectedCoordinateSystem sourceProjected && target is ProjectedCoordinateSystem targetProjected)
        {
            return HaveEquivalentDefinitionsIgnoringAxisAndUnits(sourceProjected, targetProjected);
        }

        return source is GeocentricCoordinateSystem sourceGeocentric && target is GeocentricCoordinateSystem targetGeocentric && HaveEquivalentDefinitionsIgnoringAxisAndUnits(sourceGeocentric, targetGeocentric);
    }

    private static bool HaveEquivalentDefinitionsIgnoringAxisAndUnits(
        GeographicCoordinateSystem source,
        GeographicCoordinateSystem target)
    {
        return source.Dimension == target.Dimension && source.HorizontalDatum.EqualParams(target.HorizontalDatum)
            && source.PrimeMeridian.EqualParams(target.PrimeMeridian);
    }

    private static bool HaveEquivalentDefinitionsIgnoringAxisAndUnits(
        ProjectedCoordinateSystem source,
        ProjectedCoordinateSystem target)
    {
        if (source.Dimension != target.Dimension)
        {
            return false;
        }

        HorizontalDatum? sourceHorizontalDatum = source.HorizontalDatum;
        HorizontalDatum? targetHorizontalDatum = target.HorizontalDatum;
        if ((sourceHorizontalDatum is null) != (targetHorizontalDatum is null))
        {
            return false;
        }

        bool horizontalDatumsEqual = sourceHorizontalDatum is null
            || sourceHorizontalDatum.EqualParams(ArgumentGuard.ThrowIfNull(targetHorizontalDatum, nameof(targetHorizontalDatum)));

        return horizontalDatumsEqual
            && source.Projection.EqualParams(target.Projection)
            && HaveEquivalentDefinitionsIgnoringAxisAndUnits(source.GeographicCoordinateSystem, target.GeographicCoordinateSystem);
    }

    private static bool HaveEquivalentDefinitionsIgnoringAxisAndUnits(
        GeocentricCoordinateSystem source,
        GeocentricCoordinateSystem target)
    {
        return source.Dimension == target.Dimension && source.HorizontalDatum.EqualParams(target.HorizontalDatum)
            && source.PrimeMeridian.EqualParams(target.PrimeMeridian);
    }

    private static void SimplifyTrans(ConcatenatedTransform mtrans, ref List<ICoordinateTransformationCore> mts)
    {
        foreach (ICoordinateTransformationCore t in mtrans.CoordinateTransformationList)
        {
            if (t is ConcatenatedTransform ct)
            {
                SimplifyTrans(ct, ref mts);
            }
            else
            {
                mts.Add(t);
            }
        }
    }

    private static CoordinateTransformation Geog2Geoc(GeographicCoordinateSystem source, GeocentricCoordinateSystem target)
    {
        GeocentricTransform geocMathTransform = CreateCoordinateOperation(target);
        if (source.PrimeMeridian.EqualParams(target.PrimeMeridian))
        {
            return CreateTransform(source, target, TransformType.Conversion, geocMathTransform);
        }

        var ct = new ConcatenatedTransform();
        ct.CoordinateTransformationList.Add(CreateTransform(source, target, TransformType.Transformation, new PrimeMeridianTransform(source.PrimeMeridian, target.PrimeMeridian)));
        ct.CoordinateTransformationList.Add(CreateTransform(source, target, TransformType.Conversion, geocMathTransform));
        return CreateTransform(source, target, TransformType.Conversion, ct);
    }

    private static CoordinateTransformation Geoc2Geog(GeocentricCoordinateSystem source, GeographicCoordinateSystem target)
    {
        MathTransform geocMathTransform = CreateCoordinateOperation(source).Inverse();
        if (source.PrimeMeridian.EqualParams(target.PrimeMeridian))
        {
            return CreateTransform(source, target, TransformType.Conversion, geocMathTransform);
        }

        var ct = new ConcatenatedTransform();
        ct.CoordinateTransformationList.Add(CreateTransform(source, target, TransformType.Conversion, geocMathTransform));
        ct.CoordinateTransformationList.Add(CreateTransform(source, target, TransformType.Transformation, new PrimeMeridianTransform(source.PrimeMeridian, target.PrimeMeridian)));
        return CreateTransform(source, target, TransformType.Conversion, ct);
    }

    private static CoordinateTransformation Proj2Proj(ProjectedCoordinateSystem source, ProjectedCoordinateSystem target)
    {
        if (source.GeographicCoordinateSystem.EqualParams(target.GeographicCoordinateSystem))
        {
            return CreateDirectProjectedTransform(source, target);
        }

        var ct = new ConcatenatedTransform();
        var ctFac = new CoordinateTransformationFactory();

        // First transform from projection to geographic
        ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source, source.GeographicCoordinateSystem));

        // Transform geographic to geographic:
        ICoordinateTransformation? geogToGeog = ctFac.CreateFromCoordinateSystems(
            source.GeographicCoordinateSystem,
            target.GeographicCoordinateSystem);
        if (geogToGeog is not null)
        {
            ct.CoordinateTransformationList.Add(geogToGeog);
        }

        // Transform to new projection
        ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(target.GeographicCoordinateSystem, target));

        return CreateTransform(source, target, TransformType.Transformation, ct);
    }

    private static CoordinateTransformation CreateDirectProjectedTransform(ProjectedCoordinateSystem source, ProjectedCoordinateSystem target)
    {
        MathTransform sourceInverseProjection = CreateCoordinateOperation(
            source.Projection,
            source.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid,
            source.LinearUnit).Inverse();

        MathTransform targetForwardProjection = CreateCoordinateOperation(
            target.Projection,
            target.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid,
            target.LinearUnit);

        var directMathTransform = new ConcatenatedTransform();
        directMathTransform.CoordinateTransformationList.Add(
            CreateTransform(source, target, TransformType.Conversion, sourceInverseProjection));
        directMathTransform.CoordinateTransformationList.Add(
            CreateTransform(source, target, TransformType.Conversion, targetForwardProjection));

        return CreateTransform(source, target, TransformType.Transformation, directMathTransform);
    }

    private static CoordinateTransformation Geog2Proj(GeographicCoordinateSystem source, ProjectedCoordinateSystem target)
    {
        if (source.EqualParams(target.GeographicCoordinateSystem))
        {
            MathTransform mathTransform = CreateCoordinateOperation(
                target.Projection,
                target.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid,
                target.LinearUnit);
            return CreateTransform(source, target, TransformType.Transformation, mathTransform);
        }

        // Geographic coordinatesystems differ - Create concatenated transform
        var ct = new ConcatenatedTransform();
        var ctFac = new CoordinateTransformationFactory();
        ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source, target.GeographicCoordinateSystem));
        ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(target.GeographicCoordinateSystem, target));
        return CreateTransform(source, target, TransformType.Transformation, ct);
    }

    private static CoordinateTransformation Proj2Geog(ProjectedCoordinateSystem source, GeographicCoordinateSystem target)
    {
        if (source.GeographicCoordinateSystem.EqualParams(target))
        {
            MathTransform mathTransform = CreateCoordinateOperation(
                source.Projection,
                source.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid,
                source.LinearUnit).Inverse();
            return CreateTransform(source, target, TransformType.Transformation, mathTransform);
        }
        else
        {
            // Geographic coordinate systems differ - create concatenated transform
            var ct = new ConcatenatedTransform();
            var ctFac = new CoordinateTransformationFactory();
            ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source, source.GeographicCoordinateSystem));
            ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source.GeographicCoordinateSystem, target));
            return CreateTransform(source, target, TransformType.Transformation, ct);
        }
    }

    /// <summary>
    /// Geographic to geographic transformation.
    /// </summary>
    /// <remarks>Adds a datum shift if necessary.</remarks>
    /// <param name="source">The source parameter.</param>
    /// <param name="target">The target parameter.</param>
    /// <returns>The transformation result.</returns>
    private static CoordinateTransformation CreateGeog2Geog(GeographicCoordinateSystem source, GeographicCoordinateSystem target)
    {
        if (source.HorizontalDatum.EqualParams(target.HorizontalDatum))
        {
            // No datum shift needed
            return CreateTransform(source, target, TransformType.Conversion, new GeographicTransform(source, target));
        }

        // Create datum shift
        // Convert to geocentric, perform shift and return to geographic
        var ctFac = new CoordinateTransformationFactory();
        var cFac = new CoordinateSystemFactory();
        GeocentricCoordinateSystem sourceCentric = cFac.CreateGeocentricCoordinateSystem(
            $"{source.HorizontalDatum.Name} Geocentric",
            source.HorizontalDatum,
            LinearUnit.Metre,
            source.PrimeMeridian);

        // Keep the intermediate geocentric pair on the source prime meridian; the surrounding
        // geographic legs handle prime-meridian normalization before and after the datum shift.
        GeocentricCoordinateSystem targetCentric = cFac.CreateGeocentricCoordinateSystem(
            $"{target.HorizontalDatum.Name} Geocentric",
            target.HorizontalDatum,
            LinearUnit.Metre,
            source.PrimeMeridian);
        var ct = new ConcatenatedTransform();
        AddIfNotNull(ct, ctFac.CreateFromCoordinateSystems(source, sourceCentric));
        AddIfNotNull(ct, ctFac.CreateFromCoordinateSystems(sourceCentric, targetCentric));
        AddIfNotNull(ct, ctFac.CreateFromCoordinateSystems(targetCentric, target));

        return CreateTransform(source, target, TransformType.Transformation, ct);
    }

    private static void AddIfNotNull(ConcatenatedTransform concatTrans, ICoordinateTransformation trans)
    {
        if (trans is not null)
        {
            concatTrans.CoordinateTransformationList.Add(trans);
        }
    }

    /// <summary>
    /// Geocentric to Geocentric transformation.
    /// </summary>
    /// <param name="source">The source parameter.</param>
    /// <param name="target">The target parameter.</param>
    /// <returns>The transformation result.</returns>
    private static CoordinateTransformation? CreateGeoc2Geoc(GeocentricCoordinateSystem source, GeocentricCoordinateSystem target)
    {
        var ct = new ConcatenatedTransform();

        // Does source has a datum different from WGS84 and is there a shift specified?
        if (source.HorizontalDatum.Wgs84Parameters is not null && !source.HorizontalDatum.Wgs84Parameters.HasZeroValuesOnly)
        {
            ct.CoordinateTransformationList.Add(
                CreateTransform(
                    (target.HorizontalDatum.Wgs84Parameters is null || target.HorizontalDatum.Wgs84Parameters.HasZeroValuesOnly) ? target : GeocentricCoordinateSystem.WGS84,
                    source,
                    TransformType.Transformation,
                    new DatumTransform(source.HorizontalDatum.Wgs84Parameters)));
        }

        // Does target has a datum different from WGS84 and is there a shift specified?
        if (target.HorizontalDatum.Wgs84Parameters is not null && !target.HorizontalDatum.Wgs84Parameters.HasZeroValuesOnly)
        {
            ct.CoordinateTransformationList.Add(
                CreateTransform(
                    (source.HorizontalDatum.Wgs84Parameters is null || source.HorizontalDatum.Wgs84Parameters.HasZeroValuesOnly) ? source : GeocentricCoordinateSystem.WGS84,
                    target,
                    TransformType.Transformation,
                    new DatumTransform(target.HorizontalDatum.Wgs84Parameters).Inverse()));
        }

        // If we don't have a transformation in this list, return null
        if (ct.CoordinateTransformationList.Count == 0)
        {
            return null;
        }

        // If we only have one shift, lets just return the datumshift from/to wgs84
        return ct.CoordinateTransformationList.Count == 1
            ? CreateTransform(
                source,
                target,
                TransformType.ConversionAndTransformation,
                ((ICoordinateTransformation)ct.CoordinateTransformationList[0]).MathTransform)
            : CreateTransform(source, target, TransformType.ConversionAndTransformation, ct);
    }

    /// <summary>
    /// Creates transformation from fitted coordinate system to the target one.
    /// </summary>
    /// <param name="source">The source parameter.</param>
    /// <param name="target">The target parameter.</param>
    /// <returns>The transformation result.</returns>
    private static CoordinateTransformation Fitt2Any(FittedCoordinateSystem source, CoordinateSystem target)
    {
        // transform from fitted to base system of fitted (which is equal to target)
        MathTransform mt = CreateFittedTransform(source);

        // case when target system is equal to base system of the fitted
        if (source.BaseCoordinateSystem.EqualParams(target))
        {
            // Transform form base system of fitted to target coordinate system
            return CreateTransform(source, target, TransformType.Transformation, mt);
        }

        // Transform form base system of fitted to target coordinate system
        var ct = new ConcatenatedTransform();
        ct.CoordinateTransformationList.Add(CreateTransform(source, source.BaseCoordinateSystem, TransformType.Transformation, mt));

        // Transform form base system of fitted to target coordinate system
        var ctFac = new CoordinateTransformationFactory();
        ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source.BaseCoordinateSystem, target));

        return CreateTransform(source, target, TransformType.Transformation, ct);
    }

    /// <summary>
    /// Creates transformation from source coordinate system to specified target system which is the fitted one.
    /// </summary>
    /// <param name="source">The source parameter.</param>
    /// <param name="target">The target parameter.</param>
    /// <returns>The transformation result.</returns>
    private static CoordinateTransformation Any2Fitt(CoordinateSystem source, FittedCoordinateSystem target)
    {
        // Transform form base system of fitted to target coordinate system - use invered math transform
        MathTransform invMt = CreateFittedTransform(target).Inverse();

        // case when source system is equal to base system of the fitted
        if (target.BaseCoordinateSystem.EqualParams(source))
        {
            // Transform form base system of fitted to target coordinate system
            return CreateTransform(source, target, TransformType.Transformation, invMt);
        }

        var ct = new ConcatenatedTransform();

        // First transform from source to base system of fitted
        var ctFac = new CoordinateTransformationFactory();
        ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source, target.BaseCoordinateSystem));

        // Transform form base system of fitted to target coordinate system - use invered math transform
        ct.CoordinateTransformationList.Add(CreateTransform(target.BaseCoordinateSystem, target, TransformType.Transformation, invMt));

        return CreateTransform(source, target, TransformType.Transformation, ct);
    }

    private static MathTransform CreateFittedTransform(FittedCoordinateSystem fittedSystem)
    {
        // create transform From fitted to base and inverts it
        return fittedSystem.ToBaseTransform;
    }

    /// <summary>
    /// Creates an instance of CoordinateTransformation as an anonymous transformation without neither autohority nor code defined.
    /// </summary>
    /// <param name="sourceCS">Source coordinate system.</param>
    /// <param name="targetCS">Target coordinate system.</param>
    /// <param name="transformType">Transformation type.</param>
    /// <param name="mathTransform">Math transform.</param>
    private static CoordinateTransformation CreateTransform(CoordinateSystem sourceCS, CoordinateSystem targetCS, TransformType transformType, MathTransform mathTransform)
    {
        return new CoordinateTransformation(sourceCS, targetCS, transformType, mathTransform, string.Empty, string.Empty, -1, string.Empty, string.Empty);
    }

    private static GeocentricTransform CreateCoordinateOperation(GeocentricCoordinateSystem geo)
    {
        var parameterList = new List<ProjectionParameter>(2);

        Ellipsoid ellipsoid = geo.HorizontalDatum.Ellipsoid;

        if (parameterList.Find((p) => p.Name.ToLowerInvariant().Replace(' ', '_').Equals("semi_major", StringComparison.Ordinal)) is null)
        {
            parameterList.Add(new ProjectionParameter("semi_major", ellipsoid.SemiMajorAxis));
        }

        if (parameterList.Find((p) => p.Name.ToLowerInvariant().Replace(' ', '_').Equals("semi_minor", StringComparison.Ordinal)) is null)
        {
            parameterList.Add(new ProjectionParameter("semi_minor", ellipsoid.SemiMinorAxis));
        }

        return new GeocentricTransform(parameterList);
    }

    private static MathTransform CreateCoordinateOperation(IProjection projection, Ellipsoid ellipsoid, LinearUnit unit)
    {
        var parameterList = new List<ProjectionParameter>(projection.NumParameters);
        for (int i = 0; i < projection.NumParameters; i++)
        {
            parameterList.Add(projection.GetParameter(i));
        }

        if (parameterList.Find((p) => p.Name.ToLowerInvariant().Replace(' ', '_').Equals("semi_major", StringComparison.Ordinal)) is null)
        {
            parameterList.Add(new ProjectionParameter("semi_major", ellipsoid.SemiMajorAxis));
        }

        if (parameterList.Find((p) => p.Name.ToLowerInvariant().Replace(' ', '_').Equals("semi_minor", StringComparison.Ordinal)) is null)
        {
            parameterList.Add(new ProjectionParameter("semi_minor", ellipsoid.SemiMinorAxis));
        }

        if (parameterList.Find((p) => p.Name.ToLowerInvariant().Replace(' ', '_').Equals("unit", StringComparison.Ordinal)) is null)
        {
            parameterList.Add(new ProjectionParameter("unit", unit.MetersPerUnit));
        }

        return ProjectionsRegistry.CreateProjection(projection.ClassName, parameterList);
    }
}
