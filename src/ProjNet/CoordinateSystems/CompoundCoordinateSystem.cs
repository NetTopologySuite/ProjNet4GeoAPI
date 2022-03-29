using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ProjNet.CoordinateSystems
{
    /// <summary>
    /// This is a compound coordinate system, which combines the coordinate of two other coordinate systems.
    /// For example, a compound 3D coordinate system could be made up of a
    /// horizontal coordinate system and a vertical coordinate system.
    /// </summary>
    public class CompoundCoordinateSystem : CoordinateSystem
    {
        private CoordinateSystem _headCoordinateSystem;
        private CoordinateSystem _tailCoordinateSystem;

        /// <inheritdoc/>
        public override string WKT
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append($"COMPD_CS[\"{Name}\",{HeadCoordinateSystem.WKT},{TailCoordinateSystem.WKT}");
                if (!string.IsNullOrWhiteSpace(Authority) && AuthorityCode > 0)
                {
                    sb.Append($",AUTHORITY[\"{Authority}\",\"{AuthorityCode}\"]");
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
                sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat,
                    "<CS_CoordinateSystem Dimension=\"{0}\"><CS_CompoundCoordinateSystem>{1}",
                    this.Dimension, InfoXml);
                foreach (var ai in AxisInfo)
                    sb.Append(ai.XML);
                sb.Append(HeadCoordinateSystem.XML);
                sb.Append(TailCoordinateSystem.XML);
                sb.AppendFormat("</CS_CompoundCoordinateSystem></CS_CoordinateSystem>");
                return sb.ToString();
            }
        }

        /// <summary>
        /// The head coordinate system
        /// </summary>
        public CoordinateSystem HeadCoordinateSystem { get => _headCoordinateSystem; set { _headCoordinateSystem = value; } }

        /// <summary>
        /// The tail coordinate system
        /// </summary>
        public CoordinateSystem TailCoordinateSystem { get => _tailCoordinateSystem; set { _tailCoordinateSystem = value; } }
        /// <summary>
        /// A compound coordinate system
        /// </summary>
        /// <param name="headcs">The head (first) coordinate system</param>
        /// <param name="tailcs">The tail (second) coordinate system</param>
        /// <param name="name">Name</param>
        /// <param name="authority">Authority name</param>
        /// <param name="authorityCode">Authority-specific identification code</param>
        /// <param name="alias">Alias</param>
        /// <param name="abbreviation">Abbreviation</param>
        /// <param name="remarks">Optional information</param>
        public CompoundCoordinateSystem(CoordinateSystem headcs, CoordinateSystem tailcs, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
            : base(name, authority, authorityCode, alias, abbreviation, remarks)
        {
            _headCoordinateSystem = headcs;
            _tailCoordinateSystem = tailcs;
            AxisInfo = new List<AxisInfo>();
            AxisInfo.AddRange(HeadCoordinateSystem.AxisInfo);
            AxisInfo.AddRange(TailCoordinateSystem.AxisInfo);
        }

        /// <inheritdoc/>
        public override bool EqualParams(object obj)
        {
            if( obj is CompoundCoordinateSystem compdCs )
            {
                return HeadCoordinateSystem.EqualParams(compdCs.HeadCoordinateSystem) && TailCoordinateSystem.EqualParams(compdCs.TailCoordinateSystem);
            }

            return false;
        }

        /// <inheritdoc/>
        public override IUnit GetUnits(int dimension)
        {
            if( dimension < 0 || dimension >= Dimension )
            {
                throw new ArgumentException("Dimension not valid", nameof(dimension));
            }

            if( dimension < HeadCoordinateSystem.Dimension)
            {
                return HeadCoordinateSystem.GetUnits(dimension);
            }

            return TailCoordinateSystem.GetUnits(dimension - HeadCoordinateSystem.Dimension);
        }
    }
}
