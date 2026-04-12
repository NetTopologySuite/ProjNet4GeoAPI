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
/// <para>
/// Thread safety: Instances are stateless and may be reused across threads. Shared direct-operation
/// caches initialize once through <see cref="Lazy{T}"/>. Grid resolution uses a process-wide resolver
/// protected by an internal lock, so grid-backed resolution and resolver reconfiguration may briefly
/// serialize on that shared state.
/// </para>
/// </remarks>
public class CoordinateTransformationFactory
{
    private const string GridCacheEnvironmentVariable = "PROJNET_GRID_CACHE";
    private const string GridBaseUrlEnvironmentVariable = "PROJNET_GRID_BASE_URL";
    private const string GridModeEnvironmentVariable = "PROJNET_GRID_MODE";
    private const string GridPathEnvironmentVariable = "PROJNET_GRID_PATHS";
    private const string GridRequiredEnvironmentVariable = "PROJNET_GRID_REQUIRED";
    private static readonly Lazy<Dictionary<SridPair, IReadOnlyList<CoordinateOperationDefinition>>> DirectOperationDefinitions =
        new(LoadDirectOperationDefinitions, true);

    private static readonly Lazy<Dictionary<int, IReadOnlyDictionary<string, double>>> DirectOperationParameters =
        new(LoadDirectOperationParameters, true);

    private static readonly object GridResolverSync = new();
    private static GridResourceResolver gridResolverInstance = CreateGridResolver();

    private enum CoordinateSystemRuntimeKind : byte
    {
        Unknown = 0,
        Projected = 1,
        Geographic = 2,
        Geocentric = 3,
        Fitted = 4,
    }

    /// <summary>
    /// Creates a transformation between two coordinate systems.
    /// </summary>
    /// <remarks>
    /// This method will examine the coordinate systems in order to construct
    /// a transformation between them. This method may fail if no path between
    /// the coordinate systems is found, using the normal failing behavior of
    /// the DCP (e.g. throwing an exception).</remarks>
    /// <param name="sourceCS">Source coordinate system.</param>
    /// <param name="targetCS">Target coordinate system.</param>
    /// <returns>The coordinate transformation from <paramref name="sourceCS"/> to <paramref name="targetCS"/>.</returns>
    public ICoordinateTransformation CreateFromCoordinateSystems(CoordinateSystem sourceCS, CoordinateSystem targetCS)
    {
        return CoordinateOperationResolver.Resolve(sourceCS, targetCS, this.CreateFromCoordinateSystemsWithMetadata)
            ?? throw new NotSupportedException("No support for transforming between the two specified coordinate systems");
    }

    /// <summary>
    /// Configures the grid resource resolution subsystem with a custom fetch client and search paths.
    /// </summary>
    /// <remarks>
    /// Calling this method replaces the current grid resolver instance. When no parameters are supplied,
    /// a default resolver is created using environment-variable configuration. This method is thread-safe.
    /// </remarks>
    /// <param name="fetchClient">
    /// Custom fetch client used for network retrieval; <see langword="null"/> disables network fetching.
    /// </param>
    /// <param name="localDirectories">
    /// Directories to search for grid files; <see langword="null"/> falls back to directories configured
    /// via the <c>PROJNET_GRID_PATHS</c> environment variable.
    /// </param>
    /// <param name="cacheDirectory">
    /// Directory used to store network-fetched grid files; <see langword="null"/> falls back to the
    /// <c>PROJNET_GRID_CACHE</c> environment variable.
    /// </param>
    /// <param name="mode">Resolution mode controlling whether network retrieval is attempted.</param>
    public static void ConfigureGridResolution(
        IGridResourceFetchClient? fetchClient = null,
        IEnumerable<string>? localDirectories = null,
        string? cacheDirectory = null,
        GridResourceResolutionMode mode = GridResourceResolutionMode.LocalOnly)
    {
        if (fetchClient is null
            && localDirectories is null
            && cacheDirectory is null
            && mode == GridResourceResolutionMode.LocalOnly)
        {
            lock (GridResolverSync)
            {
                gridResolverInstance = CreateGridResolver();
            }

            return;
        }

        string[] dirs = localDirectories?.ToArray() ?? ReadGridDirectoriesFromEnvironment();
        string? cache = cacheDirectory ?? Environment.GetEnvironmentVariable(GridCacheEnvironmentVariable);

        var options = new GridResourceResolverOptions(dirs, cache, mode);
        var resolver = new GridResourceResolver(options, fetchClient);

        lock (GridResolverSync)
        {
            gridResolverInstance = resolver;
        }
    }

