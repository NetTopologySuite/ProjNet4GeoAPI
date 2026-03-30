// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Globalization;
using System.Text;

/// <summary>
/// Class for defining units.
/// </summary>
public class Unit : Info, IUnit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Unit"/> class.
    /// </summary>
    /// <param name="conversionFactor">Conversion factor to base unit.</param>
    /// <param name="name">Name of unit.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    internal Unit(double conversionFactor, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
        : base(name, authority, authorityCode, alias, abbreviation, remarks)
    {
        this.ConversionFactor = conversionFactor;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Unit"/> class.
    /// </summary>
    /// <param name="name">Name of unit.</param>
    /// <param name="conversionFactor">Conversion factor to base unit.</param>
    internal Unit(string name, double conversionFactor)
        : this(conversionFactor, name, string.Empty, -1, string.Empty, string.Empty, string.Empty)
    {
    }

    /// <summary>
    /// Gets or sets the number of units per base-unit.
    /// </summary>
    public double ConversionFactor { get; set; }

    /// <summary>
    /// Gets the Well-known text for this object
    /// as defined in the simple features specification.
    /// </summary>
    public override string WKT
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat, "UNIT[\"{0}\", {1}", this.Name, this.ConversionFactor);
            if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, ", AUTHORITY[\"{0}\", \"{1}\"]", this.Authority, this.AuthorityCode);
            }

            sb.Append(']');
            return sb.ToString();
        }
    }

    /// <summary>
    /// Gets an XML representation of this object [NOT IMPLEMENTED].
    /// </summary>
    public override string XML
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        return obj is Unit unit && unit.ConversionFactor == this.ConversionFactor;
    }
}
