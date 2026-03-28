// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// A 2D cartographic coordinate system.
/// </summary>
[Serializable]
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
                    new ProjectionParameter("latitude_of_origin", 0.0),
                    new ProjectionParameter("central_meridian", 0.0),
                    new ProjectionParameter("false_easting", 0.0),
                    new ProjectionParameter("false_northing", 0.0),
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
                new AxisInfo("East", AxisOrientationEnum.East),
                new AxisInfo("North", AxisOrientationEnum.North),
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
        var pInfo = new List<ProjectionParameter>();
        pInfo.Add(new ProjectionParameter("latitude_of_origin", 0));
        pInfo.Add(new ProjectionParameter("central_meridian", (zone * 6) - 183));
        pInfo.Add(new ProjectionParameter("scale_factor", 0.9996));
        pInfo.Add(new ProjectionParameter("false_easting", 500000));
        pInfo.Add(new ProjectionParameter("false_northing", zoneIsNorth ? 0 : 10000000));

        // IProjection projection = cFac.CreateProjection("UTM" + Zone.ToString() + (ZoneIsNorth ? "N" : "S"), "Transverse_Mercator", parameters);
        var proj = new Projection(
            "Transverse_Mercator",
            pInfo,
            "UTM" + zone.ToString(CultureInfo.InvariantCulture) + (zoneIsNorth ? "N" : "S"),
            "EPSG",
            32600 + zone + (zoneIsNorth ? 0 : 100),
            string.Empty,
            string.Empty,
            string.Empty);
        var axes = new List<AxisInfo>
            {
                new AxisInfo("East", AxisOrientationEnum.East),
                new AxisInfo("North", AxisOrientationEnum.North),
            };
        return new ProjectedCoordinateSystem(
            CoordinateSystems.HorizontalDatum.WGS84,
            CoordinateSystems.GeographicCoordinateSystem.WGS84,
            CoordinateSystems.LinearUnit.Metre,
            proj,
            axes,
            "WGS 84 / UTM zone " + zone.ToString(CultureInfo.InvariantCulture) + (zoneIsNorth ? "N" : "S"),
            "EPSG",
            32600 + zone + (zoneIsNorth ? 0 : 100),
            string.Empty,
            "Large and medium scale topographic mapping and engineering survey.",
            string.Empty);
    }

    /// <inheritdoc />
    public override IUnit GetUnits(int dimension) => this.LinearUnit;

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        if (!(obj is ProjectedCoordinateSystem pcs))
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
}
