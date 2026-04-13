// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;

/// <summary>
/// Converts between geodetic and geocentric coordinate representations.
/// </summary>
/// <remarks>
/// <para>Latitude, Longitude and ellipsoidal height in terms of a 3-dimensional geographic system
/// may by expressed in terms of a geocentric (earth centered) Cartesian coordinate reference system
/// X, Y, Z with the Z axis corresponding to the earth's rotation axis positive northwards, the X
/// axis through the intersection of the prime meridian and equator, and the Y axis through
/// the intersection of the equator with longitude 90 degrees east. The geographic and geocentric
/// systems are based on the same geodetic datum.</para>
/// <para>Geocentric coordinate reference systems are conventionally taken to be defined with the X
/// axis through the intersection of the Greenwich meridian and equator. This requires that the equivalent
/// geographic coordinate reference systems based on a non-Greenwich prime meridian should first be
/// transformed to their Greenwich equivalent. Geocentric coordinates X, Y and Z take their units from
/// the units of the ellipsoid axes (a and b). As it is conventional for X, Y and Z to be in metres,
/// if the ellipsoid axis dimensions are given in another linear unit they should first be converted
/// to metres.</para>
/// <para>The forward geographic-to-geocentric conversion was independently verified against IOGP,
/// "Geomatics Guidance Note 7, part 2: Coordinate Conversions and Transformations including
/// Formulas" (publication 373-7-2, 2019), EPSG method 9602, Geographic/geocentric conversions.
/// The cartesian coordinate equations <c>X = (ν + h) * cos(φ) * cos(λ)</c>,
/// <c>Y = (ν + h) * cos(φ) * sin(λ)</c>, and
/// <c>Z = ((1 - e²) * ν + h) * sin(φ)</c> match the implementation here.</para>
/// <para>The inverse conversion follows the Bowring-style cartesian-to-geodetic formulation
/// used by PROJ's <c>cart.cpp</c>. It derives the latitude estimate from the normalized
/// auxiliary quantities <c>xφ</c> and <c>yφ</c>, switches to a geocentric-radius-based
/// height approximation near the poles to avoid division by zero, and retains an additional
/// iterative refinement only for very high altitudes to preserve round-trip accuracy there.
/// That formulation was independently verified against B. R. Bowring, "Transformation from
/// spatial to geographical coordinates," <i>Survey Review</i>, vol. 23, no. 181, pp. 323-327,
/// 1976, and later comparison literature.</para>
/// </remarks>
/// <seealso href="https://epsg.io/9602-method">EPSG method 9602: Geographic/geocentric conversions.</seealso>
/// <seealso href="https://www.researchgate.net/publication/233681872">Research comparison of Bowring-style geocentric to geodetic conversion methods.</seealso>
internal sealed class GeocentricTransform : MathTransform
{
    /// <summary>
    /// Cosine threshold used to switch to the polar height approximation near the poles.
    /// </summary>
    private const double PolarCosphiThreshold = 1e-6d;

    /// <summary>
    /// Eccentricity squared : (a² - b²)/a².
    /// </summary>
    private readonly double es;

    /// <summary>
    /// Semi-major axis length.
    /// </summary>
    private readonly double semiMajor;

    /// <summary>
    /// Minor axis.
    /// </summary>
    private readonly double semiMinor;

    /// <summary>
    /// Second eccentricity squared: <c>(a² - b²) / b²</c>.
    /// </summary>
    private readonly double ses;

    /// <summary>
    /// Indicates whether this instance runs in inverse mode.
    /// </summary>
    private bool isInverse;

    /// <summary>
    /// Cached inverse transform.
    /// </summary>
    private MathTransform? inverse;

