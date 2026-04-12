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
/// Implements the Krovak Oblique Conformal Conic map projection.
/// </summary>
/// <remarks>
/// <para>The normal case of the Lambert Conformal conic is for the axis of the cone
/// to be coincident with the minor axis of the ellipsoid, that is the axis of the cone
/// is normal to the ellipsoid at a pole. For the Oblique Conformal Conic the axis
/// of the cone is normal to the ellipsoid at a defined location and its extension
/// cuts the minor axis at a defined angle. This projection is used in the Czech Republic
/// and Slovakia under the name "Krovak" projection.</para>
/// <para>The formulation was independently verified against IOGP, "Geomatics Guidance
/// Note 7, part 2: Coordinate Conversions and Transformations including Formulas"
/// (publication 373-7-2, 2019), EPSG method 9819, Krovak. The oblique-conic setup on
/// the conformal sphere and the resulting parameter usage match the implementation here.</para>
/// </remarks>
/// <seealso href="https://epsg.io/9819-method">EPSG method 9819: Krovak.</seealso>
/// <seealso>Bugayevskiy &amp; Snyder (1995), "Map Projections: A Reference Manual", Ch. 5, Sect. 5.1.6, pp. 163-164.</seealso>
internal class KrovakProjection : MapProjection
{
    // Maximum number of iterations for iterative computations.
    private const int MaximumIterations = 15;

    // When to stop the iteration.
    private const double IterationTolerance = 1E-11;

    private const double DefaultAzimuthDegrees = 30.2881397527778d;
    private const double DefaultPseudoStandardParallelDegrees = 78.5d;

    // Azimuth of the centre line passing through the centre of the projection.
    // This is equals to the co-latitude of the cone axis at point of intersection
    // with the ellipsoid.
    private readonly double azimuth;

    // Latitude of pseudo standard parallel.
    private readonly double pseudoStandardParallel;

    // Useful variables calculated from parameters defined by user.
    private readonly double sinAzim;
    private readonly double cosAzim;
    private readonly double n;
    private readonly double tanS2;
    private readonly double alfa;
    private readonly double hae;
    private readonly double k1;
    private readonly double ka;
    private readonly double ro0;
    private readonly double rop;

    private readonly double reciprocSemiMajor;
    private readonly bool eastingNorthing;

    // Useful constant - 45° in radians.
    private const double S45 = 0.785398163397448;

    /// <summary>
    /// Initializes a new instance of the <see cref="KrovakProjection"/> class.
    /// </summary>
    /// <remarks>
    /// <para>The parameters this projection expects are listed below.</para>
    /// <list type="table">
    /// <listheader><term>Parameter</term><description>Description</description></listheader>
    /// <item><term>latitude_of_center</term><description>Geodetic latitude of the projection centre.</description></item>
    /// <item><term>longitude_of_center</term><description>Longitude of the projection centre (central meridian).</description></item>
    /// <item><term>azimuth</term><description>Azimuth of the centre line at the projection centre.</description></item>
    /// <item><term>pseudo_standard_parallel_1</term><description>Latitude of the pseudo standard parallel.</description></item>
    /// <item><term>scale_factor</term><description>Scale factor on the pseudo standard parallel.</description></item>
    /// <item><term>false_easting</term><description>Easting assigned to the projection centre.</description></item>
    /// <item><term>false_northing</term><description>Northing assigned to the projection centre.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="parameters">List of parameters to initialize the projection.</param>
    public KrovakProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KrovakProjection"/> class.
    /// </summary>
    /// <param name="parameters">List of parameters to initialize the projection.</param>
    /// <param name="inverse">The inverse projection instance, or <see langword="null"/> for a forward projection.</param>
    protected KrovakProjection(IEnumerable<ProjectionParameter> parameters, KrovakProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Krovak";

        this.Authority = "EPSG";
        this.AuthorityCode = 9819;

        this.eastingNorthing = this.Parameters.GetOptionalParameterValue("czech", 0d) == 0d;
        this.azimuth = DegreesToRadians(this.Parameters.GetOptionalParameterValue("azimuth", DefaultAzimuthDegrees));
        this.pseudoStandardParallel = DegreesToRadians(this.Parameters.GetOptionalParameterValue("pseudo_standard_parallel_1", DefaultPseudoStandardParallelDegrees));

        // Calculates useful constants.
        this.sinAzim = Math.Sin(this.azimuth);
        this.cosAzim = Math.Cos(this.azimuth);
        this.n = Math.Sin(this.pseudoStandardParallel);
        this.tanS2 = Math.Tan((this.pseudoStandardParallel / 2) + S45);

        double sinLatitudeOrigin = Math.Sin(this.latOrigin);
        double cosLatitudeOrigin = Math.Cos(this.latOrigin);
        double cosLatitudeOriginSquared = cosLatitudeOrigin * cosLatitudeOrigin;
        this.alfa = Math.Sqrt(1 + ((this.es * (cosLatitudeOriginSquared * cosLatitudeOriginSquared)) / (1 - this.es))); // parameter B
        this.hae = this.alfa * this.e / 2;
        double u0 = Math.Asin(sinLatitudeOrigin / this.alfa);

        double eccentricityLatitude = this.e * sinLatitudeOrigin;
        double g = Math.Pow((1 - eccentricityLatitude) / (1 + eccentricityLatitude), (this.alfa * this.e) / 2);
        this.k1 = Math.Pow(Math.Tan((this.latOrigin / 2) + S45), this.alfa) * g / Math.Tan((u0 / 2) + S45);
        this.ka = Math.Pow(1 / this.k1, -1 / this.alfa);

        double meridionalRadius = Math.Sqrt(1 - this.es) / (1 - (this.es * (sinLatitudeOrigin * sinLatitudeOrigin)));

        this.ro0 = this.scaleFactor * meridionalRadius / Math.Tan(this.pseudoStandardParallel);
        this.rop = this.ro0 * Math.Pow(this.tanS2, this.n);

        this.reciprocSemiMajor = 1 / this.semiMajor;
    }

