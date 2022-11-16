using ProjNet.CoordinateSystems;
using ProjNet.IO.CoordinateSystems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ProjNet.Services
{
    /// <summary>
    /// Creates a CoordinateService initiated from a file
    /// </summary>
    internal class FileCoordinateService : DefaultCoordinateService
    {
        private readonly ManualResetEvent _initialization = new ManualResetEvent(false);

        public FileCoordinateService(CoordinateSystemServices css, string filename)
            : base(css, null)
        {
            _initialization = new ManualResetEvent(false);
            Task.Run(() => FileInitialization(filename));
        }

        /// <summary>
        /// Initializes the CoordinateSystemService from a csv file
        /// </summary>
        /// <param name="filename">the csv file with a header and 7 columns as follows:
        /// code, authority, name, alias, coordinateType, isDeprecated (True/False), and wkt string</param>
        /// <returns></returns>
        private  IEnumerable<KeyValuePair<int, CoordinateSystem>> FileInitialization(string filename)
        {
            using (Stream stream = File.OpenRead(filename))
            {
                var data = ParseCsvStream(stream);
                foreach (var system in data)
                {
                    var csInfo = system.Value;
                    CoordinateSystem cs = CoordinateSystemWktReader.Parse(csInfo.WKT) as CoordinateSystem;
                    yield return new KeyValuePair<int, CoordinateSystem>(system.Value.Code, cs);
                }
            }

            _initialization.Set();
        }

        /// <summary>
        /// Parses a stream from a csv file
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static Dictionary<string, IO.CoordinateSystemInfo> ParseCsvStream(Stream s)
        {
            var epsgDatabase = new Dictionary<string, IO.CoordinateSystemInfo>();
            using (var sr = new StreamReader(s))
            {
                var header = sr.ReadLine();

                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    var contents = Regex.Split(line, "[,]{1}(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

                    int code = int.Parse(contents[0]);
                    string authority = contents[1];
                    string name = contents[2];
                    string alias = contents[3];
                    string systemType = contents[4];
                    bool isDeprecated = bool.Parse(contents[5]);
                    string wkt = contents[6];

                    var info = new IO.CoordinateSystemInfo(name,alias, authority, code, systemType, isDeprecated, wkt);
                    epsgDatabase[name] = info;
                }
            }

            return epsgDatabase;
        }
    }
}
