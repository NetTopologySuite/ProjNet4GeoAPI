// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ProjNet.IO.Wkt;

/// <summary>
/// A vertical datum defining the standard datum information.
/// </summary>
public class VerticalDatum : Datum
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerticalDatum"/> class.
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
    /// Gets the Ordnance Datum Newlyn (ODN) vertical datum.
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
            sb.AppendFormat(CultureInfo.InvariantCulture, "VERT_DATUM[\"{0}\", {1}", this.Name, (int)this.DatumType);
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
            return FormattableString.Invariant($"<CS_VerticalDatum DatumType=\"{(int)this.DatumType}\">{this.InfoXml}</CS_VerticalDatum>");
        }
    }

    /// <summary>
    /// Converts this vertical datum to a WKT syntax tree node.
    /// </summary>
    /// <returns>A <see cref="WktNode"/> representing this vertical datum.</returns>
    public WktNode ToWktNode()
    {
        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            new WktInteger((int)this.DatumType),
        };

        if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
        {
            children.Add(new WktKeywordNode(
                "AUTHORITY",
                new WktQuotedString(this.Authority),
                new WktQuotedString(this.AuthorityCode.ToString(CultureInfo.InvariantCulture))));
        }

        return new WktKeywordNode("VERT_DATUM", children);
    }

    /// <inheritdoc/>
    public override bool EqualParams(object obj)
    {
        return obj is VerticalDatum vertDatum && base.EqualParams(vertDatum);
    }
}
