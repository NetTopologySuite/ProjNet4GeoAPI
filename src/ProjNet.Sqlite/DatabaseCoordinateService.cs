using ProjNet.CoordinateSystems;
using ProjNet.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProjNet.Services
{
    /// <summary>
    /// Coordinate System Service tied to a database
    /// </summary>
    public class DatabaseCoordinateService : CoordinateSystemService, ICoordinateSystemService
    {
        private readonly DatabaseProvider _dbProvider;

        /// <summary>
        /// Initializes a DatabaseCoordinateService to retreive 
        /// Coordinate Systems from an internal database
        /// </summary>
        public DatabaseCoordinateService(CoordinateSystemFactory csFactory)
            : base(csFactory)
        {
            _dbProvider = new DatabaseProvider();
        }

        /// <summary>
        /// Initializes a DatabaseCoordinateService to retreive 
        /// Coordinate Systems from an internal database
        /// </summary>
        public DatabaseCoordinateService()
            : this(new CoordinateSystemFactory())
        {
            _dbProvider = new DatabaseProvider();
        }

        #region Interface Methods
        /// <summary>
        /// Returns the coordinate system by <paramref name="srid" /> identifier
        /// </summary>
        /// <param name="srid">The initialization for the coordinate system</param>
        /// <returns>The coordinate system.</returns>
        public CoordinateSystem GetCoordinateSystem(int srid)
        {
            var sysInfo = _dbProvider.GetCoordinateSystemInfo(srid).Result;
            if (sysInfo != null)
                return CsFactory.CreateFromWkt(sysInfo.WKT);

            return null;
        }

        /// <summary>
        /// Returns the coordinate system by <paramref name="srid" /> identifier
        /// </summary>
        /// <param name="srid">The initialization for the coordinate system</param>
        /// <returns>The coordinate system.</returns>
        public async Task<CoordinateSystem> GetCoordinateSystemAsync(int srid)
        {
            var sysInfo = await _dbProvider.GetCoordinateSystemInfo(srid);
            if (sysInfo != null)
                return CsFactory.CreateFromWkt(sysInfo.WKT);

            return null;
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
        /// Searches the table names based on an expression
        /// </summary>
        /// <param name="name"></param>
        public Task<IEnumerable<CoordinateSystemInfo>> SearchCoordinateSystemAsync(string name)
        {
            return _dbProvider.SearchCoordinateSystemAsync(name);

        }

        /// <summary>
        /// Method to get the identifier, by which this coordinate system can be accessed.
        /// </summary>
        /// <param name="authority">The authority name</param>
        /// <param name="authorityCode">The code assigned by <paramref name="authority" /></param>
        /// <returns>The identifier or <value>null</value></returns>
        public int? GetSRID(string authority, long authorityCode)
        {
            return (int)authorityCode;
        }

        /// <summary>
        /// Returns number of coordinate systems registered by the service
        /// </summary>
        public int Count => _dbProvider.GetCount().Result;

        /// <summary>
        /// Adds a coordinate system to the dictionary based on srid
        /// </summary>
        /// <param name="srid"></param>
        /// <param name="coordinateSystem"></param>
        public void AddCoordinateSystem(int srid, CoordinateSystem coordinateSystem)
        {
            if (coordinateSystem.Name == null)
                throw new ArgumentNullException("Name is null.");
            if (coordinateSystem.AuthorityCode <= 0)
                coordinateSystem.AuthorityCode = srid;
            if (string.IsNullOrEmpty(coordinateSystem.Authority))
                coordinateSystem.Authority = "EPSG";
            if (string.IsNullOrEmpty(coordinateSystem.Alias))
                coordinateSystem.Alias = coordinateSystem.Name;

            AddCoordinateSystem(coordinateSystem);
        }

        /// <summary>
        /// Adds a coordinate system to the dictionary.
        /// The authority code is defaulted as the srid.
        /// </summary>
        /// <param name="coordinateSystem"></param>
        /// <returns></returns>
        public int AddCoordinateSystem(CoordinateSystem coordinateSystem)
        {
            return _dbProvider.AddCoordinateSystem(coordinateSystem).Result;
        }

        /// <summary>
        /// Removes a coordinate system from the database
        /// </summary>
        public bool RemoveCoordinateSystem(int srid)
        {
            return _dbProvider.RemoveCoordinateSystem(srid).Result;
        }
        #endregion
    }
}
