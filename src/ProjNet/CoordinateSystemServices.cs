// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNet
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using ProjNet.CoordinateSystems;
    using ProjNet.CoordinateSystems.Transformations;
    using ProjNet.Data;

    /// <summary>
    /// A coordinate system services class.
    /// </summary>
    public class CoordinateSystemServices // : ICoordinateSystemServices
    {
        // private static ICoordinateSequenceFactory _coordinateSequenceFactory;

        ///// <summary>
        ///// Gets or sets a default coordinate sequence factory
        ///// </summary>
        // public static ICoordinateSequenceFactory CoordinateSequenceFactory
        // {
        //    get { return _coordinateSequenceFactory ?? new CoordinateArraySequenceFactory(); }
        //    set { _coordinateSequenceFactory = value; }
        // }
        private readonly Dictionary<int, CoordinateSystem> csBySrid;
        private readonly Dictionary<IInfo, int> sridByCs;

        private readonly CoordinateSystemFactory coordinateSystemFactory;
        private readonly CoordinateTransformationFactory ctFactory;
        private readonly ICoordinateSystemDefinitionProvider definitionProvider;

        private readonly ManualResetEvent initialization = new ManualResetEvent(false);

        private class CsEqualityComparer : EqualityComparer<IInfo>
        {
            /// <inheritdoc />
            public override bool Equals(IInfo x, IInfo y)
            {
                return x.AuthorityCode == y.AuthorityCode &&
                    string.Compare(x.Authority, y.Authority, StringComparison.OrdinalIgnoreCase) == 0;
            }

            /// <inheritdoc />
            public override int GetHashCode(IInfo obj)
            {
                if (obj == null)
                {
                    return 0;
                }

                return Convert.ToInt32(obj.AuthorityCode) + (obj.Authority != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Authority) : 0);
            }
        }

        private class CoordinateSystemKey : IInfo
        {
            public CoordinateSystemKey(string authority, long authorityCode)
            {
                this.Authority = authority;
                this.AuthorityCode = authorityCode;
            }

            public bool EqualParams(object obj)
            {
                throw new NotSupportedException();
            }

            public string Authority { get; private set; }
            public long AuthorityCode { get; private set; }

            public string Name
            {
                get { return null; }
            }

            public string Alias
            {
                get { return null; }
            }

            public string Abbreviation
            {
                get { return null; }
            }

            public string Remarks
            {
                get { return null; }
            }

            public string WKT
            {
                get { return null; }
            }

            public string XML
            {
                get { return null; }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateSystemServices"/> class.
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="coordinateSystemFactory">The coordinate sequence factory to use.</param>
        /// <param name="coordinateTransformationFactory">The coordinate transformation factory to use.</param>
        public CoordinateSystemServices(
            CoordinateSystemFactory coordinateSystemFactory,
            CoordinateTransformationFactory coordinateTransformationFactory)
            : this(coordinateSystemFactory, coordinateTransformationFactory, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateSystemServices"/> class.
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="definitions">An enumeration of coordinate system definitions (WKT).</param>
        public CoordinateSystemServices(IEnumerable<KeyValuePair<int, string>> definitions)
            : this(new CoordinateSystemFactory(), new CoordinateTransformationFactory(), definitions, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateSystemServices"/> class.
        /// Creates an instance of this class.
        /// </summary>
        public CoordinateSystemServices()
            : this(new CoordinateSystemFactory(), new CoordinateTransformationFactory(), null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateSystemServices"/> class.
        /// Creates an instance of this class with a managed definition provider.
        /// </summary>
        /// <param name="definitionProvider">Managed coordinate system definition provider.</param>
        public CoordinateSystemServices(ICoordinateSystemDefinitionProvider definitionProvider)
            : this(new CoordinateSystemFactory(), new CoordinateTransformationFactory(), null, definitionProvider)
        {
        }

        // public Func<string, long, string> GetDefinition { get; set; }

        /*
        public static string GetFromSpatialReferenceOrg(string authority, long code)
        {
            var url = string.Format("http://spatialreference.org/ref/{0}/{1}/ogcwkt/",
                authority.ToLowerInvariant(),
                code);
            var req = (HttpWebRequest) WebRequest.Create(url);
            using (var resp = req.GetResponse())
            {
                using (var resps = resp.GetResponseStream())
                {
                    if (resps != null)
                    {
                        using (var sr = new StreamReader(resps))
                            return sr.ReadToEnd();
                    }
                }
            }
            return null;
        }
         */

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateSystemServices"/> class.
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="coordinateSystemFactory">The coordinate sequence factory to use.</param>
        /// <param name="coordinateTransformationFactory">The coordinate transformation factory to use.</param>
        /// <param name="enumeration">An enumeration of coordinate system definitions (WKT).</param>
        public CoordinateSystemServices(
            CoordinateSystemFactory coordinateSystemFactory,
            CoordinateTransformationFactory coordinateTransformationFactory,
            IEnumerable<KeyValuePair<int, string>> enumeration)
            : this(coordinateSystemFactory, coordinateTransformationFactory, enumeration, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateSystemServices"/> class.
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="coordinateSystemFactory">The coordinate sequence factory to use.</param>
        /// <param name="coordinateTransformationFactory">The coordinate transformation factory to use.</param>
        /// <param name="enumeration">An enumeration of coordinate system definitions (WKT).</param>
        /// <param name="definitionProvider">Managed coordinate system definition provider used when <paramref name="enumeration"/> is null.</param>
        public CoordinateSystemServices(
            CoordinateSystemFactory coordinateSystemFactory,
            CoordinateTransformationFactory coordinateTransformationFactory,
            IEnumerable<KeyValuePair<int, string>> enumeration,
            ICoordinateSystemDefinitionProvider definitionProvider)
        {
            if (coordinateSystemFactory == null)
            {
                throw new ArgumentNullException(nameof(coordinateSystemFactory));
            }

            this.coordinateSystemFactory = coordinateSystemFactory;

            if (coordinateTransformationFactory == null)
            {
                throw new ArgumentNullException(nameof(coordinateTransformationFactory));
            }

            this.ctFactory = coordinateTransformationFactory;
            this.definitionProvider = definitionProvider ?? new ManagedCoordinateSystemDefinitionProvider();

            this.csBySrid = new Dictionary<int, CoordinateSystem>();
            this.sridByCs = new Dictionary<IInfo, int>(new CsEqualityComparer());

            object enumObj;
            if (enumeration != null)
            {
                enumObj = enumeration;
            }
            else if (this.definitionProvider is IManagedCoordinateSystemProvider managedCoordinateSystemProvider)
            {
                enumObj = managedCoordinateSystemProvider.GetCoordinateSystems();
            }
            else
            {
                enumObj = this.definitionProvider.GetDefinitions();
            }

            this.initialization = new ManualResetEvent(false);
            System.Threading.Tasks.Task.Run(() => FromEnumeration(new[] { this, enumObj }));
        }

        /// <summary>
        /// Gets count.
        /// </summary>
        protected int Count
        {
            get
            {
                this.initialization.WaitOne();
                return this.sridByCs.Count;
            }
        }

        // private CoordinateSystemServices(ICoordinateSystemFactory coordinateSystemFactory,
        //    ICoordinateTransformationFactory coordinateTransformationFactory,
        //    IEnumerable<KeyValuePair<int, ICoordinateSystem>> enumeration)
        //    : this(coordinateSystemFactory, coordinateTransformationFactory)
        // {
        //    var enumObj = (object)enumeration ?? DefaultInitialization();
        //    _initialization = new ManualResetEvent(false);
        //    ThreadPool.QueueUserWorkItem(FromEnumeration, new[] { this, enumObj });
        // }
        private static CoordinateSystem CreateCoordinateSystem(CoordinateSystemFactory coordinateSystemFactory, string wkt)
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
                if (cs != null)
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

        private static void FromEnumeration(object parameter)
        {
            object[] paras = (object[])parameter;
            var css = (CoordinateSystemServices)paras[0];

            if (paras[1] is IEnumerable<KeyValuePair<int, string>>)
            {
                FromEnumeration(css, (IEnumerable<KeyValuePair<int, string>>)paras[1]);
            }
            else
            {
                FromEnumeration(css, (IEnumerable<KeyValuePair<int, CoordinateSystem>>)paras[1]);
            }

            css.initialization.Set();
        }

        /// <summary>
        /// Returns the coordinate system by <paramref name="srid" /> identifier.
        /// </summary>
        /// <param name="srid">The initialization for the coordinate system.</param>
        /// <returns>The coordinate system.</returns>
        public CoordinateSystem GetCoordinateSystem(int srid)
        {
            this.initialization.WaitOne();
            return this.csBySrid.TryGetValue(srid, out var cs) ? cs : null;
        }

        /// <summary>
        /// Tries to get a coordinate system by SRID.
        /// </summary>
        /// <param name="srid">The SRID value.</param>
        /// <param name="coordinateSystem">The coordinate system if found; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if a coordinate system was found; otherwise <c>false</c>.</returns>
        public bool TryGetCoordinateSystem(int srid, out CoordinateSystem coordinateSystem)
        {
            this.initialization.WaitOne();
            return this.csBySrid.TryGetValue(srid, out coordinateSystem);
        }

        /// <summary>
        /// Returns the coordinate system by <paramref name="authority" /> and <paramref name="code" />.
        /// </summary>
        /// <param name="authority">The authority for the coordinate system.</param>
        /// <param name="code">The code assigned to the coordinate system by <paramref name="authority" />.</param>
        /// <returns>The coordinate system.</returns>
        public CoordinateSystem GetCoordinateSystem(string authority, long code)
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
        /// <param name="coordinateSystem">The coordinate system if found; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if a coordinate system was found; otherwise <c>false</c>.</returns>
        public bool TryGetCoordinateSystem(string authority, long code, out CoordinateSystem coordinateSystem)
        {
            coordinateSystem = null;
            int? srid = this.GetSRID(authority, code);
            if (!srid.HasValue)
            {
                return false;
            }

            coordinateSystem = this.GetCoordinateSystem(srid.Value);
            return coordinateSystem != null;
        }

        /// <summary>
        /// Gets all available SRID values currently loaded in the registry.
        /// </summary>
        /// <returns>Sorted SRID values.</returns>
        public int[] GetAvailableSridValues()
        {
            this.initialization.WaitOne();
            return this.csBySrid.Keys.OrderBy(v => v).ToArray();
        }

        /// <summary>
        /// Method to get the identifier, by which this coordinate system can be accessed.
        /// </summary>
        /// <param name="authority">The authority name.</param>
        /// <param name="authorityCode">The code assigned by <paramref name="authority" />.</param>
        /// <returns>The identifier or. <value>null</value></returns>
        public int? GetSRID(string authority, long authorityCode)
        {
            var key = new CoordinateSystemKey(authority, authorityCode);
            int srid;
            this.initialization.WaitOne();
            if (this.sridByCs.TryGetValue(key, out srid))
            {
                return srid;
            }

            return null;
        }

        /// <summary>
        /// Method to create a coordinate transformation between two spatial reference systems, defined by their identifiers.
        /// </summary>
        /// <remarks>This is a convenience function for <see cref="M:GeoAPI.ICoordinateSystemServices.CreateTransformation(GeoAPI.CoordinateSystems.ICoordinateSystem,GeoAPI.CoordinateSystems.ICoordinateSystem)" />.</remarks>
        /// <param name="sourceSrid">The identifier for the source spatial reference system.</param>
        /// <param name="targetSrid">The identifier for the target spatial reference system.</param>
        /// <returns>A coordinate transformation, <value>null</value> if no transformation could be created.</returns>
        public ICoordinateTransformation CreateTransformation(int sourceSrid, int targetSrid)
        {
            return this.CreateTransformation(
                this.GetCoordinateSystem(sourceSrid),
                this.GetCoordinateSystem(targetSrid));
        }

        /// <summary>
        /// Method to create a coordinate transformation between two spatial reference systems.
        /// </summary>
        /// <param name="source">The source spatial reference system.</param>
        /// <param name="target">The target spatial reference system.</param>
        /// <returns>A coordinate transformation, <value>null</value> if no transformation could be created.</returns>
        public ICoordinateTransformation CreateTransformation(CoordinateSystem source, CoordinateSystem target)
        {
            return this.ctFactory.CreateFromCoordinateSystems(source, target);
        }

        /// <summary>
        /// AddCoordinateSystem.
        /// </summary>
        /// <param name="srid">The srid parameter.</param>
        /// <param name="coordinateSystem">The coordinateSystem parameter.</param>
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

                    if (this.csBySrid.ContainsKey(srid))
                    {
                        if (ReferenceEquals(coordinateSystem, this.csBySrid[srid]))
                        {
                            return;
                        }

                        this.sridByCs.Remove(this.csBySrid[srid]);
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
        /// AddCoordinateSystem.
        /// </summary>
        /// <param name="coordinateSystem">The coordinateSystem parameter.</param>
        /// <returns>The transformation result.</returns>
        protected virtual int AddCoordinateSystem(CoordinateSystem coordinateSystem)
        {
            int srid = (int)coordinateSystem.AuthorityCode;
            this.AddCoordinateSystem(srid, coordinateSystem);

            return srid;
        }

        /// <summary>
        /// Clear.
        /// </summary>
        protected void Clear()
        {
            this.csBySrid.Clear();
        }

        /// <summary>
        /// RemoveCoordinateSystem.
        /// </summary>
        /// <param name="srid">The srid parameter.</param>
        /// <returns>The transformation result.</returns>
        /// <exception cref="NotSupportedException"></exception>
        public bool RemoveCoordinateSystem(int srid)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// GetEnumerator.
        /// </summary>
        /// <returns>The transformation result.</returns>
        public IEnumerator<KeyValuePair<int, CoordinateSystem>> GetEnumerator()
        {
            this.initialization.WaitOne();
            return this.csBySrid.GetEnumerator();
        }
    }
}
