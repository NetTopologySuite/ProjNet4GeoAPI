// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

/// <summary>
/// A 3D coordinate system, with its origin at the center of the Earth.
/// </summary>
[Serializable]
public class GeocentricCoordinateSystem : CoordinateSystem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeocentricCoordinateSystem"/> class.
    /// </summary>
    /// <param name="datum">Horizontal datum used by this coordinate system.</param>
    /// <param name="linearUnit">Linear unit applied to all axes.</param>
    /// <param name="primeMeridian">Prime meridian used for longitude reference.</param>
    /// <param name="axisInfo">Axis definition list (must contain 3 axes).</param>
    /// <param name="name">Coordinate system name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="code">Authority code.</param>
    /// <param name="alias">Alias name.</param>
    /// <param name="remarks">Additional remarks.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    internal GeocentricCoordinateSystem(
        HorizontalDatum datum,
        LinearUnit linearUnit,
        PrimeMeridian primeMeridian,
        List<AxisInfo> axisInfo,
        string name,
        string authority,
        long code,
        string alias,
        string remarks,
        string abbreviation)
        : base(name, authority, code, alias, abbreviation, remarks)
    {
        ArgumentGuard.ThrowIfNull(datum, nameof(datum));
        ArgumentGuard.ThrowIfNull(linearUnit, nameof(linearUnit));
        ArgumentGuard.ThrowIfNull(primeMeridian, nameof(primeMeridian));
        ArgumentGuard.ThrowIfNull(axisInfo, nameof(axisInfo));
        this.HorizontalDatum = datum;
        this.LinearUnit = linearUnit;
        this.PrimeMeridian = primeMeridian;
        if (axisInfo.Count != 3)
        {
            ArgumentGuard.ThrowArgument("Axis info should contain three axes for geocentric coordinate systems");
        }

        this.AxisInfo = axisInfo;
    }

    /// <summary>
    /// Gets a geocentric coordinate system based on the WGS84 ellipsoid, suitable for GPS measurements.
    /// </summary>
    public static GeocentricCoordinateSystem WGS84
    {
        get
        {
            return new CoordinateSystemFactory().CreateGeocentricCoordinateSystem(
                "WGS84 Geocentric",
                HorizontalDatum.WGS84,
                LinearUnit.Metre,
                PrimeMeridian.Greenwich);
        }
    }

    /// <summary>
    /// Gets or sets the HorizontalDatum. The horizontal datum is used to determine where
    /// the centre of the Earth is considered to be. All coordinate points will be
    /// measured from the centre of the Earth, and not the surface.
    /// </summary>
    public HorizontalDatum HorizontalDatum { get; set; }

    /// <summary>
    /// Gets or sets the units used along all the axes.
    /// </summary>
    public LinearUnit LinearUnit { get; set; }

    /// <summary>
    /// Gets or sets the prime meridian used as the longitude reference for this coordinate system.
    /// </summary>
    public PrimeMeridian PrimeMeridian { get; set; }

    /// <summary>
    /// Gets the Well-known text for this object
    /// as defined in the simple features specification.
    /// </summary>
    public override string WKT
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "GEOCCS[\"{0}\", {1}, {2}, {3}", this.Name, this.HorizontalDatum.WKT, this.PrimeMeridian.WKT, this.LinearUnit.WKT);

            // Skip axis info if they contain default values
            if (this.AxisInfo.Count != 3 ||
                this.AxisInfo[0].Name != "X" || this.AxisInfo[0].Orientation != AxisOrientationEnum.Other ||
                this.AxisInfo[1].Name != "Y" || this.AxisInfo[1].Orientation != AxisOrientationEnum.East ||
                this.AxisInfo[2].Name != "Z" || this.AxisInfo[2].Orientation != AxisOrientationEnum.North)
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
                "<CS_CoordinateSystem Dimension=\"{0}\"><CS_GeocentricCoordinateSystem>{1}",
                this.Dimension,
                this.InfoXml);
            foreach (var ai in this.AxisInfo)
            {
                sb.Append(ai.XML);
            }

            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0}{1}{2}</CS_GeocentricCoordinateSystem></CS_CoordinateSystem>",
                this.HorizontalDatum.XML,
                this.LinearUnit.XML,
                this.PrimeMeridian.XML);
            return sb.ToString();
        }
    }

    /// <inheritdoc />
    public override IUnit GetUnits(int dimension) => this.LinearUnit;

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        if (!(obj is GeocentricCoordinateSystem gcc))
        {
            return false;
        }

        return gcc.HorizontalDatum.EqualParams(this.HorizontalDatum) &&
            gcc.LinearUnit.EqualParams(this.LinearUnit) &&
            gcc.PrimeMeridian.EqualParams(this.PrimeMeridian);
    }
}
