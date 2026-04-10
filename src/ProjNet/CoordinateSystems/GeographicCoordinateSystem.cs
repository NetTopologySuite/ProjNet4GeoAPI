// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using ProjNet.IO.Wkt;

/// <summary>
/// A coordinate system based on latitude and longitude.
/// </summary>
/// <remarks>
/// Some geographic coordinate systems are Lat/Lon, and some are Lon/Lat.
/// You can find out which this is by examining the axes. You should also
/// check the angular units, since not all geographic coordinate systems
/// use degrees.
/// </remarks>
public class GeographicCoordinateSystem : HorizontalCoordinateSystem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeographicCoordinateSystem"/> class.
    /// </summary>
    /// <param name="angularUnit">Angular units.</param>
    /// <param name="horizontalDatum">Horizontal datum.</param>
    /// <param name="primeMeridian">Prime meridian.</param>
    /// <param name="axisInfo">Axis info.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    internal GeographicCoordinateSystem(
        AngularUnit angularUnit,
        HorizontalDatum horizontalDatum,
        PrimeMeridian primeMeridian,
        List<AxisInfo> axisInfo,
        string name,
        string authority,
        long authorityCode,
        string alias,
        string abbreviation,
        string remarks)
        : base(horizontalDatum, axisInfo, name, authority, authorityCode, alias, remarks, abbreviation)
    {
        this.AngularUnit = angularUnit;
        this.PrimeMeridian = primeMeridian;
        this.WGS84ConversionInfo = [];
    }

    /// <summary>
    /// Gets a decimal degrees geographic coordinate system based on the WGS84 ellipsoid, suitable for GPS measurements.
    /// </summary>
    public static GeographicCoordinateSystem WGS84
    {
        get
        {
            var axes = new List<AxisInfo>(2)
            {
                new("Lon", AxisOrientationEnum.East),
                new("Lat", AxisOrientationEnum.North),
            };
            return new GeographicCoordinateSystem(
                CoordinateSystems.AngularUnit.Degrees,
                CoordinateSystems.HorizontalDatum.WGS84,
                CoordinateSystems.PrimeMeridian.Greenwich,
                axes,
                "WGS 84",
                "EPSG",
                4326,
                string.Empty,
                string.Empty,
                string.Empty);
        }
    }

    /// <summary>
    /// Gets or sets the angular units of the geographic coordinate system.
    /// </summary>
    public AngularUnit AngularUnit { get; set; }

    /// <summary>
    /// Gets or sets the prime meridian of the geographic coordinate system.
    /// </summary>
    public PrimeMeridian PrimeMeridian { get; set; }

    /// <summary>
    /// Gets the number of available conversions to WGS84 coordinates.
    /// </summary>
    public int NumConversionToWGS84
    {
        get { return this.WGS84ConversionInfo.Count; }
    }

    /// <summary>
    /// Gets or sets the WGS84 conversion definitions.
    /// </summary>
    internal List<Wgs84ConversionInfo> WGS84ConversionInfo { get; set; }

    /// <summary>
    /// Gets the Well-known text for this object
    /// as defined in the simple features specification.
    /// </summary>
    public override string WKT => this.ToWktNode().ToString();

    /// <summary>
    /// Gets an XML representation of this object.
    /// </summary>
    public override string XML
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendFormat(
                CultureInfo.InvariantCulture.NumberFormat,
                "<CS_CoordinateSystem Dimension=\"{0}\"><CS_GeographicCoordinateSystem>{1}",
                this.Dimension,
                this.InfoXml);
            foreach (AxisInfo ai in this.AxisInfo)
            {
                sb.Append(ai.XML);
            }

            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0}{1}{2}</CS_GeographicCoordinateSystem></CS_CoordinateSystem>",
                this.HorizontalDatum.XML,
                this.AngularUnit.XML,
                this.PrimeMeridian.XML);
            return sb.ToString();
        }
    }

    /// <summary>
    /// Returns an XML representation of this geographic coordinate system as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>An <see cref="XElement"/> containing the XML representation.</returns>
    public override XElement ToXml()
    {
        var innerElement = new XElement("CS_GeographicCoordinateSystem");
        innerElement.Add(this.InfoXmlElement);
        foreach (AxisInfo ai in this.AxisInfo)
        {
            innerElement.Add(ai.ToXml());
        }

        innerElement.Add(this.HorizontalDatum.ToXml());
        innerElement.Add(this.AngularUnit.ToXml());
        innerElement.Add(this.PrimeMeridian.ToXml());

        return new XElement(
            "CS_CoordinateSystem",
            new XAttribute("Dimension", this.Dimension.ToString(CultureInfo.InvariantCulture)),
            innerElement);
    }

    /// <inheritdoc />
    public override IUnit GetUnits(int dimension) => this.AngularUnit;

    /// <inheritdoc />
    public override WktNode ToWktNode()
    {
        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            this.HorizontalDatum.ToWktNode(),
            this.PrimeMeridian.ToWktNode(),
            this.AngularUnit.ToWktNode(),
        };

        // Skip axis info if they contain default values
        if (this.AxisInfo.Count != 2 ||
            this.AxisInfo[0].Name != "Lon" || this.AxisInfo[0].Orientation != AxisOrientationEnum.East ||
            this.AxisInfo[1].Name != "Lat" || this.AxisInfo[1].Orientation != AxisOrientationEnum.North)
        {
            for (int i = 0; i < this.AxisInfo.Count; i++)
            {
                children.Add(this.GetAxis(i).ToWktNode());
            }
        }

        if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
        {
            children.Add(new WktKeywordNode(
                "AUTHORITY",
                new WktQuotedString(this.Authority),
                new WktQuotedString(this.AuthorityCode.ToString(CultureInfo.InvariantCulture))));
        }

        return new WktKeywordNode("GEOGCS", children);
    }

    /// <inheritdoc />
    public override WktNode ToWktNode(WktVersion version)
    {
        WktVersionSupport.ThrowIfUnknown(version);
        if (version == WktVersion.Wkt1)
        {
            return this.ToWktNode();
        }

        if (this.Dimension != 2)
        {
            throw new NotSupportedException("WKT2 GEOGCRS output currently supports only two-dimensional geographic coordinate systems.");
        }

        if (this.WGS84ConversionInfo.Count > 0 || this.HorizontalDatum.Wgs84Parameters is not null)
        {
            BoundCoordinateSystem boundCoordinateSystem = BoundCoordinateSystemSupport.CreateLegacyBoundCoordinateSystemForSerialization(this)
                ?? throw new NotSupportedException("WKT2 GEOGCRS output for coordinate systems with WGS84 conversion parameters could not be normalized to BOUNDCRS.");
            return BoundCoordinateSystemSupport.CreateWkt2BoundCoordinateSystemNode(boundCoordinateSystem);
        }

        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            this.HorizontalDatum.ToWktNode(version),
        };

        if (!this.PrimeMeridian.EqualParams(PrimeMeridian.Greenwich))
        {
            children.Add(this.PrimeMeridian.ToWktNode(version));
        }

        children.Add(new WktKeywordNode(
            "CS",
            new WktIdentifier("ellipsoidal"),
            new WktInteger(this.Dimension)));

        for (int i = 0; i < this.AxisInfo.Count; i++)
        {
            children.Add(this.GetAxis(i).ToWktNode(version));
        }

        children.Add(this.AngularUnit.ToWktNode(version));

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("GEOGCRS", children);
    }

    /// <summary>
    /// Creates a WKT2 base geographic CRS node for use inside compound WKT2 coordinate-system constructs.
    /// </summary>
    /// <param name="keyword">The WKT2 keyword to emit, for example <c>BASEGEOGCRS</c>.</param>
    /// <returns>A WKT2 geographic base node without the top-level <c>CS</c>, <c>AXIS</c>, and root angle-unit blocks.</returns>
    internal WktKeywordNode CreateWkt2BaseNode(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            ArgumentGuard.ThrowArgument("Invalid WKT2 base keyword.", nameof(keyword));
        }

        if (this.Dimension != 2)
        {
            throw new NotSupportedException("WKT2 geographic base output currently supports only two-dimensional geographic coordinate systems.");
        }

        if (this.WGS84ConversionInfo.Count > 0 || this.HorizontalDatum.Wgs84Parameters is not null)
        {
            throw new NotSupportedException("WKT2 geographic base output for coordinate systems with WGS84 conversion parameters is not supported inside BASEGEOGCRS. Serialize the top-level CRS as BOUNDCRS instead.");
        }

        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            this.HorizontalDatum.ToWktNode(WktVersion.Wkt22019),
        };

        if (!this.PrimeMeridian.EqualParams(PrimeMeridian.Greenwich))
        {
            children.Add(this.PrimeMeridian.ToWktNode(WktVersion.Wkt22019));
        }

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode(keyword, children);
    }

    /// <summary>
    /// Gets details on a conversion to WGS84.
    /// </summary>
    /// <param name="index">Zero-based index of the WGS84 conversion definition.</param>
    /// <returns>The <see cref="Wgs84ConversionInfo"/> at the specified index.</returns>
    public Wgs84ConversionInfo GetWgs84ConversionInfo(int index) => this.WGS84ConversionInfo[index];

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        if (obj is not GeographicCoordinateSystem gcs)
        {
            return false;
        }

        if (gcs.Dimension != this.Dimension)
        {
            return false;
        }

        if (this.WGS84ConversionInfo.Count != gcs.WGS84ConversionInfo.Count)
        {
            return false;
        }

        for (int i = 0; i < this.WGS84ConversionInfo.Count; i++)
        {
            if (!gcs.WGS84ConversionInfo[i].Equals(this.WGS84ConversionInfo[i]))
            {
                return false;
            }
        }

        if (this.AxisInfo.Count != gcs.AxisInfo.Count)
        {
            return false;
        }

        for (int i = 0; i < gcs.AxisInfo.Count; i++)
        {
            if (gcs.AxisInfo[i].Orientation != this.AxisInfo[i].Orientation)
            {
                return false;
            }
        }

        return gcs.AngularUnit.EqualParams(this.AngularUnit) &&
                gcs.HorizontalDatum.EqualParams(this.HorizontalDatum) &&
                gcs.PrimeMeridian.EqualParams(this.PrimeMeridian);
    }
}
