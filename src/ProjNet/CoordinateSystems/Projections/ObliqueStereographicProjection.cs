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
/// Implements the Oblique Stereographic Projection.
/// </summary>
/// <remarks>
/// The formulation was independently verified against IOGP, "Geomatics Guidance Note 7,
/// part 2: Coordinate Conversions and Transformations including Formulas" (publication
/// 373-7-2, 2019), EPSG method 9809, and Apache SIS projection notes. The
/// implementation follows the documented Gauss-conformal plus stereographic double
/// projection, including recovery of conformal latitude and conformal sphere radius.
/// </remarks>
/// <seealso href="https://epsg.io/9809-method">EPSG method 9809: Oblique Stereographic.</seealso>
/// <seealso href="https://sis.apache.org/apidocs/org.apache.sis.referencing/org/apache/sis/referencing/operation/projection/ObliqueStereographic.html">Apache SIS: Oblique stereographic projection notes.</seealso>
internal class ObliqueStereographicProjection : MapProjection
{
    private readonly double globalScale;
    private readonly double reciprocGlobalScale;

    private static double iterationTolerance = 1E-14;
    private static int maximumIterations = 15;
    private static double epsilon = 1E-6;
    private double c;
    private double k;
    private double ratexp;
    private double phic0;
    private double cosc0;
    private double sinc0;
    private double r2;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObliqueStereographicProjection"/> class.
    /// </summary>
    /// <param name="parameters">List of parameters to initialize the projection.</param>
    /// <remarks>
    /// <para>The parameters this projection expects are listed below.</para>
    /// <list type="table">
    /// <listheader><term>Items</term><description>Descriptions</description></listheader>
    /// <item><term>central_meridian</term><description>The longitude of the point from which the values of both the geographical coordinates on the ellipsoid and the grid coordinates on the projection are deemed to increment or decrement for computational purposes. Alternatively it may be considered as the longitude of the point which in the absence of application of false coordinates has grid coordinates of (0,0).</description></item>
    /// <item><term>latitude_of_origin</term><description>The latitude of the point from which the values of both the geographical coordinates on the ellipsoid and the grid coordinates on the projection are deemed to increment or decrement for computational purposes. Alternatively it may be considered as the latitude of the point which in the absence of application of false coordinates has grid coordinates of (0,0).</description></item>
    /// <item><term>scale_factor</term><description>The factor by which the map grid is reduced or enlarged during the projection process, defined by its value at the natural origin.</description></item>
    /// <item><term>false_easting</term><description>Since the natural origin may be at or near the centre of the projection and under normal coordinate circumstances would thus give rise to negative coordinates over parts of the mapped area, this origin is usually given false coordinates which are large enough to avoid this inconvenience. The False Easting, FE, is the easting value assigned to the abscissa (east).</description></item>
    /// <item><term>false_northing</term><description>Since the natural origin may be at or near the centre of the projection and under normal coordinate circumstances would thus give rise to negative coordinates over parts of the mapped area, this origin is usually given false coordinates which are large enough to avoid this inconvenience. The False Northing, FN, is the northing value assigned to the ordinate.</description></item>
    /// </list>
    /// </remarks>
    public ObliqueStereographicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObliqueStereographicProjection"/> class.
    /// </summary>
    /// <param name="parameters">List of parameters to initialize the projection.</param>
    /// <param name="inverse">The inverse projection instance, or <see langword="null"/> for a forward projection.</param>
    public ObliqueStereographicProjection(IEnumerable<ProjectionParameter> parameters, ObliqueStereographicProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Oblique_Stereographic";

        this.globalScale = this.scaleFactor * this.semiMajor;
        this.reciprocGlobalScale = 1 / this.globalScale;

        double sinLatitudeOrigin = Math.Sin(this.latOrigin);
        double cosLatitudeOrigin = Math.Cos(this.latOrigin);
        double cosLatitudeOriginSquared = cosLatitudeOrigin * cosLatitudeOrigin;
        this.r2 = 2.0 * Math.Sqrt(1 - this.es) / (1 - (this.es * sinLatitudeOrigin * sinLatitudeOrigin));
        this.c = Math.Sqrt(1.0 + (this.es * cosLatitudeOriginSquared * cosLatitudeOriginSquared / (1.0 - this.es)));
        this.phic0 = Math.Asin(sinLatitudeOrigin / this.c);
        this.sinc0 = Math.Sin(this.phic0);
        this.cosc0 = Math.Cos(this.phic0);
        this.ratexp = 0.5 * this.c * this.e;
        this.k = Math.Tan((0.5 * this.phic0) + (Math.PI / 4)) / (Math.Pow(Math.Tan((0.5 * this.latOrigin) + (Math.PI / 4)), this.c) * this.Srat(this.e * sinLatitudeOrigin, this.ratexp));
    }

