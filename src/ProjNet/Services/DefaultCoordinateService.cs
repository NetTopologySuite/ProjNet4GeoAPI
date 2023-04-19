using ProjNet.CoordinateSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProjNet.Services
{
    /// <summary>
    /// A default service for fetching Coordinate Systems
    /// </summary>
    public class DefaultCoordinateService : CoordinateSystemService, ICoordinateSystemService
    {
        private readonly Dictionary<int, CoordinateSystem> _csBySrid;
        private readonly Dictionary<IInfo, int> _sridByCs;
        private static ManualResetEvent _initialization = new ManualResetEvent(false);

        #region CsEqualityComparer class
        private class CsEqualityComparer : EqualityComparer<IInfo>
        {
            public override bool Equals(IInfo x, IInfo y)
            {
                return x.AuthorityCode == y.AuthorityCode &&
                    string.Compare(x.Authority, y.Authority, StringComparison.OrdinalIgnoreCase) == 0;
            }

            public override int GetHashCode(IInfo obj)
            {
                if (obj == null) return 0;
                return Convert.ToInt32(obj.AuthorityCode) + (obj.Authority != null ? obj.Authority.GetHashCode() : 0);
            }
        }
        #endregion

        #region CoordinateSystemKey class

        private class CoordinateSystemKey : IInfo
        {
            public CoordinateSystemKey(string authority, long authorityCode)
            {
                Authority = authority;
                AuthorityCode = authorityCode;
            }

            public bool EqualParams(object obj)
            {
                throw new NotSupportedException();
            }

            public string Name { get { return null; } }
            public string Authority { get; private set; }
            public long AuthorityCode { get; private set; }
            public string Alias { get { return null; } }
            public string Abbreviation { get { return null; } }
            public string Remarks { get { return null; } }
            public string WKT { get { return null; } }
            public string XML { get { return null; } }
        }

        #endregion

        /// <summary>
        /// The default coordinate system service.
        /// </summary>
        /// <param name="csFactory">The coordinate system factory</param>
        /// <param name="enumeration">KeyValuePairs of srid and wkt string. If null, then
        /// WGS84 and WebMercator are by default instatiated with this service</param>
        public DefaultCoordinateService(CoordinateSystemFactory csFactory, IEnumerable<KeyValuePair<int, string>> enumeration)
            : base(csFactory)
        {
            _csBySrid = new Dictionary<int, CoordinateSystem>();
            _sridByCs = new Dictionary<IInfo, int>(new CsEqualityComparer());

            _initialization = new ManualResetEvent(false);
            if (enumeration == null)
                Task.Run(() => Initialize(DefaultInitialization()));
            else
                Task.Run(() => Initialize(enumeration));
        }

        /// <summary>
        /// The default coordinate system service.
        /// </summary>
        /// <param name="csFactory">The coordinate system factory</param>
        /// <param name="enumeration">KeyValuePairs of srid and wkt string. If null, then
        /// WGS84 and WebMercator are by default instatiated with this service</param>
        public DefaultCoordinateService(CoordinateSystemFactory csFactory, IEnumerable<KeyValuePair<int, CoordinateSystem>> enumeration)
            : base(csFactory)
        {
            _csBySrid = new Dictionary<int, CoordinateSystem>();
            _sridByCs = new Dictionary<IInfo, int>(new CsEqualityComparer());

            _initialization = new ManualResetEvent(false);
            if (enumeration == null)
                Task.Run(() => Initialize(DefaultInitialization()));
            else
                Task.Run(() => Initialize(enumeration));
        }

        /// <summary>
        /// The default coordinate system service.
        /// </summary>
        /// <param name="enumeration">KeyValuePairs of srid and wkt string. If null, then
        /// WGS84 and WebMercator are by default instatiated with this service</param>
        public DefaultCoordinateService(IEnumerable<KeyValuePair<int, string>> enumeration)
            :base(new CoordinateSystemFactory())
        {
            _csBySrid = new Dictionary<int, CoordinateSystem>();
            _sridByCs = new Dictionary<IInfo, int>(new CsEqualityComparer());

            _initialization = new ManualResetEvent(false);
            if (enumeration == null)
                Task.Run(() => Initialize(DefaultInitialization()));
            else
                Task.Run(() => Initialize(enumeration));
        }

        private void Initialize(IEnumerable<KeyValuePair<int, string>> enumeration)
        {
            FromEnumeration(enumeration);
            InitializationSet();
        }

        private void Initialize(IEnumerable<KeyValuePair<int, CoordinateSystem>> enumeration)
        {
            FromEnumeration(enumeration);
            InitializationSet();
        }

        /// <summary>
        /// Initializes the CoordinateSystemService with default,internally set projections
        /// </summary>
        /// <returns></returns>
        private IEnumerable<KeyValuePair<int, CoordinateSystem>> DefaultInitialization()
        {
            yield return new KeyValuePair<int, CoordinateSystem>(4326, GeographicCoordinateSystem.WGS84);
            yield return new KeyValuePair<int, CoordinateSystem>(3857, ProjectedCoordinateSystem.WebMercator);
        }

        /// <summary>
        /// Enumeration method for adding crs to the dictionary
        /// </summary>
        /// <param name="enumeration"></param>
        protected void FromEnumeration(IEnumerable<KeyValuePair<int, CoordinateSystem>> enumeration)
        {
            foreach (var sridCs in enumeration)
            {
                if (sridCs.Value != null)
                {
                    AddCoordinateSystem(sridCs.Key, sridCs.Value);
                }
            }
        }

        private void FromEnumeration(IEnumerable<KeyValuePair<int, string>> enumeration)
        {
            FromEnumeration(CreateCoordinateSystems(enumeration));
        }

        private IEnumerable<KeyValuePair<int, CoordinateSystem>> CreateCoordinateSystems(
                       IEnumerable<KeyValuePair<int, string>> enumeration)
        {
            foreach (var sridWkt in enumeration)
            {
                var cs = CreateCoordinateSystem(sridWkt.Value);
                if (cs != null)
                    yield return new KeyValuePair<int, CoordinateSystem>(sridWkt.Key, cs);
            }
        }

        private CoordinateSystem CreateCoordinateSystem(string wkt)
        {
            try
            {
                return CsFactory.CreateFromWkt(wkt.Replace("ELLIPSOID", "SPHEROID"));
            }
            catch (Exception)
            {
                // as a fallback we ignore projections not supported
                return null;
            }
        }

        /// <summary>
        /// Notifies that the initialization is complete
        /// </summary>
        protected static void InitializationSet()
        {
            _initialization.Set();
        }
        

        /// <summary>
        /// Clears the dictionary
        /// </summary>
        protected void Clear()
        {
            _csBySrid.Clear();
            _sridByCs.Clear();
        }

        /// <summary>
        /// Removes a single coordinate system from the dictionary
        /// </summary>
        /// <param name="srid"></param>
        /// <returns></returns>
        public bool RemoveCoordinateSystem(int srid)
        {
            if (_csBySrid.Remove(srid))
            {
                var cs = _sridByCs.Where(f => f.Value == srid);
                foreach (var ss in cs)
                {
                    _sridByCs.Remove(ss.Key);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns an enumeration of the coordinate systems
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<int, CoordinateSystem>> GetEnumerator()
        {
            _initialization.WaitOne();
            return _csBySrid.GetEnumerator();
        }

        #region Interface Methods

        /// <summary>
        /// Adds a coordinate system to the dictionary based on srid
        /// </summary>
        /// <param name="srid"></param>
        /// <param name="coordinateSystem"></param>
        public void AddCoordinateSystem(int srid, CoordinateSystem coordinateSystem)
        {
            lock (((IDictionary)_csBySrid).SyncRoot)
            {
                lock (((IDictionary)_sridByCs).SyncRoot)
                {
                    if (_sridByCs.ContainsKey(coordinateSystem))
                        return;

                    if (_csBySrid.ContainsKey(srid))
                    {
                        if (ReferenceEquals(coordinateSystem, _csBySrid[srid]))
                            return;

                        _sridByCs.Remove(_csBySrid[srid]);
                        _csBySrid[srid] = coordinateSystem;
                        _sridByCs.Add(coordinateSystem, srid);
                    }
                    else
                    {
                        _csBySrid.Add(srid, coordinateSystem);
                        _sridByCs.Add(coordinateSystem, srid);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a coordinate system to the dictionary.
        /// The authority code is defaulted as the srid.
        /// </summary>
        /// <param name="coordinateSystem"></param>
        /// <returns></returns>
        public virtual int AddCoordinateSystem(CoordinateSystem coordinateSystem)
        {
            int srid = (int)coordinateSystem.AuthorityCode;
            AddCoordinateSystem(srid, coordinateSystem);

            return srid;
        }

        /// <summary>
        /// Returns the coordinate system by <paramref name="srid" /> identifier
        /// </summary>
        /// <param name="srid">The initialization for the coordinate system</param>
        /// <returns>The coordinate system.</returns>
        public CoordinateSystem GetCoordinateSystem(int srid)
        {
            _initialization.WaitOne();
            return _csBySrid.TryGetValue(srid, out var cs) ? cs : null;
        }

        /// <summary>
        /// Returns the coordinate system by <paramref name="authority" /> and <paramref name="code" />.
        /// </summary>
        /// <param name="authority">The authority for the coordinate system</param>
        /// <param name="code">The code assigned to the coordinate system by <paramref name="authority" />.</param>
        /// <returns>The coordinate system.</returns>
        public CoordinateSystem GetCoordinateSystem(string authority, long code)
        {
            int? srid = GetSRID(authority, code);
            if (srid.HasValue)
                return GetCoordinateSystem(srid.Value);
            return null;
        }

        /// <summary>
        /// Method to get the identifier, by which this coordinate system can be accessed.
        /// </summary>
        /// <param name="authority">The authority name</param>
        /// <param name="authorityCode">The code assigned by <paramref name="authority" /></param>
        /// <returns>The identifier or <value>null</value></returns>
        public int? GetSRID(string authority, long authorityCode)
        {
            var key = new CoordinateSystemKey(authority, authorityCode);
            int srid;
            _initialization.WaitOne();
            if (_sridByCs.TryGetValue(key, out srid))
                return srid;

            return null;
        }


        /// <summary>
        /// Returns number of coordinate systems registered by the service
        /// </summary>
        public int Count
        {
            get
            {
                _initialization.WaitOne();
                return _sridByCs.Count;
            }
        }
        #endregion
    }
}
