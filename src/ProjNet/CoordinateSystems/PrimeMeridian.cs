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
/// A meridian used to take longitude measurements from.
/// </summary>
/// <remarks>
/// <para>
/// Thread safety: Instances are immutable after construction and may be shared across threads.
/// The predefined prime-meridian accessors are thread-safe because they only expose immutable value objects.
/// </para>
/// </remarks>
public class PrimeMeridian : Info
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PrimeMeridian"/> class.
    /// </summary>
    /// <param name="longitude">Longitude of prime meridian.</param>
    /// <param name="angularUnit">Angular unit.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    internal PrimeMeridian(double longitude, AngularUnit angularUnit, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
        : base(name, authority, authorityCode, alias, abbreviation, remarks)
    {
        this.Longitude = longitude;
        this.AngularUnit = angularUnit;
    }

    /// <summary>
    /// Gets the Greenwich prime meridian (0° longitude).
    /// </summary>
    public static PrimeMeridian Greenwich => new(0.0, CoordinateSystems.AngularUnit.Degrees, "Greenwich", "EPSG", 8901, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets the Lisbon prime meridian.
    /// </summary>
    public static PrimeMeridian Lisbon => new(-9.0754862, CoordinateSystems.AngularUnit.Degrees, "Lisbon", "EPSG", 8902, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets the Paris prime meridian.
    /// </summary>
    /// <remarks>
    /// Value adopted by IGN (Paris) in 1936. Equivalent to 2 deg 20 min 14.025 sec.
    /// Preferred by EPSG over the earlier value of 2 deg 20 min 13.95 sec (2.596898 grads) used by RGS London.
    /// </remarks>
    public static PrimeMeridian Paris => new(2.5969213, CoordinateSystems.AngularUnit.Degrees, "Paris", "EPSG", 8903, string.Empty, string.Empty, "Value adopted by IGN (Paris) in 1936. Equivalent to 2 deg 20min 14.025sec. Preferred by EPSG to earlier value of 2deg 20min 13.95sec (2.596898 grads) used by RGS London.");

    /// <summary>
    /// Gets the Bogota prime meridian.
    /// </summary>
    public static PrimeMeridian Bogota => new(-74.04513, CoordinateSystems.AngularUnit.Degrees, "Bogota", "EPSG", 8904, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets the Madrid prime meridian.
    /// </summary>
    public static PrimeMeridian Madrid => new(-3.411658, CoordinateSystems.AngularUnit.Degrees, "Madrid", "EPSG", 8905, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets the Rome prime meridian.
    /// </summary>
    public static PrimeMeridian Rome => new(12.27084, CoordinateSystems.AngularUnit.Degrees, "Rome", "EPSG", 8906, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets the Bern prime meridian.
    /// </summary>
    /// <remarks>1895 value. A newer value of 7 deg 26 min 22.335 sec E was determined in 1938.</remarks>
    public static PrimeMeridian Bern => new(7.26225, CoordinateSystems.AngularUnit.Degrees, "Bern", "EPSG", 8907, string.Empty, string.Empty, "1895 value. Newer value of 7 deg 26 min 22.335 sec E determined in 1938.");

    /// <summary>
    /// Gets the Jakarta prime meridian.
    /// </summary>
    public static PrimeMeridian Jakarta => new(106.482779, CoordinateSystems.AngularUnit.Degrees, "Jakarta", "EPSG", 8908, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets the Ferro prime meridian.
    /// </summary>
    /// <remarks>Used in Austria and former Czechoslovakia.</remarks>
    public static PrimeMeridian Ferro => new(-17.66666666666667, CoordinateSystems.AngularUnit.Degrees, "Ferro", "EPSG", 8909, string.Empty, string.Empty, "Used in Austria and former Czechoslovakia.");

    /// <summary>
    /// Gets the Brussels prime meridian.
    /// </summary>
    public static PrimeMeridian Brussels => new(4.220471, CoordinateSystems.AngularUnit.Degrees, "Brussels", "EPSG", 8910, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets the Stockholm prime meridian.
    /// </summary>
    public static PrimeMeridian Stockholm => new(18.03298, CoordinateSystems.AngularUnit.Degrees, "Stockholm", "EPSG", 8911, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets the Athens prime meridian.
    /// </summary>
    /// <remarks>Used in Greece for older mapping based on the Hatt projection.</remarks>
    public static PrimeMeridian Athens => new(23.4258815, CoordinateSystems.AngularUnit.Degrees, "Athens", "EPSG", 8912, string.Empty, string.Empty, "Used in Greece for older mapping based on Hatt projection.");

    /// <summary>
    /// Gets the Oslo prime meridian.
    /// </summary>
    /// <remarks>Formerly known as Kristiania or Christiania.</remarks>
    public static PrimeMeridian Oslo => new(10.43225, CoordinateSystems.AngularUnit.Degrees, "Oslo", "EPSG", 8913, string.Empty, string.Empty, "Formerly known as Kristiania or Christiania.");

    /// <summary>
    /// Gets the longitude of the prime meridian (relative to the Greenwich prime meridian).
    /// </summary>
    public double Longitude { get; }

    /// <summary>
    /// Gets the angular unit used to express the longitude of this prime meridian.
    /// </summary>
    public AngularUnit AngularUnit { get; }

    /// <summary>
    /// Gets the Well-known text for this object
    /// as defined in the simple features specification.
    /// </summary>
    public override string WKT => this.ToWktNode().ToString();

    /// <summary>
    /// Gets an XML representation of this object.
    /// </summary>
    public override string XML => this.ToXml().ToString(SaveOptions.DisableFormatting);

    /// <summary>
    /// Creates a copy of this prime meridian with updated authority metadata.
    /// </summary>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="code">Replacement authority-specific identification code.</param>
    /// <returns>A new <see cref="PrimeMeridian"/> with updated authority metadata.</returns>
    public new PrimeMeridian WithAuthority(string authority, long code) => InfoAuthorityCloneHelper.CloneWithAuthority(this, authority, code);

    /// <summary>
    /// Creates a copy of this prime meridian with an updated name.
    /// </summary>
    /// <param name="name">Replacement name.</param>
    /// <returns>A new <see cref="PrimeMeridian"/> with the updated name.</returns>
    public new PrimeMeridian WithName(string name) => InfoAuthorityCloneHelper.CloneWithName(this, name);

    /// <summary>
    /// Returns an XML representation of this prime meridian as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>An <see cref="XElement"/> containing the XML representation.</returns>
    public XElement ToXml()
    {
        var element = new XElement(
            "CS_PrimeMeridian",
            new XAttribute("Longitude", this.Longitude.ToString(CultureInfo.InvariantCulture)));
        element.Add(this.InfoXmlElement);
        element.Add(this.AngularUnit.ToXml());
        return element;
    }

    /// <summary>
    /// Converts this prime meridian to a WKT syntax tree node.
    /// </summary>
    /// <returns>A <see cref="WktNode"/> representing this prime meridian.</returns>
    public WktNode ToWktNode()
    {
        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            new WktNumber(this.Longitude),
        };

        if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
        {
            children.Add(new WktKeywordNode(
                "AUTHORITY",
                new WktQuotedString(this.Authority),
                new WktQuotedString(this.AuthorityCode.ToString(CultureInfo.InvariantCulture))));
        }

        return new WktKeywordNode("PRIMEM", children);
    }

    /// <summary>
    /// Converts this prime meridian to a WKT syntax tree node for the requested WKT version.
    /// </summary>
    /// <param name="version">The WKT dialect to emit.</param>
    /// <returns>A <see cref="WktNode"/> representing this prime meridian in the requested WKT version.</returns>
    public WktNode ToWktNode(WktVersion version)
    {
        WktVersionSupport.ThrowIfUnknown(version);
        if (version == WktVersion.Wkt1)
        {
            return this.ToWktNode();
        }

        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            new WktNumber(this.Longitude),
            this.AngularUnit.ToWktNode(version),
        };

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("PRIMEM", children);
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        return obj is PrimeMeridian prime && prime.AngularUnit.EqualParams(this.AngularUnit) && prime.Longitude == this.Longitude;
    }
}
