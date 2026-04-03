// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Cassini-Soldner (Transverse Cylindrical Equidistant) map projection.
/// </summary>
/// <remarks>
/// <para>The Cassini-Soldner projection is a transverse cylindrical equidistant projection.
/// It maps the central meridian and its perpendicular cross-sections to straight lines while
/// preserving distances along lines perpendicular to the central meridian. Scale is true
/// along the central meridian and along lines perpendicular to it, but distortion increases
/// with distance from the central meridian.</para>
/// <para>The forward easting series was independently verified against IOGP, "Geomatics
/// Guidance Note 7, part 2: Coordinate Conversions and Transformations including
/// Formulas" (publication 373-7-2, 2019), EPSG method 9806, and John P. Snyder,
/// <i>Map Projections - A Working Manual</i>, U.S. Geological Survey Professional
/// Paper 1395 (1987). The third- and fifth-order <c>T</c> terms are subtractive,
/// matching the polynomial implemented here.</para>
/// </remarks>
/// <seealso href="https://epsg.io/9806-method">EPSG method 9806: Cassini-Soldner.</seealso>
internal class CassiniSoldnerProjection : MapProjection
{
    /// <summary>
    /// Fraction constant 1/6 used in polynomial terms.
    /// </summary>
    private const double One6th = 0.16666666666666666666d;

    /// <summary>
    /// Fraction constant 1/120 used in polynomial terms.
    /// </summary>
    private const double One120th = 0.00833333333333333333d;

    /// <summary>
    /// Fraction constant 1/24 used in polynomial terms.
    /// </summary>
    private const double One24th = 0.04166666666666666666d;

    /// <summary>
    /// Fraction constant 1/3 used in polynomial terms.
    /// </summary>
    private const double One3rd = 0.33333333333333333333d;

    /// <summary>
    /// Fraction constant 1/15 used in polynomial terms.
    /// </summary>
    private const double One15th = 0.06666666666666666666d;

    /// <summary>
    /// Ellipsoid eccentricity helper factor <c>e² / (1 - e²)</c>.
    /// </summary>
    private readonly double cFactor;

    /// <summary>
    /// Meridional distance at latitude of origin.
    /// </summary>
    private readonly double m0;

    /// <summary>
    /// Reciprocal of the semi-major axis length.
    /// </summary>
    private readonly double reciprocalSemiMajor;

    /// <summary>
    /// Initializes a new instance of the <see cref="CassiniSoldnerProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public CassiniSoldnerProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CassiniSoldnerProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public CassiniSoldnerProjection(IEnumerable<ProjectionParameter> parameters, CassiniSoldnerProjection? inverse)
        : base(parameters, inverse)
    {
        this.Authority = "EPSG";
        this.AuthorityCode = 9806;
        this.Name = "Cassini_Soldner";

        this.cFactor = this.es / (1 - this.es);
        this.m0 = this.Mlfn(this.latOrigin, Math.Sin(this.latOrigin), Math.Cos(this.latOrigin));
        this.reciprocalSemiMajor = 1d / this.semiMajor;
    }

    /// <inheritdoc/>
    public override MathTransform Inverse()
    {
        this.inverse ??= new CassiniSoldnerProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc/>
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = lon - this.centralMeridian;
        double phi = lat;

        double sinPhi, cosPhi; // sin and cos value
        Sincos(phi, out sinPhi, out cosPhi);

        double y = this.Mlfn(phi, sinPhi, cosPhi);
        double n = 1.0d / Math.Sqrt(1 - (this.es * sinPhi * sinPhi));
        double tn = Math.Tan(phi);
        double t = tn * tn;
        double a1 = lambda * cosPhi;
        double a2 = a1 * a1;
        double c = this.cFactor * Math.Pow(cosPhi, 2.0d);

        double x = n * a1 * (1.0d - (a2 * t * (One6th + ((8.0d - t + (8.0d * c)) * a2 * One120th))));
        y -= this.m0 - (n * tn * a2 * (0.5d + ((5.0d - t + (6.0d * c)) * a2 * One24th)));

        lon = x * this.semiMajor;
        lat = y * this.semiMajor;
    }

    /// <inheritdoc/>
    protected override void MetersToRadians(ref double x, ref double y)
    {
        x *= this.reciprocalSemiMajor;
        y *= this.reciprocalSemiMajor;
        double phi1 = this.Phi1(this.m0 + y);

        double tn = Math.Tan(phi1);
        double t = tn * tn;
        double n = Math.Sin(phi1);
        double r = 1.0d / (1.0d - (this.es * n * n));
        n = Math.Sqrt(r);
        r *= (1.0d - this.es) * n;
        double dd = x / n;
        double d2 = dd * dd;

        y = phi1 - ((n * tn / r) * d2 * (.5 - ((1.0 + (3.0 * t)) * d2 * One24th)));
        double lambda = dd * (1.0 + (t * d2 * (-One3rd + ((1.0 + (3.0 * t)) * d2 * One15th)))) / Math.Cos(phi1);
        x = Adjust_lon(lambda + this.centralMeridian);
    }

    private double Phi1(double arg)
    {
        const int maxIter = 10;
        const double eps = 1e-11;

        double k = 1.0d / (1.0d - this.es);

        double phi = arg;
        for (int i = maxIter; i > 0; --i)
        { // rarely goes over 2 iterations
            double sinPhi = Math.Sin(phi);
            double t = 1.0d - (this.es * sinPhi * sinPhi);
            t = (this.Mlfn(phi, sinPhi, Math.Cos(phi)) - arg) * (t * Math.Sqrt(t)) * k;
            phi -= t;
            if (Math.Abs(t) < eps)
            {
                return phi;
            }
        }

        ArgumentGuard.ThrowArgument("Convergence error.");
        return 0d;
    }
}
