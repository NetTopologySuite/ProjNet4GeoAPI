using System.Globalization;
using System.Text;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// Area description is an optional attribute which describes a geographic area over which a CRS or coordinate operation is applicable.
    /// </summary>
    /// <remarks>
    /// See 7.3.3.2 Area description in specification document.
    /// </remarks>
    public class AreaDescription : WktExtent
    {
        /// <summary>
        /// The Text Description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// AreaDescriptionParser to WKT.
        /// </summary>
        /// <returns></returns>
        public override string ToWKT()
        {
            var sb = new StringBuilder();

            sb.Append("AREA[\"");
            sb.Append(Description);
            sb.Append("\"]");

            return sb.ToString();
        }
    }
}
