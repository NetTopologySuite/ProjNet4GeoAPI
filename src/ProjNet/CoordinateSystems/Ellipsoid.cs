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
/// Defines the standard information stored with an ellipsoid used as the reference surface for a geodetic datum.
/// </summary>
/// <remarks>
/// <para>
/// Thread safety: Instances are immutable after construction and may be shared across threads.
/// The predefined ellipsoid accessors are thread-safe because they only expose immutable value objects.
/// </para>
/// </remarks>
public class Ellipsoid : Info
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Ellipsoid"/> class.
    /// </summary>
    /// <param name="semiMajorAxis">Semi major axis.</param>
    /// <param name="semiMinorAxis">Semi minor axis.</param>
    /// <param name="inverseFlattening">Inverse flattening.</param>
    /// <param name="isIvfDefinitive">Inverse Flattening is definitive for this ellipsoid (Semi-minor axis will be overridden).</param>
    /// <param name="axisUnit">Axis unit.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="code">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    internal Ellipsoid(
        double semiMajorAxis,
        double semiMinorAxis,
        double inverseFlattening,
        bool isIvfDefinitive,
        LinearUnit axisUnit,
        string name,
        string authority,
        long code,
        string alias,
        string abbreviation,
        string remarks)
        : base(name, authority, code, alias, abbreviation, remarks)
    {
        this.SemiMajorAxis = semiMajorAxis;
        this.InverseFlattening = inverseFlattening;
        this.AxisUnit = axisUnit;
        this.IsIvfDefinitive = isIvfDefinitive;
        if (isIvfDefinitive && (inverseFlattening == 0 || double.IsInfinity(inverseFlattening)))
        {
            this.SemiMinorAxis = semiMajorAxis;
        }
        else if (isIvfDefinitive)
        {
            this.SemiMinorAxis = (1.0 - (1.0 / this.InverseFlattening)) * semiMajorAxis;
        }
        else
        {
            this.SemiMinorAxis = semiMinorAxis;
        }
    }

    /// <summary>
    /// Gets the Airy 1830 ellipsoid.
    /// </summary>
    public static Ellipsoid Airy1830
    {
        get
        {
            return new Ellipsoid(
                6377563.396d,
                0d,
                299.3249646d,
                true,
                LinearUnit.Metre,
                "Airy 1830",
                "EPSG",
                7001,
                string.Empty,
                string.Empty,
                string.Empty);
        }
    }

    /// <summary>
    /// Gets the WGS 84 ellipsoid.
    /// </summary>
    /// <remarks>
    /// <para>Inverse flattening derived from four defining parameters
    /// (semi-major axis;
    /// C20 = -484.16685*10e-6;
    /// earth's angular velocity w = 7292115e11 rad/sec;
    /// gravitational constant GM = 3986005e8 m*m*m/s/s).</para>
    /// <para>This convenience accessor intentionally retains the legacy runtime alias and remarks metadata instead of
    /// exposing the generated EPSG catalog entry verbatim, so existing callers keep the established public representation
    /// while the higher-level WGS84 CRS statics resolve through the catalog.</para>
    /// </remarks>
    public static Ellipsoid WGS84
    {
        get
        {
            // Keep the legacy public representation stable even though the generated catalog can resolve EPSG:7030.
            return new Ellipsoid(
                6378137,
                0,
                298.257223563,
                true,
                LinearUnit.Metre,
                "WGS 84",
                "EPSG",
                7030,
                "WGS84",
                string.Empty,
                "Inverse flattening derived from four defining parameters (semi-major axis; C20 = -484.16685*10e-6; earth's angular velocity w = 7292115e11 rad/sec; gravitational constant GM = 3986005e8 m*m*m/s/s).");
        }
    }

    /// <summary>
    /// Gets the WGS 72 ellipsoid.
    /// </summary>
    public static Ellipsoid WGS72
    {
        get
        {
            return new Ellipsoid(
                6378135.0,
                0,
                298.26,
                true,
                LinearUnit.Metre,
                "WGS 72",
                "EPSG",
                7043,
                "WGS 72",
                string.Empty,
                string.Empty);
        }
    }

    /// <summary>
    /// Gets the GRS 1980 / International 1979 ellipsoid.
    /// </summary>
    /// <remarks>
    /// Adopted by IUGG 1979 Canberra.
    /// Inverse flattening is derived from
    /// geocentric gravitational constant GM = 3986005e8 m*m*m/s/s;
    /// dynamic form factor J2 = 108263e8 and Earth's angular velocity = 7292115e-11 rad/s.").
    /// </remarks>
    public static Ellipsoid GRS80
    {
        get
        {
            return new Ellipsoid(
                6378137,
                0,
                298.257222101,
                true,
                LinearUnit.Metre,
                "GRS 1980",
                "EPSG",
                7019,
                "International 1979",
                string.Empty,
                "Adopted by IUGG 1979 Canberra.  Inverse flattening is derived from geocentric gravitational constant GM = 3986005e8 m*m*m/s/s; dynamic form factor J2 = 108263e8 and Earth's angular velocity = 7292115e-11 rad/s.");
        }
    }

    /// <summary>
    /// Gets the International 1924 / Hayford 1909 ellipsoid.
    /// </summary>
    /// <remarks>
    /// Described as a=6378388 m. and b=6356909m. from which 1/f derived to be 296.95926.
    /// The figure was adopted as the International ellipsoid in 1924 but with 1/f taken as
    /// 297 exactly from which b is derived as 6356911.946m.
    /// </remarks>
    public static Ellipsoid International1924
    {
        get
        {
            return new Ellipsoid(
                6378388,
                0,
                297,
                true,
                LinearUnit.Metre,
                "International 1924",
                "EPSG",
                7022,
                "Hayford 1909",
                string.Empty,
                "Described as a=6378388 m. and b=6356909 m. from which 1/f derived to be 296.95926. The figure was adopted as the International ellipsoid in 1924 but with 1/f taken as 297 exactly from which b is derived as 6356911.946m.");
        }
    }

    /// <summary>
    /// Gets the Bessel 1841 ellipsoid.
    /// </summary>
    public static Ellipsoid Bessel1841
    {
        get
        {
            return new Ellipsoid(
                6377397.155d,
                0d,
                299.1528128d,
                true,
                LinearUnit.Metre,
                "Bessel 1841",
                "EPSG",
                7004,
                string.Empty,
                string.Empty,
                string.Empty);
        }
    }

    /// <summary>
    /// Gets the Clarke 1880 ellipsoid.
    /// </summary>
    /// <remarks>
    /// Clarke gave a and b and also 1/f=293.465 (to 3 decimal places).  1/f derived from a and b = 293.4663077.
    /// </remarks>
    public static Ellipsoid Clarke1880
    {
        get
        {
            return new Ellipsoid(
                20926202,
                0,
                297,
                true,
                LinearUnit.ClarkesFoot,
                "Clarke 1880",
                "EPSG",
                7034,
                "Clarke 1880",
                string.Empty,
                "Clarke gave a and b and also 1/f=293.465 (to 3 decimal places).  1/f derived from a and b = 293.4663077�");
        }
    }

    /// <summary>
    /// Gets the Clarke 1866 ellipsoid.
    /// </summary>
    /// <remarks>
    /// Original definition a=20926062 and b=20855121 (British) feet. Uses Clarke's 1865 inch-metre ratio of 39.370432 to obtain metres. (Metric value then converted to US survey feet for use in the United States using 39.37 exactly giving a=20925832.16 ft US).
    /// </remarks>
    public static Ellipsoid Clarke1866
    {
        get
        {
            return new Ellipsoid(
                6378206.4,
                6356583.8,
                double.PositiveInfinity,
                false,
                LinearUnit.Metre,
                "Clarke 1866",
                "EPSG",
                7008,
                "Clarke 1866",
                string.Empty,
                "Original definition a=20926062 and b=20855121 (British) feet. Uses Clarke's 1865 inch-metre ratio of 39.370432 to obtain metres. (Metric value then converted to US survey feet for use in the United States using 39.37 exactly giving a=20925832.16 ft US).");
        }
    }

    /// <summary>
    /// Gets the GRS 1980 Authalic Sphere.
    /// </summary>
    /// <remarks>
    /// Authalic sphere derived from GRS 1980 ellipsoid (code 7019).  (An authalic sphere is
    /// one with a surface area equal to the surface area of the ellipsoid). 1/f is infinite.
    /// </remarks>
    public static Ellipsoid Sphere
    {
        get
        {
            return new Ellipsoid(
                6370997.0,
                6370997.0,
                double.PositiveInfinity,
                false,
                LinearUnit.Metre,
                "GRS 1980 Authalic Sphere",
                "EPSG",
                7048,
                "Sphere",
                string.Empty,
                "Authalic sphere derived from GRS 1980 ellipsoid (code 7019).  (An authalic sphere is one with a surface area equal to the surface area of the ellipsoid). 1/f is infinite.");
        }
    }

    /// <summary>
    /// Gets the value of the semi-major axis.
    /// </summary>
    public double SemiMajorAxis { get; }

    /// <summary>
    /// Gets the value of the semi-minor axis.
    /// </summary>
    public double SemiMinorAxis { get; }

    /// <summary>
    /// Gets the value of the inverse of the flattening constant of the ellipsoid.
    /// </summary>
    public double InverseFlattening { get; }

    /// <summary>
    /// Gets the value of the axis unit.
    /// </summary>
    public LinearUnit AxisUnit { get; }

    /// <summary>
    /// Gets a value indicating whether the inverse flattening value is the defining parameter for this ellipsoid.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, the semi-minor axis is derived from the inverse flattening value.
    /// When <see langword="false"/>, the inverse flattening is derived from the semi-minor axis.
    /// This distinction can be important to avoid floating-point rounding errors.
    /// </remarks>
    public bool IsIvfDefinitive { get; }

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
    /// Creates a copy of this ellipsoid with updated authority metadata.
    /// </summary>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="code">Replacement authority-specific identification code.</param>
    /// <returns>A new <see cref="Ellipsoid"/> with updated authority metadata.</returns>
    public new Ellipsoid WithAuthority(string authority, long code) => InfoAuthorityCloneHelper.CloneWithAuthority(this, authority, code);

    /// <summary>
    /// Creates a copy of this ellipsoid with an updated name.
    /// </summary>
    /// <param name="name">Replacement name.</param>
    /// <returns>A new <see cref="Ellipsoid"/> with the updated name.</returns>
    public new Ellipsoid WithName(string name) => InfoAuthorityCloneHelper.CloneWithName(this, name);

    /// <summary>
    /// Returns an XML representation of this ellipsoid as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>An <see cref="XElement"/> containing the XML representation.</returns>
    public XElement ToXml()
    {
        var element = new XElement(
            "CS_Ellipsoid",
            new XAttribute("SemiMajorAxis", this.SemiMajorAxis.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("SemiMinorAxis", this.SemiMinorAxis.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("InverseFlattening", this.InverseFlattening.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("IvfDefinitive", this.IsIvfDefinitive ? "1" : "0"));
        element.Add(this.InfoXmlElement);
        element.Add(this.AxisUnit.ToXml());
        return element;
    }

    /// <summary>
    /// Converts this ellipsoid to a WKT syntax tree node.
    /// </summary>
    /// <returns>A <see cref="WktNode"/> representing this ellipsoid.</returns>
    public WktNode ToWktNode()
    {
        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            new WktNumber(this.SemiMajorAxis),
            new WktNumber(this.InverseFlattening),
        };

        if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
        {
            children.Add(new WktKeywordNode(
                "AUTHORITY",
                new WktQuotedString(this.Authority),
                new WktQuotedString(this.AuthorityCode.ToString(CultureInfo.InvariantCulture))));
        }

        return new WktKeywordNode("SPHEROID", children);
    }

    /// <summary>
    /// Converts this ellipsoid to a WKT syntax tree node for the requested WKT version.
    /// </summary>
    /// <param name="version">The WKT dialect to emit.</param>
    /// <returns>A <see cref="WktNode"/> representing this ellipsoid in the requested WKT version.</returns>
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
            new WktNumber(this.SemiMajorAxis),
            new WktNumber(this.InverseFlattening),
            this.AxisUnit.ToWktNode(version),
        };

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("ELLIPSOID", children);
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        return obj is Ellipsoid ellipsoid && ellipsoid.InverseFlattening == this.InverseFlattening &&
                ellipsoid.IsIvfDefinitive == this.IsIvfDefinitive &&
                ellipsoid.SemiMajorAxis == this.SemiMajorAxis &&
                ellipsoid.SemiMinorAxis == this.SemiMinorAxis &&
                ellipsoid.AxisUnit.EqualParams(this.AxisUnit);
    }

    /// <inheritdoc />
    private protected override Info CloneWithAuthorityCore(string authority, long code) => this.WithAuthority(authority, code);

    /// <inheritdoc />
    private protected override Info CloneWithNameCore(string name) => this.WithName(name);
}
