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
/// Explicit-operation, metadata, and grid-resolution helpers.
/// </remarks>
public partial class CoordinateTransformationFactory
{
    private static bool TryCreateExplicitOperationTransformation(
        CoordinateSystem source,
        CoordinateSystem target,
        CoordinateOperationDefinition operation,
        string? resolvedGridPath,
        [NotNullWhen(true)] out ICoordinateTransformation? transformation)
        => TryCreateExplicitOperationTransformation(source, target, operation, resolvedGridPath, null, out transformation);

    private static bool TryCreateExplicitOperationTransformation(
        CoordinateSystem source,
        CoordinateSystem target,
        CoordinateOperationDefinition operation,
        string? resolvedGridPath,
        HashSet<int>? visitedOperationCodes,
        [NotNullWhen(true)] out ICoordinateTransformation? transformation)
    {
        transformation = null;

        if (source is GeographicCoordinateSystem sourceGeographic && target is GeographicCoordinateSystem targetGeographic)
        {
            if (!TryCreateExplicitGeographicTransformation(sourceGeographic, targetGeographic, operation, visitedOperationCodes, out CoordinateTransformation? geographicTransformation))
            {
                return false;
            }

            transformation = CreateMetadataBackedTransformation(source, target, geographicTransformation, operation, resolvedGridPath);
            return true;
        }

        if (source is ProjectedCoordinateSystem sourceProjected && target is ProjectedCoordinateSystem targetProjected)
        {
            if (!TryCreateExplicitProjectedTransformation(sourceProjected, targetProjected, operation, visitedOperationCodes, out CoordinateTransformation? projectedTransformation))
            {
                return false;
            }

            transformation = CreateMetadataBackedTransformation(source, target, projectedTransformation, operation, resolvedGridPath);
            return true;
        }

        if (source is GeocentricCoordinateSystem sourceGeocentric && target is GeocentricCoordinateSystem targetGeocentric)
        {
            if (!TryCreateExplicitGeocentricTransformation(sourceGeocentric, targetGeocentric, operation, visitedOperationCodes, out CoordinateTransformation? geocentricTransformation))
            {
                return false;
            }

            transformation = CreateMetadataBackedTransformation(source, target, geocentricTransformation, operation, resolvedGridPath);
            return true;
        }

        if (operation.OperationKind == CoordinateOperationKind.ConcatenatedOperation
            && TryCreateConcatenatedOperationTransformation(source, target, operation, visitedOperationCodes, out CoordinateTransformation? concatenatedTransformation))
        {
            transformation = CreateMetadataBackedTransformation(source, target, concatenatedTransformation, operation, resolvedGridPath);
            return true;
        }

        return false;
    }

    private static bool TryCreateDirectProjectedTransformation(
        CoordinateSystem source,
        CoordinateSystem target,
        CoordinateOperationDefinition operation,
        string? resolvedGridPath,
        [NotNullWhen(true)] out ICoordinateTransformation? transformation)
    {
        transformation = null;

        if (source is not ProjectedCoordinateSystem sourceProjected || target is not ProjectedCoordinateSystem targetProjected)
        {
            return false;
        }

        CoordinateTransformation fallback = CreateDirectProjectedTransform(sourceProjected, targetProjected);
        transformation = CreateMetadataBackedTransformation(source, target, fallback, operation, resolvedGridPath);
        return true;
    }

    private static bool TryCreateExplicitGeographicTransformation(
        GeographicCoordinateSystem source,
        GeographicCoordinateSystem target,
        CoordinateOperationDefinition operation,
        [NotNullWhen(true)] out CoordinateTransformation? transformation)
        => TryCreateExplicitGeographicTransformation(source, target, operation, null, out transformation);

    private static bool TryCreateExplicitGeographicTransformation(
        GeographicCoordinateSystem source,
        GeographicCoordinateSystem target,
        CoordinateOperationDefinition operation,
        HashSet<int>? visitedOperationCodes,
        [NotNullWhen(true)] out CoordinateTransformation? transformation)
    {
        transformation = null;

        if (operation.OperationKind == CoordinateOperationKind.ConcatenatedOperation)
        {
            return TryCreateConcatenatedOperationTransformation(source, target, operation, visitedOperationCodes, out transformation);
        }

        if (operation.OperationKind != CoordinateOperationKind.Transformation)
        {
            return false;
        }

        if (TryCreateDirectGeographicMathTransform(operation, out MathTransform? directMathTransform))
        {
            transformation = CreateTransform(source, target, TransformType.Transformation, directMathTransform);
            return true;
        }

        MathTransform? geocentricMathTransform = TryCreateExplicitGeocentricMathTransform(operation, out MathTransform? explicitGeocentricMathTransform)
            ? explicitGeocentricMathTransform
            : TryCreateBursaWolfParameters(operation, out Wgs84ConversionInfo? helmert)
                ? new DatumTransform(helmert)
                : null;
        if (geocentricMathTransform is null)
        {
            return false;
        }

        var ct = new ConcatenatedTransform();
        var csFactory = new CoordinateSystemFactory();

        GeocentricCoordinateSystem sourceCentric = csFactory.CreateGeocentricCoordinateSystem(
            $"{source.HorizontalDatum.Name} Geocentric",
            source.HorizontalDatum,
            LinearUnit.Metre,
            source.PrimeMeridian);

        GeocentricCoordinateSystem targetCentric = csFactory.CreateGeocentricCoordinateSystem(
            $"{target.HorizontalDatum.Name} Geocentric",
            target.HorizontalDatum,
            LinearUnit.Metre,
            target.PrimeMeridian);

        AddIfNotNull(ct, Geog2Geoc(source, sourceCentric));
        AddIfNotNull(ct, CreateTransform(sourceCentric, targetCentric, TransformType.Transformation, geocentricMathTransform));
        AddIfNotNull(ct, Geoc2Geog(targetCentric, target));

        transformation = CreateTransform(source, target, TransformType.Transformation, ct);
        return true;
    }

