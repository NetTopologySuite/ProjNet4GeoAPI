// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Winkel II projection (<c>wink2</c>).
/// </summary>
/// <remarks>
/// <para>Winkel II is Oswald Winkel's 1918 compromise projection. The
/// implementation iteratively solves the Mollweide-like auxiliary latitude and then
/// combines that result with the Winkel horizontal averaging term.</para>
/// <para>This implementation matches PROJ's <c>wink2</c> formulation for the
/// arithmetic mean of the Mollweide and equidistant cylindrical projections. The
/// inverse transform follows PROJ's spherical <c>wink2_s_inverse</c> behavior via
/// a numerical inverse over the same forward equations.</para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/projections/wink2.html">PROJ documentation: Winkel II.</seealso>
/// <seealso href="https://desktop.arcgis.com/en/arcmap/latest/map/projections/winkel-ii.htm">ArcGIS projection reference: Winkel II.</seealso>
internal sealed class Winkel2Projection : MapProjection
{
    private const int MaximumIterations = 10;
    private const int InverseMaximumIterations = 15;
    private const double InverseTolerance = 1e-10d;
    private const double FiniteDifferenceStep = 1e-8d;
    private const double MaximumCorrection = 0.3d;

    private readonly double cosphi1;

    /// <summary>
    /// Initializes a new instance of the <see cref="Winkel2Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Winkel2Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Winkel2Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Winkel2Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Winkel_II";
        double lat1Degrees = this.Parameters.GetOptionalParameterValue("lat_1", RadiansToDegrees(this.latOrigin), "standard_parallel_1");
        this.cosphi1 = Math.Cos(DegreesToRadians(lat1Degrees));
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return this.GetOrCreateInverse(() => new Winkel2Projection(this.Parameters.ToProjectionParameter(), this));
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double yPrime = lat * (2d / PI);
        double k = PI * Math.Sin(lat);
        double phi = 1.8d * lat;
        int i = MaximumIterations;

        for (; i > 0; i--)
        {
            double v = (phi + Math.Sin(phi) - k) / (1d + Math.Cos(phi));
            phi -= v;
            if (Math.Abs(v) < Eps7)
            {
                break;
            }
        }

        phi = i == 0 ? (phi < 0d ? -HalfPi : HalfPi) : (0.5d * phi);
        double x = 0.5d * lambda * (Math.Cos(phi) + this.cosphi1);
        double y = FortPi * (Math.Sin(phi) + yPrime);

        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double targetX = x / this.SphericalRadius;
        double targetY = y / this.SphericalRadius;

        double lambda = targetX;
        double phi = targetY;
        double derivLamX = 0d;
        double derivLamY = 0d;
        double derivPhiX = 0d;
        double derivPhiY = 0d;

        for (int i = 0; i < InverseMaximumIterations; i++)
        {
            this.ForwardNormalized(lambda, phi, out double approxX, out double approxY);
            double errorX = approxX - targetX;
            double errorY = approxY - targetY;
            if (Math.Abs(errorX) <= InverseTolerance && Math.Abs(errorY) <= InverseTolerance)
            {
                x = Adjust_lon(this.centralMeridian + lambda);
                y = phi;
                return;
            }

            if (i == 0 || Math.Abs(errorX) > 1e-6d || Math.Abs(errorY) > 1e-6d)
            {
                double deltaLambdaStep = lambda > 0d ? -FiniteDifferenceStep : FiniteDifferenceStep;
                this.ForwardNormalized(lambda + deltaLambdaStep, phi, out double xLambda, out double yLambda);
                double derivXLambda = (xLambda - approxX) / deltaLambdaStep;
                double derivYLambda = (yLambda - approxY) / deltaLambdaStep;

                double deltaPhiStep = phi > 0d ? -FiniteDifferenceStep : FiniteDifferenceStep;
                this.ForwardNormalized(lambda, phi + deltaPhiStep, out double xPhi, out double yPhi);
                double derivXPhi = (xPhi - approxX) / deltaPhiStep;
                double derivYPhi = (yPhi - approxY) / deltaPhiStep;

                double determinant = (derivXLambda * derivYPhi) - (derivXPhi * derivYLambda);
                if (determinant == 0d)
                {
                    ProjectionThrowHelper.ThrowOutsideProjectionDomain();
                }

                derivLamX = derivYPhi / determinant;
                derivLamY = -derivXPhi / determinant;
                derivPhiX = -derivYLambda / determinant;
                derivPhiY = derivXLambda / determinant;
            }

            double deltaLambda = Math.Max(Math.Min((errorX * derivLamX) + (errorY * derivLamY), MaximumCorrection), -MaximumCorrection);
            double deltaPhi = Math.Max(Math.Min((errorX * derivPhiX) + (errorY * derivPhiY), MaximumCorrection), -MaximumCorrection);

            lambda -= deltaLambda;
            phi -= deltaPhi;

            if (lambda < -PI)
            {
                lambda = -PI;
            }
            else if (lambda > PI)
            {
                lambda = PI;
            }

            if (phi < -HalfPi)
            {
                phi = -HalfPi;
            }
            else if (phi > HalfPi)
            {
                phi = HalfPi;
            }
        }

        ProjectionThrowHelper.ThrowOutsideProjectionDomain();
    }

    private void ForwardNormalized(double lambda, double phi, out double x, out double y)
    {
        double yPrime = phi * (2d / PI);
        double k = PI * Math.Sin(phi);
        double phiWorking = 1.8d * phi;
        int i = MaximumIterations;

        for (; i > 0; i--)
        {
            double denominator = 1d + Math.Cos(phiWorking);
            if (Math.Abs(denominator) <= Eps10)
            {
                break;
            }

            double v = (phiWorking + Math.Sin(phiWorking) - k) / denominator;
            phiWorking -= v;
            if (Math.Abs(v) < Eps7)
            {
                break;
            }
        }

        phiWorking = i == 0 ? (phiWorking < 0d ? -HalfPi : HalfPi) : (0.5d * phiWorking);
        x = 0.5d * lambda * (Math.Cos(phiWorking) + this.cosphi1);
        y = FortPi * (Math.Sin(phiWorking) + yPrime);
    }
}
