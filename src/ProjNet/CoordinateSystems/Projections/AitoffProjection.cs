// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Aitoff projection (<c>aitoff</c>).
/// </summary>
/// <remarks>
/// Aitoff is a spherical compromise projection obtained by applying the azimuthal
/// equidistant construction to halved longitudes and then doubling the horizontal result.
/// The formulation was independently verified against the Wikipedia article
/// "Aitoff projection". The auxiliary angle
/// <c>d = acos(cos(φ) * cos(λ / 2))</c> together with the normalized forward
/// relations used by <see cref="AitoffMath"/> matches the implementation here.
/// </remarks>
/// <seealso href="https://en.wikipedia.org/wiki/Aitoff_projection">Wikipedia: Aitoff projection.</seealso>
internal sealed class AitoffProjection : MapProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AitoffProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public AitoffProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AitoffProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public AitoffProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Aitoff";
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new AitoffProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        AitoffMath.Forward(lambda, lat, false, 0d, out double x, out double y);
        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;

        AitoffMath.Inverse(xx, yy, false, 0d, out double lambda, out double phi);
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
