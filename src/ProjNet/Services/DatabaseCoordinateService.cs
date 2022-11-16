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
    internal class DatabaseCoordinateService : ICoordinateSystemService
    {
        private readonly DatabaseProvider _dbProvider;
        private readonly CoordinateSystemServices _css;

        /// <summary>
        /// Initializes a DatabaseCoordinateService to retreive 
        /// Coordinate Systems from an internal databse
        /// </summary>
        public DatabaseCoordinateService(CoordinateSystemServices css)
        {
            _css = css;
            _dbProvider = new DatabaseProvider();
        }

        /// <summary>
        /// Returns the coordinate system by <paramref name="srid" /> identifier
        /// </summary>
        /// <param name="srid">The initialization for the coordinate system</param>
        /// <returns>The coordinate system.</returns>
        public CoordinateSystem GetCoordinateSystem(int srid)
        {
            var sysInfo = _dbProvider.GetCoordinateSystemInfo(srid).Result;
            return _css.CreateFromWkt(sysInfo.WKT);
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
            return (int)authorityCode;
        }

        public int Count => _dbProvider.GetCount().Result;

        public void AddCoordinateSystem(int srid, CoordinateSystem coordinateSystem)
        {
            // At the moment, the database does not support srids
            // (i.e., it is keyed by the code and there cannot be the same code from multiple authorities)
            AddCoordinateSystem(coordinateSystem);
        }

        public int AddCoordinateSystem(CoordinateSystem coordinateSystem)
        {
            return _dbProvider.AddCoordinateSystem(coordinateSystem).Result;
        }
    }
}
