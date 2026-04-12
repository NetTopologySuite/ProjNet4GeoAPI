// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using ProjNet.IO.Wkt;

/// <summary>
/// Class for defining units.
/// </summary>
/// <remarks>
/// <para>
/// Thread safety: Instances are immutable after construction and may be shared across threads.
/// Derived predefined unit accessors are thread-safe because they only expose immutable value objects.
/// </para>
/// </remarks>
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
    /// Gets the number of units per base-unit.
    /// </summary>
    public double ConversionFactor { get; }

    /// <summary>
    /// Gets the Well-known text for this object
    /// as defined in the simple features specification.
    /// </summary>
    public override string WKT => this.ToWktNode().ToString();

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

    /// <summary>
    /// Returns an XML representation of this unit as an <see cref="XElement"/> [NOT IMPLEMENTED].
    /// </summary>
    /// <returns>Not implemented; always throws <see cref="NotImplementedException"/>.</returns>
    /// <exception cref="NotImplementedException">Always thrown because XML serialization is not supported for generic units.</exception>
    public XElement ToXml()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Converts this generic unit to a WKT syntax tree node.
    /// </summary>
    /// <returns>A <see cref="WktNode"/> representing this unit.</returns>
    public WktNode ToWktNode()
    {
        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            new WktNumber(this.ConversionFactor),
        };

        if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
        {
            children.Add(new WktKeywordNode(
                "AUTHORITY",
                new WktQuotedString(this.Authority),
                new WktQuotedString(this.AuthorityCode.ToString(CultureInfo.InvariantCulture))));
        }

        return new WktKeywordNode("UNIT", children);
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        return obj is Unit unit && unit.ConversionFactor == this.ConversionFactor;
    }
}
