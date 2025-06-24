
namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// WktLinearUnit class.
    /// </summary>
    public class WktLinearUnit : WktUnit
    {
        /// <summary>
        /// Constructor for WKT LinearUnit.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="conversionFactor"></param>
        /// <param name="authority"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="rightDelimiter"></param>
        public WktLinearUnit(string name, double conversionFactor, WktAuthority authority,
                            string keyword = "UNIT", char leftDelimiter = '[', char rightDelimiter = ']')
            : base(name, conversionFactor, authority, keyword, leftDelimiter, rightDelimiter)
        {
        }

    }
}
