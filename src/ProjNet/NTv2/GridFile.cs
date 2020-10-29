
namespace ProjNet.NTv2
{
    using ProjNet.IO;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using static MathHelper;

    /// <summary>
    /// Represents NTv2 grid file.
    /// </summary>
    public class GridFile
    {
        /// <summary>
        /// Gets the raw grid file header.
        /// </summary>
        public GridFileHeader Header { get; private set; }

        internal List<Grid> grids;

        internal double hdr_conv; // Header conversion factor
        internal double dat_conv; // Data conversion factor

        /* These are the mins and maxes across all sub-files */

        internal double lat_min; // Latitude  min (degrees)
        internal double lat_max; // Latitude  max (degrees)
        internal double lon_min; // Longitude min (degrees)
        internal double lon_max; // Longitude max (degrees)

        /// <summary>
        /// Read a NTv2 grid file.
        /// </summary>
        /// <param name="file">The grid file path.</param>
        /// <returns>The grid file.</returns>
        /// <remarks>
        /// Binary mode if filename ends with .gsb, otherwise text mode.
        /// </remarks>
        public static GridFile Open(string file)
        {
            return Open(file, file.EndsWith(".gsb", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Read a NTv2 grid file.
        /// </summary>
        /// <param name="file">The grid file path.</param>
        /// <param name="binaryFormat">Indicates whether the file should be opened as binary.</param>
        /// <returns>The grid file.</returns>
        public static GridFile Open(string file, bool binaryFormat)
        {
            using (var stream = File.OpenRead(file))
            {
                return Open(stream, binaryFormat);
            }
        }

        /// <summary>
        /// Read a NTv2 grid file from given stream.
        /// </summary>
        /// <param name="stream">The grid file stream.</param>
        /// <param name="binaryFormat">Indicates whether the file should be opened as binary.</param>
        /// <returns>The grid file.</returns>
        public static GridFile Open(Stream stream, bool binaryFormat)
        {
            GridFile g;

            if (binaryFormat)
            {
                g = new BinaryGridFileReader().Read(stream);
            }
            else
            {
                g = new TextGridFileReader().Read(stream);
            }

            g.ProcessGrids();

            return g;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridFile"/> class.
        /// </summary>
        /// <param name="header"></param>
        internal GridFile(GridFileHeader header)
        {
            Header = header;

            GetConversion(header.GS_TYPE);

            grids = new List<Grid>();
        }

        /// <summary>
        /// Transform the given coordinates.
        /// </summary>
        /// <param name="lon">The longitude.</param>
        /// <param name="lat">The latitude.</param>
        /// <param name="inverse">Indicates whether to use the inverse transform.</param>
        /// <returns>Returns true, if the grid transformation succeeded, otherwise false.</returns>
        public bool Transform(ref double lon, ref double lat, bool inverse)
        {
            double qlon = lon;
            double qlat = lat;

            // The parent grid that contains the coordinate.
            var grid = grids.Where(g => g.parent == null && g.Contains(qlon, qlat)).FirstOrDefault();

            if (grid == null)
            {
                return false;
            }

            grid = SearchSubGrid(grid, qlon, qlat);

            if (inverse)
            {
                return Inverse(grid, ref lon, ref lat);
            }
            else
            {
                return Forward(grid, ref lon, ref lat);
            }
        }

        private bool Forward(Grid grid, ref double lon, ref double lat)
        {
            double slat = 0.0;
            double slon = 0.0;

            if (!GetShift(lon, lat, grid, ref slon, ref slat))
            {
                return false;
            }

            lat += slat;
            lon += slon;

            return true;
        }

        private bool Inverse(Grid grid, ref double lon, ref double lat)
        {
            // Find the inverse shift using a fixed point iteration.
            const int MAX_ITERATIONS = 10;

            double qlat = lat;
            double qlon = lon;

            double slat = 0.0;
            double slon = 0.0;

            double dlon, dlat; // deltas

            for (int i = 0; i < MAX_ITERATIONS; i++)
            {
                if (!GetShift(qlon, qlat, grid, ref slon, ref slat))
                {
                    return false;
                }

                dlon = (qlon + slon) - lon;
                dlat = (qlat + slat) - lat;

                if (AlmostZero(dlon) && AlmostZero(dlat))
                {
                    break;
                }

                qlon = (qlon - dlon);
                qlat = (qlat - dlat);

                // TODO: check if the grid is still valid
                //       since the coordinate might be shifted to a new sub-grid.
            }

            lat = qlat;
            lon = qlon;

            return true;
        }

        private bool GetShift(double lon, double lat, Grid grid, ref double slon, ref double slat)
        {
            double lat_min = grid.lat_min;
            double lat_max = grid.lat_max;
            double lat_inc = grid.lat_inc;

            double lon_min = grid.lon_min;
            double lon_max = grid.lon_max;
            double lon_inc = grid.lon_inc;

            if (lon < lon_min || lon > lon_max || lat < lat_min || lat > lat_max)
            {
                return false;
            }

            // See BeTA2007 doc page 9 and 10.

            double fcol = (lon_max - lon) / lon_inc;
            double frow = (lat - lat_min) / lat_inc;

            int col = (int)Math.Floor(fcol);
            int row = (int)Math.Floor(frow);
            int ppr = (int)Math.Floor((lon_max - lon_min) / lon_inc + 0.5) + 1;
            int ppc = (int)Math.Floor((lat_max - lat_min) / lat_inc + 0.5) + 1;

            int se = row * ppr + col;
            int sw = se + 1;
            int ne = se + ppr;
            int nw = ne + 1;

            if (col >= ppr - 1)
            {
                // Coordinate is on the West border.
                sw = se;
                nw = sw;
            }

            if (row >= ppc - 1)
            {
                // Coordinate is on the North border.
                ne = se;
                nw = sw;
            }

            double dx = fcol - Math.Floor(fcol);
            double dy = frow - Math.Floor(frow);

            var sse = grid.shifts[se];
            var ssw = grid.shifts[sw];
            var sne = grid.shifts[ne];
            var snw = grid.shifts[nw];

            slat = (1.0 - dx) * (1.0 - dy) * sse.Item1 + dx * (1.0 - dy) * ssw.Item1 + (1.0 - dx) * dy * sne.Item1 + dx * dy * snw.Item1;
            slon = (1.0 - dx) * (1.0 - dy) * sse.Item2 + dx * (1.0 - dy) * ssw.Item2 + (1.0 - dx) * dy * sne.Item2 + dx * dy * snw.Item2;

            // The shift at this point is in decimal seconds, so convert to degrees.
            slat = slat * dat_conv / 3600.0;
            slon = -slon * dat_conv / 3600.0;

            return true;
        }

        private Grid SearchSubGrid(Grid grid, double qlon, double qlat)
        {
            var children = grid.Children;

            if (children.Count == 0)
            {
                return grid;
            }

            // Search in sub-grids. Since sub-grids should not overlap,
            // there is at most one match.
            var sub = children.Where(g => g.Contains(qlon, qlat)).FirstOrDefault();

            // TODO: need to take border conditions into account?

            if (sub == null)
            {
                return grid;
            }

            return SearchSubGrid(sub, qlon, qlat);
        }

        /// <summary>
        /// Set parent and children for each grid.
        /// </summary>
        private void ProcessGrids()
        {
            lon_min = lat_min = double.MaxValue;
            lon_max = lat_max = double.MinValue;

            var map = new Dictionary<string, Grid>();

            foreach (var g in grids)
            {
                map.Add(g.Header.SUB_NAME, g);
            }

            int id = 1;

            foreach (var g in grids)
            {
                g.Process(this, id++);

                string parent = g.Header.PARENT;

                if (parent.Equals("NONE"))
                {
                    continue;
                }

                if (!map.TryGetValue(parent, out Grid p))
                {
                    throw new Exception($"Unknown parent grid: {parent}");
                }

                g.parent = p;

                p.children.Add(g);
            }
        }

        private void GetConversion(string type)
        {
            if (type.Equals("SECONDS"))
            {
                hdr_conv = (1.0 / 3600.0);
                dat_conv = (1.0);
            }
            else if (type.Equals("MINUTES"))
            {
                hdr_conv = (1.0 / 60.0);
                dat_conv = (60.0);
            }
            else if (type.Equals("DEGREES"))
            {
                hdr_conv = (1.0);
                dat_conv = (3600.0);
            }
            else
            {
                throw new FormatException($"Invalid GS_TYPE specified: {type}.");
            }
        }
    }
}
