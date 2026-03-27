// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable

namespace ProjNet;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Data;

/// <summary>
/// Provides coordinate system lookup and transformation creation backed by a registry of SRID-keyed systems.
/// </summary>
public class CoordinateSystemServices // : ICoordinateSystemServices
{
    private readonly Dictionary<int, CoordinateSystem> csBySrid;
    private readonly Dictionary<IInfo, int> sridByCs;

    private readonly CoordinateSystemFactory coordinateSystemFactory;
    private readonly CoordinateTransformationFactory ctFactory;
    private readonly ICoordinateSystemDefinitionProvider definitionProvider;

    private readonly System.Threading.Tasks.Task initializationTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateSystemServices"/> class
    /// using the specified factories and the default definition provider.
    /// </summary>
    /// <param name="coordinateSystemFactory">The coordinate system factory to use.</param>
    /// <param name="coordinateTransformationFactory">The coordinate transformation factory to use.</param>
    public CoordinateSystemServices(
        CoordinateSystemFactory coordinateSystemFactory,
        CoordinateTransformationFactory coordinateTransformationFactory)
        : this(coordinateSystemFactory, coordinateTransformationFactory, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateSystemServices"/> class
    /// pre-populated from the supplied SRID-to-WKT definition pairs.
    /// </summary>
    /// <param name="definitions">An enumeration of SRID-to-WKT coordinate system definitions.</param>
    public CoordinateSystemServices(IEnumerable<KeyValuePair<int, string>> definitions)
        : this(new CoordinateSystemFactory(), new CoordinateTransformationFactory(), definitions, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateSystemServices"/> class
    /// using default factories and the default definition provider.
    /// </summary>
    public CoordinateSystemServices()
        : this(new CoordinateSystemFactory(), new CoordinateTransformationFactory(), null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateSystemServices"/> class
    /// using the supplied definition provider and default factories.
    /// </summary>
    /// <param name="definitionProvider">Coordinate system definition provider that supplies SRID definitions.</param>
    public CoordinateSystemServices(ICoordinateSystemDefinitionProvider definitionProvider)
        : this(new CoordinateSystemFactory(), new CoordinateTransformationFactory(), null, definitionProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateSystemServices"/> class
    /// pre-populated from the supplied SRID-to-WKT definition pairs.
    /// </summary>
    /// <param name="coordinateSystemFactory">The coordinate system factory to use.</param>
    /// <param name="coordinateTransformationFactory">The coordinate transformation factory to use.</param>
    /// <param name="enumeration">An enumeration of SRID-to-WKT coordinate system definitions.</param>
    public CoordinateSystemServices(
        CoordinateSystemFactory coordinateSystemFactory,
        CoordinateTransformationFactory coordinateTransformationFactory,
        IEnumerable<KeyValuePair<int, string>>? enumeration)
        : this(coordinateSystemFactory, coordinateTransformationFactory, enumeration, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateSystemServices"/> class
    /// with explicit control over all dependencies.
    /// </summary>
    /// <param name="coordinateSystemFactory">The coordinate system factory to use.</param>
    /// <param name="coordinateTransformationFactory">The coordinate transformation factory to use.</param>
    /// <param name="enumeration">An enumeration of SRID-to-WKT coordinate system definitions; when <see langword="null"/>, <paramref name="definitionProvider"/> is used instead.</param>
    /// <param name="definitionProvider">Definition provider used when <paramref name="enumeration"/> is <see langword="null"/>; defaults to <see cref="ManagedCoordinateSystemDefinitionProvider"/> when <see langword="null"/>.</param>
    public CoordinateSystemServices(
        CoordinateSystemFactory coordinateSystemFactory,
        CoordinateTransformationFactory coordinateTransformationFactory,
        IEnumerable<KeyValuePair<int, string>>? enumeration,
        ICoordinateSystemDefinitionProvider? definitionProvider)
    {
        this.coordinateSystemFactory = ArgumentGuard.ThrowIfNull(coordinateSystemFactory, nameof(coordinateSystemFactory));
        this.ctFactory = ArgumentGuard.ThrowIfNull(coordinateTransformationFactory, nameof(coordinateTransformationFactory));
        this.definitionProvider = definitionProvider ?? new ManagedCoordinateSystemDefinitionProvider();

        this.csBySrid = new();
        this.sridByCs = new(new CsEqualityComparer());

        object enumObj;
        if (enumeration is not null)
        {
            enumObj = enumeration;
        }
        else if (this.definitionProvider is IManagedCoordinateSystemProvider managedCoordinateSystemProvider)
        {
            enumObj = managedCoordinateSystemProvider.GetCoordinateSystems();
        }
        else
        {
            enumObj = this.definitionProvider;
        }

        this.initializationTask = System.Threading.Tasks.Task.Run(() => this.InitializeFromEnumeration(enumObj));
    }

    /// <summary>
    /// Gets the number of coordinate systems registered in this instance.
    /// </summary>
    protected int Count
    {
        get
        {
            this.WaitForInitialization();
            return this.sridByCs.Count;
        }
    }

    /// <summary>
    /// Returns the coordinate system registered under the specified SRID.
    /// </summary>
    /// <param name="srid">The SRID of the coordinate system.</param>
    /// <returns>The coordinate system, or <see langword="null"/> if not found.</returns>
    public CoordinateSystem? GetCoordinateSystem(int srid)
    {
        this.WaitForInitialization();
        return this.csBySrid.TryGetValue(srid, out var cs) ? cs : null;
    }

    /// <summary>
    /// Tries to get a coordinate system by SRID.
    /// </summary>
    /// <param name="srid">The SRID of the coordinate system.</param>
    /// <param name="coordinateSystem">The coordinate system if found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a coordinate system was found; otherwise <see langword="false"/>.</returns>
    public bool TryGetCoordinateSystem(int srid, [NotNullWhen(true)] out CoordinateSystem? coordinateSystem)
    {
        this.WaitForInitialization();
        return this.csBySrid.TryGetValue(srid, out coordinateSystem);
    }

    /// <summary>
    /// Returns the coordinate system by <paramref name="authority" /> and <paramref name="code" />.
    /// </summary>
    /// <param name="authority">The authority for the coordinate system.</param>
    /// <param name="code">The code assigned to the coordinate system by <paramref name="authority" />.</param>
    /// <returns>The coordinate system, or <see langword="null"/> when no entry is registered.</returns>
    public CoordinateSystem? GetCoordinateSystem(string authority, long code)
    {
        int? srid = this.GetSRID(authority, code);
        if (srid.HasValue)
        {
            return this.GetCoordinateSystem(srid.Value);
        }

        return null;
    }

    /// <summary>
    /// Tries to get a coordinate system by authority and code.
    /// </summary>
    /// <param name="authority">The authority name.</param>
    /// <param name="code">The authority code.</param>
    /// <param name="coordinateSystem">The coordinate system if found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a coordinate system was found; otherwise <see langword="false"/>.</returns>
    public bool TryGetCoordinateSystem(string authority, long code, [NotNullWhen(true)] out CoordinateSystem? coordinateSystem)
    {
        coordinateSystem = null;
        int? srid = this.GetSRID(authority, code);
        if (!srid.HasValue)
        {
            return false;
        }

        coordinateSystem = this.GetCoordinateSystem(srid.Value);
        return coordinateSystem is not null;
    }

    /// <summary>
    /// Gets all available SRID values currently loaded in the registry.
    /// </summary>
    /// <returns>Sorted SRID values.</returns>
    public int[] GetAvailableSridValues()
    {
        this.WaitForInitialization();
        return this.csBySrid.Keys.OrderBy(v => v).ToArray();
    }

    /// <summary>
    /// Returns the SRID under which the coordinate system identified by <paramref name="authority"/> and <paramref name="authorityCode"/> is registered.
    /// </summary>
    /// <param name="authority">The authority name.</param>
    /// <param name="authorityCode">The code assigned by <paramref name="authority"/>.</param>
    /// <returns>The SRID, or <see langword="null"/> if no matching coordinate system is registered.</returns>
    public int? GetSRID(string authority, long authorityCode)
    {
        var key = new CoordinateSystemKey(authority, authorityCode);
        int srid;
        this.WaitForInitialization();
        if (this.sridByCs.TryGetValue(key, out srid))
        {
            return srid;
        }

        return null;
    }

    /// <summary>
    /// Creates a coordinate transformation between two spatial reference systems identified by their SRIDs.
    /// </summary>
    /// <remarks>This is a convenience overload for <see cref="CreateTransformation(CoordinateSystem, CoordinateSystem)"/>.</remarks>
    /// <param name="sourceSrid">The SRID of the source spatial reference system.</param>
    /// <param name="targetSrid">The SRID of the target spatial reference system.</param>
    /// <returns>A coordinate transformation, or <see langword="null"/> if no transformation could be created.</returns>
    public ICoordinateTransformation? CreateTransformation(int sourceSrid, int targetSrid)
    {
        return this.CreateTransformation(
            this.GetCoordinateSystem(sourceSrid),
            this.GetCoordinateSystem(targetSrid));
    }

    /// <summary>
    /// Creates a coordinate transformation between two spatial reference systems.
    /// </summary>
    /// <param name="source">The source spatial reference system.</param>
    /// <param name="target">The target spatial reference system.</param>
    /// <returns>A coordinate transformation, or <see langword="null"/> if no transformation could be created.</returns>
    public ICoordinateTransformation? CreateTransformation(CoordinateSystem? source, CoordinateSystem? target)
    {
        if (source is null || target is null)
        {
            return null;
        }

        return this.ctFactory.CreateFromCoordinateSystems(source, target);
    }

    /// <summary>
    /// This operation is not supported.
    /// </summary>
    /// <param name="srid">The SRID of the coordinate system to remove.</param>
    /// <returns>This method never returns normally.</returns>
    /// <exception cref="NotSupportedException">Always thrown; removing coordinate systems is not supported.</exception>
    public bool RemoveCoordinateSystem(int srid)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Returns an enumerator that iterates over all registered SRID–coordinate-system pairs.
    /// </summary>
    /// <returns>An enumerator over the registered SRID-to-coordinate-system mappings.</returns>
    public IEnumerator<KeyValuePair<int, CoordinateSystem>> GetEnumerator()
    {
        this.WaitForInitialization();
        return this.csBySrid.GetEnumerator();
    }

    /// <summary>
    /// Registers a coordinate system under the specified SRID, replacing any existing entry for that SRID.
    /// </summary>
    /// <param name="srid">The SRID key.</param>
    /// <param name="coordinateSystem">The coordinate system to register.</param>
    protected void AddCoordinateSystem(int srid, CoordinateSystem coordinateSystem)
    {
        lock (((IDictionary)this.csBySrid).SyncRoot)
        {
            lock (((IDictionary)this.sridByCs).SyncRoot)
            {
                if (this.sridByCs.ContainsKey(coordinateSystem))
                {
                    return;
                }

                if (this.csBySrid.TryGetValue(srid, out var existingCoordinateSystem))
                {
                    if (ReferenceEquals(coordinateSystem, existingCoordinateSystem))
                    {
                        return;
                    }

                    this.sridByCs.Remove(existingCoordinateSystem);
                    this.csBySrid[srid] = coordinateSystem;
                    this.sridByCs.Add(coordinateSystem, srid);
                }
                else
                {
                    this.csBySrid.Add(srid, coordinateSystem);
                    this.sridByCs.Add(coordinateSystem, srid);
                }
            }
        }
    }

    /// <summary>
    /// Registers a coordinate system using its own <see cref="IInfo.AuthorityCode"/> as the SRID.
    /// </summary>
    /// <param name="coordinateSystem">The coordinate system to register.</param>
    /// <returns>The SRID under which the coordinate system was registered.</returns>
    protected virtual int AddCoordinateSystem(CoordinateSystem coordinateSystem)
    {
        coordinateSystem = ArgumentGuard.ThrowIfNull(coordinateSystem, nameof(coordinateSystem));
        int srid = (int)coordinateSystem.AuthorityCode;
        this.AddCoordinateSystem(srid, coordinateSystem);

        return srid;
    }

    /// <summary>
    /// Removes all registered coordinate systems.
    /// </summary>
    protected void Clear()
    {
        this.csBySrid.Clear();
    }

    private static CoordinateSystem? CreateCoordinateSystem(CoordinateSystemFactory coordinateSystemFactory, string wkt)
    {
        try
        {
            return coordinateSystemFactory.CreateFromWkt(StringCompatibility.ReplaceOrdinal(wkt, "ELLIPSOID", "SPHEROID"));
        }
        catch (Exception)
        {
            // as a fallback we ignore projections not supported
            return null;
        }
    }

    private static void FromEnumeration(
        CoordinateSystemServices css,
        IEnumerable<KeyValuePair<int, CoordinateSystem>> enumeration)
    {
        foreach (var sridCs in enumeration)
        {
            css.AddCoordinateSystem(sridCs.Key, sridCs.Value);
        }
    }

    private static IEnumerable<KeyValuePair<int, CoordinateSystem>> CreateCoordinateSystems(
        CoordinateSystemFactory factory,
        IEnumerable<KeyValuePair<int, string>> enumeration)
    {
        foreach (var sridWkt in enumeration)
        {
            var cs = CreateCoordinateSystem(factory, sridWkt.Value);
            if (cs is not null)
            {
                yield return new KeyValuePair<int, CoordinateSystem>(sridWkt.Key, cs);
            }
        }
    }

    private static void FromEnumeration(
        CoordinateSystemServices css,
        IEnumerable<KeyValuePair<int, string>> enumeration)
    {
        FromEnumeration(css, CreateCoordinateSystems(css.coordinateSystemFactory, enumeration));
    }

    private void InitializeFromEnumeration(object enumeration)
    {
        if (enumeration is ICoordinateSystemDefinitionProvider provider)
        {
            FromEnumeration(this, provider.GetDefinitions());
            return;
        }

        if (enumeration is IEnumerable<KeyValuePair<int, string>> wktEnumeration)
        {
            FromEnumeration(this, wktEnumeration);
            return;
        }

        if (enumeration is IEnumerable<KeyValuePair<int, CoordinateSystem>> coordinateSystemEnumeration)
        {
            FromEnumeration(this, coordinateSystemEnumeration);
            return;
        }

        throw new InvalidOperationException("Unsupported coordinate system initialization payload.");
    }

    private void WaitForInitialization()
    {
        try
        {
            this.initializationTask.GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Coordinate system initialization failed.", exception);
        }
    }

    private class CsEqualityComparer : EqualityComparer<IInfo>
    {
        /// <inheritdoc />
        public override bool Equals(IInfo? x, IInfo? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.AuthorityCode == y.AuthorityCode &&
                string.Equals(x.Authority, y.Authority, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public override int GetHashCode(IInfo obj)
        {
            if (obj is null)
            {
                return 0;
            }

            return Convert.ToInt32(obj.AuthorityCode) + (obj.Authority is not null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Authority) : 0);
        }
    }

    private class CoordinateSystemKey : IInfo
    {
        public CoordinateSystemKey(string authority, long authorityCode)
        {
            this.Authority = authority ?? string.Empty;
            this.AuthorityCode = authorityCode;
        }

        public string Authority { get; private set; }

        public long AuthorityCode { get; private set; }

        public string Name
        {
            get => string.Empty;
        }

        public string Alias
        {
            get => string.Empty;
        }

        public string Abbreviation
        {
            get => string.Empty;
        }

        public string Remarks
        {
            get => string.Empty;
        }

        public string WKT
        {
            get => string.Empty;
        }

        public string XML
        {
            get => string.Empty;
        }

        public bool EqualParams(object obj)
        {
            throw new NotSupportedException();
        }
    }
}
