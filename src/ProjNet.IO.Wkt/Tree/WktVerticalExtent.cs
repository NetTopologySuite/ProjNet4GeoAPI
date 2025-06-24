using System.Globalization;
using System.Text;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// Vertical extent is an optional attribute which describes a height range over
    /// which a CRS or coordinate operation is applicable. Depths have negative height values.
    /// Vertical extent is an approximate description of location; heights are relative to an unspecified mean sea level.
    /// </summary>
    /// <remarks>
    /// See 7.3.3.4 Vertical extent in specification document.
    /// </remarks>
    public class WktVerticalExtent : WktExtent
    {
        /// <summary>
        /// MinimumHeight
        /// </summary>
        public double MinimumHeight { get; set; }

        /// <summary>
        /// MaximumHeight
        /// </summary>
        public double MaximumHeight { get; set; }


        //// <summary>
        //// Optional LengthUnit.
        //// </summary>
        // TODO: public LengthUnit? LengthUnit { get; set; }


        /// <summary>
        /// VerticalExtent to WKT.
        /// </summary>
        /// <returns></returns>
        public override string ToWKT()
        {
            var sb = new StringBuilder();

            sb.Append("VERTICALEXTENT[");
            sb.Append(MinimumHeight.ToString(CultureInfo.InvariantCulture));
            sb.Append(",");
            sb.Append(MaximumHeight.ToString(CultureInfo.InvariantCulture));
            sb.Append("]");

            return sb.ToString();
        }
    }
}