    private static bool TryCreateExplicitProjectedTransformation(
        ProjectedCoordinateSystem source,
        ProjectedCoordinateSystem target,
        CoordinateOperationDefinition operation,
        [NotNullWhen(true)] out CoordinateTransformation? transformation)
        => TryCreateExplicitProjectedTransformation(source, target, operation, null, out transformation);

    private static bool TryCreateExplicitProjectedTransformation(
        ProjectedCoordinateSystem source,
        ProjectedCoordinateSystem target,
        CoordinateOperationDefinition operation,
        HashSet<int>? visitedOperationCodes,
        [NotNullWhen(true)] out CoordinateTransformation? transformation)
    {
        transformation = null;

        if (operation.OperationKind == CoordinateOperationKind.ConcatenatedOperation
            && TryGetEpsgCode(source, out int sourceSrid)
            && TryGetEpsgCode(target, out int targetSrid)
            && sourceSrid == operation.SourceSrid
            && targetSrid == operation.TargetSrid)
        {
            return TryCreateConcatenatedOperationTransformation(source, target, operation, visitedOperationCodes, out transformation);
        }

        if (!TryCreateExplicitGeographicTransformation(
                source.GeographicCoordinateSystem,
                target.GeographicCoordinateSystem,
                operation,
                visitedOperationCodes,
                out CoordinateTransformation? geographicTransformation))
        {
            return false;
        }

        var ct = new ConcatenatedTransform();
        AddIfNotNull(ct, Proj2Geog(source, source.GeographicCoordinateSystem));
        AddIfNotNull(ct, geographicTransformation);
        AddIfNotNull(ct, Geog2Proj(target.GeographicCoordinateSystem, target));

        transformation = CreateTransform(source, target, TransformType.Transformation, ct);
        return true;
    }

    private static bool TryCreateExplicitGeocentricTransformation(
        GeocentricCoordinateSystem source,
        GeocentricCoordinateSystem target,
        CoordinateOperationDefinition operation,
        [NotNullWhen(true)] out CoordinateTransformation? transformation)
        => TryCreateExplicitGeocentricTransformation(source, target, operation, null, out transformation);

    private static bool TryCreateExplicitGeocentricTransformation(
        GeocentricCoordinateSystem source,
        GeocentricCoordinateSystem target,
        CoordinateOperationDefinition operation,
        HashSet<int>? visitedOperationCodes,
        [NotNullWhen(true)] out CoordinateTransformation? transformation)
    {
        transformation = null;

        if (operation.OperationKind == CoordinateOperationKind.ConcatenatedOperation)
        {
            return TryCreateConcatenatedOperationTransformation(source, target, operation, visitedOperationCodes, out transformation);
        }

        if (operation.OperationKind != CoordinateOperationKind.Transformation)
        {
            return false;
        }

        MathTransform? mathTransform = TryCreateExplicitGeocentricMathTransform(operation, out MathTransform? explicitMathTransform)
            ? explicitMathTransform
            : TryCreateBursaWolfParameters(operation, out Wgs84ConversionInfo? helmert)
                ? new DatumTransform(helmert)
                : null;
        if (mathTransform is null)
        {
            return false;
        }

        transformation = CreateTransform(source, target, TransformType.Transformation, mathTransform);
        return true;
    }

