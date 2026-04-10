// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Wagner II projection (<c>wag2</c>).
/// </summary>
/// <remarks>
/// Wagner II is one of Karl Wagner's spherical pseudocylindrical projections from the
/// 1930s. The implementation uses Wagner's characteristic double-latitude sine transform
/// <c>asin(Cp1 * sin(Cp2 * φ))</c> before applying the final x/y scaling constants.
/// </remarks>
internal sealed class Wagner2Projection : MapProjection
{
    private const double Cx = 0.92483d;
    private const double Cy = 1.38725d;
    private const double Cp1 = 0.88022d;
    private const double Cp2 = 0.88550d;

    /// <summary>
    /// Initializes a new instance of the <see cref="Wagner2Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Wagner2Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Wagner2Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Wagner2Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Wagner_II";
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new Wagner2Projection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = Asinz(Cp1 * Math.Sin(Cp2 * lat));
        double x = Cx * lambda * Math.Cos(phi);
        double y = Cy * phi;

        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;

        double phi = yy / Cy;
        double denominator = Cx * Math.Cos(phi);
        if (Math.Abs(denominator) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lambda = xx / denominator;
        phi = Asinz(Math.Sin(phi) / Cp1) / Cp2;

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
