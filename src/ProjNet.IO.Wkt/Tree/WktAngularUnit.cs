namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// WktAngularUnit class.
    /// </summary>
    public class WktAngularUnit : WktUnit
    {
        /// <summary>
        /// Constructor for WKT AngularUnit.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="conversionFactor"></param>
        /// <param name="authority"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="rightDelimiter"></param>
        public WktAngularUnit(string name, double conversionFactor, WktAuthority authority,
                                string keyword = "UNIT", char leftDelimiter = '[', char rightDelimiter = ']')
            : base(name, conversionFactor, authority, keyword, leftDelimiter, rightDelimiter)
        {
        }

    }
}
