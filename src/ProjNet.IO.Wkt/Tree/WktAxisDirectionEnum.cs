namespace ProjNet.IO.Wkt.Tree
{

    /// <summary>
    /// WktAxisDirectionEnum.
    /// </summary>
    public enum WktAxisDirectionEnum
    {
        /// <summary>
        /// Unknown or unspecified axis direction. This can be used for local or fitted coordinate systems.
        /// </summary>
        Other = 0,

        /// <summary>
        /// Increasing ordinates values go North. This is usually used for Grid Y coordinates and Latitude.
        /// </summary>
        North = 1,

        /// <summary>
        /// Increasing ordinates values go South. This is rarely used.
        /// </summary>
        South = 2,

        /// <summary>
        /// Increasing ordinates values go East. This is rarely used.
        /// </summary>
        East = 3,

        /// <summary>
        /// Increasing ordinates values go West. This is usually used for Grid X coordinates and Longitude.
        /// </summary>
        West = 4,

        /// <summary>
        /// Increasing ordinates values go up. This is used for vertical coordinate systems.
        /// </summary>
        Up = 5,

        /// <summary>
        /// Increasing ordinates values go down. This is used for vertical coordinate systems.
        /// </summary>
        Down = 6
    }
}
