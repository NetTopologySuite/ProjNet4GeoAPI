// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the HEALPix (Hierarchical Equal Area isoLatitude Pixelization) projection (<c>healpix</c>).
/// </summary>
[Serializable]
internal class HealpixProjection : MapProjection
{
    private static readonly double Phi0Limit = Math.Asin(2d / 3d);
    private const double QuarterPi = PI / 4d;
    private const double HalfPiLocal = PI / 2d;

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly bool isEllipsoidal;
    private readonly double oneEs;
    private readonly double qp;
    private readonly double[] apa;
    private readonly double rotationRadians;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealpixProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public HealpixProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HealpixProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public HealpixProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "HEALPix";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        this.rotationRadians = DegreesToRadians(this.Parameters.GetOptionalParameterValue("rot_xy", 0d));
        this.isEllipsoidal = this.es > 0d;

        if (this.isEllipsoidal)
        {
            this.oneEs = 1d - this.es;
            this.qp = Qsfn(1d, this.e, this.oneEs);
            this.apa = Authset(this.es);
        }
        else
        {
            this.oneEs = 0d;
            this.qp = 0d;
            this.apa = Array.Empty<double>();
        }
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new HealpixProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = this.isEllipsoidal ? this.GeographicToAuthalic(lat) : lat;

        ToHealpixSphere(lambda, phi, out double xUnit, out double yUnit);
        Rotate(ref xUnit, ref yUnit, -this.rotationRadians);

        lon = this.radius * xUnit;
        lat = this.radius * yUnit;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = y * this.inverseRadius;

        Rotate(ref xUnit, ref yUnit, this.rotationRadians);
        FromHealpixSphere(xUnit, yUnit, out double lambda, out double phiAuthalic);

        x = Adjust_lon(this.centralMeridian + lambda);
        y = this.isEllipsoidal ? Authlat(phiAuthalic, this.apa) : phiAuthalic;
    }

    private static void ToHealpixSphere(double lambda, double phi, out double x, out double y)
    {
        if (Math.Abs(phi) <= Phi0Limit)
        {
            x = lambda;
            y = (3d * PI / 8d) * Math.Sin(phi);
            return;
        }

        double sigma = Math.Sqrt(Math.Max(0d, 3d * (1d - Math.Abs(Math.Sin(phi)))));
        int capNumber = (int)Math.Floor((2d * lambda / PI) + 2d);
        if (capNumber < 0)
        {
            capNumber = 0;
        }
        else if (capNumber > 3)
        {
            capNumber = 3;
        }

        double lambdaCenter = (-3d * QuarterPi) + (HalfPiLocal * capNumber);
        x = lambdaCenter + ((lambda - lambdaCenter) * sigma);
        y = Sign(phi) * QuarterPi * (2d - sigma);
    }

    private static void FromHealpixSphere(double x, double y, out double lambda, out double phi)
    {
        if (Math.Abs(y) <= QuarterPi)
        {
            lambda = x;
            phi = Math.Asin(ProjectionConstants.Clamp((8d * y) / (3d * PI), -1d, 1d));
            return;
        }

        if (Math.Abs(y) < HalfPiLocal)
        {
            int capNumber = (int)Math.Floor((2d * x / PI) + 2d);
            if (capNumber < 0)
            {
                capNumber = 0;
            }
            else if (capNumber > 3)
            {
                capNumber = 3;
            }

            double xCenter = (-3d * QuarterPi) + (HalfPiLocal * capNumber);
            double tau = 2d - ((4d * Math.Abs(y)) / PI);
            if (Math.Abs(tau) <= Eps10)
            {
                lambda = xCenter;
                phi = Sign(y) * HalfPiLocal;
                return;
            }

            lambda = xCenter + ((x - xCenter) / tau);
            phi = Sign(y) * Math.Asin(ProjectionConstants.Clamp(1d - ((tau * tau) / 3d), -1d, 1d));
            return;
        }

        lambda = -PI;
        phi = Sign(y) * HalfPiLocal;
    }

    private static void Rotate(ref double x, ref double y, double angle)
    {
        if (Math.Abs(angle) <= Eps10)
        {
            return;
        }

        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);
        double xr = (x * cos) - (y * sin);
        double yr = (y * cos) + (x * sin);
        x = xr;
        y = yr;
    }

    private double GeographicToAuthalic(double phi)
    {
        double q = Qsfn(Math.Sin(phi), this.e, this.oneEs);
        return Math.Asin(ProjectionConstants.Clamp(q / this.qp, -1d, 1d));
    }

}
