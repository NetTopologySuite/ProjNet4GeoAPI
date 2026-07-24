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
/// <remarks>
/// <para>HEALPix is an equal-area pseudocylindrical projection that combines an
/// equatorial Lambert cylindrical equal-area zone with polar regions based on an
/// interrupted Collignon construction.</para>
/// <para>The formulation was independently verified against K. M. Gorski et al.,
/// "HEALPix: A Framework for High-Resolution Discretization and Fast Analysis of
/// Data Distributed on the Sphere," <i>Astrophysical Journal</i>, vol. 622, no. 2,
/// pp. 759-771, 2005. The equatorial relation
/// <c>y = 3 * π / 8 * sin(φ)</c> and the polar
/// <c>σ = sqrt(3 * (1 - abs(sin(φ))))</c> construction match the
/// implementation here.</para>
/// </remarks>
/// <seealso href="https://en.wikipedia.org/wiki/HEALPix">Wikipedia: HEALPix.</seealso>
internal sealed class HealpixProjection : MapProjection
{
    private const double CapEpsilon = 1e-15d;
    private static readonly double Phi0Limit = Math.Asin(ProjectionConstants.TwoThirds);

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly bool isEllipsoidal;
    private readonly bool isRhealpix;
    private readonly double oneEs;
    private readonly double qp;
    private readonly double[] apa;
    private readonly double rotationRadians;
    private readonly int northSquare;
    private readonly int southSquare;

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
        this.isRhealpix = this.Parameters.GetOptionalParameterValue("rhealpix_mode", 0d) > Eps10;
        this.northSquare = ReadSquareIndex(this.Parameters, "north_square");
        this.southSquare = ReadSquareIndex(this.Parameters, "south_square");
        this.Name = this.isRhealpix ? "rHEALPix" : "HEALPix";
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
            this.apa = [];
        }

        double authalicScale = this.isEllipsoidal ? Math.Sqrt(0.5d * this.qp) : 1d;
        this.radius = this.semiMajor * this.scaleFactor * authalicScale;
        this.inverseRadius = 1d / this.radius;
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
        if (this.isRhealpix)
        {
            CombineCaps(ref xUnit, ref yUnit, this.northSquare, this.southSquare, inverse: false);
        }
        else
        {
            Rotate(ref xUnit, ref yUnit, -this.rotationRadians);
        }

        lon = this.radius * xUnit;
        lat = this.radius * yUnit;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = y * this.inverseRadius;

        if (this.isRhealpix)
        {
            CombineCaps(ref xUnit, ref yUnit, this.northSquare, this.southSquare, inverse: true);
        }
        else
        {
            Rotate(ref xUnit, ref yUnit, this.rotationRadians);
        }

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

        double lambdaCenter = (-3d * FortPi) + (HalfPi * capNumber);
        x = lambdaCenter + ((lambda - lambdaCenter) * sigma);
        y = Sign(phi) * FortPi * (2d - sigma);
    }

    private static void FromHealpixSphere(double x, double y, out double lambda, out double phi)
    {
        if (Math.Abs(y) <= FortPi)
        {
            lambda = x;
            phi = Math.Asin(ProjectionConstants.Clamp((8d * y) / (3d * PI), -1d, 1d));
            return;
        }

        if (Math.Abs(y) < HalfPi)
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

            double xCenter = (-3d * FortPi) + (HalfPi * capNumber);
            double tau = 2d - ((4d * Math.Abs(y)) / PI);
            if (Math.Abs(tau) <= Eps10)
            {
                lambda = xCenter;
                phi = Sign(y) * HalfPi;
                return;
            }

            lambda = xCenter + ((x - xCenter) / tau);
            phi = Sign(y) * Math.Asin(ProjectionConstants.Clamp(1d - ((tau * tau) / 3d), -1d, 1d));
            return;
        }

        lambda = -PI;
        phi = Sign(y) * HalfPi;
    }

    private static void CombineCaps(ref double x, ref double y, int northSquare, int southSquare, bool inverse)
    {
        GetCap(
            x,
            y,
            northSquare,
            southSquare,
            inverse,
            out bool isPolar,
            out bool isNorth,
            out int capNumber,
            out double capX,
            out double capY);
        if (!isPolar)
        {
            return;
        }

        double deltaX = x - capX;
        double deltaY = y - capY;
        int pole = isNorth ? northSquare : southSquare;
        int quarterTurns = inverse
            ? (isNorth ? -(capNumber - pole) : capNumber - pole)
            : (isNorth ? capNumber - pole : -(capNumber - pole));
        RotateQuarterTurns(ref deltaX, ref deltaY, quarterTurns);

        double anchorX = (-3d * FortPi) + ((inverse ? capNumber : pole) * HalfPi);
        double anchorY = isNorth ? HalfPi : -HalfPi;
        x = deltaX + anchorX;
        y = deltaY + anchorY;
    }

    private static void GetCap(
        double x,
        double y,
        int northSquare,
        int southSquare,
        bool inverse,
        out bool isPolar,
        out bool isNorth,
        out int capNumber,
        out double capX,
        out double capY)
    {
        isPolar = false;
        isNorth = false;
        capNumber = 0;
        capX = x;
        capY = y;

        if (!inverse)
        {
            if (y > FortPi)
            {
                isPolar = true;
                isNorth = true;
                capY = HalfPi;
            }
            else if (y < -FortPi)
            {
                isPolar = true;
                capY = -HalfPi;
            }
            else
            {
                return;
            }

            if (x < -HalfPi)
            {
                capX = -3d * FortPi;
                capNumber = 0;
            }
            else if (x < 0d)
            {
                capX = -FortPi;
                capNumber = 1;
            }
            else if (x < HalfPi)
            {
                capX = FortPi;
                capNumber = 2;
            }
            else
            {
                capX = 3d * FortPi;
                capNumber = 3;
            }

            return;
        }

        double classificationX = x;
        if (y > FortPi)
        {
            isPolar = true;
            isNorth = true;
            capX = (-3d * FortPi) + (northSquare * HalfPi);
            capY = HalfPi;
            classificationX -= northSquare * HalfPi;
        }
        else if (y < -FortPi)
        {
            isPolar = true;
            capX = (-3d * FortPi) + (southSquare * HalfPi);
            capY = -HalfPi;
            classificationX -= southSquare * HalfPi;
        }
        else
        {
            return;
        }

        if (isNorth)
        {
            if (y >= -classificationX - FortPi - CapEpsilon && y < classificationX + (5d * FortPi) - CapEpsilon)
            {
                capNumber = (northSquare + 1) % 4;
                return;
            }

            if (y > -classificationX - FortPi + CapEpsilon && y >= classificationX + (5d * FortPi) - CapEpsilon)
            {
                capNumber = (northSquare + 2) % 4;
                return;
            }

            if (y <= -classificationX - FortPi + CapEpsilon && y > classificationX + (5d * FortPi) + CapEpsilon)
            {
                capNumber = (northSquare + 3) % 4;
                return;
            }

            capNumber = northSquare;
            return;
        }

        if (y <= classificationX + FortPi + CapEpsilon && y > -classificationX - (5d * FortPi) + CapEpsilon)
        {
            capNumber = (southSquare + 1) % 4;
            return;
        }

        if (y < classificationX + FortPi - CapEpsilon && y <= -classificationX - (5d * FortPi) + CapEpsilon)
        {
            capNumber = (southSquare + 2) % 4;
            return;
        }

        if (y >= classificationX + FortPi - CapEpsilon && y < -classificationX - (5d * FortPi) - CapEpsilon)
        {
            capNumber = (southSquare + 3) % 4;
            return;
        }

        capNumber = southSquare;
    }

    private static int ReadSquareIndex(ProjectionParameterSet parameters, string name)
    {
        double value = parameters.GetOptionalParameterValue(name, 0d);
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            ArgumentGuard.ThrowArgument($"Invalid value for {name} parameter: expected an integer between 0 and 3.", nameof(parameters));
        }

        int square = (int)Math.Round(value);
        if (Math.Abs(value - square) > Eps10 || square < 0 || square > 3)
        {
            return ArgumentGuard.ThrowArgument<int>($"Invalid value for {name} parameter: expected an integer between 0 and 3.", nameof(parameters));
        }

        return square;
    }

    private static void RotateQuarterTurns(ref double x, ref double y, int quarterTurns)
    {
        int normalized = quarterTurns % 4;
        if (normalized < 0)
        {
            normalized += 4;
        }

        double originalX = x;
        double originalY = y;
        switch (normalized)
        {
            case 1:
                x = -originalY;
                y = originalX;
                break;
            case 2:
                x = -originalX;
                y = -originalY;
                break;
            case 3:
                x = originalY;
                y = -originalX;
                break;
        }
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
