
namespace ProjNet.NTv2
{
    using System;
    using System.Collections.Generic;
    using static MathHelper;

    /// <summary>
    /// A record holding either the shift values (lon/lat) or the corresponding accuracies.
    /// </summary>
    public struct ShiftRecord
    {
        /// <summary>
        /// Gets the first item (latitude shift).
        /// </summary>
        public float Item1;

        /// <summary>
        /// Gets the second item (longitude shift).
        /// </summary>
        public float Item2;
    }

    /// <summary>
    /// Represents a sub-grid in the grid file.
    /// </summary>
    public class Grid
    {
        /// <summary>
        /// Gets the raw sub-grid header.
        /// </summary>
        public GridHeader Header { get; }

        /// <summary>
        /// Gets the number of records.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Gets the number of grid rows.
        /// </summary>
        public int Rows { get; private set; }

        /// <summary>
        /// Gets the number of grid columns.
        /// </summary>
        public int Columns { get; private set; }

        /// <summary>
        /// Gets the parent grid (<c>null</c> if grid is top-level).
        /// </summary>
        public Grid Parent => parent;

        /// <summary>
        /// Gets the collection of sub-grids.
        /// </summary>
        public List<Grid> Children => children;

        internal Grid parent;
        internal List<Grid> children;

        internal int id; // Record id

        internal double lat_min; // Latitude  min (degrees)
        internal double lat_max; // Latitude  max (degrees)
        internal double lat_inc; // Latitude  inc (degrees)

        internal double lon_min; // Longitude min (degrees)
        internal double lon_max; // Longitude max (degrees)
        internal double lon_inc; // Longitude inc (degrees)

        internal readonly ShiftRecord[] shifts;
        internal readonly ShiftRecord[] accuracies;

        /// <summary>
        /// Initializes a new instance of the <see cref="Grid"/> class.
        /// </summary>
        /// <param name="header">The sub-grid header.</param>
        /// <param name="accuracies">Indicates whether to allocate the accuracies array.</param>
        public Grid(GridHeader header, bool accuracies)
        {
            Header = header;

            shifts = new ShiftRecord[header.GS_COUNT];

            if (accuracies)
            {
                this.accuracies = new ShiftRecord[header.GS_COUNT];
            }

            children = new List<Grid>();
        }

        /// <summary>
        /// Check whether the given coordinate lies within the grid.
        /// </summary>
        /// <param name="lon">The longitude.</param>
        /// <param name="lat">The latitude.</param>
        /// <returns>Returns true if the coordinate lies within the grid.</returns>
        public bool Contains(double lon, double lat)
        {
            return (lat <= lat_max) && (lat > lat_min) && (lon <= lon_max) && (lon > lon_min);
        }

        /// <summary>
        /// Convert grid bounds to degrees and fix NTv2 positive-west / negative-east
        /// to standard negative-west / positive-east notation.
        /// </summary>
        internal void Process(GridFile g, int id)
        {
            var h = Header;

            this.id = id;

            lat_min = h.S_LAT * g.hdr_conv;
            lat_max = h.N_LAT * g.hdr_conv;
            lat_inc = h.LAT_INC * g.hdr_conv;

            // NOTE: reversing sign!
            lon_max = h.E_LONG * -g.hdr_conv;
            lon_min = h.W_LONG * -g.hdr_conv;
            lon_inc = h.LONG_INC * g.hdr_conv;

            Count = h.GS_COUNT;

            Rows = Round((lat_max - lat_min) / lat_inc) + 1;
            Columns = Round((lon_max - lon_min) / lon_inc) + 1;

            // Collect the min/max of all the sub-grids.

            if (g.lon_min > lon_min) g.lon_min = lon_min;
            if (g.lat_min > lat_min) g.lat_min = lat_min;
            if (g.lon_max < lon_max) g.lon_max = lon_max;
            if (g.lat_max < lat_max) g.lat_max = lat_max;
        }
    }
}