    /// <inheritdoc />
    protected override void DegreesToTarget(ref double lon, ref double lat)
    {
        this.DegreesToMeters(ref lon, ref lat);
        this.MetersToKrovakTarget(ref lon, ref lat);
    }

    /// <inheritdoc />
    protected override void DegreesToTarget(Span<double> lons, Span<double> lats, int strideX, int strideY)
    {
        this.DegreesToMeters(lons, lats, strideX, strideY);
        for (int i = 0, j = 0; i < lons.Length; i += strideX, j += strideY)
        {
            this.MetersToKrovakTarget(ref lons[i], ref lats[j]);
        }
    }

    /// <inheritdoc />
    protected override void SourceToDegrees(ref double x, ref double y)
    {
        this.KrovakTargetToMeters(ref x, ref y);
        this.MetersToDegrees(ref x, ref y);
    }

    /// <inheritdoc />
    protected override void SourceToDegrees(Span<double> xs, Span<double> ys, int strideX, int strideY)
    {
        for (int i = 0, j = 0; i < xs.Length; i += strideX, j += strideY)
        {
            this.KrovakTargetToMeters(ref xs[i], ref ys[j]);
        }

        this.MetersToDegrees(xs, ys, strideX, strideY);
    }

    /// <summary>
    /// Converts coordinates in radians to projected meters.
    /// </summary>
    /// <param name="lon">The longitude of the point in radians when entering, its x-ordinate in meters after exit.</param>
    /// <param name="lat">The latitude of the point in radians when entering, its y-ordinate in meters after exit.</param>
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = lon - this.centralMeridian;
        double phi = lat;

        double eccentricitySinPhi = this.e * Math.Sin(phi);
        double conformalScale = Math.Pow((1.0 - eccentricitySinPhi) / (1.0 + eccentricitySinPhi), this.hae);
        double conformalLatitude = 2 * (Math.Atan(Math.Pow(Math.Tan((phi / 2) + S45), this.alfa) / this.k1 * conformalScale) - S45);
        double deltaV = -lambda * this.alfa;
        double cosConformalLatitude = Math.Cos(conformalLatitude);
        double pseudoLatitude = Math.Asin((this.cosAzim * Math.Sin(conformalLatitude)) + (this.sinAzim * cosConformalLatitude * Math.Cos(deltaV)));
        double pseudoLongitude = Math.Asin(cosConformalLatitude * Math.Sin(deltaV) / Math.Cos(pseudoLatitude));
        double eps = this.n * pseudoLongitude;
        double radialDistance = this.rop / Math.Pow(Math.Tan((pseudoLatitude / 2) + S45), this.n);

