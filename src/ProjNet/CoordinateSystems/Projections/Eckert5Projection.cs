// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Eckert V projection (<c>eck5</c>).
/// </summary>
/// <remarks>
/// Eckert V is a spherical pseudocylindrical projection with sinusoidal meridians and
/// evenly spaced straight parallels. The formulation was independently verified against
/// John P. Snyder, <i>Map Projections - A Working Manual</i>
/// (USGS Professional Paper 1395, 1987) and Max Eckert's 1906 description of the family.
/// The forward equations <c>x = Xf * (1 + cos(φ)) * λ</c> and <c>y = Yf * φ</c>
/// match the implementation here.
/// </remarks>
/// <seealso href="https://en.wikipedia.org/wiki/Eckert_projection">Wikipedia: Eckert projection family.</seealso>
internal sealed class Eckert5Projection : MapProjection
{
    private const double Xf = 0.44101277172455148219d;
    private const double Rxf = 2.26750802723822639137d;
    private const double Yf = 0.88202554344910296438d;
    private const double Ryf = 1.13375401361911319568d;

    /// <summary>
    /// Initializes a new instance of the <see cref="Eckert5Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Eckert5Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Eckert5Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Eckert5Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Eckert_V";
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new Eckert5Projection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double x = Xf * (1d + Math.Cos(lat)) * lambda;
        double y = Yf * lat;
        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;
        double phi = Ryf * yy;
        double denominator = 1d + Math.Cos(phi);
        if (Math.Abs(denominator) <= Eps10)
        {
            throw new System.InvalidOperationException("Input data outside projection domain.");
        }

        double lambda = Rxf * xx / denominator;
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
