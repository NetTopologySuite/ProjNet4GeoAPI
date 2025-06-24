
using ProjNet.IO.Wkt.Core;
using System;
using System.Globalization;
using System.Text;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// Identifier is an optional attribute which references an external
    /// description of the object and which may be applied to a coordinate reference system, a coordinate
    /// operation or a boundCRS. It may also be utilised for components of these objects although this
    /// is not recommended except for coordinate operation methods (including map projections)
    /// and parameters. Multiple identifiers may be given for any object.
    ///
    /// When an identifier is given for a coordinate reference system, coordinate operation or boundCRS,
    /// it applies to the whole object including all of its components.
    ///
    /// Should any attributes or values given in the cited identifier be in conflict with attributes
    /// or values given explicitly in the WKT description, the WKT values shall prevail.
    /// </summary>
    /// <remarks>
    /// See 7.3.4 Identifier in specification document.
    /// </remarks>
    public class WktIdentifier : IWktAttribute
    {
        /// <summary>
        /// AuthorityName
        /// </summary>
        public string AuthorityName { get; set; }

        /// <summary>
        /// AuthorityUniqueIdentifier
        /// </summary>
        public object AuthorityUniqueIdentifier { get; set; }

        /// <summary>
        /// Version (Optional)
        /// </summary>
        public object Version { get; set; }

        /// <summary>
        /// AuthorityCitation (Optional)
        /// </summary>
        public  WktAuthorityCitation AuthorityCitation { get; set; }

        /// <summary>
        /// Id Uri (Optional)
        /// </summary>
        /// <returns></returns>
        public WktUri IdUri { get; set; }


        /// <summary>
        /// ToWKT.
        /// </summary>
        /// <returns></returns>
        public string ToWKT()
        {
            var sb = new StringBuilder();

            sb.Append("ID[");

            sb.Append($@"""{AuthorityName}""");
            sb.Append($@"""{AuthorityUniqueIdentifier}""");

            if (Version != null)
            {
                if (Version is double d)
                {
                    sb.Append(d.ToString(CultureInfo.InvariantCulture));
                }
                else if (Version is string vs)
                {
                    sb.Append($@"""{vs}""");
                }
            }

            if (AuthorityCitation != null)
            {
                sb.Append(AuthorityCitation.ToWKT());
            }

            if (IdUri != null)
            {
                sb.Append(IdUri.ToWKT());
            }

            sb.Append("]");

            return sb.ToString();
        }
    }
}
