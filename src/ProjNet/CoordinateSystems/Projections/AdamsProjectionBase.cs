// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Shared implementation for Adams/Guyou/Peirce quincuncial projections.
/// </summary>
[Serializable]
internal abstract class AdamsProjectionBase : MapProjection
{
    private const double Tolerance = 1e-9d;
    private const double InverseTolerance = 1e-10d;
    private const double InitialGuessEpsilon = 1e-7d;
    private const double JacobianTolerance = 1e-18d;
    private const double FiniteDifferenceStep = 1e-6d;
    private const double StepClamp = 0.3d;
    private const double Rsqrt2 = 0.7071067811865475244008443620d;
    private const double CompleteEllipticHalf = 1.8540746773013719d;
    private const double ShapeShiftDistance = CompleteEllipticHalf * 2d;

    private static readonly double[] EllipticIntegralCoefficients =
    [
        -8.58691003636495e-07d,
        2.02692115653689e-07d,
        3.12960480765314e-05d,
        5.30394739921063e-05d,
        -0.0012804644680613d,
        -0.00575574836830288d,
        0.0914203033408211d,
    ];

    private readonly AdamsMode mode;
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly PeirceShape peirceShape;
    private readonly double scrollX;
    private readonly double scrollY;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdamsProjectionBase"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    /// <param name="name">Projection name.</param>
    /// <param name="mode">Projection mode.</param>
    protected AdamsProjectionBase(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse, string name, AdamsMode mode)
        : base(parameters, inverse)
    {
        this.Name = name;
        this.mode = mode;
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        if (mode == AdamsMode.PeirceQ)
        {
            double shapeCode = this.Parameters.GetOptionalParameterValue("shape", 1d);
            this.peirceShape = ParsePeirceShape(shapeCode);
            this.scrollX = this.Parameters.GetOptionalParameterValue("scrollx", 0d);
            this.scrollY = this.Parameters.GetOptionalParameterValue("scrolly", 0d);
            if (this.peirceShape == PeirceShape.Horizontal && Math.Abs(this.scrollX) > 1d)
            {
                ArgumentGuard.ThrowArgument("Invalid value for scrollx: |scrollx| should be between -1 and 1.");
            }

            if (this.peirceShape == PeirceShape.Vertical && Math.Abs(this.scrollY) > 1d)
            {
                ArgumentGuard.ThrowArgument("Invalid value for scrolly: |scrolly| should be between -1 and 1.");
            }
        }
        else
        {
            this.peirceShape = PeirceShape.Diamond;
            this.scrollX = 0d;
            this.scrollY = 0d;
        }
    }

    /// <summary>
    /// Enumerates supported Adams-family projection modes.
    /// </summary>
    protected enum AdamsMode
    {
        /// <summary>
        /// Guyou projection.
        /// </summary>
        Guyou,

        /// <summary>
        /// Peirce quincuncial projection.
        /// </summary>
        PeirceQ,

        /// <summary>
        /// Adams Hemisphere in a Square.
        /// </summary>
        AdamsHemi,

        /// <summary>
        /// Adams World in a Square I.
        /// </summary>
        AdamsWs1,

        /// <summary>
        /// Adams World in a Square II.
        /// </summary>
        AdamsWs2,
    }

    private enum PeirceShape
    {
        Square = 0,
        Diamond = 1,
        NHemisphere = 2,
        SHemisphere = 3,
        Horizontal = 4,
        Vertical = 5,
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;

        this.ForwardNormalized(lambda, phi, out double xUnit, out double yUnit);

        lon = this.radius * xUnit;
        lat = this.radius * yUnit;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = y * this.inverseRadius;

        bool canInvert = this.mode == AdamsMode.AdamsWs2
            || (this.mode == AdamsMode.PeirceQ
                && (this.peirceShape == PeirceShape.Square || this.peirceShape == PeirceShape.Diamond));

        if (!canInvert)
        {
            throw new InvalidOperationException($"{this.Name} does not support inverse projection in this wave.");
        }

