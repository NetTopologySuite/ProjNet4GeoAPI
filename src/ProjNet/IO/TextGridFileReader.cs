
namespace ProjNet.IO
{
    using ProjNet.NTv2;
    using System;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// Reads a text grid file (NTv2).
    /// </summary>
    class TextGridFileReader
    {
        const int EXPECTED_OREC = 11;
        const int EXPECTED_SREC = 11;

        /// <summary>
        /// Reads a text grid file (.gsa).
        /// </summary>
        /// <param name="file">The text grid file.</param>
        /// <returns>The grid file.</returns>
        public GridFile Read(string file)
        {
            using (var stream = File.OpenRead(file))
            {
                return Read(stream);
            }
        }

        /// <summary>
        /// Reads a grid file from given stream.
        /// </summary>
        /// <param name="stream">The stream containing the text grid data.</param>
        /// <returns>The grid file.</returns>
        public GridFile Read(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var header = ReadHeader(reader);

                var g = new GridFile(header);

                for (int i = 0; i < header.NUM_FILE; i++)
                {
                    var s = ReadGridHeader(reader);

                    g.grids.Add(ReadGrid(reader, s));
                }

                return g;
            }
        }

        private GridFileHeader ReadHeader(StreamReader reader)
        {
            var header = new GridFileHeader();

            int i = 0;

            string line;

            while (i < EXPECTED_OREC && (line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                string key = line.Substring(0, 8).Trim().ToUpperInvariant();

                string value = line.Substring(8).Trim();

                var format = CultureInfo.InvariantCulture.NumberFormat;

                switch (key)
                {
                    case "NUM_OREC":
                        header.NUM_OREC = int.Parse(value);
                        if (header.NUM_OREC != EXPECTED_OREC)
                        {
                            throw new FormatException("Invalid grid header (expected NUM_OREC = 11).");
                        }
                        break;
                    case "NUM_SREC":
                        header.NUM_SREC = int.Parse(value);
                        if (header.NUM_SREC != EXPECTED_SREC)
                        {
                            throw new FormatException("Invalid grid header (expected NUM_SREC = 11).");
                        }
                        break;
                    case "NUM_FILE":
                        header.NUM_FILE = int.Parse(value);
                        break;
                    case "GS_TYPE":
                        header.GS_TYPE = value;
                        break;
                    case "VERSION":
                        header.VERSION = value;
                        break;
                    case "SYSTEM_F":
                        header.SYSTEM_F = value;
                        break;
                    case "SYSTEM_T":
                        header.SYSTEM_T = value;
                        break;
                    case "MAJOR_F":
                        header.MAJOR_F = double.Parse(value, format);
                        break;
                    case "MINOR_F":
                        header.MINOR_F = double.Parse(value, format);
                        break;
                    case "MAJOR_T":
                        header.MAJOR_T = double.Parse(value, format);
                        break;
                    case "MINOR_T":
                        header.MINOR_T = double.Parse(value, format);
                        break;
                    default:
                        break;
                }

                i++;
            }

            if (i != EXPECTED_OREC)
            {
                throw new FormatException("Invalid grid header.");
            }

            return header;
        }

        private GridHeader ReadGridHeader(StreamReader reader)
        {
            var header = new GridHeader();

            int i = 0;

            string line;

            var format = CultureInfo.InvariantCulture.NumberFormat;

            while (i < EXPECTED_SREC && (line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                string key = line.Substring(0, 8).Trim().ToUpperInvariant();

                string value = line.Substring(8).Trim();

                switch (key)
                {
                    case "SUB_NAME":
                        header.SUB_NAME = value;
                        break;
                    case "PARENT":
                        header.PARENT = value;
                        break;
                    case "CREATED":
                        header.CREATED = value;
                        break;
                    case "UPDATED":
                        header.UPDATED = value;
                        break;
                    case "S_LAT":
                        header.S_LAT = double.Parse(value, format);
                        break;
                    case "N_LAT":
                        header.N_LAT = double.Parse(value, format);
                        break;
                    case "E_LONG":
                        header.E_LONG = double.Parse(value, format);
                        break;
                    case "W_LONG":
                        header.W_LONG = double.Parse(value, format);
                        break;
                    case "LAT_INC":
                        header.LAT_INC = double.Parse(value, format);
                        break;
                    case "LONG_INC":
                        header.LONG_INC = double.Parse(value, format);
                        break;
                    case "GS_COUNT":
                        header.GS_COUNT = int.Parse(value);
                        break;
                    default:
                        break;
                }

                i++;
            }

            if (i != EXPECTED_SREC)
            {
                throw new FormatException("Invalid sub-grid header.");
            }

            return header;
        }

        private Grid ReadGrid(StreamReader reader, GridHeader header, bool accuracies = false)
        {
            int i = 0;
            int count = header.GS_COUNT;

            var grid = new Grid(header, accuracies);

            string line;

            char[] separator = new char[] { ' ' };

            ShiftRecord record;

            var format = CultureInfo.InvariantCulture.NumberFormat;

            while (i < count && (line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                var s = line.Split(separator, StringSplitOptions.RemoveEmptyEntries);

                record.Item1 = float.Parse(s[0], format);
                record.Item2 = float.Parse(s[1], format);

                grid.shifts[i] = record;

                if (accuracies && s.Length > 2)
                {
                    record.Item1 = float.Parse(s[2], format);
                    record.Item2 = float.Parse(s[3], format);

                    grid.accuracies[i] = record;
                }

                i++;
            }

            return grid;
        }
    }
}
