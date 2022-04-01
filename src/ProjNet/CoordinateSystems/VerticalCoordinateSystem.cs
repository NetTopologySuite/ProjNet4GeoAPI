using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ProjNet.CoordinateSystems
{
    /// <summary>
	/// A 1D coordinate system suitable vertical coordinates
    /// </summary>
    public class VerticalCoordinateSystem : CoordinateSystem
    {
        /// <summary>
        /// Creates an instance of a VerticalCoordinateSystem
        /// </summary>
        /// <param name="linearUnit">The linear unit</param>
        /// <param name="verticalDatum">The vertical datum</param>
        /// <param name="axisInfo">Axis information</param>
        /// <param name="name">Name</param>
		/// <param name="authority">Authority name</param>
		/// <param name="authorityCode">Authority-specific identification code.</param>
		/// <param name="alias">Alias</param>
		/// <param name="abbreviation">Abbreviation</param>
		/// <param name="remarks">Provider-supplied remarks</param>
        public VerticalCoordinateSystem(LinearUnit linearUnit, VerticalDatum verticalDatum, AxisInfo axisInfo, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks) : base(name, authority, authorityCode, alias, abbreviation, remarks)
        {
            VerticalDatum = verticalDatum;
            AxisInfo = new List<AxisInfo>() { axisInfo };
            LinearUnit = linearUnit;
        }

        /// <summary>
        /// Gets or sets the VerticalDatum
        /// </summary>
        public VerticalDatum VerticalDatum { get; set; }

        /// <summary>
        /// Gets or sets the LinearUnit
        /// </summary>
        public LinearUnit LinearUnit { get; set; }

        /// <summary>
        /// Creates a meter unit coordinate system with <see cref="VerticalDatum.ODN"/>
        /// </summary>
        public static VerticalCoordinateSystem ODN =>
            new VerticalCoordinateSystem(
                new LinearUnit(1, "metre", "EPSG", 9001, string.Empty, "m", string.Empty)
                , VerticalDatum.ODN, new AxisInfo("Up", AxisOrientationEnum.Up)
                , "Newlyn"
                , "EPSG"
                , 5701
                , string.Empty
                , "ODN"
                , string.Empty
                );
        /// <inheritdoc/>
        public override string WKT
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendFormat("VERT_CS[\"{0}\", {1}, {2}", Name, VerticalDatum.WKT, LinearUnit.WKT);
                //Skip axis info if they contain default values
                if (AxisInfo.Count != 1 ||
                    AxisInfo[0].Name != "Up" || AxisInfo[0].Orientation != AxisOrientationEnum.Up)
                {
                    sb.AppendFormat(", {0}", GetAxis(0).WKT);
                }
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
                var sb = new StringBuilder();
                sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat,
                    "<CS_CoordinateSystem Dimension=\"{0}\"><CS_VerticalCoordinateSystem>{1}",
                    this.Dimension, InfoXml);
                foreach (var ai in AxisInfo)
                    sb.Append(ai.XML);
                sb.AppendFormat("{0}{1}</CS_VerticalCoordinateSystem></CS_CoordinateSystem>",
                    VerticalDatum.XML, LinearUnit.XML);
                return sb.ToString();
            }
        }

        /// <inheritdoc/>
        public override bool EqualParams(object obj)
        {
            if (!(obj is VerticalCoordinateSystem vcs))
                return false;

            if (vcs.Dimension != Dimension) return false;
            if (AxisInfo.Count != vcs.AxisInfo.Count) return false;
            for (int i = 0; i < vcs.AxisInfo.Count; i++)
                if (vcs.AxisInfo[i].Orientation != AxisInfo[i].Orientation)
                    return false;
            return vcs.LinearUnit.EqualParams(LinearUnit) &&
                    vcs.VerticalDatum.EqualParams(VerticalDatum);
        }

        /// <inheritdoc/>
        public override IUnit GetUnits(int dimension)
        {
            if( dimension != 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(dimension), "Vertical Coordinate Systems have only one dimension");
            }

            return LinearUnit;
        }
    }
}
