// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Boggs Eumorphic projection (<c>boggs</c>).
/// </summary>
/// <remarks>
/// The Boggs Eumorphic projection is a compromise pseudocylindrical projection combining
/// properties of the sinusoidal and Mollweide projections.
/// Inverse projection is not supported in this implementation.
/// <para>The forward formulation was independently verified against the standard Boggs
/// construction as the mean of sinusoidal and Mollweide-style behavior. The implementation
/// matches the auxiliary-angle iteration for <c>θ + sin(θ) = π * sin(φ)</c> and
/// the resulting easting and northing equations.</para>
/// </remarks>
internal class BoggsProjection : MapProjection
{
    private const int Iterations = 20;
    private const double Epsilon = 1e-7d;
    private const double Fxc = 2.00276d;
    private const double Fxc2 = 1.11072d;
    private const double Fyc = 0.49931d;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoggsProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public BoggsProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoggsProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public BoggsProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Boggs";
    }

    /// <inheritdoc />
    protected override bool HasInverseSupport => false;

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return this.GetOrCreateInverse(() => new BoggsProjection(this.Parameters.ToProjectionParameter(), this));
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double theta = lat;
        double x = 0d;
        if (Math.Abs(Math.Abs(lat) - HalfPi) >= Epsilon)
        {
            double c = Math.Sin(theta) * PI;
            for (int i = Iterations; i > 0; i--)
            {
                double th1 = (theta + Math.Sin(theta) - c) / (1d + Math.Cos(theta));
                theta -= th1;
                if (Math.Abs(th1) < Epsilon)
                {
                    break;
                }
            }

            theta *= 0.5d;
            double denominator = (1d / Math.Cos(lat)) + (Fxc2 / Math.Cos(theta));
            if (Math.Abs(denominator) <= Eps10)
            {
                ArgumentGuard.ThrowArgument("Input data outside projection domain.");
            }

            x = Fxc * lambda / denominator;
        }

        double y = Fyc * (lat + (Math.Sqrt(2d) * Math.Sin(theta)));
        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Boggs does not support inverse projection in this wave.");
    }
}
