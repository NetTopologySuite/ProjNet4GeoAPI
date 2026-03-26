// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Putnins P6 projection (<c>putp6</c>).
/// </summary>
[Serializable]
internal class PutninsP6Projection : MapProjection
{
    private const double DefaultCx = 1.01346d;
    private const double DefaultCy = 0.91910d;
    private const double DefaultA = 4d;
    private const double DefaultB = 2.1471437182129378784d;
    private const double DefaultD = 2d;
    private const double Epsilon = 1e-10d;
    private const int Iterations = 10;
    private const double PoleValue = 1.732050807568877d;

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double cx;
    private readonly double cy;
    private readonly double a;
    private readonly double b;
    private readonly double d;

    /// <summary>
    /// Initializes a new instance of the <see cref="PutninsP6Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public PutninsP6Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PutninsP6Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public PutninsP6Projection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Putnins_P6";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
        this.cx = this.Parameters.GetOptionalParameterValue("putp6_cx", DefaultCx);
        this.cy = this.Parameters.GetOptionalParameterValue("putp6_cy", DefaultCy);
        this.a = this.Parameters.GetOptionalParameterValue("putp6_a", DefaultA);
        this.b = this.Parameters.GetOptionalParameterValue("putp6_b", DefaultB);
        this.d = this.Parameters.GetOptionalParameterValue("putp6_d", DefaultD);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new PutninsP6Projection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double p = this.b * Math.Sin(lat);
        double phi = 1.10265779d * lat;
        int i = Iterations;

        for (; i > 0; i--)
        {
            double r = Math.Sqrt(1d + (phi * phi));
            double denominator = this.a - (2d * r);
            if (Math.Abs(denominator) <= Eps10)
            {
                break;
            }

            double v = (((this.a - r) * phi) - Math.Log(phi + r) - p) / denominator;
            phi -= v;
            if (Math.Abs(v) < Epsilon)
            {
                break;
            }
        }

        double sqrtOnePlusPhiSquared;
        if (i == 0)
        {
            phi = p < 0d ? -PoleValue : PoleValue;
            sqrtOnePlusPhiSquared = 2d;
        }
        else
        {
            sqrtOnePlusPhiSquared = Math.Sqrt(1d + (phi * phi));
        }

        double x = this.cx * lambda * (this.d - sqrtOnePlusPhiSquared);
        double y = this.cy * phi;

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;
        double phi = yy / this.cy;
        double r = Math.Sqrt(1d + (phi * phi));
        double denominator = this.cx * (this.d - r);
        if (Math.Abs(denominator) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lambda = xx / denominator;
        double sinPhi = (((this.a - r) * phi) - Math.Log(phi + r)) / this.b;
        phi = Asinz(sinPhi);

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
