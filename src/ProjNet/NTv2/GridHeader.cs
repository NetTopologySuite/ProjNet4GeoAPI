namespace ProjNet.NTv2
{
    /// <summary>
    /// The raw sub-grid header read from a grid file.
    /// </summary>
    public class GridHeader
    {
        /// <summary>
        /// Gets the sub-file name.
        /// </summary>
        public string SUB_NAME { get; set; }

        /// <summary>
        /// Gets the parent name (or "NONE").
        /// </summary>
        public string PARENT { get; set; }

        /// <summary>
        /// Gets the creation date.
        /// </summary>
        public string CREATED { get; set; }

        /// <summary>
        /// Gets the last revision date.
        /// </summary>
        public string UPDATED { get; set; }

        /// <summary>
        /// Gets the south latitude (in gs-units).
        /// </summary>
        public double S_LAT { get; set; }

        /// <summary>
        /// Gets the north latitude (in gs-units).
        /// </summary>
        public double N_LAT { get; set; }

        /// <summary>
        /// Gets the east longitude (in gs-units).
        /// </summary>
        public double E_LONG { get; set; }

        /// <summary>
        /// Gets the west longitude (in gs-units).
        /// </summary>
        public double W_LONG { get; set; }

        /// <summary>
        /// Gets the latitude increment (in gs-units).
        /// </summary>
        public double LAT_INC { get; set; }

        /// <summary>
        /// Gets the longitude increment (in gs-units).
        /// </summary>
        public double LONG_INC { get; set; }

        /// <summary>
        /// Gets the number of grid-shift records.
        /// </summary>
        public int GS_COUNT { get; set; }
    }
}