        double lambda;
        double phi;
        bool success;
        if (this.mode == AdamsMode.AdamsWs2)
        {
            success = this.TryInverseAdamsWs2(xUnit, yUnit, out lambda, out phi);
        }
        else
        {
            success = this.TryInversePeirce(xUnit, yUnit, out lambda, out phi);
        }

        if (!success)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }

    private static PeirceShape ParsePeirceShape(double shapeCode)
    {
        int code = (int)Math.Round(shapeCode, MidpointRounding.AwayFromZero);
        if (Math.Abs(shapeCode - code) > 1e-12d)
        {
            ArgumentGuard.ThrowArgument("Invalid value for shape parameter.");
        }

        return code switch
        {
            0 => PeirceShape.Square,
            1 => PeirceShape.Diamond,
            2 => PeirceShape.NHemisphere,
            3 => PeirceShape.SHemisphere,
            4 => PeirceShape.Horizontal,
            5 => PeirceShape.Vertical,
            _ => ArgumentGuard.ThrowArgument<PeirceShape>("Invalid value for shape parameter."),
        };
    }

    private static void RotateFortyFive(ref double x, ref double y)
    {
        double temp = x;
        x = Rsqrt2 * (x - y);
        y = Rsqrt2 * (temp + y);
    }

    private static double EllipticIntegralHalf(double phi)
    {
        const double c0 = 2.19174570831038d;
        double y = phi * (2d / PI);
        y = (2d * y * y) - 1d;
        double y2 = 2d * y;
        double d1 = 0d;
        double d2 = 0d;
        foreach (double c in EllipticIntegralCoefficients)
        {
            double temp = d1;
            d1 = (y2 * d1) - d2 + c;
            d2 = temp;
        }

        return phi * ((y * d1) - d2 + (0.5d * c0));
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }

    private static bool IsApproximatelyZero(double value)
    {
        return Math.Abs(value) <= 1e-12d;
    }

