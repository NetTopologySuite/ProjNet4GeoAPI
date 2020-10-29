using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ProjNet.CoordinateSystems;

namespace ProjNET.Tests
{
    internal class SRIDReader
    {
        private static readonly Lazy<CoordinateSystemFactory> csFactory = 
            new Lazy<CoordinateSystemFactory>(() => new CoordinateSystemFactory());

        private static Dictionary<int, string> sridCache;

        /// <summary>
        /// Enumerates all SRID's in the SRID.csv file.
        /// </summary>
        /// <returns>Enumerator</returns>
        public static Dictionary<int, string> GetSrids(string filename = null)
        {
            var stream = string.IsNullOrWhiteSpace(filename)
                ? Assembly.GetExecutingAssembly().GetManifestResourceStream("ProjNET.Tests.SRID.csv")
                : File.OpenRead(filename);

            var result = new Dictionary<int, string>();

            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    int split = line.IndexOf(';');
                    if (split <= -1) continue;

                    int id = int.Parse(line.Substring(0, split));

                    result[id] = line.Substring(split + 1);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a coordinate system from the SRID.csv file
        /// </summary>
        /// <param name="id">EPSG ID</param>
        /// <param name="file">(optional) path to CSV File with WKT definitions.</param>
        /// <returns>Coordinate system, or <value>null</value> if no entry with <paramref name="id"/> was not found.</returns>
        public static CoordinateSystem GetCSbyID(int id, string file = null)
        {
            if (sridCache == null)
            {
                sridCache = GetSrids(file);
            }

            if (!sridCache.TryGetValue(id, out string wkt))
            {
                return null;
            }

            return csFactory.Value.CreateFromWkt(wkt);
        }
    }
}
