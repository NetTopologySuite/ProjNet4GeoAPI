using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProjNet.Resources
{
    internal enum GridResourceResolutionMode
    {
        LocalOnly = 0,
        LocalThenNetwork = 1
    }

    internal interface IGridResourceFetchClient
    {
        bool TryFetch(string gridName, string targetFilePath);
    }

    internal sealed class GridResourceResolverOptions
    {
        internal GridResourceResolverOptions(IEnumerable<string> localDirectories, string cacheDirectory, GridResourceResolutionMode mode)
        {
            if (localDirectories == null)
                throw new ArgumentNullException(nameof(localDirectories));

            LocalDirectories = localDirectories
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(Path.GetFullPath)
                .ToArray();
            CacheDirectory = string.IsNullOrWhiteSpace(cacheDirectory) ? null : Path.GetFullPath(cacheDirectory);
            Mode = mode;
        }

        internal string CacheDirectory { get; }

        internal IReadOnlyList<string> LocalDirectories { get; }

        internal GridResourceResolutionMode Mode { get; }
    }

    internal sealed class GridResourceResolver
    {
        private static readonly IGridResourceFetchClient DefaultFetchClient = new NoOpGridResourceFetchClient();

        private readonly IGridResourceFetchClient _fetchClient;
        private readonly GridResourceResolverOptions _options;
        private readonly Dictionary<string, string> _resolvedPathByGridName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly object _sync = new object();

        internal GridResourceResolver(GridResourceResolverOptions options, IGridResourceFetchClient fetchClient = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _fetchClient = fetchClient ?? DefaultFetchClient;
        }

        internal bool TryResolve(string gridName, out string resolvedPath)
        {
            if (string.IsNullOrWhiteSpace(gridName))
                throw new ArgumentException("Grid name must not be empty.", nameof(gridName));

            if (TryResolveFromCache(gridName, out resolvedPath))
                return true;

            if (TryResolveFromLocalSources(gridName, out resolvedPath))
            {
                RememberResolvedPath(gridName, resolvedPath);
                return true;
            }

            if (_options.Mode == GridResourceResolutionMode.LocalThenNetwork && TryResolveFromNetwork(gridName, out resolvedPath))
            {
                RememberResolvedPath(gridName, resolvedPath);
                return true;
            }

            resolvedPath = null;
            return false;
        }

        private void RememberResolvedPath(string gridName, string resolvedPath)
        {
            lock (_sync)
                _resolvedPathByGridName[gridName] = resolvedPath;
        }

        private bool TryResolveFromCache(string gridName, out string resolvedPath)
        {
            lock (_sync)
            {
                if (_resolvedPathByGridName.TryGetValue(gridName, out resolvedPath))
                {
                    if (File.Exists(resolvedPath))
                        return true;

                    _resolvedPathByGridName.Remove(gridName);
                }
            }

            resolvedPath = null;
            return false;
        }

        private bool TryResolveFromLocalSources(string gridName, out string resolvedPath)
        {
            if (Path.IsPathRooted(gridName) && File.Exists(gridName))
            {
                resolvedPath = Path.GetFullPath(gridName);
                return true;
            }

            string fileName = Path.GetFileName(gridName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                resolvedPath = null;
                return false;
            }

            foreach (string localDirectory in _options.LocalDirectories)
            {
                string candidatePath = Path.Combine(localDirectory, fileName);
                if (!File.Exists(candidatePath))
                    continue;

                resolvedPath = candidatePath;
                return true;
            }

            resolvedPath = null;
            return false;
        }

        private bool TryResolveFromNetwork(string gridName, out string resolvedPath)
        {
            if (string.IsNullOrWhiteSpace(_options.CacheDirectory))
            {
                resolvedPath = null;
                return false;
            }

            Directory.CreateDirectory(_options.CacheDirectory);
            string fileName = Path.GetFileName(gridName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                resolvedPath = null;
                return false;
            }

            string targetPath = Path.Combine(_options.CacheDirectory, fileName);
            if (File.Exists(targetPath))
            {
                resolvedPath = targetPath;
                return true;
            }

            if (!_fetchClient.TryFetch(gridName, targetPath) || !File.Exists(targetPath))
            {
                resolvedPath = null;
                return false;
            }

            resolvedPath = targetPath;
            return true;
        }

        private sealed class NoOpGridResourceFetchClient : IGridResourceFetchClient
        {
            public bool TryFetch(string gridName, string targetFilePath)
            {
                return false;
            }
        }
    }
}
