// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Winkel II projection (<c>wink2</c>).
/// </summary>
[Serializable]
internal class Winkel2Projection : MapProjection
{
    private const int MaximumIterations = 10;
    private const double LoopTolerance = 1e-7;

    private readonly double radius;
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
    public Winkel2Projection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Winkel_II";
        this.radius = this.semiMajor * this.scaleFactor;
        double lat1Degrees = this.Parameters.GetOptionalParameterValue("lat_1", RadiansToDegrees(this.latOrigin), "standard_parallel_1");
        this.cosphi1 = Math.Cos(DegreesToRadians(lat1Degrees));
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new Winkel2Projection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
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
            if (Math.Abs(v) < LoopTolerance)
            {
                break;
            }
        }

        phi = i == 0 ? (phi < 0d ? -HalfPi : HalfPi) : (0.5d * phi);
        double x = 0.5d * lambda * (Math.Cos(phi) + this.cosphi1);
        double y = FortPi * (Math.Sin(phi) + yPrime);

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Always thrown; the Winkel II projection does not support inverse transformation.</exception>
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Winkel II does not support inverse projection in this wave.");
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
            if (Math.Abs(v) < LoopTolerance)
            {
                break;
            }
        }

        phiWorking = i == 0 ? (phiWorking < 0d ? -HalfPi : HalfPi) : (0.5d * phiWorking);
        x = 0.5d * lambda * (Math.Cos(phiWorking) + this.cosphi1);
        y = FortPi * (Math.Sin(phiWorking) + yPrime);
    }
}
