// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Hammer projection (<c>hammer</c>).
/// </summary>
/// <remarks>
/// Hammer is an equal-area pseudocylindrical projection derived from the Lambert azimuthal
/// equal-area construction and parameterized here through the optional <c>w</c> and <c>m</c>
/// scale factors. The formulation was independently verified against the Wikipedia article
/// "Hammer projection" and Eric W. Weisstein's MathWorld entry
/// "Hammer-Aitoff Equal-Area Projection". The normalized factor
/// <c>sqrt(2 / (1 + cos(phi) * cos(w * lambda)))</c> and the resulting scaled forward
/// equations match the implementation here.
/// </remarks>
internal class HammerProjection : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double w;
    private readonly double m;
    private readonly double inverseM;

    /// <summary>
    /// Initializes a new instance of the <see cref="HammerProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public HammerProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HammerProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public HammerProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Hammer";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        this.w = Math.Abs(this.Parameters.GetOptionalParameterValue("W", 0.5d, "w"));
        if (this.w <= 0d)
        {
            ArgumentGuard.ThrowArgument("Invalid value for W: it should be > 0.");
        }

        this.m = Math.Abs(this.Parameters.GetOptionalParameterValue("M", 1d, "m"));
        if (this.m <= 0d)
        {
            ArgumentGuard.ThrowArgument("Invalid value for M: it should be > 0.");
        }

        this.inverseM = 1d / this.m;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new HammerProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = this.w * Adjust_lon(lon - this.centralMeridian);
        double cosPhi = Math.Cos(lat);
        double denominator = 1d + (cosPhi * Math.Cos(lambda));
        if (denominator == 0d)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double d = Math.Sqrt(2d / denominator);
        lon = this.radius * ((this.m / this.w) * d * cosPhi * Math.Sin(lambda));
        lat = this.radius * (this.inverseM * d * Math.Sin(lat));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = y * this.inverseRadius;

        double z = 1d - (0.25d * this.w * this.w * xUnit * xUnit) - (0.25d * yUnit * yUnit);
        if (z < 0d)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        z = Math.Sqrt(z);
        if (Math.Abs((2d * z * z) - 1d) < Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lambda = Math.Atan2(this.w * xUnit * z, (2d * z * z) - 1d) / this.w;
        double phi = Math.Asin(ProjectionConstants.Clamp(z * yUnit, -1d, 1d));

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