    /// <summary>
    /// Projection parameters used to initialize the transform.
    /// </summary>
    private List<ProjectionParameter> parameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeocentricTransform"/> class.
    /// </summary>
    /// <param name="parameters">List of parameters to initialize the projection.</param>
    /// <param name="isInverse">
    /// <see langword="true"/> to convert geocentric Cartesian (meters) to geodetic (degrees);
    /// <see langword="false"/> to convert geodetic (degrees) to geocentric Cartesian (meters).
    /// </param>
    public GeocentricTransform(List<ProjectionParameter> parameters, bool isInverse)
        : this(parameters)
    {
        this.isInverse = isInverse;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeocentricTransform"/> class.
    /// </summary>
    /// <param name="parameters">List of parameters to initialize the projection.</param>
    internal GeocentricTransform(List<ProjectionParameter> parameters)
    {
        this.parameters = parameters;
        ProjectionParameter? semiMajorParameterCandidate = this.parameters.Find(
            par => par.Name.Equals("semi_major", StringComparison.OrdinalIgnoreCase));
        ProjectionParameter semiMajorParameter = ArgumentGuard.ThrowIfNull(semiMajorParameterCandidate, nameof(semiMajorParameterCandidate));
        this.semiMajor = semiMajorParameter.Value;

        ProjectionParameter? semiMinorParameterCandidate = this.parameters.Find(
            par => par.Name.Equals("semi_minor", StringComparison.OrdinalIgnoreCase));
        ProjectionParameter semiMinorParameter = ArgumentGuard.ThrowIfNull(semiMinorParameterCandidate, nameof(semiMinorParameterCandidate));
        this.semiMinor = semiMinorParameter.Value;

        this.es = 1.0 - ((this.semiMinor * this.semiMinor) / (this.semiMajor * this.semiMajor)); // e^2
        this.ses = (Math.Pow(this.semiMajor, 2) - Math.Pow(this.semiMinor, 2)) / Math.Pow(this.semiMinor, 2);
    }

    /// <inheritdoc/>
    public override int DimSource => 3;

    /// <inheritdoc/>
    public override int DimTarget => 3;

    /// <summary>
    /// Gets an XML representation of this object.
    /// </summary>
    public override string XML => throw new NotImplementedException("The method or operation is not implemented.");

    /// <summary>
    /// Returns the inverse of this conversion.
    /// </summary>
    /// <returns>A <see cref="MathTransform"/> that reverses this geocentric conversion.</returns>
    public override MathTransform Inverse()
    {
        this.inverse ??= new GeocentricTransform(this.parameters, !this.isInverse);

        return this.inverse;
    }

    /// <summary>
    /// Computes the normal radius of curvature at a latitude.
    /// </summary>
    /// <param name="semiMajorAxis">The semi-major axis.</param>
    /// <param name="eccentricitySquared">The ellipsoid eccentricity squared.</param>
    /// <param name="sinPhi">The sine of the geodetic latitude.</param>
    /// <returns>The normal radius of curvature.</returns>
    private static double GetNormalRadiusOfCurvature(double semiMajorAxis, double eccentricitySquared, double sinPhi)
    {
        return eccentricitySquared == 0d
            ? semiMajorAxis
            : semiMajorAxis / Math.Sqrt(1d - (eccentricitySquared * sinPhi * sinPhi));
    }

    /// <summary>
    /// Computes the geocentric radius used by the polar height approximation.
    /// </summary>
    /// <param name="semiMajorAxis">The semi-major axis.</param>
    /// <param name="semiMinorOverSemiMajor">The semi-minor to semi-major axis ratio.</param>
    /// <param name="cosPhi">The cosine of the geodetic latitude.</param>
    /// <param name="sinPhi">The sine of the geodetic latitude.</param>
    /// <returns>The geocentric radius at the latitude.</returns>
    private static double GetGeocentricRadius(double semiMajorAxis, double semiMinorOverSemiMajor, double cosPhi, double sinPhi)
    {
        double cosPhiSquared = cosPhi * cosPhi;
        double sinPhiSquared = sinPhi * sinPhi;
        double semiMinorOverSemiMajorSquared = semiMinorOverSemiMajor * semiMinorOverSemiMajor;
        double weightedSinPhiSquared = semiMinorOverSemiMajorSquared * sinPhiSquared;
        return semiMajorAxis * Math.Sqrt(
            (cosPhiSquared + (semiMinorOverSemiMajorSquared * weightedSinPhiSquared)) /
            (cosPhiSquared + weightedSinPhiSquared));
    }

    /// <summary>
    /// Converts a point (lon, lat, z) in degrees to (x, y, z) in meters.
    /// </summary>
    /// <param name="lon">The longitude in degree.</param>
    /// <param name="lat">The latitude in degree.</param>
    /// <param name="z">The z-ordinate value.</param>
    private void DegreesToMeters(ref double lon, ref double lat, ref double z)
    {
        lon = DegreesToRadians(lon);
        lat = DegreesToRadians(lat);
        z = double.IsNaN(z) ? 0 : z;

        double v = this.semiMajor / Math.Sqrt(1 - (this.es * Math.Pow(Math.Sin(lat), 2)));
        double x = (v + z) * Math.Cos(lat) * Math.Cos(lon);
        double y = (v + z) * Math.Cos(lat) * Math.Sin(lon);
        z = (((1 - this.es) * v) + z) * Math.Sin(lat);

        lon = x;
        lat = y;
    }

    /// <summary>
    /// Converts coordinates in projected meters to decimal degrees.
    /// </summary>
    /// <param name="x">The x-ordinate when entering, the longitude value upon exit.</param>
    /// <param name="y">The y-ordinate when entering, the latitude value upon exit.</param>
    /// <param name="z">The z-ordinate value.</param>
    private void MetersToDegrees(ref double x, ref double y, ref double z)
    {
        double xDivA = x / this.semiMajor;
        double yDivA = y / this.semiMajor;
        double zDivA = z / this.semiMajor;
        double pDivA = Math.Sqrt((xDivA * xDivA) + (yDivA * yDivA));
        double semiMinorOverSemiMajor = this.semiMinor / this.semiMajor;
        double scaledPDivA = pDivA * semiMinorOverSemiMajor;
        double norm = Math.Sqrt((zDivA * zDivA) + (scaledPDivA * scaledPDivA));

        double c;
        double s;
        if (norm != 0d)
        {
            double inverseNorm = 1d / norm;
            c = scaledPDivA * inverseNorm;
            s = zDivA * inverseNorm;
        }
        else
        {
            c = 1d;
            s = 0d;
        }

        double yPhi = zDivA + (this.ses * semiMinorOverSemiMajor * s * s * s);
        double xPhi = pDivA - (this.es * c * c * c);
        double normPhi = Math.Sqrt((yPhi * yPhi) + (xPhi * xPhi));

        double cosPhi;
        double sinPhi;
        if (normPhi != 0d)
        {
            double inverseNormPhi = 1d / normPhi;
            cosPhi = xPhi * inverseNormPhi;
            sinPhi = yPhi * inverseNormPhi;
        }
        else
        {
            cosPhi = 1d;
            sinPhi = 0d;
        }

        double lat;
        if (xPhi <= 0d)
        {
            lat = z >= 0d ? Math.PI * 0.5 : -Math.PI * 0.5;
            cosPhi = 0d;
            sinPhi = z >= 0d ? 1d : -1d;
        }
        else
        {
            lat = Math.Atan(yPhi / xPhi);
        }

        double lon = Math.Atan2(yDivA, xDivA);
        double height;
        if (cosPhi < PolarCosphiThreshold)
        {
            double radius = GetGeocentricRadius(this.semiMajor, semiMinorOverSemiMajor, cosPhi, sinPhi);
            height = Math.Abs(z) - radius;
        }
        else
        {
            double normalRadius = GetNormalRadiusOfCurvature(this.semiMajor, this.es, sinPhi);
            height = (this.semiMajor * pDivA / cosPhi) - normalRadius;
        }

        if (Math.Abs(height) > 50000d && xPhi > 0d)
        {
            const double convergenceTolerance = 1e-12d;
            for (int i = 0; i < 10; i++)
            {
                sinPhi = Math.Sin(lat);
                double normalRadius = GetNormalRadiusOfCurvature(this.semiMajor, this.es, sinPhi);
                double nextLatitude = Math.Atan2(z + (this.es * normalRadius * sinPhi), Math.Sqrt((x * x) + (y * y)));
                if (Math.Abs(nextLatitude - lat) < convergenceTolerance)
                {
                    lat = nextLatitude;
                    break;
                }

                lat = nextLatitude;
            }

            sinPhi = Math.Sin(lat);
            cosPhi = Math.Cos(lat);
            if (cosPhi < PolarCosphiThreshold)
            {
                double radius = GetGeocentricRadius(this.semiMajor, semiMinorOverSemiMajor, cosPhi, sinPhi);
                height = Math.Abs(z) - radius;
            }
            else
            {
                double normalRadius = GetNormalRadiusOfCurvature(this.semiMajor, this.es, sinPhi);
                double radialDistance = Math.Sqrt((x * x) + (y * y));
                height = (radialDistance / cosPhi) - normalRadius;
            }
        }

        x = RadiansToDegrees(lon);
        y = RadiansToDegrees(lat);
        z = height;
    }

    /// <inheritdoc/>
    public sealed override void Transform(ref double x, ref double y, ref double z)
    {
        if (this.isInverse)
        {
            this.MetersToDegrees(ref x, ref y, ref z);
        }
        else
        {
            this.DegreesToMeters(ref x, ref y, ref z);
        }
    }

    /// <summary>
    /// Reverses the transformation.
    /// </summary>
    public override void Invert()
    {
        this.isInverse = !this.isInverse;
    }
}
