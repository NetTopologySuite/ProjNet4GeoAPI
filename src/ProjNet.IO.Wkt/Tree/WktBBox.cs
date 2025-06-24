using System.Globalization;
using System.Text;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// BoundingBox is an optional WKT attribute.
    /// </summary>
    /// <remarks>
    /// See 7.3.3.3 Geographic bounding box in specification document.
    /// </remarks>
    public class BBox : WktExtent
    {
        /// <summary>
        /// The lower left latitude (Ymin)
        /// </summary>
        public double LowerLeftLatitude { get; set; }

        /// <summary>
        /// The lower left longitude (Xmin)
        /// </summary>
        public double LowerLeftLongitude { get; set; }

        /// <summary>
        /// The upper right latitude (Ymax)
        /// </summary>
        public double UpperRightLatitude { get; set; }

        /// <summary>
        /// The upper right longitude (Xmax)
        /// </summary>
        public double UpperRightLongitude { get; set; }


        /// <summary>
        /// BBox to WKT.
        /// </summary>
        /// <returns></returns>
        public override string ToWKT()
        {
            var sb = new StringBuilder();

            sb.Append("BBOX[");
            sb.Append(LowerLeftLatitude.ToString(CultureInfo.InvariantCulture));
            sb.Append(",");
            sb.Append(LowerLeftLongitude.ToString(CultureInfo.InvariantCulture));
            sb.Append(",");
            sb.Append(UpperRightLatitude.ToString(CultureInfo.InvariantCulture));
            sb.Append(",");
            sb.Append(UpperRightLongitude.ToString(CultureInfo.InvariantCulture));
            sb.Append("]");

            return sb.ToString();
        }
    }
}
