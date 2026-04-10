// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Gall (Gall Stereographic) projection (<c>gall</c>).
/// </summary>
/// <remarks>
/// Gall Stereographic is a cylindrical compromise projection introduced by James Gall in
/// 1855. In normalized form it scales longitude by <c>cos(π / 4) / sqrt(2)</c> and uses the
/// latitude relation <c>y = (1 + sqrt(2)) * tan(φ / 2)</c>.
/// </remarks>
internal class GallProjection : MapProjection
{
    private const double Yf = 1.70710678118654752440d;
    private const double Xf = 0.70710678118654752440d;
    private const double Ryf = 0.58578643762690495119d;
    private const double Rxf = 1.41421356237309504880d;


    /// <summary>
    /// Initializes a new instance of the <see cref="GallProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public GallProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GallProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public GallProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Gall";
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new GallProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double x = Xf * lambda;
        double y = Yf * Math.Tan(0.5d * lat);
        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;
        double lambda = Rxf * xx;
        double phi = 2d * Math.Atan(yy * Ryf);
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