    /// <summary>
    /// Attempts to create a projection pipeline math transform from a PROJ-style operation string.
    /// </summary>
    /// <param name="operation">Operation string in pipeline syntax.</param>
    /// <param name="transform">Created math transform when successful.</param>
    /// <param name="skipReason">Reason why the transform could not be created.</param>
    /// <returns><see langword="true"/> when the transform was created; otherwise <see langword="false"/>.</returns>
    internal static bool TryCreateProjPipelineMathTransform(string operation, [NotNullWhen(true)] out MathTransform? transform, out string? skipReason) => ProjPipelineMathTransformFactory.TryCreateMathTransform(operation, out transform, out skipReason);

    /// <summary>
    /// Attempts to resolve a grid resource name to a concrete file path.
    /// </summary>
    /// <param name="gridName">Grid resource name or path token.</param>
    /// <param name="resolvedPath">Resolved local file path when available.</param>
    /// <returns><see langword="true"/> when resolution succeeded; otherwise <see langword="false"/>.</returns>
    internal static bool TryResolveGridResourcePath(string gridName, [NotNullWhen(true)] out string? resolvedPath) => GetGridResolver().TryResolve(gridName, out resolvedPath);

    /// <summary>
    /// Asynchronously attempts to resolve a grid resource name to a concrete file path.
    /// </summary>
    /// <param name="gridName">Grid resource name or path token.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The resolved local file path when available; otherwise <see langword="null"/>.</returns>
    internal static Task<string?> TryResolveGridResourcePathAsync(string gridName, CancellationToken cancellationToken = default) => GetGridResolver().TryResolveAsync(gridName, cancellationToken);

    private ICoordinateTransformation? CreateFromCoordinateSystemsWithMetadata(CoordinateSystem sourceCS, CoordinateSystem targetCS)
    {
        if (BoundCoordinateSystemSupport.ContainsBoundCoordinateSystem(sourceCS)
            || BoundCoordinateSystemSupport.ContainsBoundCoordinateSystem(targetCS))
        {
            CoordinateSystem normalizedSource = BoundCoordinateSystemSupport.NormalizeCoordinateSystemForRuntime(sourceCS);
            CoordinateSystem normalizedTarget = BoundCoordinateSystemSupport.NormalizeCoordinateSystemForRuntime(targetCS);
            ICoordinateTransformation? normalizedTransformation = this.CreateFromCoordinateSystemsWithMetadata(normalizedSource, normalizedTarget);
            return normalizedTransformation is null
                ? null
                : RebindTransformation(sourceCS, targetCS, normalizedTransformation);
        }

        if (TryCreateVerticalBoundCompoundTransformation(sourceCS, targetCS, out ICoordinateTransformation? verticalBoundTransformation))
        {
            return verticalBoundTransformation;
        }

        if (TryGetDirectProjectedOperation(sourceCS, targetCS, out CoordinateOperationDefinition? operation, out string? resolvedGridPath))
        {
            if (TryCreateExplicitOperationTransformation(sourceCS, targetCS, operation, resolvedGridPath, out ICoordinateTransformation? directExplicitTransformation))
            {
                return directExplicitTransformation;
            }

            if (TryCreateDirectProjectedTransformation(sourceCS, targetCS, operation, resolvedGridPath, out ICoordinateTransformation? directTransformation))
            {
                return directTransformation;
            }

            ICoordinateTransformation? fallbackWithMetadata = this.CreateFromCoordinateSystemsCore(sourceCS, targetCS);
            return fallbackWithMetadata is null
                ? null
                : (ICoordinateTransformation)CreateMetadataBackedTransformation(sourceCS, targetCS, fallbackWithMetadata, operation, resolvedGridPath);
        }

        if (TryGetEpsgCode(sourceCS, out int sourceSrid)
            && TryGetEpsgCode(targetCS, out int targetSrid)
            && TryCreateExplicitOperationTransformationBySridPair(sourceCS, targetCS, sourceSrid, targetSrid, out ICoordinateTransformation? directMetadataTransformation))
        {
            return directMetadataTransformation;
        }

        if (sourceCS is ProjectedCoordinateSystem sourceProjected
            && targetCS is ProjectedCoordinateSystem targetProjected
            && TryGetEpsgCode(sourceProjected.GeographicCoordinateSystem, out sourceSrid)
            && TryGetEpsgCode(targetProjected.GeographicCoordinateSystem, out targetSrid)
            && TryCreateExplicitOperationTransformationBySridPair(sourceCS, targetCS, sourceSrid, targetSrid, out ICoordinateTransformation? baseMetadataTransformation))
        {
            return baseMetadataTransformation;
        }

        return this.CreateFromCoordinateSystemsCore(sourceCS, targetCS);
    }

