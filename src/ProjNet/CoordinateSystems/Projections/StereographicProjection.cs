// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements PROJ-style stereographic projection support for polar, oblique, and equatorial modes.
/// </summary>
/// <remarks>
/// <para>
/// The implementation follows PROJ's <c>stere.cpp</c> and supports ellipsoidal and spherical
/// formulations of <c>+proj=stere</c>, including polar true-scale handling via <c>lat_ts</c>.
/// </para>
/// <para>
/// This projection is distinct from <c>sterea</c>, which uses the double-projection
/// oblique stereographic alternative algorithm.
/// </para>
/// </remarks>
internal sealed class StereographicProjection : MapProjection
{
    private const int MaximumIterations = 8;
    private const double IterationTolerance = 1e-10d;
    private const double PolarTolerance = 1e-8d;
    private const double PoleEpsilon = 1e-15d;

    private readonly double globalScale;
    private readonly double reciprocalGlobalScale;
    private readonly double akm1;
    private readonly double sinX1;
    private readonly double cosX1;
    private readonly Mode mode;

    /// <summary>
    /// Initializes a new instance of the <see cref="StereographicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public StereographicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StereographicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public StereographicProjection(IEnumerable<ProjectionParameter> parameters, StereographicProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Stereographic";
        this.globalScale = this.semiMajor;
        this.reciprocalGlobalScale = 1d / this.globalScale;

        double absLatitudeOfOrigin = Math.Abs(this.latOrigin);
        if (Math.Abs(absLatitudeOfOrigin - HalfPi) < Eps10)
        {
            this.mode = this.latOrigin < 0d ? Mode.SouthPole : Mode.NorthPole;
        }
        else
        {
            this.mode = absLatitudeOfOrigin > Eps10 ? Mode.Oblique : Mode.Equatorial;
        }

