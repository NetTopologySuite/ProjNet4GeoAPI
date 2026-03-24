// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2002 Urban Science Applications, Inc.
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from GeoTools.NET.

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
    private readonly double ns;                /* ratio of angle between meridian */
    private readonly double f0;                /* flattening of ellipsoid         */
    private readonly double rh;                /* height above ellipsoid          */

    /// <summary>
    /// Initializes a new instance of the <see cref="LambertConformalConic2SP"/> class.
    /// Creates an instance of an LambertConformalConic2SPProjection projection object.
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
    /// Creates an instance of an Albers projection object.
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
    /// <param name="inverse">Indicates whether the projection forward (meters to degrees or degrees to meters).</param>
    protected LambertConformalConic2SP(IEnumerable<ProjectionParameter> parameters, LambertConformalConic2SP inverse)
        : base(parameters, inverse)
    {
        this.Name = "Lambert_Conformal_Conic_2SP";
        this.Authority = "EPSG";
        this.AuthorityCode = 9802;

        // Check for missing parameters
        // Since this implementation supports conic 1SP and 2SP we add the support for the 1SP implementation here.
        // There is no need for standard_parallel_1 and standard_parallel_2 parameters in this version: https://pro.arcgis.com/en/pro-app/latest/help/mapping/properties/lambert-conformal-conic.htm
        double lat1 = DegreesToRadians(this.Parameters.GetParameterValue("standard_parallel_1", LatitudeOfOriginFallback));
        double lat2 = DegreesToRadians(this.Parameters.GetParameterValue("standard_parallel_2", LatitudeOfOriginFallback));

        double sin_po;                  /* sin value                            */
        double cos_po;                  /* cos value                            */
        double con;                     /* temporary variable                   */
        double ms1;                     /* small m 1                            */
        double ms2;                     /* small m 2                            */
        double ts0;                     /* small t 0                            */
        double ts1;                     /* small t 1                            */
        double ts2;                     /* small t 2                            */

        /* Standard Parallels cannot be equal and on opposite sides of the equator
        ------------------------------------------------------------------------*/
        if (Math.Abs(lat1 + lat2) < Epsln)
        {
            // Debug.Assert(true,"LambertConformalConic:LambertConformalConic() - Equal Latitiudes for St. Parallels on opposite sides of equator");
            throw new ArgumentException("Equal latitudes for St. Parallels on opposite sides of equator.");
        }

        Sincos(lat1, out sin_po, out cos_po);
        con = sin_po;
        ms1 = Msfnz(this.e, sin_po, cos_po);
        ts1 = Tsfnz(this.e, lat1, sin_po);
        Sincos(lat2, out sin_po, out cos_po);
        ms2 = Msfnz(this.e, sin_po, cos_po);
        ts2 = Tsfnz(this.e, lat2, sin_po);
        sin_po = Math.Sin(this.latOrigin);
        ts0 = Tsfnz(this.e, this.latOrigin, sin_po);

        if (Math.Abs(lat1 - lat2) > Epsln)
        {
            this.ns = Math.Log(ms1 / ms2) / Math.Log(ts1 / ts2);
        }
        else
        {
            this.ns = con;
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
        double dLongitude = lon;
        double dLatitude = lat;

        double con;    /* temporary angle variable             */
        double rh1;    /* height above ellipsoid               */
        double sinphi; /* sin value                            */
        double theta;  /* angle                                */
        double ts;     /* small value t                        */

        con = Math.Abs(Math.Abs(dLatitude) - HalfPi);
        if (con > Epsln)
        {
            sinphi = Math.Sin(dLatitude);
            ts = Tsfnz(this.e, dLatitude, sinphi);
            rh1 = this.semiMajor * this.f0 * Math.Pow(ts, this.ns);
        }
        else
        {
            con = dLatitude * this.ns;
            if (con <= 0)
            {
                throw new ArgumentException("Latitude is outside the valid range for this projection.", nameof(lat));
            }

            rh1 = 0;
        }

        theta = this.ns * Adjust_lon(dLongitude - this.centralMeridian);

        lon = rh1 * Math.Sin(theta);
        lat = this.rh - (rh1 * Math.Cos(theta));
    }

    /// <summary>
    /// Method to convert a point from meters to radians.
    /// </summary>
    /// <param name="x">The x-ordinate when entering, the longitude value upon exit.</param>
    /// <param name="y">The y-ordinate when entering, the latitude value upon exit.</param>
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double rh1; /* height above ellipsoid   */
        double con; /* sign variable        */
        double ts; /* small t           */
        double theta; /* angle          */

        // long flag; /* error flag         */
        double dX = x;
        double dY = this.rh - y;
        if (this.ns > 0)
        {
            rh1 = Math.Sqrt((dX * dX) + (dY * dY));
            con = 1.0;
        }
        else
        {
            rh1 = -Math.Sqrt((dX * dX) + (dY * dY));
            con = -1.0;
        }

        theta = 0.0;
        if (rh1 != 0)
        {
            theta = Math.Atan2(con * dX, con * dY);
        }

        if ((rh1 != 0) || (this.ns > 0.0))
        {
            con = 1.0 / this.ns;
            ts = Math.Pow(rh1 / (this.semiMajor * this.f0), con);
            y = Phi2z(this.e, ts, out long flag);
            if (flag != 0)
            {
                throw new ArgumentException("Inverse projection failed to converge.", nameof(y));
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
