// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

    /// <summary>
/// Implements the Bertin 1953 projection (<c>bertin1953</c>).
/// </summary>
/// <remarks>
/// <para>The Bertin 1953 projection is a spherical world map projection derived from the
/// historical design introduced by Jacques Bertin in 1953. This implementation follows
/// the fixed-parameter computational formulation documented by PROJ and Philippe Riviere
/// (2017), using the published constants <c>Fu = 1.4</c>, <c>K = 12</c>, <c>W = 1.68</c>,
/// a latitude rotation of -42 degrees, and a longitude offset of -16.5 degrees before
/// the final warping step.</para>
/// <para>Inverse projection is not supported in this implementation.</para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/projections/bertin1953.html">PROJ documentation: Bertin 1953.</seealso>
/// <seealso href="https://visionscarto.net/bertin-projection-1953">Philippe Riviere (2017): Bertin Projection (1953).</seealso>
internal sealed class Bertin1953Projection : MapProjection
{
    private const double Fu = 1.4d;
    private const double K = 12d;
    private const double W = 1.68d;
    private const double DeltaPhi = -42d * PI / 180d;
    private const double DeltaGamma = 0d;
    private const double LambdaOffset = -16.5d * PI / 180d;

    private readonly double cosDeltaPhi;
    private readonly double sinDeltaPhi;
    private readonly double cosDeltaGamma;
    private readonly double sinDeltaGamma;

    /// <summary>
    /// Initializes a new instance of the <see cref="Bertin1953Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Bertin1953Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Bertin1953Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Bertin1953Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Bertin_1953";
        this.cosDeltaPhi = Math.Cos(DeltaPhi);
        this.sinDeltaPhi = Math.Sin(DeltaPhi);
        this.cosDeltaGamma = Math.Cos(DeltaGamma);
        this.sinDeltaGamma = Math.Sin(DeltaGamma);
    }

    /// <inheritdoc />
    protected override bool HasInverseSupport => false;

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return this.GetOrCreateInverse(() => new Bertin1953Projection(this.Parameters.ToProjectionParameter(), this));
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = lon + LambdaOffset;
        double phi = lat;

        double cosPhi = Math.Cos(phi);
        double x = Math.Cos(lambda) * cosPhi;
        double y = Math.Sin(lambda) * cosPhi;
        double z = Math.Sin(phi);

        double z0 = (z * this.cosDeltaPhi) + (x * this.sinDeltaPhi);
        lambda = Math.Atan2(
            (y * this.cosDeltaGamma) - (z0 * this.sinDeltaGamma),
            (x * this.cosDeltaPhi) - (z * this.sinDeltaPhi));
        z0 = (z0 * this.cosDeltaGamma) + (y * this.sinDeltaGamma);
        phi = Asinz(z0);
        lambda = Adjust_lon(lambda);

        if ((lambda + phi) < -Fu)
        {
            double d = (lambda - phi + 1.6d) * (lambda + phi + Fu) / 8d;
            lambda += d;
            phi -= 0.8d * d * Math.Sin(phi + (PI * 0.5d));
        }

        cosPhi = Math.Cos(phi);
        double denom = 1d + (cosPhi * Math.Cos(lambda * 0.5d));
        if (Math.Abs(denom) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double dd = Math.Sqrt(2d / denom);
        double xOut = W * dd * cosPhi * Math.Sin(lambda * 0.5d);
        double yOut = dd * Math.Sin(phi);

        double post = (1d - Math.Cos(lambda * phi)) / K;
        if (yOut < 0d)
        {
            xOut *= 1d + post;
        }

        if (yOut > 0d)
        {
            yOut *= 1d + ((post / 1.5d) * xOut * xOut);
        }

        lon = this.SphericalRadius * xOut;
        lat = this.SphericalRadius * yOut;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Bertin 1953 does not support inverse projection in this wave.");
    }
}
