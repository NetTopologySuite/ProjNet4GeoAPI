using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ProjNet.CoordinateSystems
{
    /// <summary>
    /// A vertical datum defining the standard datum information
    /// </summary>
    public class VerticalDatum : Datum
    {
        /// <summary>
		/// Initializes a new instance of a vertical datum
		/// </summary>
		/// <param name="type">Datum type</param>
		/// <param name="name">Name</param>
		/// <param name="authority">Authority name</param>
		/// <param name="code">Authority-specific identification code.</param>
		/// <param name="alias">Alias</param>
		/// <param name="abbreviation">Abbreviation</param>
		/// <param name="remarks">Provider-supplied remarks</param>
        public VerticalDatum(DatumType type, string name, string authority, long code, string alias, string remarks, string abbreviation) : base(type, name, authority, code, alias, remarks, abbreviation)
        {
        }

        /// <summary>
        /// ODN - VerticalDatum
        /// </summary>
        public static VerticalDatum ODN
        {
            get
            {
                return new VerticalDatum(DatumType.VD_GeoidModelDerived, "Ordnance Datum Newlyn", "EPSG", 5101, string.Empty, string.Empty, string.Empty);
            }
        }

        /// <inheritdoc/>
        public override string WKT
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendFormat("DATUM[\"{0}\", {1}", Name, (int)DatumType);
                if (!string.IsNullOrWhiteSpace(Authority) && AuthorityCode > 0)
                    sb.AppendFormat(", AUTHORITY[\"{0}\", \"{1}\"]", Authority, AuthorityCode);
                sb.Append("]");
                return sb.ToString();
            }
        }

        /// <inheritdoc/>
        public override string XML
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture.NumberFormat,
                    "<CS_VerticalDatum DatumType=\"{0}\">{1}{2}</CS_VerticalDatum>",
                    (int)DatumType, InfoXml);
            }
        }

        /// <inheritdoc/>
        public override bool EqualParams(object obj)
        {
            if( obj is VerticalDatum vertDatum )
            {
                return base.EqualParams(vertDatum);
            }
            return false;
        }
    }
}
