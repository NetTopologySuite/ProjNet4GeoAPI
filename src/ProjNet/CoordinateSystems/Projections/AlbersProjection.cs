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
///     Implements the Albers projection.
/// </summary>
/// <remarks>
///     <para>Implements the Albers projection. The Albers projection is most commonly
///     used to project the United States of America. It gives the northern
///     border with Canada a curved appearance.</para>
///
///     <para>The Albers Equal Area projection has the property that the area bounded
///     by any pair of parallels and meridians is exactly reproduced between the
///     image of those parallels and meridians in the projected domain, that is,
///     the projection preserves the correct area of the earth though distorts
///     direction, distance and shape somewhat.</para>
///
///     <para>The ellipsoidal formulation was independently verified against IOGP,
///     "Geomatics Guidance Note 7, part 2: Coordinate Conversions and
///     Transformations including Formulas" (publication 373-7-2, 2019), EPSG
///     method 9822, Albers Equal Area. The authalic <c>q</c>-function and
///     derived <c>ρ</c> relationships match the implementation here.</para>
/// </remarks>
/// <seealso href="https://epsg.io/9822-method">EPSG method 9822: Albers Equal Area.</seealso>
internal class AlbersProjection : MapProjection
{
    /// <summary>
    /// Albers projection constant <c>c</c>.
    /// </summary>
    private readonly double c;

    /// <summary>
    /// Radial distance at the latitude of origin.
    /// </summary>
    private readonly double ro0;

    /// <summary>
    /// Projection exponent <c>n</c>.
    /// </summary>
    private readonly double n;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlbersProjection"/> class.
    /// </summary>
    /// <param name="parameters">List of parameters to initialize the projection.</param>
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
    public AlbersProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlbersProjection"/> class.
    /// </summary>
    /// <remarks>
    /// <para>The parameters this projection expects are listed below.</para>
    /// <list type="table">
    /// <listheader><term>Items</term><description>Descriptions</description></listheader>
    /// <item><term>latitude_of_center</term><description>The latitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
    /// <item><term>longitude_of_center</term><description>The longitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
    /// <item><term>standard_parallel_1</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is nearest the pole.  Scale is true along this parallel.</description></item>
    /// <item><term>standard_parallel_2</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is furthest from the pole.  Scale is true along this parallel.</description></item>
    /// <item><term>false_easting</term><description>The easting value assigned to the false origin.</description></item>
    /// <item><term>false_northing</term><description>The northing value assigned to the false origin.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="parameters">List of parameters to initialize the projection.</param>
    /// <param name="inverse">The inverse projection instance, or <see langword="null"/> for a forward projection.</param>
    protected AlbersProjection(IEnumerable<ProjectionParameter> parameters, AlbersProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Albers_Conic_Equal_Area";

        double lat0 = this.latOrigin;
        double lat1 = DegreesToRadians(this.Parameters.GetParameterValue("standard_parallel_1"));
        double lat2 = DegreesToRadians(this.Parameters.GetParameterValue("standard_parallel_2"));

        if (Math.Abs(lat1 + lat2) < double.Epsilon)
        {
            ArgumentGuard.ThrowArgument("Equal latitudes for standard parallels on opposite sides of Equator.");
        }

        double alpha1 = this.Alpha(lat1);
        double alpha2 = this.Alpha(lat2);

        double m1 = Math.Cos(lat1) / Math.Sqrt(1 - (this.es * Math.Pow(Math.Sin(lat1), 2)));
        double m2 = Math.Cos(lat2) / Math.Sqrt(1 - (this.es * Math.Pow(Math.Sin(lat2), 2)));

        this.n = (Math.Pow(m1, 2) - Math.Pow(m2, 2)) / (alpha2 - alpha1);
        this.c = Math.Pow(m1, 2) + (this.n * alpha1);

        this.ro0 = this.Ro(this.Alpha(lat0));
    }

    /// <summary>
    /// Converts coordinates in radians to projected meters.
    /// </summary>
    /// <param name="lon">The longitude of the point in radians when entering, its x-ordinate in meters after exit.</param>
    /// <param name="lat">The latitude of the point in radians when entering, its y-ordinate in meters after exit.</param>
    protected sealed override void RadiansToMeters(ref double lon, ref double lat)
    {
        double a = this.Alpha(lat);
        double ro = this.Ro(a);
        double theta = this.n * (lon - this.centralMeridian);

        lon = ro * Math.Sin(theta);
        lat = this.ro0 - (ro * Math.Cos(theta));
    }

    /// <summary>
    /// Converts coordinates in projected meters to radians.
    /// </summary>
    /// <param name="x">The x-ordinate of the point in meters when entering, its longitude in radians after exit.</param>
    /// <param name="y">The y-ordinate of the point in meters when entering, its latitude in radians after exit.</param>
    protected sealed override void MetersToRadians(ref double x, ref double y)
    {
        double deltaY = this.ro0 - y;
        double theta = Math.Atan2(x, deltaY);
        double ro = Math.Sqrt((x * x) + (deltaY * deltaY));
        if (this.n < 0.0)
        {
            ro = -ro;
            x = -x;
            deltaY = -deltaY;
            theta = Math.Atan2(x, deltaY);
        }

        double q = (this.c - (Math.Pow(ro, 2) * Math.Pow(this.n, 2) / Math.Pow(this.semiMajor, 2))) / this.n;

        double lat;
        if (this.es <= Eps10)
        {
            lat = Asinz(q * 0.5);
        }
        else
        {
            lat = Math.Asin(q * 0.5);
            double preLat = double.MaxValue;
            int iterationCounter = 0;
            while (Math.Abs(lat - preLat) > 0.000001)
            {
                preLat = lat;
                double sin = Math.Sin(lat);
                double e2sin2 = this.es * Math.Pow(sin, 2);
                lat += Math.Pow(1 - e2sin2, 2) / (2 * Math.Cos(lat)) *
                       ((q / (1 - this.es)) - (sin / (1 - e2sin2)) +
                        (1 / (2 * this.e) * Math.Log((1 - (this.e * sin)) / (1 + (this.e * sin)))));
                iterationCounter++;
                if (iterationCounter > 25)
                {
                    ArgumentGuard.ThrowArgument(
                        "Transformation failed to converge in Albers backwards transformation");
                }
            }
        }

        x = this.centralMeridian + (theta / this.n);
        y = lat;
    }

    /// <summary>
    /// Returns the inverse of this projection.
    /// </summary>
    /// <returns>IMathTransform that is the reverse of the current projection.</returns>
    public override MathTransform Inverse()
    {
        this.inverse ??= new AlbersProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    private double Alpha(double lat)
    {
        double sin = Math.Sin(lat);
        if (this.es <= Eps10)
        {
            return sin + sin;
        }

        double sinsq = Math.Pow(sin, 2);
        return (1 - this.es) * ((sin / (1 - (this.es * sinsq))) - (1 / (2 * this.e) * Math.Log((1 - (this.e * sin)) / (1 + (this.e * sin)))));
    }

    private double Ro(double a)
    {
        return this.semiMajor * Math.Sqrt(this.c - (this.n * a)) / this.n;
    }
}