    private static bool TryCreateConcatenatedOperationTransformation(
        CoordinateSystem source,
        CoordinateSystem target,
        CoordinateOperationDefinition operation,
        HashSet<int>? visitedOperationCodes,
        [NotNullWhen(true)] out CoordinateTransformation? transformation)
    {
        transformation = null;

        if (operation.OperationKind != CoordinateOperationKind.ConcatenatedOperation)
        {
            return false;
        }

        visitedOperationCodes ??= [];
        if (!visitedOperationCodes.Add(operation.OperationCode))
        {
            return false;
        }

        try
        {
            if (!EpsgGeneratedOperationsCatalog.TryGetConcatenatedOperationStepCount(operation.OperationCode, out int stepCount)
                || stepCount <= 0
                || !TryResolveCatalogCoordinateSystem(operation.SourceSrid, out CoordinateSystem? currentCoordinateSystem))
            {
                return false;
            }

            int currentSrid = operation.SourceSrid;
            var concatenatedTransform = new ConcatenatedTransform();
            for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
            {
                if (!EpsgGeneratedOperationsCatalog.TryGetConcatenatedOperationStep(operation.OperationCode, stepIndex, out int stepOperationCode)
                    || !TryGetDirectOperationDefinition(stepOperationCode, out CoordinateOperationDefinition? stepOperation)
                    || !TryResolveCatalogCoordinateSystem(stepOperation.SourceSrid, out CoordinateSystem? stepSource)
                    || !TryResolveCatalogCoordinateSystem(stepOperation.TargetSrid, out CoordinateSystem? stepTarget)
                    || !TryCreateOperationTransformationFromDefinition(stepSource, stepTarget, stepOperation, visitedOperationCodes, out ICoordinateTransformation? stepTransformation))
                {
                    return false;
                }

                bool useForwardDirection;
                CoordinateSystem nextCoordinateSystem;
                if (stepOperation.SourceSrid == currentSrid)
                {
                    useForwardDirection = true;
                    nextCoordinateSystem = stepTarget;
                }
                else if (stepOperation.TargetSrid == currentSrid)
                {
                    useForwardDirection = false;
                    nextCoordinateSystem = stepSource;
                }
                else
                {
                    return false;
                }

                if (!TryOrientOperationTransformation(
                        currentCoordinateSystem,
                        nextCoordinateSystem,
                        stepTransformation,
                        useForwardDirection,
                        out ICoordinateTransformation? orientedStep))
                {
                    return false;
                }

                AddIfNotNull(concatenatedTransform, orientedStep);
                currentCoordinateSystem = nextCoordinateSystem;
                currentSrid = TryGetEpsgCode(currentCoordinateSystem, out int nextSrid) ? nextSrid : 0;
            }

            if (concatenatedTransform.CoordinateTransformationList.Count != stepCount
                || currentSrid != operation.TargetSrid)
            {
                return false;
            }

            transformation = CreateTransform(source, target, TransformType.Transformation, concatenatedTransform);
            return true;
        }
        finally
        {
            visitedOperationCodes.Remove(operation.OperationCode);
        }
    }

    private static bool TryOrientOperationTransformation(
        CoordinateSystem source,
        CoordinateSystem target,
        ICoordinateTransformation transformation,
        bool useForwardDirection,
        [NotNullWhen(true)] out ICoordinateTransformation? orientedTransformation)
    {
        orientedTransformation = null;

        if (useForwardDirection)
        {
            orientedTransformation = transformation;
            return true;
        }

        if (!transformation.MathTransform.IsInvertible)
        {
            return false;
        }

        orientedTransformation = new CoordinateTransformation(
            source,
            target,
            transformation.TransformType,
            transformation.MathTransform.Inverse(),
            transformation.Name,
            transformation.Authority,
            transformation.AuthorityCode,
            transformation.AreaOfUse,
            transformation.Remarks);
        return true;
    }

    private static bool TryCreateOperationTransformationFromDefinition(
        CoordinateSystem source,
        CoordinateSystem target,
        CoordinateOperationDefinition operation,
        HashSet<int> visitedOperationCodes,
        [NotNullWhen(true)] out ICoordinateTransformation? transformation)
    {
        transformation = null;

        if (!TryResolveExactOperationGridPath(operation, out string? resolvedGridPath))
        {
            return false;
        }

        if (TryCreateExplicitOperationTransformation(source, target, operation, resolvedGridPath, visitedOperationCodes, out transformation))
        {
            return true;
        }

        if (TryCreateDirectProjectedTransformation(source, target, operation, resolvedGridPath, out transformation))
        {
            return true;
        }

        if (TryCreateVerticalBoundCompoundTransformation(source, target, out ICoordinateTransformation? verticalBoundTransformation))
        {
            transformation = CreateMetadataBackedTransformation(source, target, verticalBoundTransformation, operation, resolvedGridPath);
            return true;
        }

        if (!CanUseOperationCoreFallback(source, target))
        {
            return false;
        }

        var factory = new CoordinateTransformationFactory();
        ICoordinateTransformation fallback = factory.CreateFromCoordinateSystemsCore(source, target);
        transformation = CreateMetadataBackedTransformation(source, target, fallback, operation, resolvedGridPath);
        return true;
    }

    private static bool TryResolveCatalogCoordinateSystem(int srid, [NotNullWhen(true)] out CoordinateSystem? coordinateSystem)
    {
        coordinateSystem = null;
        return srid > 0 && EpsgCoordinateSystemFactory.TryResolveCoordinateSystem(srid, out coordinateSystem);
    }

    private static bool TryResolveExactOperationGridPath(CoordinateOperationDefinition operation, out string? resolvedGridPath)
    {
        resolvedGridPath = null;

        if (string.IsNullOrWhiteSpace(operation.ParameterFileName))
        {
            return true;
        }

        if (GetGridResolver().TryResolve(operation.ParameterFileName, out resolvedGridPath))
        {
            return true;
        }

        return IsGridRequiredModeEnabled()
            ? throw new InvalidOperationException($"DataUnavailable: Required grid resource '{operation.ParameterFileName}' was not found.")
            : false;
    }

