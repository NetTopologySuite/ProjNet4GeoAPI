// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Larrivee projection (<c>larr</c>).
/// </summary>
/// <remarks>
/// Inverse projection is not supported.
/// </remarks>
[Serializable]
internal class LarriveeProjection : MapProjection
{
    private const double Sixth = 0.16666666666666666d;

    private readonly double radius;

    /// <summary>
    /// Initializes a new instance of the <see cref="LarriveeProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public LarriveeProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LarriveeProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public LarriveeProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Larrivee";
        this.radius = this.semiMajor * this.scaleFactor;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new LarriveeProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double denominator = Math.Cos(0.5d * lat) * Math.Cos(Sixth * lambda);
        if (Math.Abs(denominator) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double x = 0.5d * lambda * (1d + Math.Sqrt(Math.Cos(lat)));
        double y = lat / denominator;
        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Larrivee does not support inverse projection in this wave.");
    }
}

