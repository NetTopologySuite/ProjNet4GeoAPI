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
/// A 2D cartographic coordinate system.
/// </summary>
/// <remarks>
/// <para>
/// Thread safety: Instances are immutable after construction and may be shared across threads.
/// <see cref="WebMercator"/> is initialized once and then reused safely. The
/// <c>WGS84_UTM(int, bool)</c> helper is also thread-safe for concurrent callers; the generated
/// EPSG cache may synchronize the first materialization of a requested UTM SRID, but steady-state
/// access remains lock-free.
/// </para>
/// </remarks>
public class ProjectedCoordinateSystem : HorizontalCoordinateSystem
{
    private const string LegacyWebMercatorAlias = "WGS 84 / Popular Visualisation Pseudo-Mercator";

    private const string LegacyWebMercatorAbbreviation = "WebMercator";

    private const string LegacyWebMercatorRemarks = "Certain Web mapping and visualisation applications. " +
                                                    "Uses spherical development of ellipsoidal coordinates. Relative to an ellipsoidal development errors of up to 800 metres in position and 0.7 percent in scale may arise. It is not a recognised geodetic system: see WGS 84 / World Mercator (CRS code 3395).";

    private const string LegacyWgs84UtmRemarks = "Large and medium scale topographic mapping and engineering survey.";