    private static bool CanUseOperationCoreFallback(CoordinateSystem source, CoordinateSystem target)
    {
        CoordinateSystemRuntimeKind sourceKind = GetCoordinateSystemRuntimeKind(source);
        CoordinateSystemRuntimeKind targetKind = GetCoordinateSystemRuntimeKind(target);
        if (sourceKind == CoordinateSystemRuntimeKind.Unknown || targetKind == CoordinateSystemRuntimeKind.Unknown)
        {
            return false;
        }

        if (sourceKind == CoordinateSystemRuntimeKind.Fitted || targetKind == CoordinateSystemRuntimeKind.Fitted)
        {
            return true;
        }

        int route = ((int)sourceKind * 10) + (int)targetKind;
        return route is 11 or 12 or 21 or 22 or 23 or 32 or 33;
    }

    private static bool TryCreateDirectGeographicMathTransform(
        CoordinateOperationDefinition operation,
        [NotNullWhen(true)] out MathTransform? transform)
    {
        transform = null;

        string normalizedMethodName = NormalizeOperationMethodName(operation.MethodName);
        if (!IsGeographicOffsetMethod(normalizedMethodName)
            || !TryGetDirectOperationParameters(operation, out IReadOnlyDictionary<string, double>? parameters))
        {
            return false;
        }

        double longitudeOffset = GetOperationParameterOrDefault(parameters, "Longitude offset");
        double latitudeOffset = GetOperationParameterOrDefault(parameters, "Latitude offset");
        transform = GeogOffsetMathTransform.Create(longitudeOffset, latitudeOffset, 0d);
        return true;
    }

    private static bool TryCreateExplicitGeocentricMathTransform(
        CoordinateOperationDefinition operation,
        [NotNullWhen(true)] out MathTransform? transform)
    {
        transform = null;

        string normalizedMethodName = NormalizeOperationMethodName(operation.MethodName);
        return IsTimeDependentHelmertMethod(normalizedMethodName)
            ? TryCreateTimeDependentHelmertMathTransform(operation, normalizedMethodName, out transform)
            : IsMolodenskyBadekasMethod(normalizedMethodName)
                && TryCreateMolodenskyBadekasMathTransform(operation, normalizedMethodName, out transform);
    }

    private static bool TryCreateTimeDependentHelmertMathTransform(
        CoordinateOperationDefinition operation,
        string normalizedMethodName,
        [NotNullWhen(true)] out MathTransform? transform)
    {
        transform = null;

        if (!TryGetDirectOperationParameters(operation, out IReadOnlyDictionary<string, double>? parameters))
        {
            return false;
        }

        bool isPositionVector = IsPositionVectorMethod(normalizedMethodName);
        bool isCoordinateFrame = IsCoordinateFrameMethod(normalizedMethodName);
        if (!isPositionVector && !isCoordinateFrame)
        {
            return false;
        }

        double translationX = GetOperationParameterOrDefault(parameters, "X-axis translation");
        double translationY = GetOperationParameterOrDefault(parameters, "Y-axis translation");
        double translationZ = GetOperationParameterOrDefault(parameters, "Z-axis translation");
        double rotationX = GetOperationParameterOrDefault(parameters, "X-axis rotation") * TransformationMath.ArcSecondToRadians;
        double rotationY = GetOperationParameterOrDefault(parameters, "Y-axis rotation") * TransformationMath.ArcSecondToRadians;
        double rotationZ = GetOperationParameterOrDefault(parameters, "Z-axis rotation") * TransformationMath.ArcSecondToRadians;
        double scale = GetOperationParameterOrDefault(parameters, "Scale difference");
        double translationRateX = GetOperationParameterOrDefault(parameters, "Rate of change of X-axis translation");
        double translationRateY = GetOperationParameterOrDefault(parameters, "Rate of change of Y-axis translation");
        double translationRateZ = GetOperationParameterOrDefault(parameters, "Rate of change of Z-axis translation");
        double rotationRateX = GetOperationParameterOrDefault(parameters, "Rate of change of X-axis rotation") * TransformationMath.ArcSecondToRadians;
        double rotationRateY = GetOperationParameterOrDefault(parameters, "Rate of change of Y-axis rotation") * TransformationMath.ArcSecondToRadians;
        double rotationRateZ = GetOperationParameterOrDefault(parameters, "Rate of change of Z-axis rotation") * TransformationMath.ArcSecondToRadians;
        double scaleRate = GetOperationParameterOrDefault(parameters, "Rate of change of scale difference");
        double epochReference = GetOperationParameterOrDefault(parameters, "Parameter reference epoch");

        if (scale <= TransformationMath.MinValidPpmScale)
        {
            return false;
        }

        bool hasKinematicRates = translationRateX != 0d
            || translationRateY != 0d
            || translationRateZ != 0d
            || rotationRateX != 0d
            || rotationRateY != 0d
            || rotationRateZ != 0d
            || scaleRate != 0d;
        bool noRotation = rotationX == 0d
            && rotationY == 0d
            && rotationZ == 0d
            && rotationRateX == 0d
            && rotationRateY == 0d
            && rotationRateZ == 0d;

        transform = HelmertMathTransform.Create(
            translationX,
            translationY,
            translationZ,
            rotationX,
            rotationY,
            rotationZ,
            scale,
            0d,
            translationRateX,
            translationRateY,
            translationRateZ,
            rotationRateX,
            rotationRateY,
            rotationRateZ,
            scaleRate,
            0d,
            hasKinematicRates,
            epochReference,
            false,
            noRotation,
            false,
            isPositionVector);
        return true;
    }

