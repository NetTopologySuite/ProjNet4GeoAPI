// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Winkel Tripel projection (<c>wintri</c>).
/// </summary>
[Serializable]
internal class WinkelTripelProjection : MapProjection
{
    /// <summary>
    /// Default cosine of the standard parallel (approximately 50°28').
    /// </summary>
    private const double DefaultCosphi1 = 0.636619772367581343d;

    private readonly double radius;
    private readonly double inverseRadius;
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
    public WinkelTripelProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Winkel_Tripel";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

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
        if (this.inverse is null)
        {
            this.inverse = new WinkelTripelProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        AitoffMath.Forward(lambda, lat, true, this.cosphi1, out double x, out double y);
        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;

        AitoffMath.Inverse(xx, yy, true, this.cosphi1, out double lambda, out double phi);
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