    private void ForwardNormalized(double lambda, double phi, out double x, out double y)
    {
        double a = 0d;
        double b = 0d;
        bool sm = false;
        bool sn = false;

        switch (this.mode)
        {
            case AdamsMode.Guyou:
                if ((Math.Abs(lambda) - Tolerance) > HalfPi)
                {
                    ArgumentGuard.ThrowArgument("Input data outside projection domain.");
                }

                if (Math.Abs(Math.Abs(phi) - HalfPi) < Tolerance)
                {
                    x = 0d;
                    y = phi < 0d ? -CompleteEllipticHalf : CompleteEllipticHalf;
                    return;
                }

                {
                    double sl = Math.Sin(lambda);
                    double sp = Math.Sin(phi);
                    double cp = Math.Cos(phi);
                    a = Math.Acos(Clamp(((cp * sl) - sp) * Rsqrt2, -1d, 1d));
                    b = Math.Acos(Clamp(((cp * sl) + sp) * Rsqrt2, -1d, 1d));
                    sm = lambda < 0d;
                    sn = phi < 0d;
                }

                break;

            case AdamsMode.PeirceQ:
                if (this.peirceShape == PeirceShape.NHemisphere && phi < -Tolerance)
                {
                    ArgumentGuard.ThrowArgument("Input data outside projection domain.");
                }

                if (this.peirceShape == PeirceShape.SHemisphere && phi > -Tolerance)
                {
                    ArgumentGuard.ThrowArgument("Input data outside projection domain.");
                }

                {
                    double sl = Math.Sin(lambda);
                    double cl = Math.Cos(lambda);
                    double cp = Math.Cos(phi);
                    a = Math.Acos(Clamp(cp * (sl + cl) * Rsqrt2, -1d, 1d));
                    b = Math.Acos(Clamp(cp * (sl - cl) * Rsqrt2, -1d, 1d));
                    sm = sl < 0d;
                    sn = cl > 0d;
                }

                break;

            case AdamsMode.AdamsHemi:
                if ((Math.Abs(lambda) - Tolerance) > HalfPi)
                {
                    ArgumentGuard.ThrowArgument("Input data outside projection domain.");
                }

                {
                    double sp = Math.Sin(phi);
                    a = Math.Cos(phi) * Math.Sin(lambda);
                    sm = (sp + a) < 0d;
                    sn = (sp - a) < 0d;
                    a = Math.Acos(Clamp(a, -1d, 1d));
                    b = HalfPi - phi;
                }

                break;

            case AdamsMode.AdamsWs1:
                {
                    double sp = Math.Tan(0.5d * phi);
                    b = Math.Cos(Asinz(sp)) * Math.Sin(0.5d * lambda);
                    a = Math.Acos(Clamp((b - sp) * Rsqrt2, -1d, 1d));
                    b = Math.Acos(Clamp((b + sp) * Rsqrt2, -1d, 1d));
                    sm = lambda < 0d;
                    sn = phi < 0d;
                }

                break;

            case AdamsMode.AdamsWs2:
                {
                    double sp = Math.Tan(0.5d * phi);
                    a = Math.Cos(Asinz(sp)) * Math.Sin(0.5d * lambda);
                    sm = (sp + a) < 0d;
                    sn = (sp - a) < 0d;
                    b = Math.Acos(Clamp(sp, -1d, 1d));
                    a = Math.Acos(Clamp(a, -1d, 1d));
                }

                break;
        }

        double m = Asinz(Math.Sqrt(1d + Math.Min(0d, Math.Cos(a + b))));
        if (sm)
        {
            m = -m;
        }

        double n = Asinz(Math.Sqrt(Math.Abs(1d - Math.Max(0d, Math.Cos(a - b)))));
        if (sn)
        {
            n = -n;
        }

        x = EllipticIntegralHalf(m);
        y = EllipticIntegralHalf(n);

        if (this.mode == AdamsMode.PeirceQ)
        {
            this.ApplyPeirceLayout(ref x, ref y, lambda, phi);
        }

        if (this.mode == AdamsMode.AdamsHemi || this.mode == AdamsMode.AdamsWs2)
        {
            RotateFortyFive(ref x, ref y);
        }
    }

    private void ApplyPeirceLayout(ref double x, ref double y, double lambda, double phi)
    {
        if (this.peirceShape == PeirceShape.Square || this.peirceShape == PeirceShape.Diamond)
        {
            if (phi < 0d)
            {
                if (lambda < (-0.75d * PI))
                {
                    y = ShapeShiftDistance - y;
                }

                if (lambda < (-0.25d * PI) && lambda >= (-0.75d * PI))
                {
                    x = -ShapeShiftDistance - x;
                }

                if (lambda < (0.25d * PI) && lambda >= (-0.25d * PI))
                {
                    y = -ShapeShiftDistance - y;
                }

                if (lambda < (0.75d * PI) && lambda >= (0.25d * PI))
                {
                    x = ShapeShiftDistance - x;
                }

                if (lambda >= (0.75d * PI))
                {
                    y = ShapeShiftDistance - y;
                }
            }
        }

        if (this.peirceShape == PeirceShape.Square)
        {
            RotateFortyFive(ref x, ref y);
        }

        if (this.peirceShape == PeirceShape.Horizontal)
        {
            if (phi < 0d)
            {
                x = ShapeShiftDistance - x;
            }

            x -= ShapeShiftDistance * 0.5d;
            if (Math.Abs(this.scrollX) > 0d)
            {
                const double scale = 2d;
                double threshold = ShapeShiftDistance * 0.5d;
                x += this.scrollX * (threshold * 2d * scale);
                if (x >= threshold * scale)
                {
                    x -= ShapeShiftDistance * scale;
                }
                else if (x < -(threshold * scale))
                {
                    x += ShapeShiftDistance * scale;
                }
            }
        }

        if (this.peirceShape == PeirceShape.Vertical)
        {
            if (phi < 0d)
            {
                y = ShapeShiftDistance - y;
            }

            y -= ShapeShiftDistance * 0.5d;
            if (Math.Abs(this.scrollY) > 0d)
            {
                const double scale = 2d;
                double threshold = ShapeShiftDistance * 0.5d;
                y += this.scrollY * (threshold * 2d * scale);
                if (y >= threshold * scale)
                {
                    y -= ShapeShiftDistance * scale;
                }
                else if (y < -(threshold * scale))
                {
                    y += ShapeShiftDistance * scale;
                }
            }
        }
    }

