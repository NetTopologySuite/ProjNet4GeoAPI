// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
public partial class CoordinateTransformationFactory
{
    private const string GridCacheEnvironmentVariable = "PROJNET_GRID_CACHE";
    private const string GridBaseUrlEnvironmentVariable = "PROJNET_GRID_BASE_URL";
    private const string GridModeEnvironmentVariable = "PROJNET_GRID_MODE";
    private const string GridPathEnvironmentVariable = "PROJNET_GRID_PATHS";
    private const string GridRequiredEnvironmentVariable = "PROJNET_GRID_REQUIRED";
    private static readonly Lazy<Dictionary<SridPair, IReadOnlyList<CoordinateOperationDefinition>>> DirectOperationDefinitions =
        new(LoadDirectOperationDefinitions, true);

    private static readonly Lazy<Dictionary<int, CoordinateOperationDefinition>> DirectOperationDefinitionsByCode =
        new(LoadDirectOperationDefinitionsByCode, true);

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
    /// Attempts to resolve a grid resource name to a concrete file path.
    /// </summary>
    /// <param name="gridName">Grid resource name or path token.</param>
    /// <param name="resolvedPath">Resolved local file path when available.</param>
    /// <returns><see langword="true"/> when resolution succeeded; otherwise <see langword="false"/>.</returns>
    internal static bool TryResolveGridResourcePath(string gridName, [NotNullWhen(true)] out string? resolvedPath) => GetGridResolver().TryResolve(gridName, out resolvedPath);

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
}
