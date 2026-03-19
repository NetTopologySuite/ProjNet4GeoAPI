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
    /// A 1D coordinate system suitable vertical coordinates.
    /// </summary>
    public class VerticalCoordinateSystem : CoordinateSystem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VerticalCoordinateSystem"/> class.
        /// Creates an instance of a VerticalCoordinateSystem.
        /// </summary>
        /// <param name="linearUnit">The linear unit.</param>
        /// <param name="verticalDatum">The vertical datum.</param>
        /// <param name="axisInfo">Axis information.</param>
        /// <param name="name">Name.</param>
        /// <param name="authority">Authority name.</param>
        /// <param name="authorityCode">Authority-specific identification code.</param>
        /// <param name="alias">Alias.</param>
        /// <param name="abbreviation">Abbreviation.</param>
        /// <param name="remarks">Provider-supplied remarks.</param>
        public VerticalCoordinateSystem(
            LinearUnit linearUnit,
            VerticalDatum verticalDatum,
            AxisInfo axisInfo,
            string name,
            string authority,
            long authorityCode,
            string alias,
            string abbreviation,
            string remarks)
            : base(name, authority, authorityCode, alias, abbreviation, remarks)
        {
            this.VerticalDatum = verticalDatum;
            this.AxisInfo = new List<AxisInfo>() { axisInfo };
            this.LinearUnit = linearUnit;
        }

        /// <summary>
        /// Gets or sets the VerticalDatum.
        /// </summary>
        public VerticalDatum VerticalDatum { get; set; }

        /// <summary>
        /// Gets or sets the LinearUnit.
        /// </summary>
        public LinearUnit LinearUnit { get; set; }

        /// <summary>
        /// Gets creates a meter unit coordinate system with <see cref="VerticalDatum.ODN"/>.
        /// </summary>
        public static VerticalCoordinateSystem ODN =>
                new VerticalCoordinateSystem(
                    new LinearUnit(1, "metre", "EPSG", 9001, string.Empty, "m", string.Empty),
                    VerticalDatum.ODN,
                    new AxisInfo("Up", AxisOrientationEnum.Up),
                    "Newlyn",
                    "EPSG",
                    5701,
                string.Empty,
                "ODN",
                string.Empty);

        /// <inheritdoc/>
        public override string WKT
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendFormat(CultureInfo.InvariantCulture, "VERT_CS[\"{0}\", {1}, {2}", this.Name, this.VerticalDatum.WKT, this.LinearUnit.WKT);

                // Skip axis info if they contain default values
                if (this.AxisInfo.Count != 1 ||
                    this.AxisInfo[0].Name != "Up" || this.AxisInfo[0].Orientation != AxisOrientationEnum.Up)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, ", {0}", this.GetAxis(0).WKT);
                }

                if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, ", AUTHORITY[\"{0}\", \"{1}\"]", this.Authority, this.AuthorityCode);
                }

                sb.Append("]");
                return sb.ToString();
            }
        }

        /// <inheritdoc/>
        public override string XML
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendFormat(
                    CultureInfo.InvariantCulture.NumberFormat,
                    "<CS_CoordinateSystem Dimension=\"{0}\"><CS_VerticalCoordinateSystem>{1}",
                    this.Dimension,
                    this.InfoXml);
                foreach (var ai in this.AxisInfo)
                {
                    sb.Append(ai.XML);
                }

                sb.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "{0}{1}</CS_VerticalCoordinateSystem></CS_CoordinateSystem>",
                    this.VerticalDatum.XML,
                    this.LinearUnit.XML);
                return sb.ToString();
            }
        }

        /// <inheritdoc/>
        public override bool EqualParams(object obj)
        {
            if (!(obj is VerticalCoordinateSystem vcs))
            {
                return false;
            }

            if (vcs.Dimension != this.Dimension)
            {
                return false;
            }

            if (this.AxisInfo.Count != vcs.AxisInfo.Count)
            {
                return false;
            }

            for (int i = 0; i < vcs.AxisInfo.Count; i++)
            {
                if (vcs.AxisInfo[i].Orientation != this.AxisInfo[i].Orientation)
                {
                    return false;
                }
            }

            return vcs.LinearUnit.EqualParams(this.LinearUnit) &&
                    vcs.VerticalDatum.EqualParams(this.VerticalDatum);
        }

        /// <inheritdoc/>
        public override IUnit GetUnits(int dimension)
        {
            if (dimension != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dimension), "Vertical Coordinate Systems have only one dimension");
            }

            return this.LinearUnit;
        }
    }
}