    private bool TryInverseAdamsWs2(double x, double y, out double lambda, out double phi)
    {
        phi = Clamp(y / 2.62181347d, -1d, 1d) * HalfPi;
        lambda = Math.Abs(phi) >= HalfPi
            ? 0d
            : Clamp(x / 2.62205760d / Math.Cos(phi), -1d, 1d) * PI;

        return this.TryGenericInverse2D(x, y, lambda, phi, InverseTolerance, out lambda, out phi);
    }

    private bool TryInversePeirce(double x, double y, out double lambda, out double phi)
    {
        if (this.peirceShape == PeirceShape.Square)
        {
            return this.TryInversePeirceSquare(x, y, out lambda, out phi);
        }

        return this.TryInversePeirceDiamond(x, y, out lambda, out phi);
    }

    private bool TryInversePeirceSquare(double x, double y, out double lambda, out double phi)
    {
        phi = 0d;
        if (IsApproximatelyZero(x) && y < 0d)
        {
            lambda = -PI / 4d;
            if (Math.Abs(y) < 2.622057580396d)
            {
                phi = PI / 4d;
            }
        }
        else if (x > 0d && IsApproximatelyZero(y))
        {
            lambda = PI / 4d;
        }
        else if (x < 0d && IsApproximatelyZero(y))
        {
            lambda = -3d * PI / 4d;
            phi = ((PI / 2d) / 2.622057574224d * x) + (PI / 2d);
        }
        else if (IsApproximatelyZero(x) && y > 0d)
        {
            lambda = 3d * PI / 4d;
        }
        else if (x >= 0d && y <= 0d)
        {
            lambda = 0d;
            if (IsApproximatelyZero(x) && IsApproximatelyZero(y))
            {
                phi = PI / 2d;
                return true;
            }
        }
        else if (x >= 0d && y >= 0d)
        {
            lambda = PI / 2d;
        }
        else if (x <= 0d && y >= 0d)
        {
            lambda = Math.Abs(x) < Math.Abs(y) ? PI * 0.9d : -PI * 0.9d;
        }
        else
        {
            lambda = -PI / 2d;
        }

        return this.TryGenericInverse2D(x, y, lambda, phi, InverseTolerance, out lambda, out phi);
    }

    private bool TryInversePeirceDiamond(double x, double y, out double lambda, out double phi)
    {
        phi = 0d;
        if (x >= 0d && y <= 0d)
        {
            lambda = PI / 4d;
            if (x > 0d && IsApproximatelyZero(y))
            {
                lambda = PI / 2d;
                phi = 0d;
            }
            else if (IsApproximatelyZero(x) && IsApproximatelyZero(y))
            {
                lambda = 0d;
                phi = PI / 2d;
                return true;
            }
            else if (IsApproximatelyZero(x) && y < 0d)
            {
                lambda = 0d;
                phi = PI / 4d;
            }
        }
        else if (x >= 0d && y >= 0d)
        {
            lambda = 3d * PI / 4d;
        }
        else if (x <= 0d && y >= 0d)
        {
            lambda = -3d * PI / 4d;
        }
        else
        {
            lambda = -PI / 4d;
        }

        if (Math.Abs(x) > CompleteEllipticHalf + 1e-3d || Math.Abs(y) > CompleteEllipticHalf + 1e-3d)
        {
            phi = -PI / 4d;
        }

        return this.TryGenericInverse2D(x, y, lambda, phi, InverseTolerance, out lambda, out phi);
    }

