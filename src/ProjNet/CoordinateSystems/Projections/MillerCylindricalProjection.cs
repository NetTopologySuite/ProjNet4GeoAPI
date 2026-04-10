// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Miller Cylindrical projection (<c>mill</c>).
/// </summary>
/// <remarks>
/// A compromise cylindrical projection that reduces the high-latitude area exaggeration
/// of the Mercator projection by compressing the latitude formula. Poles cannot be projected.
/// <para>The formulation was independently verified against the Wikipedia article
/// "Miller cylindrical projection". The forward northing
/// <c>1.25 * ln(tan(π / 4 + 0.4 * φ))</c> and its inverse recovery
/// <c>2.5 * (atan(exp(0.8 * y)) - π / 4)</c> match the implementation here.</para>
/// <para>See also John P. Snyder, "Map Projections - A Working Manual",
/// U.S. Geological Survey Professional Paper 1395, 1987, Ch. 11, pp. 86-89,
/// eqs. (11-1) through (11-4), for the Miller cylindrical derivation.</para>
/// </remarks>
/// <seealso href="https://en.wikipedia.org/wiki/Miller_cylindrical_projection">Wikipedia: Miller cylindrical projection.</seealso>
/// <seealso>Bugayevskiy &amp; Snyder (1995), "Map Projections: A Reference Manual", Ch. 6, Sect. 6.3.8, pp. 182-184.</seealso>
internal class MillerCylindricalProjection : MapProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MillerCylindricalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public MillerCylindricalProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MillerCylindricalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public MillerCylindricalProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Miller_Cylindrical";
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new MillerCylindricalProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        if (double.IsNaN(lon) || double.IsNaN(lat))
        {
            lon = double.NaN;
            lat = double.NaN;
            return;
        }

        if (Math.Abs(Math.Abs(lat) - HalfPi) <= Epsln)
        {
            ArgumentGuard.ThrowArgument("Transformation cannot be computed at the poles.");
        }

        double lambda = Adjust_lon(lon - this.centralMeridian);
        lon = this.SphericalRadius * lambda;
        lat = this.SphericalRadius * 1.25d * Math.Log(Math.Tan(FortPi + (0.4d * lat)));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        x = Adjust_lon(this.centralMeridian + (x * this.InverseSphericalRadius));
        y = 2.5d * (Math.Atan(Math.Exp((0.8d * y) * this.InverseSphericalRadius)) - FortPi);
    }
}
