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
        private static readonly Lazy<CoordinateSystemFactory> CoordinateSystemFactory = 
            new Lazy<CoordinateSystemFactory>(() => new CoordinateSystemFactory());

        public struct WktString {
            /// <summary>
            /// Well-known ID
            /// </summary>
            public int WktId;
            /// <summary>
            /// Well-known Text
            /// </summary>
            public string Wkt;
        }

        /// <summary>
        /// Enumerates all SRID's in the SRID.csv file.
        /// </summary>
        /// <returns>Enumerator</returns>
        public static IEnumerable<WktString> GetSrids(string filename = null)
        {
            var stream = string.IsNullOrWhiteSpace(filename)
                ? Assembly.GetExecutingAssembly().GetManifestResourceStream("ProjNET.Tests.SRID.csv")
                : File.OpenRead(filename);

            using (var sr = new StreamReader(stream, Encoding.UTF8))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    int split = line.IndexOf(';');
                    if (split <= -1) continue;

                    var wkt = new WktString
                    {
                        WktId = int.Parse(line.Substring(0, split)),
                        Wkt = line.Substring(split + 1)
                    };
                    yield return wkt;
                }
            }
        }

        /// <summary>
        /// Gets a coordinate system from the SRID.csv file
        /// </summary>
        /// <param name="id">EPSG ID</param>
        /// <param name="file">(optional) path to CSV File with WKT definitions.</param>
        /// <returns>Coordinate system, or <value>null</value> if no entry with <paramref name="id"/> was not found.</returns>
        public static CoordinateSystem GetCSbyID(int id, string file = null)
        {
            //ICoordinateSystemFactory factory = new CoordinateSystemFactory();
            foreach (var wkt in GetSrids(file))
                if (wkt.WktId == id)
                    return CoordinateSystemFactory.Value.CreateFromWkt(wkt.Wkt);
            return null;
        }
    }
}
