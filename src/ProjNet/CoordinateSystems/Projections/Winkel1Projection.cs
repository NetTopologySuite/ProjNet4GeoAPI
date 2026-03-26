// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Winkel I projection (<c>wink1</c>).
/// </summary>
[Serializable]
internal class Winkel1Projection : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double cosphi1;

    /// <summary>
    /// Initializes a new instance of the <see cref="Winkel1Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Winkel1Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Winkel1Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Winkel1Projection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Winkel_I";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        double latTs = DegreesToRadians(this.Parameters.GetOptionalParameterValue("lat_ts", RadiansToDegrees(this.latOrigin), "latitude_true_scale"));
        this.cosphi1 = Math.Cos(latTs);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new Winkel1Projection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double x = 0.5d * lambda * (this.cosphi1 + Math.Cos(lat));
        double y = lat;

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;
        double denominator = this.cosphi1 + Math.Cos(yy);
        if (Math.Abs(denominator) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lambda = 2d * xx / denominator;
        x = Adjust_lon(this.centralMeridian + lambda);
        y = yy;
    }
}
