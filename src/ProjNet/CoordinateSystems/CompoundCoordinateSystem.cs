// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// This is a compound coordinate system, which combines the coordinate of two other coordinate systems.
/// For example, a compound 3D coordinate system could be made up of a
/// horizontal coordinate system and a vertical coordinate system.
/// </summary>
public class CompoundCoordinateSystem : CoordinateSystem
{
    private CoordinateSystem headCoordinateSystem;
    private CoordinateSystem tailCoordinateSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundCoordinateSystem"/> class.
    /// A compound coordinate system.
    /// </summary>
    /// <param name="headcs">The head (first) coordinate system.</param>
    /// <param name="tailcs">The tail (second) coordinate system.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Optional information.</param>
    public CompoundCoordinateSystem(CoordinateSystem headcs, CoordinateSystem tailcs, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
        : base(name, authority, authorityCode, alias, abbreviation, remarks)
    {
        this.headCoordinateSystem = headcs;
        this.tailCoordinateSystem = tailcs;
        this.AxisInfo = [];
        this.AxisInfo.AddRange(this.HeadCoordinateSystem.AxisInfo);
        this.AxisInfo.AddRange(this.TailCoordinateSystem.AxisInfo);
    }

    /// <inheritdoc/>
    public override string WKT
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append("COMPD_CS[\"");
            sb.Append(this.Name);
            sb.Append("\",");
            sb.Append(this.HeadCoordinateSystem.WKT);
            sb.Append(',');
            sb.Append(this.TailCoordinateSystem.WKT);
            if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
            {
                sb.Append(",AUTHORITY[\"");
                sb.Append(this.Authority);
                sb.Append("\",\"");
                sb.Append(this.AuthorityCode.ToString(CultureInfo.InvariantCulture));
                sb.Append("\"]");
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
            var sb = new StringBuilder();
            sb.AppendFormat(
                CultureInfo.InvariantCulture.NumberFormat,
                "<CS_CoordinateSystem Dimension=\"{0}\"><CS_CompoundCoordinateSystem>{1}",
                this.Dimension,
                this.InfoXml);
            foreach (AxisInfo ai in this.AxisInfo)
            {
                sb.Append(ai.XML);
            }

            sb.Append(this.HeadCoordinateSystem.XML);
            sb.Append(this.TailCoordinateSystem.XML);
            sb.Append("</CS_CompoundCoordinateSystem></CS_CoordinateSystem>");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Gets or sets the head coordinate system.
    /// </summary>
    public CoordinateSystem HeadCoordinateSystem
    {
        get => this.headCoordinateSystem; set { this.headCoordinateSystem = value; }
    }

    /// <summary>
    /// Gets or sets the tail coordinate system.
    /// </summary>
    public CoordinateSystem TailCoordinateSystem
    {
        get => this.tailCoordinateSystem; set { this.tailCoordinateSystem = value; }
    }

    /// <inheritdoc/>
    public override bool EqualParams(object obj)
    {
        return obj is CompoundCoordinateSystem compdCs && this.HeadCoordinateSystem.EqualParams(compdCs.HeadCoordinateSystem) && this.TailCoordinateSystem.EqualParams(compdCs.TailCoordinateSystem);
    }

    /// <inheritdoc/>
    public override IUnit GetUnits(int dimension)
    {
        if (dimension < 0 || dimension >= this.Dimension)
        {
            ArgumentGuard.ThrowArgument("Dimension not valid", nameof(dimension));
        }

        return dimension < this.HeadCoordinateSystem.Dimension
            ? this.HeadCoordinateSystem.GetUnits(dimension)
            : this.TailCoordinateSystem.GetUnits(dimension - this.HeadCoordinateSystem.Dimension);
    }
}
