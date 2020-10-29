namespace ProjNet.NTv2
{
    /// <summary>
    /// The raw grid file header.
    /// </summary>
    public class GridFileHeader
    {
        /// <summary>
        /// Gets the number of grid header records (should be 11).
        /// </summary>
        public int NUM_OREC { get; set; }

        /// <summary>
        /// Gets the number of sub-grid header records (should be 11).
        /// </summary>
        public int NUM_SREC { get; set; }

        /// <summary>
        /// Gets the number of sub-grid files.
        /// </summary>
        public int NUM_FILE { get; set; }

        /// <summary>
        /// Gets the shift unit (e.g. "SECONDS").
        /// </summary>
        public string GS_TYPE { get; set; }

        /// <summary>
        /// Gets the file version (e.g. "NTv2.0")
        /// </summary>
        public string VERSION { get; set; }

        /// <summary>
        /// Gets the source reference system (from).
        /// </summary>
        public string SYSTEM_F { get; set; }

        /// <summary>
        /// Gets the target reference system (to).
        /// </summary>
        public string SYSTEM_T { get; set; }

        /// <summary>
        /// Gets the source ellipsoid semi-major axis (from).
        /// </summary>
        public double MAJOR_F { get; set; }

        /// <summary>
        /// Gets the source ellipsoid semi-minor axis (from).
        /// </summary>
        public double MINOR_F { get; set; }

        /// <summary>
        /// Gets the target ellipsoid semi-major axis (to).
        /// </summary>
        public double MAJOR_T { get; set; }

        /// <summary>
        /// Gets the target ellipsoid semi-minor axis (to).
        /// </summary>
        public double MINOR_T { get; set; }
    }
}
