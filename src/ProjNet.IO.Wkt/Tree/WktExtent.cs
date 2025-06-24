namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// Extent attribute
    /// </summary>
    public abstract class WktExtent : IWktAttribute
    {
        /// <summary>
        /// Convert (back) to WKT.
        /// </summary>
        /// <returns></returns>
        public abstract string ToWKT();

    }
}
