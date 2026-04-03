// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Rectangular Polyconic projection (<c>rpoly</c>).
/// </summary>
/// <remarks>
/// Inverse projection is not supported in this implementation.
/// <para>The forward formulation was independently verified against the historical War
/// Department rectangular polyconic construction. The implementation matches the
/// true-scale-latitude branch and the simpler equatorial branch used when <c>lat_ts</c> is
/// not provided.</para>
/// </remarks>
internal sealed class RectangularPolyconicProjection : MapProjection
{
    private readonly double radius;
    private readonly double modeFxa;
    private readonly double modeFxb;
    private readonly bool mode;

    /// <inheritdoc />
    protected override bool HasInverseSupport => false;

    /// <summary>
    /// Initializes a new instance of the <see cref="RectangularPolyconicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public RectangularPolyconicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RectangularPolyconicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public RectangularPolyconicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Rectangular_Polyconic";
        this.radius = this.semiMajor * this.scaleFactor;

        double phi1 = Math.Abs(DegreesToRadians(this.Parameters.GetOptionalParameterValue("lat_ts", 0d)));
        this.mode = phi1 > Eps10;
        if (this.mode)
        {
            this.modeFxb = 0.5d * Math.Sin(phi1);
            this.modeFxa = 0.5d / this.modeFxb;
        }
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return this.GetOrCreateInverse(() => new RectangularPolyconicProjection(this.Parameters.ToProjectionParameter(), this));
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;
        double fa = this.mode ? Math.Tan(lambda * this.modeFxb) * this.modeFxa : 0.5d * lambda;
        double xUnit;
        double yUnit;

        if (Math.Abs(phi) < 1e-9d)
        {
            xUnit = fa + fa;
            yUnit = -this.latOrigin;
        }
        else
        {
            yUnit = 1d / Math.Tan(phi);
            fa = 2d * Math.Atan(fa * Math.Sin(phi));
            xUnit = Math.Sin(fa) * yUnit;
            yUnit = (phi - this.latOrigin) + ((1d - Math.Cos(fa)) * yUnit);
        }

        lon = this.radius * xUnit;
        lat = this.radius * yUnit;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Rectangular Polyconic does not support inverse projection in this wave.");
    }
}
