// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Compact Miller projection (<c>comill</c>).
/// </summary>
[Serializable]
internal class CompactMillerProjection : MapProjection
{
    private const double K1 = 0.9902d;
    private const double K2 = 0.1604d;
    private const double K3 = -0.03054d;
    private const double C1 = K1;
    private const double C2 = 3d * K2;
    private const double C3 = 5d * K3;
    private const double Epsilon = 1e-11d;
    private const double MaxYFactor = 0.6000207669862655d;
    private const int MaxIterations = 100;

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double maxY;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompactMillerProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public CompactMillerProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompactMillerProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public CompactMillerProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Compact_Miller";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
        this.maxY = MaxYFactor * PI;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new CompactMillerProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double latSquared = lat * lat;
        double y = lat * (K1 + (latSquared * (K2 + (K3 * latSquared))));
        lon = this.radius * lambda;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;
        if (yy > this.maxY)
        {
            yy = this.maxY;
        }
        else if (yy < -this.maxY)
        {
            yy = -this.maxY;
        }

        double yc = yy;
        bool converged = false;
        for (int i = MaxIterations; i > 0; i--)
        {
            double y2 = yc * yc;
            double f = (yc * (K1 + (y2 * (K2 + (K3 * y2))))) - yy;
            double fDerivative = C1 + (y2 * (C2 + (C3 * y2)));
            double tolerance = f / fDerivative;
            yc -= tolerance;
            if (Math.Abs(tolerance) < Epsilon)
            {
                converged = true;
                break;
            }
        }

        if (!converged)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        x = Adjust_lon(this.centralMeridian + xx);
        y = yc;
    }
}
