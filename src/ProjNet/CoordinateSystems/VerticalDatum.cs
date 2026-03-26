// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable

namespace ProjNet.CoordinateSystems;

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
                "<CS_VerticalDatum DatumType=\"{0}\">{1}</CS_VerticalDatum>",
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
