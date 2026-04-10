// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Tobler-Mercator projection (<c>tobmerc</c>).
/// </summary>
/// <remarks>
/// Tobler-Mercator is a modified spherical Mercator projection proposed by Waldo Tobler to
/// temper high-latitude east-west exaggeration. The implementation keeps the Mercator
/// northing <c>ln(tan(π / 4 + φ / 2))</c> but scales longitude by <c>cos(φ)²</c>.
/// </remarks>
internal class ToblerMercatorProjection : MapProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToblerMercatorProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public ToblerMercatorProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToblerMercatorProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public ToblerMercatorProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Tobler_Mercator";
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new ToblerMercatorProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        if (Math.Abs(lat) >= HalfPi)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lambda = Adjust_lon(lon - this.centralMeridian);
        double cosPhi = Math.Cos(lat);
        lon = this.SphericalRadius * lambda * cosPhi * cosPhi;
        lat = this.SphericalRadius * Math.Log(Math.Tan(FortPi + (0.5d * lat)));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        y = Math.Atan(Math.Sinh(y * this.InverseSphericalRadius));
        double cosPhi = Math.Cos(y);
        x = Adjust_lon(this.centralMeridian + ((x * this.InverseSphericalRadius) / (cosPhi * cosPhi)));
    }
}
