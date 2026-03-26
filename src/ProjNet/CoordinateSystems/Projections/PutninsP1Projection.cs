// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Putnins P1 projection (<c>putp1</c>).
/// </summary>
[Serializable]
internal class PutninsP1Projection : MapProjection
{
    private const double Cx = 1.89490d;
    private const double Cy = 0.94745d;
    private const double A = -0.5d;
    private const double B = 0.30396355092701331433d;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="PutninsP1Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public PutninsP1Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PutninsP1Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public PutninsP1Projection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Putnins_P1";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new PutninsP1Projection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double y = Cy * lat;
        double underRoot = 1d - (B * lat * lat);
        if (underRoot < 0d)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double x = Cx * lambda * (A + Math.Sqrt(underRoot));
        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;
        double phi = yy / Cy;

        double underRoot = 1d - (B * phi * phi);
        if (underRoot < 0d)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double denominator = Cx * (A + Math.Sqrt(underRoot));
        if (Math.Abs(denominator) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lambda = xx / denominator;
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
