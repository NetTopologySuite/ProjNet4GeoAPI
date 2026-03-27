// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;

/// <summary>
/// Shared normalized Aitoff/Winkel Tripel forward and inverse equations.
/// </summary>
internal static class AitoffMath
{
    private const double Tolerance = 1e-12;
    private const int MaxIterations = 10;
    private const int MaxRounds = 20;
    private const double JacobianTolerance = 1e-18;
    private const double Pi = Math.PI;
    private const double HalfPi = 0.5d * Pi;

    /// <summary>
    /// Evaluates the normalized forward equations for Aitoff and Winkel Tripel.
    /// </summary>
    /// <param name="lambda">Longitude in radians relative to central meridian.</param>
    /// <param name="phi">Latitude in radians.</param>
    /// <param name="winkelTripel">Whether Winkel Tripel blending is enabled.</param>
    /// <param name="cosphi1">Cosine of standard parallel (Winkel Tripel only).</param>
    /// <param name="x">Projected x in normalized units.</param>
    /// <param name="y">Projected y in normalized units.</param>
    internal static void Forward(double lambda, double phi, bool winkelTripel, double cosphi1, out double x, out double y)
    {
        double c = 0.5d * lambda;
        double d = Math.Acos(ProjectionConstants.Clamp(Math.Cos(phi) * Math.Cos(c), -1d, 1d));
        if (d == 0d)
        {
            x = 0d;
            y = 0d;
        }
        else
        {
            double inverseSinD = 1d / Math.Sin(d);
            x = 2d * d * Math.Cos(phi) * Math.Sin(c) * inverseSinD;
            y = d * Math.Sin(phi) * inverseSinD;
        }

        if (winkelTripel)
        {
            x = 0.5d * (x + (lambda * cosphi1));
            y = 0.5d * (y + phi);
        }
    }

    /// <summary>
    /// Solves the normalized inverse equations for Aitoff and Winkel Tripel.
    /// </summary>
    /// <param name="x">Projected x in normalized units.</param>
    /// <param name="y">Projected y in normalized units.</param>
    /// <param name="winkelTripel">Whether Winkel Tripel blending is enabled.</param>
    /// <param name="cosphi1">Cosine of standard parallel (Winkel Tripel only).</param>
    /// <param name="lambda">Recovered longitude in radians relative to central meridian.</param>
    /// <param name="phi">Recovered latitude in radians.</param>
    internal static void Inverse(double x, double y, bool winkelTripel, double cosphi1, out double lambda, out double phi)
    {
        if (Math.Abs(x) <= Tolerance && Math.Abs(y) <= Tolerance)
        {
            lambda = 0d;
            phi = 0d;
            return;
        }

        lambda = x;
        phi = y;

        bool converged = false;
        for (int round = 0; round < MaxRounds; round++)
        {
            for (int iteration = 0; iteration < MaxIterations; iteration++)
            {
                double sl = Math.Sin(0.5d * lambda);
                double cl = Math.Cos(0.5d * lambda);
                double sp = Math.Sin(phi);
                double cp = Math.Cos(phi);

                double value = cp * cl;
                double c = 1d - (value * value);
                if (c <= Tolerance)
                {
                    ArgumentGuard.ThrowArgument("Input data outside projection domain.");
                }

                double denominator = Math.Pow(c, 1.5d);
                if (Math.Abs(denominator) <= JacobianTolerance)
                {
                    ArgumentGuard.ThrowArgument("Input data outside projection domain.");
                }

                double d = Math.Acos(ProjectionConstants.Clamp(value, -1d, 1d)) / denominator;
                double f1 = 2d * d * c * cp * sl;
                double f2 = d * c * sp;
                double f1p = 2d * (((sl * cl * sp * cp) / c) - (d * sp * sl));
                double f1l = ((cp * cp * sl * sl) / c) + (d * cp * cl * sp * sp);
                double f2p = ((sp * sp * cl) / c) + (d * sl * sl * cp);
                double f2l = 0.5d * (((sp * cp * sl) / c) - (d * sp * cp * cp * sl * cl));

                if (winkelTripel)
                {
                    f1 = 0.5d * (f1 + (lambda * cosphi1));
                    f2 = 0.5d * (f2 + phi);
                    f1p *= 0.5d;
                    f1l = 0.5d * (f1l + cosphi1);
                    f2p = 0.5d * (f2p + 1d);
                    f2l *= 0.5d;
                }

                f1 -= x;
                f2 -= y;

                double determinant = (f1p * f2l) - (f2p * f1l);
                if (Math.Abs(determinant) <= JacobianTolerance)
                {
                    ArgumentGuard.ThrowArgument("Input data outside projection domain.");
                }

                double deltaLambda = ((f2 * f1p) - (f1 * f2p)) / determinant;
                double deltaPhi = ((f1 * f2l) - (f2 * f1l)) / determinant;
                deltaLambda -= Math.Truncate(deltaLambda / Pi) * Pi;

                phi -= deltaPhi;
                lambda -= deltaLambda;

                if (Math.Abs(deltaPhi) <= Tolerance && Math.Abs(deltaLambda) <= Tolerance)
                {
                    break;
                }
            }

            if (phi > HalfPi)
            {
                phi -= 2d * (phi - HalfPi);
            }
            else if (phi < -HalfPi)
            {
                phi -= 2d * (phi + HalfPi);
            }

            if (!winkelTripel && Math.Abs(Math.Abs(phi) - HalfPi) < Tolerance)
            {
                lambda = 0d;
            }

            Forward(lambda, phi, winkelTripel, cosphi1, out double projectedX, out double projectedY);
            if (Math.Abs(x - projectedX) <= Tolerance && Math.Abs(y - projectedY) <= Tolerance)
            {
                converged = true;
                break;
            }
        }

        if (!converged)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }
    }

}
