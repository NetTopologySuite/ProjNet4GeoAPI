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
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Definition of linear units.
    /// </summary>
    [Serializable]
    public class LinearUnit : Info, IUnit
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinearUnit"/> class.
        /// Creates an instance of a linear unit.
        /// </summary>
        /// <param name="metersPerUnit">Number of meters per <see cref="LinearUnit" />.</param>
        /// <param name="name">Name.</param>
        /// <param name="authority">Authority name.</param>
        /// <param name="authorityCode">Authority-specific identification code.</param>
        /// <param name="alias">Alias.</param>
        /// <param name="abbreviation">Abbreviation.</param>
        /// <param name="remarks">Provider-supplied remarks.</param>
        public LinearUnit(double metersPerUnit, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
            : base(name, authority, authorityCode, alias, abbreviation, remarks)
        {
            this.MetersPerUnit = metersPerUnit;
        }

        /// <summary>
        /// Gets the meters linear unit.
        /// Also known as International metre. SI standard unit.
        /// </summary>
        public static LinearUnit Metre
        {
            get { return new LinearUnit(1.0, "metre", "EPSG", 9001, "m", string.Empty, "Also known as International metre. SI standard unit."); }
        }

        /// <summary>
        /// Gets the foot linear unit (1ft = 0.3048m).
        /// </summary>
        public static LinearUnit Foot
        {
            get { return new LinearUnit(0.3048, "foot", "EPSG", 9002, "ft", string.Empty, string.Empty); }
        }

        /// <summary>
        /// Gets the US Survey foot linear unit (1ftUS = 0.304800609601219m).
        /// </summary>
        public static LinearUnit USSurveyFoot
        {
            get { return new LinearUnit(0.304800609601219, "US survey foot", "EPSG", 9003, "American foot", "ftUS", "Used in USA."); }
        }

        /// <summary>
        /// Gets the Nautical Mile linear unit (1NM = 1852m).
        /// </summary>
        public static LinearUnit NauticalMile
        {
            get { return new LinearUnit(1852, "nautical mile", "EPSG", 9030, "NM", string.Empty, string.Empty); }
        }

        /// <summary>
        /// Gets clarke's foot.
        /// </summary>
        /// <remarks>
        /// Assumes Clarke's 1865 ratio of 1 British foot = 0.3047972654 French legal metres applies to the international metre.
        /// Used in older Australian, southern African &amp; British West Indian mapping.
        /// </remarks>
        public static LinearUnit ClarkesFoot
        {
            get { return new LinearUnit(0.3047972654, "Clarke's foot", "EPSG", 9005, "Clarke's foot", string.Empty, "Assumes Clarke's 1865 ratio of 1 British foot = 0.3047972654 French legal metres applies to the international metre. Used in older Australian, southern African & British West Indian mapping."); }
        }

        /// <summary>
        /// Gets or sets the number of meters per <see cref="LinearUnit"/>.
        /// </summary>
        public double MetersPerUnit { get; set; }

        /// <summary>
        /// Gets the Well-known text for this object
        /// as defined in the simple features specification.
        /// </summary>
        public override string WKT
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat, "UNIT[\"{0}\", {1}", this.Name, this.MetersPerUnit);
                if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, ", AUTHORITY[\"{0}\", \"{1}\"]", this.Authority, this.AuthorityCode);
                }

                sb.Append("]");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        public override string XML
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture.NumberFormat, "<CS_LinearUnit MetersPerUnit=\"{0}\">{1}</CS_LinearUnit>", this.MetersPerUnit, this.InfoXml);
            }
        }

        /// <summary>
        /// Checks whether the values of this instance is equal to the values of another instance.
        /// Only parameters used for coordinate system are used for comparison.
        /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
        /// </summary>
        /// <param name="obj">The obj parameter.</param>
        /// <returns>True if equal.</returns>
        public override bool EqualParams(object obj)
        {
            if (!(obj is LinearUnit))
            {
                return false;
            }

            return (obj as LinearUnit).MetersPerUnit == this.MetersPerUnit;
        }
    }
}
