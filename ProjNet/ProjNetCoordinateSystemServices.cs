using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using GeoAPI;
using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using ProjNet.CoordinateSystems;

namespace ProjNet
{
    public class CoordinateSystemServices : ICoordinateSystemServices
    {
        private readonly Dictionary<int, ICoordinateSystem> _csBySrid;
        private readonly Dictionary<IInfo, int> _sridByCs;

        private readonly ICoordinateSystemFactory _coordinateSystemFactory;
        private readonly ICoordinateTransformationFactory _ctFactory;
        
        private readonly ManualResetEvent _initialization = new ManualResetEvent(false);

        #region CsEqualityComparer class
        private class CsEqualityComparer : EqualityComparer<IInfo>
        {
            public override bool Equals(IInfo x, IInfo y)
            {
                return x.AuthorityCode == y.AuthorityCode &&
#if PCL
                    string.Compare(x.Authority, y.Authority, CultureInfo.InvariantCulture, CompareOptions.OrdinalIgnoreCase) == 0;
#else
                    string.Compare(x.Authority, y.Authority, true, CultureInfo.InvariantCulture) == 0;
#endif
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

        public CoordinateSystemServices(ICoordinateSystemFactory coordinateSystemFactory,
            ICoordinateTransformationFactory coordinateTransformationFactory, System.Threading.WaitCallback inititalization = null, object arguments = null)
        {
            if (coordinateSystemFactory == null)
                throw new ArgumentNullException("coordinateSystemFactory");

            if (coordinateTransformationFactory == null)
                throw new ArgumentNullException("coordinateTransformationFactory");

            _csBySrid = new Dictionary<int, ICoordinateSystem>();
            _sridByCs = new Dictionary<IInfo, int>(new CsEqualityComparer());

            inititalization = inititalization ?? DefaultInitialization;

            _coordinateSystemFactory = coordinateSystemFactory;
            _ctFactory = coordinateTransformationFactory;

            ThreadPool.QueueUserWorkItem(inititalization, CreateArguments(arguments));
        }

        private object[] CreateArguments(object arguments)
        {
            var res = new List<object>();
            res.Add(this);

            if (arguments != null)
            {
                var argumentArray = arguments as IEnumerable<object>;
                if (argumentArray != null)
                    res.AddRange(argumentArray);
                else
                    res.Add(arguments);
            }
            return res.ToArray();
        }

        private ICoordinateSystem CreateCoordinateSystem(string wkt)
        {
            try
            {
                return _coordinateSystemFactory.CreateFromWkt(wkt.Replace("ELLIPSOID", "SPHEROID"));
            }
            catch (Exception)
            {
                // as a fallback we ignore projections not supported
                return null;
            }
        }

        public static void DefaultInitialization(object parameter)
        {
            var paras = (object[])parameter;
            var css = (CoordinateSystemServices)paras[0];

            css.AddCoordinateSystem(4326, GeographicCoordinateSystem.WGS84);
            css.AddCoordinateSystem(3857, ProjectedCoordinateSystem.WebMercator);

            css._initialization.Set();
        }

        public static void LoadXml(object parameter)
        {
            var paras = (object[])parameter;
            var css = (CoordinateSystemServices)paras[0];

#if !PCL && DEBUG
            Console.WriteLine("Reading SpatialRefSys.xml");
            var sw = new Stopwatch();
            sw.Start();
#endif

            var document = XDocument.Load((Stream) paras[1]);

            var rs = from tmp in document.Elements("SpatialReference").Elements("ReferenceSystem") select tmp;

            foreach (var node in rs)
            {
                var sridElement = node.Element("SRID");
                if (sridElement != null)
                {
                    var srid = int.Parse(sridElement.Value);
                    var cs = css.CreateCoordinateSystem(node.LastNode.ToString());

                    if (cs != null)
                    {
                        css.AddCoordinateSystem(srid, cs);
                    }
                    else
                    {
                        Debug.WriteLine("SRID {0} not supported", srid);
                    }
                }
            }
#if !PCL && DEBUG
            sw.Stop();
            Console.WriteLine("Read SpatialRefSys.xml in {0:N0}ms", sw.ElapsedMilliseconds);
#endif
            css._initialization.Set();
        }

        public ICoordinateSystem GetCoordinateSystem(int srid)
        {
            ICoordinateSystem cs;
            _initialization.WaitOne();
            return _csBySrid.TryGetValue(srid, out cs) ? cs : null;
        }

        public ICoordinateSystem GetCoordinateSystem(string authority, long code)
        {
            var srid = GetSRID(authority, code);
            if (srid.HasValue)
                return GetCoordinateSystem(srid.Value);
            return null;
        }

        public int? GetSRID(string authority, long authorityCode)
        {
            var key = new CoordinateSystemKey(authority, authorityCode);
            int srid;
            _initialization.WaitOne();
            if (_sridByCs.TryGetValue(key, out srid))
                return srid;

            return null;
        }

        public ICoordinateTransformation CreateTransformation(int sourceSrid, int targetSrid)
        {
            return CreateTransformation(GetCoordinateSystem(sourceSrid),
                GetCoordinateSystem(targetSrid));
        }

        public ICoordinateTransformation CreateTransformation(ICoordinateSystem src, ICoordinateSystem tgt)
        {
            return _ctFactory.CreateFromCoordinateSystems(src, tgt);
        }

        [Obsolete]
        public string GetCoordinateSystemInitializationString(int srid)
        {
            ICoordinateSystem cs;
            if (_csBySrid.TryGetValue(srid, out cs))
                return cs.WKT;
            throw new ArgumentOutOfRangeException("srid");
        }

        [Obsolete]
        public ICoordinateSystemFactory CoordinateSystemFactory
        {
            get { return _coordinateSystemFactory; }
        }

        [Obsolete]
        public ICoordinateTransformationFactory CoordinateTransformationFactory
        {
            get { return _ctFactory; }
        }

        protected void AddCoordinateSystem(int srid, ICoordinateSystem coordinateSystem)
        {
            lock (((IDictionary) _csBySrid).SyncRoot)
            {
                lock (((IDictionary) _sridByCs).SyncRoot)
                {
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

        protected virtual int AddCoordinateSystem(ICoordinateSystem coordinateSystem)
        {
            var srid = (int) coordinateSystem.AuthorityCode;
            AddCoordinateSystem(srid, coordinateSystem);

            return srid;
        }

        protected void Clear()
        {
            _csBySrid.Clear();
        }

        protected int Count
        {
            get
            {
                _initialization.WaitOne();
                return _sridByCs.Count;
            }
        }

        public bool RemoveCoordinateSystem(int srid)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<KeyValuePair<int, ICoordinateSystem>> GetEnumerator()
        {
            _initialization.WaitOne();
            return _csBySrid.GetEnumerator();
        }
    }
}