    private static readonly Lazy<ProjectedCoordinateSystem> WebMercatorCoordinateSystem =
        new(CreateWebMercatorCoordinateSystem, true);

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
    /// <param name="remarks">Provider-supplied remarks.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="defaultEnvelope">Default envelope for the coordinate system domain.</param>
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
        string abbreviation,
        double[]? defaultEnvelope = null)
        : base(datum, axisInfo, name, authority, code, alias, remarks, abbreviation, defaultEnvelope)
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
        get { return WebMercatorCoordinateSystem.Value; }
    }

    /// <summary>
    /// Gets the geographic coordinate system on which this projection is based.
    /// </summary>
    public GeographicCoordinateSystem GeographicCoordinateSystem { get; }

    /// <summary>
    /// Gets the <see cref="LinearUnit">LinearUnits</see>. The linear unit must be the same as the <see cref="CoordinateSystem"/> units.
    /// </summary>
    public LinearUnit LinearUnit { get; }

    /// <summary>
    /// Gets the projection.
    /// </summary>
    public IProjection Projection { get; }

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
    /// Universal Transverse Mercator - WGS84.
    /// </summary>
    /// <param name="zone">UTM zone.</param>
    /// <param name="zoneIsNorth"><see langword="true"/> for the Northern Hemisphere; <see langword="false"/> for the Southern Hemisphere.</param>
    /// <returns>UTM/WGS84 coordsys.</returns>
    public static ProjectedCoordinateSystem WGS84_UTM(int zone, bool zoneIsNorth)
    {
        return CreateWgs84UtmCoordinateSystem(zone, zoneIsNorth);
    }

    /// <summary>
    /// Creates a copy of this coordinate system with updated authority metadata.
    /// </summary>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="code">Replacement authority-specific identification code.</param>
    /// <returns>A new <see cref="ProjectedCoordinateSystem"/> with updated authority metadata.</returns>
    public new ProjectedCoordinateSystem WithAuthority(string authority, long code) => InfoAuthorityCloneHelper.CloneWithAuthority(this, authority, code);

    /// <summary>
    /// Creates a copy of this coordinate system with an updated name.
    /// </summary>
    /// <param name="name">Replacement name.</param>
    /// <returns>A new <see cref="ProjectedCoordinateSystem"/> with the updated name.</returns>
    public new ProjectedCoordinateSystem WithName(string name) => InfoAuthorityCloneHelper.CloneWithName(this, name);

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
        else
        {
            innerElement.Add(XElement.Parse(this.Projection.XML));
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
            throw new InvalidOperationException($"Projected coordinate system '{this.Name}' declared dimension {this.Dimension}, but provides {this.AxisInfo.Count} axes.");
        }

        BoundCoordinateSystem? boundCoordinateSystem = BoundCoordinateSystemSupport.CreateLegacyBoundCoordinateSystemForSerialization(this);
        if (boundCoordinateSystem is not null)
        {
            return BoundCoordinateSystemSupport.CreateWkt2BoundCoordinateSystemNode(boundCoordinateSystem);
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

    /// <summary>
    /// Creates a WKT2 base projected CRS node for use inside derived WKT2 coordinate-system constructs.
    /// </summary>
    /// <param name="keyword">The WKT2 keyword to emit, for example <c>BASEPROJCRS</c>.</param>
    /// <returns>A WKT2 projected base node without the surrounding top-level derived-CRS wrapper.</returns>
    internal WktKeywordNode CreateWkt2BaseNode(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            ArgumentGuard.ThrowArgument("Invalid WKT2 base keyword.", nameof(keyword));
        }

        if (this.Dimension != 2)
        {
            throw new NotSupportedException("WKT2 projected base output currently supports only two-dimensional projected coordinate systems.");
        }

        if (this.AxisInfo.Count != this.Dimension)
        {
            throw new InvalidOperationException($"Projected coordinate system '{this.Name}' declared dimension {this.Dimension}, but provides {this.AxisInfo.Count} axes.");
        }

        if (BoundCoordinateSystemSupport.CreateLegacyBoundCoordinateSystemForSerialization(this) is not null)
        {
            throw new NotSupportedException("WKT2 projected base output for coordinate systems with retained bound metadata is not supported inside BASEPROJCRS. Serialize the top-level CRS as BOUNDCRS instead.");
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
            children.Add(this.GetAxis(i).ToWktNode(WktVersion.Wkt22019));
        }

        children.Add(this.LinearUnit.ToWktNode(WktVersion.Wkt22019));

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode(keyword, children);
    }

    private static WktKeywordNode CreateWkt2ConversionNode(IProjection projection, AngularUnit angularUnit, LinearUnit linearUnit)
    {
        string methodKey = ProjectionSerializationSupport.NormalizeMethodKey(projection.ClassName);
        string methodName = ProjectionSerializationSupport.GetMethodName(projection.ClassName);
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
            new WktQuotedString(ProjectionSerializationSupport.GetParameterName(methodKey, parameter.Name)),
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
        if (ProjectionSerializationSupport.ParameterUsesAngularUnit(parameterName))
        {
            return angularUnit.ToWktNode(WktVersion.Wkt22019);
        }

        if (ProjectionSerializationSupport.ParameterUsesLinearUnit(parameterName))
        {
            return linearUnit.ToWktNode(WktVersion.Wkt22019);
        }

        return ProjectionSerializationSupport.ParameterUsesScaleUnit(parameterName)
            ? new WktKeywordNode(
                "SCALEUNIT",
                new WktQuotedString("unity"),
                new WktNumber(1))
            : null;
    }

    private static ProjectedCoordinateSystem CreateWebMercatorCoordinateSystem()
    {
        return Wgs84CatalogBootstrap.TryGetCoordinateSystem(
            Wgs84CatalogBootstrap.WebMercatorSrid,
            out ProjectedCoordinateSystem? coordinateSystem)
            ? NormalizeToLegacyWebMercatorShape(coordinateSystem)
            : throw new InvalidOperationException("The generated EPSG catalog could not resolve the WGS 84 / Pseudo-Mercator projected coordinate system.");
    }

    private static ProjectedCoordinateSystem NormalizeToLegacyWebMercatorShape(ProjectedCoordinateSystem coordinateSystem)
    {
        GeographicCoordinateSystem geographicCoordinateSystem = GeographicCoordinateSystem.WGS84;
        return new ProjectedCoordinateSystem(
            geographicCoordinateSystem.HorizontalDatum,
            geographicCoordinateSystem,
            coordinateSystem.LinearUnit,
            new Projection(
                "Popular Visualisation Pseudo-Mercator",
                CopyProjectionParameters(coordinateSystem.Projection),
                "Popular Visualisation Pseudo-Mercator",
                "EPSG",
                3856,
                "Pseudo-Mercator",
                string.Empty,
                string.Empty),
            [new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North)],
            coordinateSystem.Name,
            coordinateSystem.Authority,
            coordinateSystem.AuthorityCode,
            LegacyWebMercatorAlias,
            LegacyWebMercatorRemarks,
            LegacyWebMercatorAbbreviation);
    }

    private static List<ProjectionParameter> CopyProjectionParameters(IProjection projection)
    {
        var parameters = new List<ProjectionParameter>(projection.NumParameters);
        for (int i = 0; i < projection.NumParameters; i++)
        {
            ProjectionParameter parameter = projection.GetParameter(i);
            parameters.Add(new ProjectionParameter(parameter.Name, parameter.Value));
        }

        return parameters;
    }

    private static ProjectedCoordinateSystem CreateWgs84UtmCoordinateSystem(int zone, bool zoneIsNorth)
    {
        int srid = GetWgs84UtmSrid(zone, zoneIsNorth);
        return Wgs84CatalogBootstrap.TryGetCoordinateSystem(
            srid,
            out ProjectedCoordinateSystem? coordinateSystem)
            ? NormalizeToLegacyWgs84UtmShape(coordinateSystem, zone, zoneIsNorth, srid)
            : throw new InvalidOperationException($"The generated EPSG catalog could not resolve the WGS 84 / UTM zone {zone.ToString(CultureInfo.InvariantCulture)}{(zoneIsNorth ? "N" : "S")} projected coordinate system.");
    }

    private static ProjectedCoordinateSystem NormalizeToLegacyWgs84UtmShape(ProjectedCoordinateSystem coordinateSystem, int zone, bool zoneIsNorth, int srid)
    {
        GeographicCoordinateSystem geographicCoordinateSystem = GeographicCoordinateSystem.WGS84;
        return new ProjectedCoordinateSystem(
            geographicCoordinateSystem.HorizontalDatum,
            geographicCoordinateSystem,
            coordinateSystem.LinearUnit,
            new Projection(
                "Transverse_Mercator",
                CopyProjectionParameters(coordinateSystem.Projection),
                CreateLegacyUtmProjectionName(zone, zoneIsNorth),
                "EPSG",
                srid,
                string.Empty,
                string.Empty,
                string.Empty),
            [new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North)],
            coordinateSystem.Name,
            coordinateSystem.Authority,
            coordinateSystem.AuthorityCode,
            string.Empty,
            LegacyWgs84UtmRemarks,
            string.Empty);
    }

    private static int GetWgs84UtmSrid(int zone, bool zoneIsNorth)
    {
        return 32600 + zone + (zoneIsNorth ? 0 : 100);
    }

    private static string CreateLegacyUtmProjectionName(int zone, bool zoneIsNorth)
    {
        return $"UTM{zone.ToString(CultureInfo.InvariantCulture)}{(zoneIsNorth ? "N" : "S")}";
    }
}
