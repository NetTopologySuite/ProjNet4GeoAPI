// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical August Epicycloidal projection (<c>august</c>).
/// </summary>
/// <remarks>
/// <para>The August Epicycloidal projection is a spherical conformal world-map
/// construction that transforms the reduced longitude and latitude through an
/// epicycloidal polynomial form. This implementation follows PROJ's
/// <c>august</c> formulation and uses the standard 4/3 scale factor in the
/// forward equations.</para>
/// <para>Snyder catalogs it as F. W. O. August's conformal epicycloidal world
/// projection, whose 180-degree meridians form a two-cusped epicycloid while the
/// equator and central meridian remain straight.</para>
/// <para>This projection remains forward-only in PROJ and in this implementation, so
/// inverse projection is not supported.</para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/projections/august.html">PROJ documentation: August Epicycloidal.</seealso>
internal class AugustProjection : MapProjection
{
    private const double M = 1.333333333333333d;

    private readonly double radius;

    /// <summary>
    /// Initializes a new instance of the <see cref="AugustProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public AugustProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AugustProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public AugustProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "August_Epicycloidal";
        this.radius = this.semiMajor * this.scaleFactor;
    }

    /// <inheritdoc />
    protected override bool HasInverseSupport => false;

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return this.GetOrCreateInverse(() => new AugustProjection(this.Parameters.ToProjectionParameter(), this));
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double t = Math.Tan(0.5d * lat);
        double c1 = Math.Sqrt(1d - (t * t));
        lambda *= 0.5d;
        double c = 1d + (c1 * Math.Cos(lambda));
        if (Math.Abs(c) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double x1 = Math.Sin(lambda) * c1 / c;
        double y1 = t / c;
        double x12 = x1 * x1;
        double y12 = y1 * y1;

        lon = this.radius * (M * x1 * (3d + x12 - (3d * y12)));
        lat = this.radius * (M * y1 * (3d + (3d * x12) - y12));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("August Epicycloidal does not support inverse projection in this wave.");
    }
}
