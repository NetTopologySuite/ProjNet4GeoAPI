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
/// A 2D cartographic coordinate system.
/// </summary>
public class ProjectedCoordinateSystem : HorizontalCoordinateSystem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectedCoordinateSystem"/> class.
    /// </summary>
    /// <param name="datum">Horizontal datum.</param>
    /// <param name="geographicCoordinateSystem">Geographic coordinate system.</param>
    /// <param name="linearUnit">Linear unit.</param>
    /// <param name="projection">Projection.</param>
    /// <param name="axisInfo">Axis info.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="code">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    internal ProjectedCoordinateSystem(
        HorizontalDatum datum,
        GeographicCoordinateSystem geographicCoordinateSystem,
        LinearUnit linearUnit,
        IProjection projection,
        List<AxisInfo> axisInfo,
        string name,
        string authority,
        long code,
        string alias,
        string remarks,
        string abbreviation)
        : base(datum, axisInfo, name, authority, code, alias, remarks, abbreviation)
    {
        this.GeographicCoordinateSystem = geographicCoordinateSystem;
        this.LinearUnit = linearUnit;
        this.Projection = projection;
    }

    /// <summary>
    /// Gets a WebMercator coordinate reference system.
    /// </summary>
    public static ProjectedCoordinateSystem WebMercator
    {
        get
        {
            var pInfo = new List<ProjectionParameter>
                {
                    new("latitude_of_origin", 0.0),
                    new("central_meridian", 0.0),
                    new("false_easting", 0.0),
                    new("false_northing", 0.0),
                };

            var proj = new Projection(
                "Popular Visualisation Pseudo-Mercator",
                pInfo,
                "Popular Visualisation Pseudo-Mercator",
                "EPSG",
                3856,
                "Pseudo-Mercator",
                string.Empty,
                string.Empty);

            var axes = new List<AxisInfo>
            {
                new("East", AxisOrientationEnum.East),
                new("North", AxisOrientationEnum.North),
            };

            const string remarks = "Certain Web mapping and visualisation applications. " +
                                   "Uses spherical development of ellipsoidal coordinates. Relative to an ellipsoidal development errors of up to 800 metres in position and 0.7 percent in scale may arise. It is not a recognised geodetic system: see WGS 84 / World Mercator (CRS code 3395).";

            return new ProjectedCoordinateSystem(
                CoordinateSystems.HorizontalDatum.WGS84,
                CoordinateSystems.GeographicCoordinateSystem.WGS84,
                CoordinateSystems.LinearUnit.Metre,
                proj,
                axes,
                "WGS 84 / Pseudo-Mercator",
                "EPSG",
                3857,
                "WGS 84 / Popular Visualisation Pseudo-Mercator",
                remarks,
                "WebMercator");
        }
    }

    /// <summary>
    /// Gets or sets the geographic coordinate system on which this projection is based.
    /// </summary>
    public GeographicCoordinateSystem GeographicCoordinateSystem { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="LinearUnit">LinearUnits</see>. The linear unit must be the same as the <see cref="CoordinateSystem"/> units.
    /// </summary>
    public LinearUnit LinearUnit { get; set; }

    /// <summary>
    /// Gets or sets the projection.
    /// </summary>
    public IProjection Projection { get; set; }

    /// <summary>
    /// Gets the Well-known text for this object
    /// as defined in the simple features specification.
    /// </summary>
    public override string WKT
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "PROJCS[\"{0}\", {1}, {2}", this.Name, this.GeographicCoordinateSystem.WKT, this.Projection.WKT);
            for (int i = 0; i < this.Projection.NumParameters; i++)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat, ", {0}", this.Projection.GetParameter(i).WKT);
            }

            sb.AppendFormat(CultureInfo.InvariantCulture, ", {0}", this.LinearUnit.WKT);

            // Skip axis info if they contain default values
            if (this.AxisInfo.Count != 2 ||
                this.AxisInfo[0].Name != "X" || this.AxisInfo[0].Orientation != AxisOrientationEnum.East ||
                this.AxisInfo[1].Name != "Y" || this.AxisInfo[1].Orientation != AxisOrientationEnum.North)
            {
                for (int i = 0; i < this.AxisInfo.Count; i++)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, ", {0}", this.GetAxis(i).WKT);
                }
            }

            // Skip authority and code if not defined
            if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, ", AUTHORITY[\"{0}\", \"{1}\"]", this.Authority, this.AuthorityCode);
            }

            sb.Append(']');
            return sb.ToString();
        }
    }

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
                "<CS_CoordinateSystem Dimension=\"{0}\"><CS_ProjectedCoordinateSystem>{1}",
                this.Dimension,
                this.InfoXml);
            foreach (AxisInfo ai in this.AxisInfo)
            {
                sb.Append(ai.XML);
            }

            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0}{1}{2}</CS_ProjectedCoordinateSystem></CS_CoordinateSystem>",
                this.GeographicCoordinateSystem.XML,
                this.LinearUnit.XML,
                this.Projection.XML);
            return sb.ToString();
        }
    }

    /// <summary>
    /// Universal Transverse Mercator - WGS84.
    /// </summary>
    /// <param name="zone">UTM zone.</param>
    /// <param name="zoneIsNorth"><see langword="true"/> for the Northern Hemisphere; <see langword="false"/> for the Southern Hemisphere.</param>
    /// <returns>UTM/WGS84 coordsys.</returns>
    public static ProjectedCoordinateSystem WGS84_UTM(int zone, bool zoneIsNorth)
    {
        var pInfo = new List<ProjectionParameter>
        {
            new("latitude_of_origin", 0),
            new("central_meridian", (zone * 6) - 183),
            new("scale_factor", 0.9996),
            new("false_easting", 500000),
            new("false_northing", zoneIsNorth ? 0 : 10000000),
        };

        // IProjection projection = cFac.CreateProjection("UTM" + Zone.ToString() + (ZoneIsNorth ? "N" : "S"), "Transverse_Mercator", parameters);
        var proj = new Projection(
            "Transverse_Mercator",
            pInfo,
            $"UTM{zone.ToString(CultureInfo.InvariantCulture)}{(zoneIsNorth ? "N" : "S")}",
            "EPSG",
            32600 + zone + (zoneIsNorth ? 0 : 100),
            string.Empty,
            string.Empty,
            string.Empty);
        var axes = new List<AxisInfo>
            {
                new("East", AxisOrientationEnum.East),
                new("North", AxisOrientationEnum.North),
            };
        return new ProjectedCoordinateSystem(
            CoordinateSystems.HorizontalDatum.WGS84,
            CoordinateSystems.GeographicCoordinateSystem.WGS84,
            CoordinateSystems.LinearUnit.Metre,
            proj,
            axes,
            $"WGS 84 / UTM zone {zone.ToString(CultureInfo.InvariantCulture)}{(zoneIsNorth ? "N" : "S")}",
            "EPSG",
            32600 + zone + (zoneIsNorth ? 0 : 100),
            string.Empty,
            "Large and medium scale topographic mapping and engineering survey.",
            string.Empty);
    }

    /// <summary>
    /// Returns an XML representation of this projected coordinate system as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>An <see cref="XElement"/> containing the XML representation.</returns>
    public override XElement ToXml()
    {
        var innerElement = new XElement("CS_ProjectedCoordinateSystem");
        innerElement.Add(this.InfoXmlElement);
        foreach (AxisInfo ai in this.AxisInfo)
        {
            innerElement.Add(ai.ToXml());
        }

        innerElement.Add(this.GeographicCoordinateSystem.ToXml());
        innerElement.Add(this.LinearUnit.ToXml());
        if (this.Projection is Projection projection)
        {
            innerElement.Add(projection.ToXml());
        }

        return new XElement(
            "CS_CoordinateSystem",
            new XAttribute("Dimension", this.Dimension.ToString(CultureInfo.InvariantCulture)),
            innerElement);
    }

    /// <inheritdoc />
    public override IUnit GetUnits(int dimension) => this.LinearUnit;

    /// <inheritdoc />
    public override WktNode ToWktNode()
    {
        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            this.GeographicCoordinateSystem.ToWktNode(),
        };

        if (this.Projection is Projection proj)
        {
            children.Add(proj.ToWktNode());
            for (int i = 0; i < proj.NumParameters; i++)
            {
                children.Add(proj.GetParameter(i).ToWktNode());
            }
        }
        else
        {
            children.Add(new WktIdentifier(this.Projection.WKT));
            for (int i = 0; i < this.Projection.NumParameters; i++)
            {
                children.Add(this.Projection.GetParameter(i).ToWktNode());
            }
        }

        children.Add(this.LinearUnit.ToWktNode());

        // Skip axis info if they contain default values
        if (this.AxisInfo.Count != 2 ||
            this.AxisInfo[0].Name != "X" || this.AxisInfo[0].Orientation != AxisOrientationEnum.East ||
            this.AxisInfo[1].Name != "Y" || this.AxisInfo[1].Orientation != AxisOrientationEnum.North)
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

        return new WktKeywordNode("PROJCS", children);
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
            throw new NotSupportedException("WKT2 PROJCRS output currently supports only two-dimensional projected coordinate systems.");
        }

        if (this.AxisInfo.Count != this.Dimension)
        {
            ArgumentGuard.ThrowArgument($"Projected coordinate system '{this.Name}' declared dimension {this.Dimension}, but provides {this.AxisInfo.Count} axes.");
        }

        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            this.GeographicCoordinateSystem.CreateWkt2BaseNode("BASEGEOGCRS"),
            CreateWkt2ConversionNode(this.Projection, this.GeographicCoordinateSystem.AngularUnit, this.LinearUnit),
            new WktKeywordNode(
                "CS",
                new WktIdentifier("Cartesian"),
                new WktInteger(this.Dimension)),
        };

        for (int i = 0; i < this.AxisInfo.Count; i++)
        {
            children.Add(this.GetAxis(i).ToWktNode(version));
        }

        children.Add(this.LinearUnit.ToWktNode(version));

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("PROJCRS", children);
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        if (obj is not ProjectedCoordinateSystem pcs)
        {
            return false;
        }

        if (pcs.Dimension != this.Dimension)
        {
            return false;
        }

        for (int i = 0; i < pcs.Dimension; i++)
        {
            if (pcs.GetAxis(i).Orientation != this.GetAxis(i).Orientation)
            {
                return false;
            }

            if (!pcs.GetUnits(i).EqualParams(this.GetUnits(i)))
            {
                return false;
            }
        }

        return pcs.GeographicCoordinateSystem.EqualParams(this.GeographicCoordinateSystem) &&
                pcs.HorizontalDatum.EqualParams(this.HorizontalDatum) &&
                pcs.LinearUnit.EqualParams(this.LinearUnit) &&
                pcs.Projection.EqualParams(this.Projection);
    }

    private static WktKeywordNode CreateWkt2ConversionNode(IProjection projection, AngularUnit angularUnit, LinearUnit linearUnit)
    {
        string methodKey = NormalizeProjectionMethodKey(projection.ClassName);
        string methodName = GetWkt2ProjectionMethodName(projection.ClassName);
        string conversionName = string.IsNullOrWhiteSpace(projection.Name) ? methodName : projection.Name;

        var children = new List<WktNode>
        {
            new WktQuotedString(conversionName),
            new WktKeywordNode(
                "METHOD",
                new WktQuotedString(methodName)),
        };

        for (int i = 0; i < projection.NumParameters; i++)
        {
            children.Add(CreateWkt2ProjectionParameterNode(methodKey, projection.GetParameter(i), angularUnit, linearUnit));
        }

        return new WktKeywordNode("CONVERSION", children);
    }

    private static WktKeywordNode CreateWkt2ProjectionParameterNode(string methodKey, ProjectionParameter parameter, AngularUnit angularUnit, LinearUnit linearUnit)
    {
        var children = new List<WktNode>
        {
            new WktQuotedString(GetWkt2ProjectionParameterName(methodKey, parameter.Name)),
            new WktNumber(parameter.Value),
        };

        WktNode? unitNode = CreateWkt2ProjectionParameterUnitNode(parameter.Name, angularUnit, linearUnit);
        if (unitNode is not null)
        {
            children.Add(unitNode);
        }

        return new WktKeywordNode("PARAMETER", children);
    }

    private static WktNode? CreateWkt2ProjectionParameterUnitNode(string parameterName, AngularUnit angularUnit, LinearUnit linearUnit)
    {
        string parameterKey = NormalizeProjectionParameterKey(parameterName);
        return parameterKey switch
        {
            "LATITUDE_OF_ORIGIN" or
            "LONGITUDE_OF_ORIGIN" or
            "CENTRAL_MERIDIAN" or
            "STANDARD_PARALLEL_1" or
            "STANDARD_PARALLEL_2" or
            "LATITUDE_OF_CENTER" or
            "LONGITUDE_OF_CENTER" or
            "LATITUDE_OF_PROJECTION_CENTER" or
            "LONGITUDE_OF_PROJECTION_CENTER" or
            "AZIMUTH" or
            "RECTIFIED_GRID_ANGLE" => angularUnit.ToWktNode(WktVersion.Wkt22019),
            "FALSE_EASTING" or
            "FALSE_NORTHING" or
            "EASTING" or
            "NORTHING" or
            "SEMI_MAJOR" or
            "SEMI_MINOR" => linearUnit.ToWktNode(WktVersion.Wkt22019),
            "SCALE_FACTOR" => new WktKeywordNode(
                "SCALEUNIT",
                new WktQuotedString("unity"),
                new WktNumber(1)),
            _ => null,
        };
    }

    private static string GetWkt2ProjectionMethodName(string className)
    {
        return NormalizeProjectionMethodKey(className) switch
        {
            "TRANSVERSE_MERCATOR" => "Transverse Mercator",
            "LAMBERT_CONFORMAL_CONIC_1SP" => "Lambert Conic Conformal (1SP)",
            "LAMBERT_CONFORMAL_CONIC_2SP" => "Lambert Conic Conformal (2SP)",
            "MERCATOR_1SP" => "Mercator (variant A)",
            "MERCATOR_2SP" => "Mercator (variant B)",
            "CASSINI_SOLDNER" => "Cassini-Soldner",
            "ALBERS_CONIC_EQUAL_AREA" => "Albers Equal Area",
            "OBLIQUE_STEREOGRAPHIC" => "Oblique Stereographic",
            "LAMBERT_AZIMUTHAL_EQUAL_AREA" => "Lambert Azimuthal Equal Area",
            "KROVAK" => "Krovak",
            "POPULAR_VISUALISATION_PSEUDO_MERCATOR" => "Popular Visualisation Pseudo Mercator",
            _ => className,
        };
    }

    private static string GetWkt2ProjectionParameterName(string methodKey, string parameterName)
    {
        string parameterKey = NormalizeProjectionParameterKey(parameterName);
        return methodKey switch
        {
            "LAMBERT_CONFORMAL_CONIC_2SP" => parameterKey switch
            {
                "LATITUDE_OF_ORIGIN" => "Latitude of false origin",
                "CENTRAL_MERIDIAN" => "Longitude of false origin",
                "STANDARD_PARALLEL_1" => "Latitude of 1st standard parallel",
                "STANDARD_PARALLEL_2" => "Latitude of 2nd standard parallel",
                "FALSE_EASTING" => "Easting at false origin",
                "FALSE_NORTHING" => "Northing at false origin",
                _ => GetDefaultWkt2ProjectionParameterName(parameterKey, parameterName),
            },
            _ => GetDefaultWkt2ProjectionParameterName(parameterKey, parameterName),
        };
    }

    private static string GetDefaultWkt2ProjectionParameterName(string parameterKey, string parameterName)
    {
        return parameterKey switch
        {
            "LATITUDE_OF_ORIGIN" => "Latitude of natural origin",
            "LONGITUDE_OF_ORIGIN" or "CENTRAL_MERIDIAN" => "Longitude of natural origin",
            "STANDARD_PARALLEL_1" => "Latitude of 1st standard parallel",
            "STANDARD_PARALLEL_2" => "Latitude of 2nd standard parallel",
            "FALSE_EASTING" => "False easting",
            "FALSE_NORTHING" => "False northing",
            "SCALE_FACTOR" => "Scale factor at natural origin",
            "LATITUDE_OF_CENTER" or "LATITUDE_OF_PROJECTION_CENTER" => "Latitude of projection centre",
            "LONGITUDE_OF_CENTER" or "LONGITUDE_OF_PROJECTION_CENTER" => "Longitude of projection centre",
            "AZIMUTH" => "Azimuth of initial line",
            "RECTIFIED_GRID_ANGLE" => "Angle from Rectified to Skew Grid",
            _ => parameterName,
        };
    }

    private static string NormalizeProjectionMethodKey(string className)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            return string.Empty;
        }

        string normalized = className
            .ToUpperInvariant()
            .Trim();
        normalized = normalized.Replace("(", string.Empty);
        normalized = normalized.Replace(")", string.Empty);
        normalized = normalized.Replace("-", "_");
        normalized = normalized.Replace("/", "_");
        normalized = normalized.Replace(" ", "_");

        while (normalized.IndexOf("__", StringComparison.Ordinal) >= 0)
        {
            normalized = normalized.Replace("__", "_");
        }

        return normalized switch
        {
            "LAMBERT_CONIC_CONFORMAL_1SP" => "LAMBERT_CONFORMAL_CONIC_1SP",
            "LAMBERT_CONIC_CONFORMAL_2SP" => "LAMBERT_CONFORMAL_CONIC_2SP",
            "MERCATOR_VARIANT_A" => "MERCATOR_1SP",
            "MERCATOR_VARIANT_B" => "MERCATOR_2SP",
            _ => normalized,
        };
    }

    private static string NormalizeProjectionParameterKey(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return string.Empty;
        }

        string normalized = parameterName
            .ToUpperInvariant()
            .Trim();
        normalized = normalized.Replace("(", string.Empty);
        normalized = normalized.Replace(")", string.Empty);
        normalized = normalized.Replace("-", "_");
        normalized = normalized.Replace("/", "_");
        normalized = normalized.Replace(" ", "_");
        normalized = normalized.Replace(".", "_");

        while (normalized.IndexOf("__", StringComparison.Ordinal) >= 0)
        {
            normalized = normalized.Replace("__", "_");
        }

        return normalized;
    }
}
