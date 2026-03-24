// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// A coordinate system based on latitude and longitude.
/// </summary>
/// <remarks>
/// Some geographic coordinate systems are Lat/Lon, and some are Lon/Lat.
/// You can find out which this is by examining the axes. You should also
/// check the angular units, since not all geographic coordinate systems
/// use degrees.
/// </remarks>
[Serializable]
public class GeographicCoordinateSystem : HorizontalCoordinateSystem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeographicCoordinateSystem"/> class.
    /// Creates an instance of a Geographic Coordinate System.
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
    }

    /// <summary>
    /// Gets creates a decimal degrees geographic coordinate system based on the WGS84 ellipsoid, suitable for GPS measurements.
    /// </summary>
    public static GeographicCoordinateSystem WGS84
    {
        get
        {
            var axes = new List<AxisInfo>(2);
            axes.Add(new AxisInfo("Lon", AxisOrientationEnum.East));
            axes.Add(new AxisInfo("Lat", AxisOrientationEnum.North));
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
    public override string WKT
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "GEOGCS[\"{0}\", {1}, {2}, {3}", this.Name, this.HorizontalDatum.WKT, this.PrimeMeridian.WKT, this.AngularUnit.WKT);

            // Skip axis info if they contain default values
            if (this.AxisInfo.Count != 2 ||
                this.AxisInfo[0].Name != "Lon" || this.AxisInfo[0].Orientation != AxisOrientationEnum.East ||
                this.AxisInfo[1].Name != "Lat" || this.AxisInfo[1].Orientation != AxisOrientationEnum.North)
            {
                for (int i = 0; i < this.AxisInfo.Count; i++)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, ", {0}", this.GetAxis(i).WKT);
                }
            }

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
                "<CS_CoordinateSystem Dimension=\"{0}\"><CS_GeographicCoordinateSystem>{1}",
                this.Dimension,
                this.InfoXml);
            foreach (var ai in this.AxisInfo)
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
    /// Gets units for dimension within coordinate system. Each dimension in
    /// the coordinate system has corresponding units.
    /// </summary>
    /// <param name="dimension">Dimension.</param>
    /// <returns>Unit.</returns>
    public override IUnit GetUnits(int dimension) => this.AngularUnit;

    /// <summary>
    /// Gets details on a conversion to WGS84.
    /// </summary>
    /// <param name="index">The index value.</param>
    /// <returns>The computed value.</returns>
    public Wgs84ConversionInfo GetWgs84ConversionInfo(int index) => this.WGS84ConversionInfo[index];

    /// <summary>
    /// Checks whether the values of this instance is equal to the values of another instance.
    /// Only parameters used for coordinate system are used for comparison.
    /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
    /// </summary>
    /// <param name="obj">The obj parameter.</param>
    /// <returns>True if equal.</returns>
    public override bool EqualParams(object obj)
    {
        if (!(obj is GeographicCoordinateSystem gcs))
        {
            return false;
        }

        if (gcs.Dimension != this.Dimension)
        {
            return false;
        }

        if (this.WGS84ConversionInfo != null && gcs.WGS84ConversionInfo == null)
        {
            return false;
        }

        if (this.WGS84ConversionInfo == null && gcs.WGS84ConversionInfo != null)
        {
            return false;
        }

        if (this.WGS84ConversionInfo != null && gcs.WGS84ConversionInfo != null)
        {
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
