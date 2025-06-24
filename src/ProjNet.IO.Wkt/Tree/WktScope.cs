using System.Globalization;
using System.Text;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// ScopeParser is an optional attribute which describes the purpose or purposes for which a CRS or a coordinate operation is applied.
    /// </summary>
    /// <remarks>
    /// See 7.3.2 ScopeParser in specification document.
    /// </remarks>
    public class WktScope : IWktAttribute
    {
        /// <summary>
        /// The Text Description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// ScopeParser to WKT.
        /// </summary>
        /// <returns></returns>
        public string ToWKT()
        {
            var sb = new StringBuilder();

            sb.Append("SCOPE[\"");
            sb.Append(Description);
            sb.Append("\"]");

            return sb.ToString();
        }
    }
}