    private static bool TryCreateMolodenskyBadekasMathTransform(
        CoordinateOperationDefinition operation,
        string normalizedMethodName,
        [NotNullWhen(true)] out MathTransform? transform)
    {
        transform = null;

        if (!TryGetDirectOperationParameters(operation, out IReadOnlyDictionary<string, double>? parameters))
        {
            return false;
        }

        bool isPositionVector = ContainsOrdinal(normalizedMethodName, "badekaspv");
        bool isCoordinateFrame = ContainsOrdinal(normalizedMethodName, "badekascf");
        if (!isPositionVector && !isCoordinateFrame)
        {
            return false;
        }

        if (!TryGetRequiredOperationParameter(parameters, "Ordinate 1 of evaluation point", out double pivotX)
            || !TryGetRequiredOperationParameter(parameters, "Ordinate 2 of evaluation point", out double pivotY)
            || !TryGetRequiredOperationParameter(parameters, "Ordinate 3 of evaluation point", out double pivotZ))
        {
            return false;
        }

        double translationX = GetOperationParameterOrDefault(parameters, "X-axis translation");
        double translationY = GetOperationParameterOrDefault(parameters, "Y-axis translation");
        double translationZ = GetOperationParameterOrDefault(parameters, "Z-axis translation");
        double rotationX = GetOperationParameterOrDefault(parameters, "X-axis rotation");
        double rotationY = GetOperationParameterOrDefault(parameters, "Y-axis rotation");
        double rotationZ = GetOperationParameterOrDefault(parameters, "Z-axis rotation");
        double scale = GetOperationParameterOrDefault(parameters, "Scale difference");

        if (scale <= TransformationMath.MinValidPpmScale)
        {
            return false;
        }

        transform = MolobadekasMathTransform.Create(
            translationX,
            translationY,
            translationZ,
            rotationX,
            rotationY,
            rotationZ,
            scale,
            pivotX,
            pivotY,
            pivotZ,
            isPositionVector);
        return true;
    }

    private static bool TryCreateVerticalBoundCompoundTransformation(
        CoordinateSystem source,
        CoordinateSystem target,
        [NotNullWhen(true)] out ICoordinateTransformation? transformation)
    {
        transformation = null;

        if (source is CompoundCoordinateSystem sourceCompound
            && target is CompoundCoordinateSystem targetCompound
            && sourceCompound.TailCoordinateSystem is VerticalCoordinateSystem sourceVertical
            && sourceVertical.BoundGridTransformation is VerticalBoundGridTransformation sourceBinding
            && sourceCompound.HeadCoordinateSystem.EqualParams(sourceBinding.HubCoordinateSystem.HeadCoordinateSystem)
            && targetCompound.EqualParams(sourceBinding.HubCoordinateSystem))
        {
            if (!TryCreateVerticalBoundGridMathTransform(sourceBinding.ParameterFileName, out MathTransform? gridMathTransform))
            {
                return false;
            }

            transformation = CreateTransform(source, target, TransformType.Transformation, gridMathTransform.Inverse());
            return true;
        }

        if (source is CompoundCoordinateSystem sourceHubCompound
            && target is CompoundCoordinateSystem targetBoundCompound
            && targetBoundCompound.TailCoordinateSystem is VerticalCoordinateSystem targetVertical
            && targetVertical.BoundGridTransformation is VerticalBoundGridTransformation targetBinding
            && targetBoundCompound.HeadCoordinateSystem.EqualParams(targetBinding.HubCoordinateSystem.HeadCoordinateSystem)
            && sourceHubCompound.EqualParams(targetBinding.HubCoordinateSystem))
        {
            if (!TryCreateVerticalBoundGridMathTransform(targetBinding.ParameterFileName, out MathTransform? gridMathTransform))
            {
                return false;
            }

            transformation = CreateTransform(source, target, TransformType.Transformation, gridMathTransform);
            return true;
        }

        return false;
    }

