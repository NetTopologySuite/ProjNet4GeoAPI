using ProjNet.CoordinateSystems;
using ProjNet.IO.CoordinateSystems;
using ProjNet.Sqlite.Extensions;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProjNet.IO
{
    /// <summary>
    /// Handles calls to the database
    /// </summary>
    public class DatabaseProvider
    {
        private string _connectionString;
        SQLiteAsyncConnection Database;

        /// <summary>
        /// Creates an instance of the DatabaseProvider
        /// </summary>
        public DatabaseProvider()
        {
            string currentDir = AppContext.BaseDirectory;
            _connectionString = Path.Combine(currentDir, "proj.db");
        }

        /// <summary>
        /// Creates an instance of the DatabaseProvider
        /// </summary>
        public DatabaseProvider(string database)
        {
            _connectionString = database;
        }

        /// <summary>
        /// Initializes the Database 
        /// </summary>
        /// <returns></returns>
        async Task Init()
        {
            if (Database is not null)
                return;

            Database = new SQLiteAsyncConnection(_connectionString, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);
            var result = await Database.CreateTableAsync<CoordinateSystemInfo>();
        }

        /// <summary>
        /// Returns the coordinate info base on the code
        /// </summary>
        /// <param name="srid">the code to retrieve</param>
        public async Task<CoordinateSystemInfo> GetCoordinateSystemInfo(int srid)
        {
            await Init();
            return await Database.Table<CoordinateSystemInfo>().Where(i => i.Code == srid).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Searches the table names based on an expression
        /// </summary>
        /// <param name="searchKey"></param>
        public async Task<IEnumerable<CoordinateSystemInfo>> SearchCoordinateSystemAsync(string searchKey)
        {
            await Init();
            var filters = searchKey.ToLower().Split(' ').ToList();
            
            return await Like(filters);
       }

        private async Task<IEnumerable<CoordinateSystemInfo>> Like(List<string> filters)
        {
            var data =await Database.Table<CoordinateSystemInfo>().ToArrayAsync();
            foreach (string filter in filters)
                data = data.Where(x => x.Name.ToLower().Contains(filter)).ToArray();

            return data;
        }

            /// <summary>
            /// Returns the number of entries in the database
            /// </summary>
            internal async Task<int> GetCount()
        {
            await Init();
            return await Database.Table<CoordinateSystemInfo>().CountAsync();
        }

        /// <summary>
        /// Adds a coordinate system to the database
        /// </summary>
        internal async Task<int> AddCoordinateSystem(CoordinateSystem coordinateSystem)
        {
            var coordType = GetSystemType(coordinateSystem.WKT);
            var coordInfo = new CoordinateSystemInfo(coordinateSystem.Name, coordinateSystem.Alias,
                coordinateSystem.Authority, (int)coordinateSystem.AuthorityCode,
                coordType.ToString(), false, coordinateSystem.WKT);

            return await AddCoordinateSystem(coordInfo);
        }

        internal async Task<int> AddCoordinateSystem(CoordinateSystemInfo coordinateInfo)
        {
            await Init();
            return await Database.InsertAsync(coordinateInfo);
        }

        /// <summary>
        /// Removes a coordninate system from the database
        /// </summary>

        internal async Task<bool> RemoveCoordinateSystem(int srid)
        {
            await Init();
            var csInfo = await GetCoordinateSystemInfo(srid);
            if (csInfo != null)
                return await Database.DeleteAsync(csInfo) > 0;

            return false;
        }

        /// <summary>
        /// Parses the wkt to get the appropriate CoordinateSystemType
        /// </summary>
        private CoordinateSystemType GetSystemType(string wkt)
        {
            int bracket = wkt.IndexOf("[");
            if (bracket >= 0)
            {
                string coordType = wkt.Substring(0, bracket);
                switch (coordType)
                {
                    case "PROJCS":
                        return CoordinateSystemType.projected;
                    case "GEOGCS":
                        return CoordinateSystemType.geographic2D;
                    case "COMPD_CS":
                        return CoordinateSystemType.compound;
                    case "VERT_CS":
                        return CoordinateSystemType.vertical;
                }
            }

            return CoordinateSystemType.unknown;
        }


    }
}