    private bool TryGenericInverse2D(
        double targetX,
        double targetY,
        double initialLambda,
        double initialPhi,
        double deltaXyTolerance,
        out double lambda,
        out double phi)
    {
        lambda = initialLambda;
        phi = initialPhi;
        bool haveDerivatives = false;
        double derivLamX = 0d;
        double derivLamY = 0d;
        double derivPhiX = 0d;
        double derivPhiY = 0d;

        for (int i = 0; i < 15; i++)
        {
            if (!this.TryForwardForInverse(lambda, phi, out double approxX, out double approxY))
            {
                return false;
            }

            double deltaX = approxX - targetX;
            double deltaY = approxY - targetY;
            if (Math.Abs(deltaX) < deltaXyTolerance && Math.Abs(deltaY) < deltaXyTolerance)
            {
                return true;
            }

            if (i == 0 || Math.Abs(deltaX) > FiniteDifferenceStep || Math.Abs(deltaY) > FiniteDifferenceStep || !haveDerivatives)
            {
                if (!this.TryComputeInverseJacobian(lambda, phi, approxX, approxY, out derivLamX, out derivLamY, out derivPhiX, out derivPhiY))
                {
                    if (!haveDerivatives)
                    {
                        return false;
                    }
                }
                else
                {
                    haveDerivatives = true;
                }
            }

            if (!haveDerivatives)
            {
                return false;
            }

            double deltaLambda = Clamp((deltaX * derivLamX) + (deltaY * derivLamY), -StepClamp, StepClamp);
            lambda -= deltaLambda;
            lambda = Clamp(lambda, -PI, PI);

            double deltaPhi = Clamp((deltaX * derivPhiX) + (deltaY * derivPhiY), -StepClamp, StepClamp);
            phi -= deltaPhi;
            phi = Clamp(phi, -HalfPi, HalfPi);
        }

        if (Math.Abs(lambda) < InitialGuessEpsilon)
        {
            lambda = 0d;
        }

        if (Math.Abs(phi) < InitialGuessEpsilon)
        {
            phi = 0d;
        }

        return false;
    }

    private bool TryComputeInverseJacobian(
        double lambda,
        double phi,
        double approxX,
        double approxY,
        out double derivLamX,
        out double derivLamY,
        out double derivPhiX,
        out double derivPhiY)
    {
        derivLamX = 0d;
        derivLamY = 0d;
        derivPhiX = 0d;
        derivPhiY = 0d;

        double dLam = lambda > 0d ? -FiniteDifferenceStep : FiniteDifferenceStep;
        if (!this.TryForwardForInverse(lambda + dLam, phi, out double xLam, out double yLam))
        {
            return false;
        }

        double derivXLam = (xLam - approxX) / dLam;
        double derivYLam = (yLam - approxY) / dLam;

        double dPhi = phi > 0d ? -FiniteDifferenceStep : FiniteDifferenceStep;
        if (!this.TryForwardForInverse(lambda, phi + dPhi, out double xPhi, out double yPhi))
        {
            return false;
        }

        double derivXPhi = (xPhi - approxX) / dPhi;
        double derivYPhi = (yPhi - approxY) / dPhi;
        double det = (derivXLam * derivYPhi) - (derivXPhi * derivYLam);
        if (Math.Abs(det) <= JacobianTolerance)
        {
            return false;
        }

        derivLamX = derivYPhi / det;
        derivLamY = -derivXPhi / det;
        derivPhiX = -derivYLam / det;
        derivPhiY = derivXLam / det;
        return true;
    }

    private bool TryForwardForInverse(double lambda, double phi, out double x, out double y)
    {
        try
        {
            this.ForwardNormalized(lambda, phi, out x, out y);
            return !(double.IsNaN(x) || double.IsNaN(y));
        }
        catch (ArgumentException)
        {
            x = 0d;
            y = 0d;
            return false;
        }
    }
}