    private static bool TryCreateExplicitOperationTransformation(
        CoordinateSystem source,
        CoordinateSystem target,
        CoordinateOperationDefinition operation,
        string? resolvedGridPath,
        [NotNullWhen(true)] out ICoordinateTransformation? transformation)
    {
        transformation = null;

        if (source is GeographicCoordinateSystem sourceGeographic && target is GeographicCoordinateSystem targetGeographic)
        {
            if (!TryCreateExplicitGeographicTransformation(sourceGeographic, targetGeographic, operation, out CoordinateTransformation? geographicTransformation))
            {
                return false;
            }

            transformation = CreateMetadataBackedTransformation(source, target, geographicTransformation, operation, resolvedGridPath);
            return true;
        }

        if (source is ProjectedCoordinateSystem sourceProjected && target is ProjectedCoordinateSystem targetProjected)
        {
            if (!TryCreateExplicitProjectedTransformation(sourceProjected, targetProjected, operation, out CoordinateTransformation? projectedTransformation))
            {
                return false;
            }

            transformation = CreateMetadataBackedTransformation(source, target, projectedTransformation, operation, resolvedGridPath);
            return true;
        }

        if (source is GeocentricCoordinateSystem sourceGeocentric && target is GeocentricCoordinateSystem targetGeocentric)
        {
            if (!TryCreateExplicitGeocentricTransformation(sourceGeocentric, targetGeocentric, operation, out CoordinateTransformation? geocentricTransformation))
            {
                return false;
            }

            transformation = CreateMetadataBackedTransformation(source, target, geocentricTransformation, operation, resolvedGridPath);
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
    {
        transformation = null;

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
    {
        transformation = null;

        if (!TryCreateExplicitGeographicTransformation(
                source.GeographicCoordinateSystem,
                target.GeographicCoordinateSystem,
                operation,
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
    {
        transformation = null;

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

        if (!EpsgGeneratedCatalog.TryGetExplicitOperationParameters(operation.OperationCode, out EpsgExplicitOperationRecord operationParameters))
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

        transformation = new CoordinateTransformation(
            source,
            target,
            TransformType.Conversion,
            mathTransform,
            string.Empty,
            string.Empty,
            -1,
            string.Empty,
            string.Empty);
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
            return new CoordinateTransformation(source, target, TransformType.Conversion, geocMathTransform, string.Empty, string.Empty, -1, string.Empty, string.Empty);
        }

        var ct = new ConcatenatedTransform();
        ct.CoordinateTransformationList.Add(new CoordinateTransformation(source, target, TransformType.Transformation, new PrimeMeridianTransform(source.PrimeMeridian, target.PrimeMeridian), string.Empty, string.Empty, -1, string.Empty, string.Empty));
        ct.CoordinateTransformationList.Add(new CoordinateTransformation(source, target, TransformType.Conversion, geocMathTransform, string.Empty, string.Empty, -1, string.Empty, string.Empty));
        return new CoordinateTransformation(source, target, TransformType.Conversion, ct, string.Empty, string.Empty, -1, string.Empty, string.Empty);
    }

    private static CoordinateTransformation Geoc2Geog(GeocentricCoordinateSystem source, GeographicCoordinateSystem target)
    {
        MathTransform geocMathTransform = CreateCoordinateOperation(source).Inverse();
        if (source.PrimeMeridian.EqualParams(target.PrimeMeridian))
        {
            return new CoordinateTransformation(source, target, TransformType.Conversion, geocMathTransform, string.Empty, string.Empty, -1, string.Empty, string.Empty);
        }

        var ct = new ConcatenatedTransform();
        ct.CoordinateTransformationList.Add(new CoordinateTransformation(source, target, TransformType.Conversion, geocMathTransform, string.Empty, string.Empty, -1, string.Empty, string.Empty));
        ct.CoordinateTransformationList.Add(new CoordinateTransformation(source, target, TransformType.Transformation, new PrimeMeridianTransform(source.PrimeMeridian, target.PrimeMeridian), string.Empty, string.Empty, -1, string.Empty, string.Empty));
        return new CoordinateTransformation(source, target, TransformType.Conversion, ct, string.Empty, string.Empty, -1, string.Empty, string.Empty);
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

        return new CoordinateTransformation(
            source,
            target,
            TransformType.Transformation,
            ct,
            string.Empty,
            string.Empty,
            -1,
            string.Empty,
            string.Empty);
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
            new CoordinateTransformation(
                source,
                target,
                TransformType.Conversion,
                sourceInverseProjection,
                string.Empty,
                string.Empty,
                -1,
                string.Empty,
                string.Empty));
        directMathTransform.CoordinateTransformationList.Add(
            new CoordinateTransformation(
                source,
                target,
                TransformType.Conversion,
                targetForwardProjection,
                string.Empty,
                string.Empty,
                -1,
                string.Empty,
                string.Empty));

        return new CoordinateTransformation(
            source,
            target,
            TransformType.Transformation,
            directMathTransform,
            string.Empty,
            string.Empty,
            -1,
            string.Empty,
            string.Empty);
    }

    private static CoordinateTransformation Geog2Proj(GeographicCoordinateSystem source, ProjectedCoordinateSystem target)
    {
        if (source.EqualParams(target.GeographicCoordinateSystem))
        {
            MathTransform mathTransform = CreateCoordinateOperation(
                target.Projection,
                target.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid,
                target.LinearUnit);
            return new CoordinateTransformation(
                source,
                target,
                TransformType.Transformation,
                mathTransform,
                string.Empty,
                string.Empty,
                -1,
                string.Empty,
                string.Empty);
        }

        // Geographic coordinatesystems differ - Create concatenated transform
        var ct = new ConcatenatedTransform();
        var ctFac = new CoordinateTransformationFactory();
        ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source, target.GeographicCoordinateSystem));
        ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(target.GeographicCoordinateSystem, target));
        return new CoordinateTransformation(
            source,
            target,
            TransformType.Transformation,
            ct,
            string.Empty,
            string.Empty,
            -1,
            string.Empty,
            string.Empty);
    }

    private static CoordinateTransformation Proj2Geog(ProjectedCoordinateSystem source, GeographicCoordinateSystem target)
    {
        if (source.GeographicCoordinateSystem.EqualParams(target))
        {
            MathTransform mathTransform = CreateCoordinateOperation(
                source.Projection,
                source.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid,
                source.LinearUnit).Inverse();
            return new CoordinateTransformation(
                source,
                target,
                TransformType.Transformation,
                mathTransform,
                string.Empty,
                string.Empty,
                -1,
                string.Empty,
                string.Empty);
        }
        else
        {
            // Geographic coordinate systems differ - create concatenated transform
            var ct = new ConcatenatedTransform();
            var ctFac = new CoordinateTransformationFactory();
            ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source, source.GeographicCoordinateSystem));
            ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source.GeographicCoordinateSystem, target));
            return new CoordinateTransformation(
                source,
                target,
                TransformType.Transformation,
                ct,
                string.Empty,
                string.Empty,
                -1,
                string.Empty,
                string.Empty);
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
            return new CoordinateTransformation(
                source,
                target,
                TransformType.Conversion,
                new GeographicTransform(source, target),
                string.Empty,
                string.Empty,
                -1,
                string.Empty,
                string.Empty);
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

        return new CoordinateTransformation(
            source,
            target,
            TransformType.Transformation,
            ct,
            string.Empty,
            string.Empty,
            -1,
            string.Empty,
            string.Empty);
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
                new CoordinateTransformation(
                    (target.HorizontalDatum.Wgs84Parameters is null || target.HorizontalDatum.Wgs84Parameters.HasZeroValuesOnly) ? target : GeocentricCoordinateSystem.WGS84,
                    source,
                    TransformType.Transformation,
                    new DatumTransform(source.HorizontalDatum.Wgs84Parameters),
                    string.Empty,
                    string.Empty,
                    -1,
                    string.Empty,
                    string.Empty));
        }

        // Does target has a datum different from WGS84 and is there a shift specified?
        if (target.HorizontalDatum.Wgs84Parameters is not null && !target.HorizontalDatum.Wgs84Parameters.HasZeroValuesOnly)
        {
            ct.CoordinateTransformationList.Add(
                new CoordinateTransformation(
                    (source.HorizontalDatum.Wgs84Parameters is null || source.HorizontalDatum.Wgs84Parameters.HasZeroValuesOnly) ? source : GeocentricCoordinateSystem.WGS84,
                    target,
                    TransformType.Transformation,
                    new DatumTransform(target.HorizontalDatum.Wgs84Parameters).Inverse(),
                    string.Empty,
                    string.Empty,
                    -1,
                    string.Empty,
                    string.Empty));
        }

        // If we don't have a transformation in this list, return null
        if (ct.CoordinateTransformationList.Count == 0)
        {
            return null;
        }

        // If we only have one shift, lets just return the datumshift from/to wgs84
        return ct.CoordinateTransformationList.Count == 1
            ? new CoordinateTransformation(
                source,
                target,
                TransformType.ConversionAndTransformation,
                ((ICoordinateTransformation)ct.CoordinateTransformationList[0]).MathTransform,
                string.Empty,
                string.Empty,
                -1,
                string.Empty,
                string.Empty)
            : new CoordinateTransformation(source, target, TransformType.ConversionAndTransformation, ct, string.Empty, string.Empty, -1, string.Empty, string.Empty);
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

    private static Dictionary<int, IReadOnlyDictionary<string, double>> LoadDirectOperationParameters()
    {
        var parametersByOperation = new Dictionary<int, Dictionary<string, double>>();

        foreach (EpsgOperationParameterRecord parameter in EpsgGeneratedCatalog.OperationParameters)
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