        // x and y are reverted
        lat = -(radialDistance * Math.Cos(eps)) * this.semiMajor;
        lon = -(radialDistance * Math.Sin(eps)) * this.semiMajor;
    }

    /// <summary>
    /// Converts coordinates in projected meters to radians.
    /// </summary>
    /// <param name="x">The x-ordinate in projected meters when entering, the longitude in radians after exit.</param>
    /// <param name="y">The y-ordinate in projected meters when entering, the latitude in radians after exit.</param>
    protected override void MetersToRadians(ref double x, ref double y)
    {
        x *= this.reciprocSemiMajor;
        y *= this.reciprocSemiMajor;

        // x -> southing, y -> westing
        double radialDistance = Math.Sqrt((x * x) + (y * y));
        double eps = Math.Atan2(-x, -y);
        double pseudoLongitude = eps / this.n;
        double pseudoLatitude = 2 * (Math.Atan(Math.Pow(this.ro0 / radialDistance, 1 / this.n) * this.tanS2) - S45);
        double cosPseudoLatitude = Math.Cos(pseudoLatitude);
        double conformalLatitude = Math.Asin((this.cosAzim * Math.Sin(pseudoLatitude)) - (this.sinAzim * cosPseudoLatitude * Math.Cos(pseudoLongitude)));
        double inverseConformalScale = this.ka * Math.Pow(Math.Tan((conformalLatitude / 2.0) + S45), 1 / this.alfa);
        double deltaV = Math.Asin((cosPseudoLatitude * Math.Sin(pseudoLongitude)) / Math.Cos(conformalLatitude));
        double lambda = -deltaV / this.alfa;
        double phi = 0d;

        // iteration calculation
        for (int iter = MaximumIterations; ;)
        {
            double fi1 = phi;
            double esf = this.e * Math.Sin(fi1);
            phi = 2.0 * (Math.Atan(inverseConformalScale * Math.Pow((1.0 + esf) / (1.0 - esf), this.e / 2.0)) - S45);
            if (Math.Abs(fi1 - phi) <= IterationTolerance)
            {
                break;
            }

            if (--iter < 0)
            {
                break;
            }
        }

        x = lambda + this.centralMeridian;
        y = phi;
    }

    /// <summary>
    /// Returns the inverse of this projection.
    /// </summary>
    /// <returns>IMathTransform that is the reverse of the current projection.</returns>
    public override MathTransform Inverse()
    {
        this.inverse ??= this.CreateInverseProjection();

        return this.inverse;
    }

    /// <summary>
    /// Creates the cached inverse projection instance for the current variant.
    /// </summary>
    /// <returns>The inverse projection instance.</returns>
    protected virtual KrovakProjection CreateInverseProjection() => new(this.Parameters.ToProjectionParameter(), this);

    /// <summary>
    /// Computes the modified Krovak correction terms for the current variant.
    /// </summary>
    /// <param name="southing">The southing in metres.</param>
    /// <param name="westing">The westing in metres.</param>
    /// <param name="deltaSouthing">Receives the correction term for the southing component.</param>
    /// <param name="deltaWesting">Receives the correction term for the westing component.</param>
    /// <returns><see langword="true"/> when the current variant applies a modified Krovak correction; otherwise <see langword="false"/>.</returns>
    protected virtual bool TryComputeModifiedDelta(
        double southing,
        double westing,
        out double deltaSouthing,
        out double deltaWesting)
    {
        deltaSouthing = 0d;
        deltaWesting = 0d;
        return false;
    }

    private void MetersToKrovakTarget(ref double x, ref double y)
    {
        double southing = -y;
        double westing = -x;
        if (this.TryComputeModifiedDelta(southing, westing, out double deltaSouthing, out double deltaWesting))
        {
            southing -= deltaSouthing;
            westing -= deltaWesting;
        }

        if (this.eastingNorthing)
        {
            x = -westing - this.falseEasting;
            y = -southing - this.falseNorthing;
        }
        else
        {
            x = westing + this.falseEasting;
            y = southing + this.falseNorthing;
        }

        x *= this.reciprocalMetersPerUnit;
        y *= this.reciprocalMetersPerUnit;
    }

    private void KrovakTargetToMeters(ref double x, ref double y)
    {
        x *= this.metersPerUnit;
        y *= this.metersPerUnit;

        double southing;
        double westing;
        if (this.eastingNorthing)
        {
            westing = -x - this.falseEasting;
            southing = -y - this.falseNorthing;
        }
        else
        {
            westing = x - this.falseEasting;
            southing = y - this.falseNorthing;
        }

        if (this.TryComputeModifiedDelta(southing, westing, out double deltaSouthing, out double deltaWesting))
        {
            southing += deltaSouthing;
            westing += deltaWesting;
        }

        x = -westing;
        y = -southing;
    }
}