    /// <summary>
    /// Converts coordinates in projected meters to radians.
    /// </summary>
    /// <param name="x">The x-ordinate in projected meters.</param>
    /// <param name="y">The y-ordinate in projected meters.</param>
    protected override void MetersToRadians(ref double x, ref double y)
    {
        x *= this.reciprocGlobalScale;
        y *= this.reciprocGlobalScale;

        double rho = Math.Sqrt((x * x) + (y * y));
        if (Math.Abs(rho) < epsilon)
        {
            x = 0.0;
            y = this.phic0;
        }
        else
        {
            double centralAngle = 2.0 * Math.Atan2(rho, this.r2);
            double sinCentralAngle = Math.Sin(centralAngle);
            double cosCentralAngle = Math.Cos(centralAngle);
            double denominator = (rho * this.cosc0 * cosCentralAngle) - (y * this.sinc0 * sinCentralAngle);
            x = Math.Atan2(x * sinCentralAngle, denominator);
            y = (cosCentralAngle * this.sinc0) + (y * sinCentralAngle * this.cosc0 / rho);

            if (Math.Abs(y) >= 1.0)
            {
                y = (y < 0.0) ? -Math.PI / 2.0 : Math.PI / 2.0;
            }
            else
            {
                y = Math.Asin(y);
            }
        }

        x /= this.c;
        double num = Math.Pow(Math.Tan((0.5 * y) + (Math.PI / 4.0)) / this.k, 1.0 / this.c);
        for (int iter = maximumIterations; ;)
        {
            double phi = (2.0 * Math.Atan(num * this.Srat(this.e * Math.Sin(y), -0.5 * this.e))) - (Math.PI / 2.0);
            if (Math.Abs(phi - y) < iterationTolerance)
            {
                break;
            }

            y = phi;
            if (--iter < 0)
            {
                throw new InvalidOperationException("Oblique Stereographics doesn't converge");
            }
        }

        x += this.centralMeridian;
    }

    /// <summary>
    /// Method to convert a point (lon, lat) in radians to (x, y) in meters.
    /// </summary>
    /// <param name="lon">The longitude of the point in radians when entering, its x-ordinate in meters after exit.</param>
    /// <param name="lat">The latitude of the point in radians when entering, its y-ordinate in meters after exit.</param>
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double longitude = lon - this.centralMeridian;
        double latitude = lat;

        latitude = (2.0 * Math.Atan(this.k * Math.Pow(Math.Tan((0.5 * latitude) + (Math.PI / 4)), this.c)
                               * this.Srat(this.e * Math.Sin(latitude), this.ratexp)))
            - (Math.PI / 2);
        longitude *= this.c;
        double sinLatitude = Math.Sin(latitude);
        double cosLatitude = Math.Cos(latitude);
        double cosLongitude = Math.Cos(longitude);
        double radialScale = this.r2 / (1.0 + (this.sinc0 * sinLatitude) + (this.cosc0 * cosLatitude * cosLongitude));

        lon = radialScale * cosLatitude * Math.Sin(longitude) * this.globalScale;
        lat = radialScale * ((this.cosc0 * sinLatitude) - (this.sinc0 * cosLatitude * cosLongitude)) * this.globalScale;
    }

    /// <summary>
    /// Returns the inverse of this projection.
    /// </summary>
    /// <returns>IMathTransform that is the reverse of the current projection.</returns>
    public override MathTransform Inverse()
    {
        this.inverse ??= new ObliqueStereographicProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    private double Srat(double esinp, double exp)
    {
        return Math.Pow((1.0 - esinp) / (1.0 + esinp), exp);
    }
}
