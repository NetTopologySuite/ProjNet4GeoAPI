// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical van der Grinten II projection (<c>vandg2</c>).
/// </summary>
/// <remarks>
/// Inverse projection is not supported in this implementation.
/// <para>The forward formulation was independently verified against the van der Grinten II
/// construction. The implementation matches the reduced-latitude term <c>bt</c>, the
/// auxiliary quantity <c>at</c>, and the resulting circular-arc placement.</para>
/// </remarks>
internal class VanDerGrinten2Projection : MapProjection
{
    private const double Tolerance = 1e-10d;

    private readonly double radius;

    /// <summary>
    /// Initializes a new instance of the <see cref="VanDerGrinten2Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public VanDerGrinten2Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VanDerGrinten2Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public VanDerGrinten2Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Van_der_Grinten_II";
        this.radius = this.semiMajor * this.scaleFactor;
    }

    /// <inheritdoc />
    protected override bool HasInverseSupport => false;

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return this.GetOrCreateInverse(() => new VanDerGrinten2Projection(this.Parameters.ToProjectionParameter(), this));
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double bt = Math.Abs((2d / PI) * lat);
        double ct = 1d - (bt * bt);
        if (ct < 0d)
        {
            ct = 0d;
        }
        else
        {
            ct = Math.Sqrt(ct);
        }

        double x;
        double y;
        if (Math.Abs(lambda) < Tolerance)
        {
            x = 0d;
            y = PI * (lat < 0d ? -bt : bt) / (1d + ct);
        }
        else
        {
            double at = 0.5d * Math.Abs((PI / lambda) - (lambda / PI));
            double x1 = ((ct * Math.Sqrt(1d + (at * at))) - (at * ct * ct)) / (1d + ((at * at) * (bt * bt)));
            x = PI * x1;
            y = PI * Math.Sqrt(Math.Max(0d, 1d - (x1 * (x1 + (2d * at))) + Tolerance));

            if (lambda < 0d)
            {
                x = -x;
            }

            if (lat < 0d)
            {
                y = -y;
            }
        }

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("van der Grinten II does not support inverse projection in this wave.");
    }
}
