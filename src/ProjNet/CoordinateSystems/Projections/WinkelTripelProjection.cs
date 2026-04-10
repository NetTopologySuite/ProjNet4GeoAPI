// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Winkel Tripel projection (<c>wintri</c>).
/// </summary>
/// <remarks>
/// Winkel Tripel blends the Aitoff projection with an equirectangular projection at a
/// standard parallel whose cosine defaults to <c>2 / π</c>. The formulation was
/// independently verified against the Wikipedia article "Winkel tripel projection".
/// The averaged forward relations <c>0.5 * (xAitoff + λ * cos(phi1))</c> and
/// <c>0.5 * (yAitoff + φ)</c> match the implementation here.
/// </remarks>
/// <seealso href="https://en.wikipedia.org/wiki/Winkel_tripel_projection">Wikipedia: Winkel tripel projection.</seealso>
internal sealed class WinkelTripelProjection : MapProjection
{
    /// <summary>
    /// Default cosine of the standard parallel (approximately 50°28').
    /// </summary>
    private const double DefaultCosphi1 = 0.636619772367581343d;

    private readonly double cosphi1;

    /// <summary>
    /// Initializes a new instance of the <see cref="WinkelTripelProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public WinkelTripelProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WinkelTripelProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public WinkelTripelProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Winkel_Tripel";

        bool hasLat1 = this.Parameters.ContainsKey("lat_1") || this.Parameters.ContainsKey("standard_parallel_1");
        if (hasLat1)
        {
            double lat1 = DegreesToRadians(this.Parameters.GetParameterValue("lat_1", "standard_parallel_1"));
            this.cosphi1 = Math.Cos(lat1);
            if (Math.Abs(this.cosphi1) <= Eps10)
            {
                ArgumentGuard.ThrowArgument("Invalid value for lat_1: |lat_1| should be < 90°.");
            }
        }
        else
        {
            this.cosphi1 = DefaultCosphi1;
        }
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new WinkelTripelProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        AitoffMath.Forward(lambda, lat, true, this.cosphi1, out double x, out double y);
        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;

        AitoffMath.Inverse(xx, yy, true, this.cosphi1, out double lambda, out double phi);
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
