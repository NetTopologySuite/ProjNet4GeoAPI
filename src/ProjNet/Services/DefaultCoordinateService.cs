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
    internal class DefaultCoordinateService : ICoordinateSystemService
    {
        private readonly Dictionary<int, CoordinateSystem> _csBySrid;
        private readonly Dictionary<IInfo, int> _sridByCs;
        private readonly ManualResetEvent _initialization = new ManualResetEvent(false);
        private CoordinateSystemServices _css;


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

        public DefaultCoordinateService(CoordinateSystemServices css, IEnumerable<KeyValuePair<int, string>> enumeration)
        {
            _css = css;
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
            _initialization.Set();
        }

        private void Initialize(IEnumerable<KeyValuePair<int, CoordinateSystem>> enumeration)
        {
            FromEnumeration(enumeration);
            _initialization.Set();
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
                return _css.CreateFromWkt(wkt.Replace("ELLIPSOID", "SPHEROID"));
            }
            catch (Exception)
            {
                // as a fallback we ignore projections not supported
                return null;
            }
        }

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

        public virtual int AddCoordinateSystem(CoordinateSystem coordinateSystem)
        {
            int srid = (int)coordinateSystem.AuthorityCode;
            AddCoordinateSystem(srid, coordinateSystem);

            return srid;
        }

        protected void Clear()
        {
            _csBySrid.Clear();
        }

        public int Count
        {
            get
            {
                _initialization.WaitOne();
                return _sridByCs.Count;
            }
        }

        #region Public Methods

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

        public IEnumerator<KeyValuePair<int, CoordinateSystem>> GetEnumerator()
        {
            _initialization.WaitOne();
            return _csBySrid.GetEnumerator();
        }
        #endregion
    }
}