        double latitudeOfTrueScale = Math.Abs(DegreesToRadians(this.Parameters.GetOptionalParameterValue("lat_ts", 90d)));
        if (this.es != 0d)
        {
            switch (this.mode)
            {
                case Mode.NorthPole:
                case Mode.SouthPole:
                    if (Math.Abs(latitudeOfTrueScale - HalfPi) < Eps10)
                    {
                        this.akm1 = 2d * this.scaleFactor / Math.Sqrt(Math.Pow(1d + this.e, 1d + this.e) * Math.Pow(1d - this.e, 1d - this.e));
                    }
                    else
                    {
                        double sinLatitudeOfTrueScale = Math.Sin(latitudeOfTrueScale);
                        double trueScaleTs = MathHelpers.Tsfn(latitudeOfTrueScale, sinLatitudeOfTrueScale, this.e);
                        double eccentricTrueScale = this.e * sinLatitudeOfTrueScale;
                        this.akm1 = Math.Cos(latitudeOfTrueScale) / trueScaleTs;
                        this.akm1 /= Math.Sqrt(1d - (eccentricTrueScale * eccentricTrueScale));
                    }

                    break;
                case Mode.Oblique:
                case Mode.Equatorial:
                    double sinLatitudeOfOrigin = Math.Sin(this.latOrigin);
                    double x = (2d * Math.Atan(MathHelpers.Ssfn(this.latOrigin, sinLatitudeOfOrigin, this.e))) - HalfPi;
                    double eccentricOrigin = this.e * sinLatitudeOfOrigin;
                    this.akm1 = (2d * this.scaleFactor * Math.Cos(this.latOrigin)) / Math.Sqrt(1d - (eccentricOrigin * eccentricOrigin));
                    this.sinX1 = Math.Sin(x);
                    this.cosX1 = Math.Cos(x);
                    break;
            }
        }
        else
        {
            switch (this.mode)
            {
                case Mode.Oblique:
                    this.sinX1 = Math.Sin(this.latOrigin);
                    this.cosX1 = Math.Cos(this.latOrigin);
                    this.akm1 = 2d * this.scaleFactor;
                    break;
                case Mode.Equatorial:
                    this.akm1 = 2d * this.scaleFactor;
                    break;
                case Mode.SouthPole:
                case Mode.NorthPole:
                    this.akm1 = Math.Abs(latitudeOfTrueScale - HalfPi) >= Eps10
                        ? Math.Cos(latitudeOfTrueScale) / Math.Tan(FortPi - (0.5d * latitudeOfTrueScale))
                        : 2d * this.scaleFactor;
                    break;
            }
        }
    }

    private enum Mode
    {
        SouthPole = 0,
        NorthPole = 1,
        Oblique = 2,
        Equatorial = 3,
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new StereographicProjection(this.Parameters.ToProjectionParameter(), this);
        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = lon - this.centralMeridian;
        double phi = lat;
        double x;
        double y;

        if (this.es != 0d)
        {
            this.EllipsoidalForward(lambda, phi, out x, out y);
        }
        else
        {
            this.SphericalForward(lambda, phi, out x, out y);
        }

        lon = x * this.globalScale;
        lat = y * this.globalScale;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        x *= this.reciprocalGlobalScale;
        y *= this.reciprocalGlobalScale;

        if (this.es != 0d)
        {
            this.EllipsoidalInverse(ref x, ref y);
        }
        else
        {
            this.SphericalInverse(ref x, ref y);
        }
    }

    private void EllipsoidalForward(double lambda, double phi, out double x, out double y)
    {
        double cosLambda = Math.Cos(lambda);
        double sinLambda = Math.Sin(lambda);
        double sinPhi = Math.Sin(phi);
        double xUnit;
        double yUnit;

        switch (this.mode)
        {
            case Mode.Oblique:
            case Mode.Equatorial:
                double xLatitude = (2d * Math.Atan(MathHelpers.Ssfn(phi, sinPhi, this.e))) - HalfPi;
                double sinX = Math.Sin(xLatitude);
                double cosX = Math.Cos(xLatitude);
                if (this.mode == Mode.Oblique)
                {
                    double denominator = this.cosX1 * (1d + (this.sinX1 * sinX) + (this.cosX1 * cosX * cosLambda));
                    if (Math.Abs(denominator) <= double.Epsilon)
                    {
                        x = HugeVal;
                        y = HugeVal;
                        return;
                    }

                    double a = this.akm1 / denominator;
                    yUnit = a * ((this.cosX1 * sinX) - (this.sinX1 * cosX * cosLambda));
                    xUnit = a * cosX;
                }
                else
                {
                    double denominator = 1d + (cosX * cosLambda);
                    if (Math.Abs(denominator) <= double.Epsilon)
                    {
                        x = HugeVal;
                        y = HugeVal;
                        return;
                    }

                    double a = this.akm1 / denominator;
                    yUnit = a * sinX;
                    xUnit = a * cosX;
                }

                x = xUnit * sinLambda;
                y = yUnit;
                return;
            case Mode.SouthPole:
                phi = -phi;
                cosLambda = -cosLambda;
                sinPhi = -sinPhi;
                goto case Mode.NorthPole;
            case Mode.NorthPole:
                xUnit = Math.Abs(phi - HalfPi) < PoleEpsilon ? 0d : this.akm1 * MathHelpers.Tsfn(phi, sinPhi, this.e);
                yUnit = -xUnit * cosLambda;
                x = xUnit * sinLambda;
                y = yUnit;
                return;
            default:
                throw new InvalidOperationException("Unsupported stereographic mode.");
        }
    }

    private void SphericalForward(double lambda, double phi, out double x, out double y)
    {
        double sinPhi = Math.Sin(phi);
        double cosPhi = Math.Cos(phi);
        double cosLambda = Math.Cos(lambda);
        double sinLambda = Math.Sin(lambda);

        switch (this.mode)
        {
            case Mode.Equatorial:
                {
                    double denominator = 1d + (cosPhi * cosLambda);
                    if (denominator <= Eps10)
                    {
                        x = HugeVal;
                        y = HugeVal;
                        return;
                    }

                    double radialScale = this.akm1 / denominator;
                    x = radialScale * cosPhi * sinLambda;
                    y = radialScale * sinPhi;
                    return;
                }

            case Mode.Oblique:
                {
                    double denominator = 1d + (this.sinX1 * sinPhi) + (this.cosX1 * cosPhi * cosLambda);
                    if (denominator <= Eps10)
                    {
                        x = HugeVal;
                        y = HugeVal;
                        return;
                    }

                    double radialScale = this.akm1 / denominator;
                    x = radialScale * cosPhi * sinLambda;
                    y = radialScale * ((this.cosX1 * sinPhi) - (this.sinX1 * cosPhi * cosLambda));
                    return;
                }

            case Mode.NorthPole:
                cosLambda = -cosLambda;
                phi = -phi;
                goto case Mode.SouthPole;
            case Mode.SouthPole:
                if (Math.Abs(phi - HalfPi) < PolarTolerance)
                {
                    x = HugeVal;
                    y = HugeVal;
                    return;
                }

                y = this.akm1 * Math.Tan(FortPi + (0.5d * phi));
                x = sinLambda * y;
                y *= cosLambda;
                return;
            default:
                throw new InvalidOperationException("Unsupported stereographic mode.");
        }
    }

    private void EllipsoidalInverse(ref double x, ref double y)
    {
        double rho = Math.Sqrt((x * x) + (y * y));
        double tp;
        double phiL;
        double halfPiShift;
        double halfE;

        switch (this.mode)
        {
            case Mode.Oblique:
            case Mode.Equatorial:
                tp = 2d * Math.Atan2(rho * this.cosX1, this.akm1);
                double cosPhi = Math.Cos(tp);
                double sinPhi = Math.Sin(tp);
                phiL = rho == 0d
                    ? Math.Asin(ProjectionConstants.Clamp(cosPhi * this.sinX1, -1d, 1d))
                    : Math.Asin(ProjectionConstants.Clamp((cosPhi * this.sinX1) + ((y * sinPhi * this.cosX1) / rho), -1d, 1d));

                tp = Math.Tan(0.5d * (HalfPi + phiL));
                x *= sinPhi;
                y = (rho * this.cosX1 * cosPhi) - (y * this.sinX1 * sinPhi);
                halfPiShift = HalfPi;
                halfE = 0.5d * this.e;
                break;
            case Mode.NorthPole:
                y = -y;
                goto case Mode.SouthPole;
            case Mode.SouthPole:
                tp = -rho / this.akm1;
                phiL = HalfPi - (2d * Math.Atan(tp));
                halfPiShift = -HalfPi;
                halfE = -0.5d * this.e;
                break;
            default:
                throw new InvalidOperationException("Unsupported stereographic mode.");
        }

        for (int i = MaximumIterations; i > 0; i--)
        {
            double sinPhi = this.e * Math.Sin(phiL);
            double phi = (2d * Math.Atan(tp * Math.Pow((1d + sinPhi) / (1d - sinPhi), halfE))) - halfPiShift;
            if (Math.Abs(phiL - phi) < IterationTolerance)
            {
                if (this.mode == Mode.SouthPole)
                {
                    phi = -phi;
                }

                x = (x == 0d && y == 0d) ? 0d : Math.Atan2(x, y);
                x += this.centralMeridian;
                y = phi;
                return;
            }

            phiL = phi;
        }

        throw new InvalidOperationException("Stereographic inverse did not converge.");
    }

    private void SphericalInverse(ref double x, ref double y)
    {
        double rh = Math.Sqrt((x * x) + (y * y));
        double c = 2d * Math.Atan(rh / this.akm1);
        double sinC = Math.Sin(c);
        double cosC = Math.Cos(c);
        double phi;
        double lambda = 0d;

        switch (this.mode)
        {
            case Mode.Equatorial:
                phi = Math.Abs(rh) <= Eps10 ? 0d : Math.Asin(ProjectionConstants.Clamp((y * sinC) / rh, -1d, 1d));
                if (cosC != 0d || x != 0d)
                {
                    lambda = Math.Atan2(x * sinC, cosC * rh);
                }

                break;
            case Mode.Oblique:
                phi = Math.Abs(rh) <= Eps10
                    ? this.latOrigin
                    : Math.Asin(ProjectionConstants.Clamp((cosC * this.sinX1) + ((y * sinC * this.cosX1) / rh), -1d, 1d));
                c = cosC - (this.sinX1 * Math.Sin(phi));
                if (c != 0d || x != 0d)
                {
                    lambda = Math.Atan2(x * sinC * this.cosX1, c * rh);
                }

                break;
            case Mode.NorthPole:
                y = -y;
                goto case Mode.SouthPole;
            case Mode.SouthPole:
                phi = Math.Abs(rh) <= Eps10
                    ? this.latOrigin
                    : Math.Asin(ProjectionConstants.Clamp(this.mode == Mode.SouthPole ? -cosC : cosC, -1d, 1d));
                lambda = (x == 0d && y == 0d) ? 0d : Math.Atan2(x, y);
                break;
            default:
                throw new InvalidOperationException("Unsupported stereographic mode.");
        }

        x = lambda + this.centralMeridian;
        y = phi;
    }

    private static class MathHelpers
    {
        public static double Ssfn(double phi, double sinPhi, double eccentricity)
        {
            double eccentricSinPhi = eccentricity * sinPhi;
            return Math.Tan(0.5d * (HalfPi + phi)) * Math.Pow((1d - eccentricSinPhi) / (1d + eccentricSinPhi), 0.5d * eccentricity);
        }

        public static double Tsfn(double phi, double sinPhi, double eccentricity)
        {
            double cosPhi = Math.Cos(phi);
            double t = sinPhi > 0d ? cosPhi / (1d + sinPhi) : (1d - sinPhi) / cosPhi;
            return Math.Exp(eccentricity * Atanh(eccentricity * sinPhi)) * t;
        }

        public static double Atanh(double x)
        {
            return 0.5d * Math.Log((1d + x) / (1d - x));
        }
    }
}
