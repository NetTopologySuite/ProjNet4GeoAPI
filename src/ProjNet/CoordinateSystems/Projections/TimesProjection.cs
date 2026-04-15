// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Times projection (<c>times</c>).
/// </summary>
/// <remarks>
/// <para>The Times projection is a spherical compromise projection popularized by
/// <i>The Times Atlas</i>. It combines the substitution <c>t = tan(φ / 2)</c> with a
/// polynomial longitude scale <c>x = λ * (X0 - X1 * sin(π / 4 * t)²)</c> and the
/// simple latitude relation <c>y = Y0 * t</c>.</para>
/// <para>The polynomial formulation was independently checked against PROJ's
/// <c>times</c> documentation, which cites Snyder's <i>Flattening the Earth</i>
/// (1993, pp. 213-214). The implementation uses the published fixed coefficients
/// <c>X0</c>, <c>X1</c>, and <c>Y0</c> for the standard Times Atlas variant.</para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/projections/times.html">PROJ documentation: Times projection.</seealso>
internal sealed class TimesProjection : MapProjection
{
    private const double X0 = 0.74482d;
    private const double X1 = 0.34588d;
    private const double Y0 = 1.70711d;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimesProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public TimesProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimesProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public TimesProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Times";
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new TimesProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double t = Math.Tan(lat / 2d);
        double s = Math.Sin(FortPi * t);
        double s2 = s * s;
        double x = lambda * (X0 - (X1 * s2));
        double y = Y0 * t;

        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;

        double t = yy / Y0;
        double s = Math.Sin(FortPi * t);
        double s2 = s * s;
        double denominator = X0 - (X1 * s2);
        if (Math.Abs(denominator) <= Eps10)
        {
            throw new System.InvalidOperationException("Input data outside projection domain.");
        }

        double lambda = xx / denominator;
        double phi = 2d * Math.Atan(t);
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
