// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Patterson cylindrical projection (<c>patterson</c>).
/// </summary>
/// <remarks>
/// The Patterson projection is a cylindrical projection whose y-coordinates are computed via
/// a polynomial formula designed for a visually balanced appearance. The inverse is solved
/// iteratively via Newton–Raphson iteration.
/// </remarks>
internal class PattersonProjection : MapProjection
{
    private const double K1 = 1.0148d;
    private const double K2 = 0.23185d;
    private const double K3 = -0.14499d;
    private const double K4 = 0.02406d;
    private const int Iterations = 12;

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double maxY;

    /// <summary>
    /// Initializes a new instance of the <see cref="PattersonProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public PattersonProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PattersonProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public PattersonProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Patterson";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
        this.maxY = ForwardPolynomial(HalfPi);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new PattersonProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        lon = this.radius * lambda;
        lat = this.radius * ForwardPolynomial(lat);
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        x = Adjust_lon(this.centralMeridian + (x * this.inverseRadius));

        double targetY = ProjectionConstants.Clamp(y * this.inverseRadius, -this.maxY, this.maxY);
        double phi = targetY / K1;

        for (int i = 0; i < Iterations; i++)
        {
            double f = ForwardPolynomial(phi) - targetY;
            double df = ForwardPolynomialDerivative(phi);
            double delta = f / df;
            phi -= delta;
            if (Math.Abs(delta) <= 1e-12d)
            {
                break;
            }
        }

        y = ProjectionConstants.Clamp(phi, -HalfPi, HalfPi);
    }

    private static double ForwardPolynomial(double phi)
    {
        double phi2 = phi * phi;
        double phi4 = phi2 * phi2;
        double phi6 = phi4 * phi2;
        double phi8 = phi4 * phi4;

        return (K1 * phi) + (K2 * phi * phi4) + (K3 * phi * phi6) + (K4 * phi * phi8);
    }

    private static double ForwardPolynomialDerivative(double phi)
    {
        double phi2 = phi * phi;
        double phi4 = phi2 * phi2;
        double phi6 = phi4 * phi2;
        double phi8 = phi4 * phi4;

        return K1 + (5d * K2 * phi4) + (7d * K3 * phi6) + (9d * K4 * phi8);
    }
}
