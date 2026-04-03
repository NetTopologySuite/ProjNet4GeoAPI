// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Wagner VII projection (<c>wag7</c>).
/// </summary>
/// <remarks>
/// Wagner VII is an equal-area polyconic projection with curved meridians and parallels.
/// Inverse projection is not supported in this implementation.
/// <para>The forward formulation was independently verified against the standard Wagner VII
/// construction. The implementation matches the auxiliary latitude
/// <c>theta = asin(0.9063077870 * sin(phi))</c>, the one-third longitude step, and the final
/// Hammer-like normalization factor.</para>
/// </remarks>
internal class Wagner7Projection : MapProjection
{
    private const double YPreFactor = 0.90630778703664996d;
    private const double XFactor = 2.66723d;
    private const double YFactor = 1.24104d;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="Wagner7Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Wagner7Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Wagner7Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Wagner7Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Wagner_VII";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new Wagner7Projection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double yTemp = YPreFactor * Math.Sin(lat);
        double theta = Asinz(yTemp);
        double cosTheta = Math.Cos(theta);
        double lambdaThird = lambda / 3d;

        double x = XFactor * cosTheta * Math.Sin(lambdaThird);
        double denominator = Math.Sqrt(0.5d * (1d + (cosTheta * Math.Cos(lambdaThird))));
        if (denominator <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double scale = 1d / denominator;
        x *= scale;
        double y = yTemp * YFactor * scale;

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Wagner VII does not support inverse projection in this wave.");
    }
}
