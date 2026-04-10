// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Wagner III projection (<c>wag3</c>).
/// </summary>
/// <remarks>
/// Wagner III is one of Karl Wagner's spherical pseudocylindrical projections from the
/// 1930s. It derives its longitude scale from the true-scale latitude parameter through
/// <c>cx = cos(ts) / cos(2 * ts / 3)</c> and then applies the family form
/// <c>x = cx * λ * cos(2 * φ / 3)</c>.
/// </remarks>
internal sealed class Wagner3Projection : MapProjection
{
    private const double TwoThird = 0.6666666666666666666667d;

    private readonly double cx;

    /// <summary>
    /// Initializes a new instance of the <see cref="Wagner3Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Wagner3Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Wagner3Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Wagner3Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Wagner_III";

        double latTsDeg = this.Parameters.GetOptionalParameterValue("lat_ts", 0d, "latitude_true_scale");
        double ts = DegreesToRadians(latTsDeg);
        double denominator = Math.Cos((2d * ts) / 3d);
        if (Math.Abs(denominator) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        this.cx = Math.Cos(ts) / denominator;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new Wagner3Projection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double x = this.cx * lambda * Math.Cos(TwoThird * lat);
        double y = lat;

        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;

        double phi = yy;
        double denominator = this.cx * Math.Cos(TwoThird * phi);
        if (Math.Abs(denominator) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lambda = xx / denominator;
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