    private static bool TryCreateVerticalBoundGridMathTransform(string parameterFileName, [NotNullWhen(true)] out MathTransform? transform)
    {
        transform = null;

        if (string.IsNullOrWhiteSpace(parameterFileName))
        {
            return false;
        }

        if (!GetGridResolver().TryResolve(parameterFileName, out string? resolvedGridPath))
        {
            if (IsGridRequiredModeEnabled())
            {
                throw new InvalidOperationException($"DataUnavailable: Required grid resource '{parameterFileName}' was not found.");
            }

            return false;
        }

        string gridPath = ArgumentGuard.ThrowIfNull(resolvedGridPath, nameof(resolvedGridPath));
        string[] gridPaths = [gridPath];
        string extension = Path.GetExtension(gridPath);
        transform = extension.Equals(".tif", StringComparison.OrdinalIgnoreCase) || extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase)
            ? new GeoTiffVGridShiftMathTransform(gridPaths, -1d)
            : new GtxVGridShiftMathTransform(gridPaths);
        return true;
    }

    private static bool TryCreateBursaWolfParameters(CoordinateOperationDefinition operation, [NotNullWhen(true)] out Wgs84ConversionInfo? parameters)
    {
        parameters = null;

        if (operation is null)
        {
            return false;
        }

        if (!EpsgGeneratedOperationsCatalog.TryGetExplicitOperationParameters(operation.OperationCode, out EpsgExplicitOperationRecord operationParameters))
        {
            return false;
        }

        parameters = new Wgs84ConversionInfo(
            operationParameters.Dx,
            operationParameters.Dy,
            operationParameters.Dz,
            operationParameters.Ex,
            operationParameters.Ey,
            operationParameters.Ez,
            operationParameters.Ppm);
        return true;
    }

    private static CoordinateTransformation CreateMetadataBackedTransformation(
        CoordinateSystem source,
        CoordinateSystem target,
        ICoordinateTransformation fallback,
        CoordinateOperationDefinition operation,
        string? resolvedGridPath)
    {
        string operationName = !string.IsNullOrWhiteSpace(operation.MethodName)
            ? operation.MethodName
            : fallback.Name;

        string remarks = fallback.Remarks;
        if (!string.IsNullOrWhiteSpace(operation.ParameterFileName))
        {
            string gridReference = !string.IsNullOrWhiteSpace(resolvedGridPath)
                ? $"{operation.ParameterFileName} ({resolvedGridPath})"
                : operation.ParameterFileName;
            remarks = string.IsNullOrWhiteSpace(remarks)
                ? $"Grid: {gridReference}"
                : $"{remarks}; Grid: {gridReference}";
        }

        return new CoordinateTransformation(
            source,
            target,
            fallback.TransformType,
            fallback.MathTransform,
            operationName,
            "EPSG",
            operation.OperationCode,
            fallback.AreaOfUse,
            remarks);
    }

    private static CoordinateTransformation RebindTransformation(
        CoordinateSystem source,
        CoordinateSystem target,
        ICoordinateTransformation transformation)
    {
        return new CoordinateTransformation(
            source,
            target,
            transformation.TransformType,
            transformation.MathTransform,
            transformation.Name,
            transformation.Authority,
            transformation.AuthorityCode,
            transformation.AreaOfUse,
            transformation.Remarks);
    }

    private static Dictionary<SridPair, IReadOnlyList<CoordinateOperationDefinition>> LoadDirectOperationDefinitions()
    {
        var provider = new ManagedCoordinateOperationDefinitionProvider();
        var definitions = new Dictionary<SridPair, List<CoordinateOperationDefinition>>();

        foreach (CoordinateOperationDefinition definition in provider.GetDefinitions())
        {
            if (definition.SourceSrid <= 0 || definition.TargetSrid <= 0)
            {
                continue;
            }

            if (definition.SourceSrid == definition.TargetSrid)
            {
                continue;
            }

            if (definition.OperationKind == CoordinateOperationKind.PointMotionOperation)
            {
                continue;
            }

            var key = new SridPair(definition.SourceSrid, definition.TargetSrid);
            if (!definitions.TryGetValue(key, out List<CoordinateOperationDefinition>? operations))
            {
                operations = [];
                definitions[key] = operations;
            }

            operations.Add(definition);
        }

        var result = new Dictionary<SridPair, IReadOnlyList<CoordinateOperationDefinition>>(definitions.Count);
        foreach (KeyValuePair<SridPair, List<CoordinateOperationDefinition>> pair in definitions)
        {
            pair.Value.Sort(OperationDefinitionComparer.Instance);
            result[pair.Key] = pair.Value;
        }

        return result;
    }

    private static Dictionary<int, CoordinateOperationDefinition> LoadDirectOperationDefinitionsByCode()
    {
        var provider = new ManagedCoordinateOperationDefinitionProvider();
        var result = new Dictionary<int, CoordinateOperationDefinition>();

        foreach (CoordinateOperationDefinition definition in provider.GetDefinitions())
        {
            if (definition.OperationCode <= 0 || definition.SourceSrid <= 0 || definition.TargetSrid <= 0)
            {
                continue;
            }

            result[definition.OperationCode] = definition;
        }

        return result;
    }

    private static Dictionary<int, IReadOnlyDictionary<string, double>> LoadDirectOperationParameters()
    {
        var parametersByOperation = new Dictionary<int, Dictionary<string, double>>();

        foreach (EpsgOperationParameterRecord parameter in EpsgGeneratedOperationsCatalog.OperationParameters)
        {
            if (!parametersByOperation.TryGetValue(parameter.OperationCode, out Dictionary<string, double>? parameters))
            {
                parameters = new Dictionary<string, double>(StringComparer.Ordinal);
                parametersByOperation[parameter.OperationCode] = parameters;
            }

            parameters[parameter.Name] = parameter.Value;
        }

        var result = new Dictionary<int, IReadOnlyDictionary<string, double>>(parametersByOperation.Count);
        foreach (KeyValuePair<int, Dictionary<string, double>> pair in parametersByOperation)
        {
            result[pair.Key] = pair.Value;
        }

        return result;
    }

    private static bool TryGetDirectOperationParameters(
        CoordinateOperationDefinition operation,
        [NotNullWhen(true)] out IReadOnlyDictionary<string, double>? parameters)
    {
        parameters = null;
        return operation is not null
            && DirectOperationParameters.Value.TryGetValue(operation.OperationCode, out parameters);
    }

    private static bool TryGetDirectOperationDefinition(
        int operationCode,
        [NotNullWhen(true)] out CoordinateOperationDefinition? operation)
    {
        operation = null;
        return operationCode > 0 && DirectOperationDefinitionsByCode.Value.TryGetValue(operationCode, out operation);
    }

    private static bool TryGetRequiredOperationParameter(
        IReadOnlyDictionary<string, double> parameters,
        string name,
        out double value)
    {
        value = 0d;
        return parameters is not null && parameters.TryGetValue(name, out value);
    }

    private static double GetOperationParameterOrDefault(
        IReadOnlyDictionary<string, double> parameters,
        string name,
        double defaultValue = 0d)
    {
        return parameters is not null && parameters.TryGetValue(name, out double value)
            ? value
            : defaultValue;
    }

    private static string NormalizeOperationMethodName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        char[] buffer = new char[value.Length];
        int length = 0;
        for (int i = 0; i < value.Length; i++)
        {
            char character = value[i];
            if (char.IsLetterOrDigit(character))
            {
                buffer[length] = char.ToLowerInvariant(character);
                length++;
            }
        }

        return length == 0 ? string.Empty : new string(buffer, 0, length);
    }

    private static bool IsCoordinateFrameMethod(string normalizedMethodName)
    {
        return ContainsOrdinal(normalizedMethodName, "coordinateframe");
    }

    private static bool IsGeographicOffsetMethod(string normalizedMethodName)
    {
        return ContainsOrdinal(normalizedMethodName, "geographic2doffsets");
    }

    private static bool IsMolodenskyBadekasMethod(string normalizedMethodName)
    {
        return ContainsOrdinal(normalizedMethodName, "molodenskybadekas");
    }

    private static bool IsPositionVectorMethod(string normalizedMethodName)
    {
        return ContainsOrdinal(normalizedMethodName, "positionvector");
    }

    private static bool IsTimeDependentHelmertMethod(string normalizedMethodName)
    {
        return ContainsOrdinal(normalizedMethodName, "timedependent")
            && (IsPositionVectorMethod(normalizedMethodName) || IsCoordinateFrameMethod(normalizedMethodName));
    }

    private static bool ContainsOrdinal(string value, string substring)
    {
#if NETSTANDARD2_0
        return value.IndexOf(substring, StringComparison.Ordinal) >= 0;
#else
        return value.Contains(substring, StringComparison.Ordinal);
#endif
    }

    private static GridResourceResolver GetGridResolver()
    {
        lock (GridResolverSync)
        {
            return gridResolverInstance;
        }
    }

    private static GridResourceResolver CreateGridResolver()
    {
        string[] localDirectories = ReadGridDirectoriesFromEnvironment();
        string? cacheDirectory = Environment.GetEnvironmentVariable(GridCacheEnvironmentVariable);
        GridResourceResolutionMode mode = ParseGridResolutionMode(Environment.GetEnvironmentVariable(GridModeEnvironmentVariable));
        IGridResourceFetchClient? fetchClient = CreateGridFetchClientFromEnvironment(mode);

        var options = new GridResourceResolverOptions(localDirectories, cacheDirectory, mode);
        return new GridResourceResolver(options, fetchClient);
    }

    private static string[] ReadGridDirectoriesFromEnvironment()
    {
        string? configuredPaths = Environment.GetEnvironmentVariable(GridPathEnvironmentVariable);
        return string.IsNullOrWhiteSpace(configuredPaths)
            ? []
            : configuredPaths.Split([';', Path.PathSeparator], StringSplitOptions.RemoveEmptyEntries);
    }

    private static GridResourceResolutionMode ParseGridResolutionMode(string? configuredMode)
    {
        return "LocalThenNetwork".Equals(configuredMode, StringComparison.OrdinalIgnoreCase)
            || "network".Equals(configuredMode, StringComparison.OrdinalIgnoreCase)
            ? GridResourceResolutionMode.LocalThenNetwork
            : GridResourceResolutionMode.LocalOnly;
    }

    private static HttpGridResourceFetchClient? CreateGridFetchClientFromEnvironment(GridResourceResolutionMode mode)
    {
        if (mode != GridResourceResolutionMode.LocalThenNetwork)
        {
            return null;
        }

        string? baseUrl = Environment.GetEnvironmentVariable(GridBaseUrlEnvironmentVariable);
        return string.IsNullOrWhiteSpace(baseUrl)
            ? null
            : new HttpGridResourceFetchClient(baseUrl);
    }

    private static double NormalizeAccuracy(double accuracy) => accuracy > 0d ? accuracy : double.MaxValue;

    private static bool TryGetDirectProjectedOperation(
        CoordinateSystem source,
        CoordinateSystem target,
        [NotNullWhen(true)] out CoordinateOperationDefinition? operation,
        out string? resolvedGridPath)
    {
        operation = null;
        resolvedGridPath = null;

        if (source is not ProjectedCoordinateSystem || target is not ProjectedCoordinateSystem)
        {
            return false;
        }

        return TryGetEpsgCode(source, out int sourceSrid) && TryGetEpsgCode(target, out int targetSrid) && TryGetDirectOperationBySridPair(sourceSrid, targetSrid, out operation, out resolvedGridPath);
    }

    private static bool TryGetDirectOperation(
        CoordinateSystem source,
        CoordinateSystem target,
        [NotNullWhen(true)] out CoordinateOperationDefinition? operation,
        out string? resolvedGridPath)
    {
        operation = null;
        resolvedGridPath = null;

        return TryGetEpsgCode(source, out int sourceSrid) && TryGetEpsgCode(target, out int targetSrid) && TryGetDirectOperationBySridPair(sourceSrid, targetSrid, out operation, out resolvedGridPath);
    }

    private static bool TryGetDirectOperationBySridPair(
        int sourceSrid,
        int targetSrid,
        [NotNullWhen(true)] out CoordinateOperationDefinition? operation,
        out string? resolvedGridPath)
    {
        operation = null;
        resolvedGridPath = null;

        if (!DirectOperationDefinitions.Value.TryGetValue(new SridPair(sourceSrid, targetSrid), out IReadOnlyList<CoordinateOperationDefinition>? operations))
        {
            return false;
        }

        string? missingGridFile = null;
        foreach (CoordinateOperationDefinition candidate in operations)
        {
            if (string.IsNullOrWhiteSpace(candidate.ParameterFileName))
            {
                operation = candidate;
                return true;
            }

            if (GetGridResolver().TryResolve(candidate.ParameterFileName, out resolvedGridPath))
            {
                operation = candidate;
                return true;
            }

            missingGridFile ??= candidate.ParameterFileName;
        }

        if (!string.IsNullOrWhiteSpace(missingGridFile))
        {
            return IsGridRequiredModeEnabled()
                ? throw new InvalidOperationException($"DataUnavailable: Required grid resource '{missingGridFile}' was not found.")
                : false;
        }

        return false;
    }

    private static bool TryCreateExplicitOperationTransformationBySridPair(
        CoordinateSystem source,
        CoordinateSystem target,
        int sourceSrid,
        int targetSrid,
        [NotNullWhen(true)] out ICoordinateTransformation? transformation)
    {
        transformation = null;

        if (!DirectOperationDefinitions.Value.TryGetValue(new SridPair(sourceSrid, targetSrid), out IReadOnlyList<CoordinateOperationDefinition>? operations))
        {
            return false;
        }

        string? missingGridFile = null;
        foreach (CoordinateOperationDefinition candidate in operations)
        {
            string? resolvedGridPath = null;
            if (!string.IsNullOrWhiteSpace(candidate.ParameterFileName))
            {
                if (!GetGridResolver().TryResolve(candidate.ParameterFileName, out resolvedGridPath))
                {
                    missingGridFile ??= candidate.ParameterFileName;
                    continue;
                }
            }

            if (TryCreateExplicitOperationTransformation(source, target, candidate, resolvedGridPath, out transformation))
            {
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(missingGridFile))
        {
            return IsGridRequiredModeEnabled()
                ? throw new InvalidOperationException($"DataUnavailable: Required grid resource '{missingGridFile}' was not found.")
                : false;
        }

        return false;
    }

    private static bool IsGridRequiredModeEnabled()
    {
        string? configuredValue = Environment.GetEnvironmentVariable(GridRequiredEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return false;
        }

        if ("1".Equals(configuredValue, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return "true".Equals(configuredValue, StringComparison.OrdinalIgnoreCase) || "yes".Equals(configuredValue, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetEpsgCode(CoordinateSystem coordinateSystem, out int srid)
    {
        srid = 0;

        if (coordinateSystem is null)
        {
            return false;
        }

        if (!"EPSG".Equals(coordinateSystem.Authority, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (coordinateSystem.AuthorityCode <= 0 || coordinateSystem.AuthorityCode > int.MaxValue)
        {
            return false;
        }

        srid = (int)coordinateSystem.AuthorityCode;
        return true;
    }

    private sealed class OperationDefinitionComparer : IComparer<CoordinateOperationDefinition>
    {
        internal static readonly OperationDefinitionComparer Instance = new();

        public int Compare(CoordinateOperationDefinition? left, CoordinateOperationDefinition? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is null)
            {
                return 1;
            }

            if (right is null)
            {
                return -1;
            }

            int accuracyComparison = NormalizeAccuracy(left.Accuracy).CompareTo(NormalizeAccuracy(right.Accuracy));
            if (accuracyComparison != 0)
            {
                return accuracyComparison;
            }

            bool leftRequiresGrid = !string.IsNullOrWhiteSpace(left.ParameterFileName);
            bool rightRequiresGrid = !string.IsNullOrWhiteSpace(right.ParameterFileName);
            if (leftRequiresGrid != rightRequiresGrid)
            {
                return leftRequiresGrid ? 1 : -1;
            }

            bool leftHasMethod = !string.IsNullOrWhiteSpace(left.MethodName);
            bool rightHasMethod = !string.IsNullOrWhiteSpace(right.MethodName);
            if (leftHasMethod != rightHasMethod)
            {
                return leftHasMethod ? -1 : 1;
            }

            int areaComparison = left.GetApproximateAreaOfUseCoverage().CompareTo(right.GetApproximateAreaOfUseCoverage());
            return areaComparison != 0 ? areaComparison : left.OperationCode.CompareTo(right.OperationCode);
        }
    }
}
