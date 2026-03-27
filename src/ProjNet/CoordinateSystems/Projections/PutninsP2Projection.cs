// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Putnins P2 projection (<c>putp2</c>).
/// </summary>
[Serializable]
internal class PutninsP2Projection : MapProjection
{
    private const double Cx = 1.89490d;
    private const double Cy = 1.71848d;
    private const double Cp = 0.6141848493043784d;
    private const double Epsilon = 1e-10d;
    private const int Iterations = 10;
    private const double PiDiv3 = 1.0471975511965977d;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="PutninsP2Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public PutninsP2Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PutninsP2Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public PutninsP2Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Putnins_P2";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new PutninsP2Projection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double p = Cp * Math.Sin(lat);
        double latSquared = lat * lat;
        double phi = lat * (0.615709d + (latSquared * (0.00909953d + (latSquared * 0.0046292d))));
        int i = Iterations;

        for (; i > 0; i--)
        {
            double c = Math.Cos(phi);
            double s = Math.Sin(phi);
            double denominator = 1d + (c * (c - 1d)) - (s * s);
            if (Math.Abs(denominator) <= Eps10)
            {
                break;
            }

            double v = (phi + (s * (c - 1d)) - p) / denominator;
            phi -= v;
            if (Math.Abs(v) < Epsilon)
            {
                break;
            }
        }

        if (i == 0)
        {
            phi = phi < 0d ? -PiDiv3 : PiDiv3;
        }

        double x = Cx * lambda * (Math.Cos(phi) - 0.5d);
        double y = Cy * Math.Sin(phi);

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;

        double phi = Asinz(yy / Cy);
        double cosPhi = Math.Cos(phi);
        double denominator = Cx * (cosPhi - 0.5d);
        if (Math.Abs(denominator) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lambda = xx / denominator;
        phi = Asinz((phi + (Math.Sin(phi) * (cosPhi - 1d))) / Cp);

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
