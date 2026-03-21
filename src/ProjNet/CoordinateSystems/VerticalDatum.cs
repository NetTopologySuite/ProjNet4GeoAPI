// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNet.CoordinateSystems
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// A vertical datum defining the standard datum information.
    /// </summary>
    public class VerticalDatum : Datum
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VerticalDatum"/> class.
        /// Initializes a new instance of a vertical datum.
        /// </summary>
        /// <param name="type">Datum type.</param>
        /// <param name="name">Name.</param>
        /// <param name="authority">Authority name.</param>
        /// <param name="code">Authority-specific identification code.</param>
        /// <param name="alias">Alias.</param>
        /// <param name="abbreviation">Abbreviation.</param>
        /// <param name="remarks">Provider-supplied remarks.</param>
        public VerticalDatum(DatumType type, string name, string authority, long code, string alias, string remarks, string abbreviation)
            : base(type, name, authority, code, alias, remarks, abbreviation)
        {
        }

        /// <summary>
        /// Gets oDN - VerticalDatum.
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
                sb.AppendFormat(CultureInfo.InvariantCulture, "DATUM[\"{0}\", {1}", this.Name, (int)this.DatumType);
                if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, ", AUTHORITY[\"{0}\", \"{1}\"]", this.Authority, this.AuthorityCode);
                }

                sb.Append(']');
                return sb.ToString();
            }
        }

        /// <inheritdoc/>
        public override string XML
        {
            get
            {
                return string.Format(
                    CultureInfo.InvariantCulture.NumberFormat,
                    "<CS_VerticalDatum DatumType=\"{0}\">{1}{2}</CS_VerticalDatum>",
                    (int)this.DatumType,
                    this.InfoXml);
            }
        }

        /// <inheritdoc/>
        public override bool EqualParams(object obj)
        {
            if (obj is VerticalDatum vertDatum)
            {
                return base.EqualParams(vertDatum);
            }

            return false;
        }
    }
}
