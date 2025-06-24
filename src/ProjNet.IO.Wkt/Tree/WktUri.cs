using System.Text;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// Uri a simple wkt helper class containing a Uri (sub) Attribute.
    /// </summary>
    public class WktUri: IWktAttribute
    {
        /// <summary>
        /// Ref property.
        /// </summary>
        public string Ref { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="uriRef"></param>
        public WktUri(string uriRef)
        {
            Ref = uriRef;
        }

        /// <summary>
        /// ToWKT version of this WKT Uri.
        /// </summary>
        /// <returns></returns>
        public string ToWKT()
        {
            var sb = new StringBuilder();

            sb.Append($@"URI[""");
            sb.Append(Ref);
            sb.Append($@"""]");

            return sb.ToString();
        }

        /// <summary>
        /// ToString version of this WKT Uri.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Ref;
        }

    }
}
