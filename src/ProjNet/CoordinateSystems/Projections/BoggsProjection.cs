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
[Serializable]
internal class BoggsProjection : MapProjection
{
    private const int Iterations = 20;
    private const double Epsilon = 1e-7d;
    private const double Fxc = 2.00276d;
    private const double Fxc2 = 1.11072d;
    private const double Fyc = 0.49931d;

    private readonly double radius;

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
    public BoggsProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Boggs";
        this.radius = this.semiMajor * this.scaleFactor;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new BoggsProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double theta = lat;
        double x;
        if (Math.Abs(Math.Abs(lat) - HalfPi) < Epsilon)
        {
            x = 0d;
        }
        else
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
                throw new ArgumentException("Input data outside projection domain.");
            }

            x = Fxc * lambda / denominator;
        }

        double y = Fyc * (lat + (Math.Sqrt(2d) * Math.Sin(theta)));
        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Boggs does not support inverse projection in this wave.");
    }
}
