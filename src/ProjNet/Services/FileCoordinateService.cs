using ProjNet.CoordinateSystems;
using ProjNet.IO.CoordinateSystems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ProjNet.Services
{
    /// <summary>
    /// Creates a CoordinateService instantiated from a file
    /// </summary>
    internal class FileCoordinateService : DefaultCoordinateService
    {
        public FileCoordinateService(string filename)
           : base(new CoordinateSystemFactory(), FileInitialization(filename))
        {


        }

        public FileCoordinateService(string filename, char delimiter, CsvDefinition definition)
           : base(new CoordinateSystemFactory(), FileInitialization(filename, delimiter, definition))
        {


        }

        public FileCoordinateService(CoordinateSystemFactory csFactory, string filename)
            : base(csFactory, FileInitialization(filename))
        {
        }

        /// <summary>
        /// Initializes the CoordinateSystemService from a csv file
        /// </summary>
        /// <param name="filename">the csv file without a header, code in column 1, wkt in column 2</param>
        /// <returns></returns>
        private static IEnumerable<KeyValuePair<int, CoordinateSystem>> FileInitialization(string filename)
        {
            var definition = new CsvDefinition()
            {
                HasHeader = false,
                Code = 0,
                WKT = 1
            };

            return FileInitialization(filename, ';', definition);
        }

        /// <summary>
        /// Initializes the CoordinateSystemService from a csv file
        /// </summary>
        /// <param name="filename">the csv file with a header and 7 columns as follows:
        /// code, authority, name, alias, coordinateType, isDeprecated (True/False), and wkt string</param>
        /// <param name="delimiter">Character to delimate the csv</param>
        /// <param name="definition">The definition of csv columns in the file</param>
        /// <returns></returns>
        private static IEnumerable<KeyValuePair<int, CoordinateSystem>> FileInitialization(string filename, char delimiter, CsvDefinition definition)
        {
            using (Stream stream = File.OpenRead(filename))
            {
                if (filename.ToLower().EndsWith("csv"))
                {
                    var data = ParseCsvStream(stream, delimiter, definition);
                    foreach (var system in data)
                    {
                        yield return new KeyValuePair<int, CoordinateSystem>(system.Key, system.Value);
                    }
                }
                else
                    throw new NotSupportedException("File format is not supported.");
            }

            InitializationSet();
        }

        /// <summary>
        /// Parses a stream from a csv file
        /// </summary>
        /// <param name="stream"></param>
        /// /// <param name="delimiter">Character to delimate the csv</param>
        /// <param name="definition">The definition of csv columns in the file</param>
        /// <returns></returns>
        public static Dictionary<int, CoordinateSystem> ParseCsvStream(Stream stream, char delimiter, CsvDefinition definition)
        {
            var keyValue = new Dictionary<int, CoordinateSystem>();
            string regex = "[,]{1}(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))";
            regex = regex.Replace(',', delimiter);

            if (definition.Code < 0)
                throw new ArgumentException("Code column index cannot be less that 0");
            if (definition.WKT < 0)
                throw new ArgumentException("WKT column index cannot be less that 0");

            using (var sr = new StreamReader(stream))
            {

                if (definition.HasHeader)
                    _ = sr.ReadLine();

                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] contents = Regex.Split(line, regex);

                    int code = int.Parse(contents[definition.Code]);
                    string wkt = contents[definition.WKT];
                    string authority = string.Empty;
                    string name = string.Empty;
                    string alias = string.Empty;
                    string coordType = string.Empty;
                    string isDeprecated = string.Empty;

                    if (definition.Authority > -1)
                        authority = contents[definition.Authority];
                    if (definition.Name > -1)
                        name = contents[definition.Name];
                    if (definition.Alias > -1)
                        alias = contents[definition.Alias];
                    if (definition.SystemType > -1)
                        coordType = contents[definition.SystemType];
                    if (definition.IsDeprecated > -1)
                        isDeprecated = contents[definition.IsDeprecated];

                    wkt = wkt.Trim('"');

                    var cs = CoordinateSystemWktReader.Parse(wkt) as CoordinateSystem;
                    if (cs != null)
                    {
                        cs.Name = name;
                        cs.Alias = alias;
                        cs.Authority = authority;
                        //cs.Remarks = isDeprecated;
                        cs.Remarks = coordType;
                    }

                    keyValue[code] = cs;
                }
            }

            return keyValue;
        }

        /// <summary>
        /// Default properties to associate a column index to
        /// </summary>
        public class CsvDefinition
        {
            public bool HasHeader { get; set; }
            public int Authority { get; set; } = -1;
            public int Code { get; set; } = -1;
            public int Name { get; set; } = -1;
            public int Alias { get; set; } = -1;
            public int IsDeprecated { get; set; } = -1;
            public int SystemType { get; set; } = -1;
            public int WKT { get; set; } = -1;
        }
    }
}
