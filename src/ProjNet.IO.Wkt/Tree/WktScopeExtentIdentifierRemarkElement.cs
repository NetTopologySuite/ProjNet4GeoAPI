using System.Collections.Generic;
using System.Text;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// WktScopeExtentIdentifierRemarkElement class.
    /// </summary>
    public class WktScopeExtentIdentifierRemarkElement : IWktAttribute
    {
        /// <summary>
        /// Optional Scope attribute.
        /// </summary>
        public WktScope Scope { get; set; }

        /// <summary>
        /// Zero or more Extent attribute(s).
        /// </summary>
        public IList<WktExtent> Extents { get; set; } = new List<WktExtent>();

        /// <summary>
        /// Zero or more Identifier attribute(s).
        /// </summary>
        public IList<WktIdentifier> Identifiers { get; set; } = new List<WktIdentifier>();

        /// <summary>
        /// Optional Remark attrbiute.
        /// </summary>
        public WktRemark Remark { get; set; }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="extents"></param>
        /// <param name="identifiers"></param>
        /// <param name="remark"></param>
        public WktScopeExtentIdentifierRemarkElement(
            WktScope scope = null,
            IList<WktExtent> extents = null,
            IList<WktIdentifier> identifiers = null,
            WktRemark remark = null)
        {
            Scope = scope;
            Extents = extents ?? Extents;
            Identifiers = identifiers ?? Identifiers;
            Remark = remark;
        }

        /// <summary>
        /// ToWKT.
        /// </summary>
        /// <returns></returns>
        public string ToWKT()
        {
            var sb = new StringBuilder();

            if (Scope != null)
            {
                sb.Append($",{Scope.ToWKT()}");
            }

            if (Extents != null)
            {
                foreach (var extent in Extents)
                {
                    sb.Append($",{extent.ToWKT()})");
                }
            }
            if (Identifiers != null)
            {
                foreach (var identifier in Identifiers)
                {
                    sb.Append($",{identifier.ToWKT()})");
                }
            }

            if (Remark != null)
            {
                sb.Append($",{Remark.ToWKT()}");
            }

            return sb.ToString();
        }
    }
}
