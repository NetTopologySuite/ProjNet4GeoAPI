// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Fahey projection (<c>fahey</c>).
/// </summary>
/// <remarks>
/// Fahey is a spherical pseudocylindrical compromise projection commonly associated with
/// Fahey's modern atlas usage. The implementation uses the half-angle substitution
/// <c>t = tan(phi / 2)</c>, followed by the compact relations
/// <c>x = XFactor * lambda * sqrt(1 - t^2)</c> and <c>y = YFactor * t</c>.
/// </remarks>
internal class FaheyProjection : MapProjection
{
    private const double Tolerance = 1e-6d;
    private const double XFactor = 0.819152d;
    private const double YFactor = 1.819152d;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="FaheyProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public FaheyProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FaheyProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public FaheyProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Fahey";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new FaheyProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double t = Math.Tan(0.5d * lat);
        double y = YFactor * t;
        double x = XFactor * lambda * Math.Sqrt(Math.Max(0d, 1d - (t * t)));

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;
        double t = yy / YFactor;
        double phi = 2d * Math.Atan(t);
        double oneMinusTSquared = 1d - (t * t);
        double lambda = Math.Abs(oneMinusTSquared) < Tolerance
            ? 0d
            : xx / (XFactor * Math.Sqrt(oneMinusTSquared));

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
