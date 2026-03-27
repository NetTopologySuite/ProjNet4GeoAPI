// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2002 Urban Science Applications, Inc.
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from GeoTools.NET.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Lambert Conformal Conic 2SP Projection.
/// </summary>
/// <remarks>
/// <para>The Lambert Conformal Conic projection is a standard projection for presenting maps
/// of land areas whose East-West extent is large compared with their North-South extent.
/// This projection is "conformal" in the sense that lines of latitude and longitude,
/// which are perpendicular to one another on the earth's surface, are also perpendicular
/// to one another in the projected domain.</para>
/// </remarks>
[Serializable]
internal class LambertConformalConic2SP : MapProjection
{
    private static readonly string[] LatitudeOfOriginFallback = { "latitude_of_origin" };

    /// <summary>
    /// Ratio of angular change between meridians.
    /// </summary>
    private readonly double ns;

    /// <summary>
    /// Projection constant derived from standard parallels.
    /// </summary>
    private readonly double f0;

    /// <summary>
    /// Radial distance at the latitude of origin.
    /// </summary>
    private readonly double rh;

    /// <summary>
    /// Initializes a new instance of the <see cref="LambertConformalConic2SP"/> class.
    /// </summary>
    /// <remarks>
    /// <para>The parameters this projection expects are listed below.</para>
    /// <list type="table">
    /// <listheader><term>Items</term><description>Descriptions</description></listheader>
    /// <item><term>latitude_of_false_origin</term><description>The latitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
    /// <item><term>longitude_of_false_origin</term><description>The longitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
    /// <item><term>latitude_of_1st_standard_parallel</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is nearest the pole.  Scale is true along this parallel.</description></item>
    /// <item><term>latitude_of_2nd_standard_parallel</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is furthest from the pole.  Scale is true along this parallel.</description></item>
    /// <item><term>easting_at_false_origin</term><description>The easting value assigned to the false origin.</description></item>
    /// <item><term>northing_at_false_origin</term><description>The northing value assigned to the false origin.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="parameters">List of parameters to initialize the projection.</param>
    public LambertConformalConic2SP(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LambertConformalConic2SP"/> class.
    /// </summary>
    /// <remarks>
    /// <para>The parameters this projection expects are listed below.</para>
    /// <list type="table">
    /// <listheader><term>Parameter</term><description>Description</description></listheader>
    /// <item><term>latitude_of_origin</term><description>The latitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
    /// <item><term>central_meridian</term><description>The longitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
    /// <item><term>standard_parallel_1</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is nearest the pole.  Scale is true along this parallel.</description></item>
    /// <item><term>standard_parallel_2</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is furthest from the pole.  Scale is true along this parallel.</description></item>
    /// <item><term>false_easting</term><description>The easting value assigned to the false origin.</description></item>
    /// <item><term>false_northing</term><description>The northing value assigned to the false origin.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="parameters">List of parameters to initialize the projection.</param>
    /// <param name="inverse">The inverse projection instance, or <see langword="null"/> for a forward projection.</param>
    protected LambertConformalConic2SP(IEnumerable<ProjectionParameter> parameters, LambertConformalConic2SP? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Lambert_Conformal_Conic_2SP";
        this.Authority = "EPSG";
        this.AuthorityCode = 9802;

        // This implementation supports both 2SP and 1SP-style inputs by falling back to latitude_of_origin.
        double lat1 = DegreesToRadians(this.Parameters.GetParameterValue("standard_parallel_1", LatitudeOfOriginFallback));
        double lat2 = DegreesToRadians(this.Parameters.GetParameterValue("standard_parallel_2", LatitudeOfOriginFallback));

        // Standard parallels cannot be equal and on opposite sides of the equator.
        if (Math.Abs(lat1 + lat2) < Epsln)
        {
            ArgumentGuard.ThrowArgument("Equal latitudes for St. Parallels on opposite sides of equator.");
        }

        Sincos(lat1, out double sinLatitude1, out double cosLatitude1);
        double ms1 = Msfnz(this.e, sinLatitude1, cosLatitude1);
        double ts1 = Tsfnz(this.e, lat1, sinLatitude1);

        Sincos(lat2, out double sinLatitude2, out double cosLatitude2);
        double ms2 = Msfnz(this.e, sinLatitude2, cosLatitude2);
        double ts2 = Tsfnz(this.e, lat2, sinLatitude2);

        double sinLatitudeOrigin = Math.Sin(this.latOrigin);
        double ts0 = Tsfnz(this.e, this.latOrigin, sinLatitudeOrigin);

        if (Math.Abs(lat1 - lat2) > Epsln)
        {
            this.ns = Math.Log(ms1 / ms2) / Math.Log(ts1 / ts2);
        }
        else
        {
            this.ns = sinLatitude1;
        }

        this.f0 = ms1 / (this.ns * Math.Pow(ts1, this.ns));
        this.rh = this.semiMajor * this.f0 * Math.Pow(ts0, this.ns);
    }

    /// <summary>
    /// Method to convert a point (lon, lat) in radians to (x, y) in meters.
    /// </summary>
    /// <param name="lon">The longitude of the point in radians when entering, its x-ordinate in meters after exit.</param>
    /// <param name="lat">The latitude of the point in radians when entering, its y-ordinate in meters after exit.</param>
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double longitude = lon;
        double latitude = lat;
        double radialDistance;

        double latitudeDistanceFromPole = Math.Abs(Math.Abs(latitude) - HalfPi);
        if (latitudeDistanceFromPole > Epsln)
        {
            double sinLatitude = Math.Sin(latitude);
            double ts = Tsfnz(this.e, latitude, sinLatitude);
            radialDistance = this.semiMajor * this.f0 * Math.Pow(ts, this.ns);
        }
        else
        {
            double signedLatitude = latitude * this.ns;
            if (signedLatitude <= 0)
            {
                ArgumentGuard.ThrowArgument("Latitude is outside the valid range for this projection.", nameof(lat));
            }

            radialDistance = 0;
        }

        double theta = this.ns * Adjust_lon(longitude - this.centralMeridian);

        lon = radialDistance * Math.Sin(theta);
        lat = this.rh - (radialDistance * Math.Cos(theta));
    }

    /// <summary>
    /// Method to convert a point from meters to radians.
    /// </summary>
    /// <param name="x">The x-ordinate when entering, the longitude value upon exit.</param>
    /// <param name="y">The y-ordinate when entering, the latitude value upon exit.</param>
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double dX = x;
        double dY = this.rh - y;
        double sign;
        double radialDistance;

        if (this.ns > 0)
        {
            radialDistance = Math.Sqrt((dX * dX) + (dY * dY));
            sign = 1.0;
        }
        else
        {
            radialDistance = -Math.Sqrt((dX * dX) + (dY * dY));
            sign = -1.0;
        }

        double theta = radialDistance != 0 ? Math.Atan2(sign * dX, sign * dY) : 0.0;

        if ((radialDistance != 0) || (this.ns > 0.0))
        {
            double exponent = 1.0 / this.ns;
            double ts = Math.Pow(radialDistance / (this.semiMajor * this.f0), exponent);
            y = Phi2z(this.e, ts, out long flag);
            if (flag != 0)
            {
                ArgumentGuard.ThrowArgument("Inverse projection failed to converge.", nameof(y));
            }
        }
        else
        {
            y = -HalfPi;
        }

        x = Adjust_lon((theta / this.ns) + this.centralMeridian);
    }

    /// <summary>
    /// Returns the inverse of this projection.
    /// </summary>
    /// <returns>IMathTransform that is the reverse of the current projection.</returns>
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new LambertConformalConic2SP(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }
}
