// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Urmaev V projection (<c>urm5</c>, no inverse).
/// </summary>
/// <remarks>
/// <para>Urmaev V is a forward-only spherical projection associated with Urmaev. It
/// combines the parameterized <c>asin(n * sin(φ))</c> auxiliary latitude with an
/// additional cubic y-scaling term controlled by <c>q</c>. Inverse projection is not
/// supported in this implementation.</para>
/// <para>This implementation matches PROJ's <c>urm5</c> parameterization with explicit
/// <c>n</c>, <c>q</c>, and <c>α</c> constants. It belongs to the Urmaev family of
/// pseudocylindrical projections and retains the historical forward-only behavior of the
/// published Urmaev V form.</para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/projections/urm5.html">PROJ documentation: Urmaev V.</seealso>
internal sealed class Urmaev5Projection : MapProjection
{
    private readonly double n;
    private readonly double m;
    private readonly double rmn;
    private readonly double q3;

    /// <summary>
    /// Initializes a new instance of the <see cref="Urmaev5Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Urmaev5Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Urmaev5Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Urmaev5Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Urmaev_V";
        this.n = this.Parameters.GetParameterValue("n");
        if (this.n <= 0d || this.n > 1d)
        {
            ArgumentGuard.ThrowArgument("Invalid value for n: it should be in ]0,1] range.", nameof(parameters));
        }

        double q = this.Parameters.GetOptionalParameterValue("q", 0d);
        this.q3 = q / 3d;

        double alpha = DegreesToRadians(this.Parameters.GetOptionalParameterValue("alpha", 0d));
        double t = this.n * Math.Sin(alpha);
        double denom = Math.Sqrt(1d - (t * t));
        if (denom == 0d)
        {
            ArgumentGuard.ThrowArgument("Invalid value for n / alpha: n * sin(|alpha|) should be < 1.", nameof(parameters));
        }

        this.m = Math.Cos(alpha) / denom;
        this.rmn = 1d / (this.m * this.n);
    }

    /// <inheritdoc />
    protected override bool HasInverseSupport => false;

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return this.GetOrCreateInverse(() => new Urmaev5Projection(this.Parameters.ToProjectionParameter(), this));
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = Asinz(this.n * Math.Sin(lat));
        double t = phi * phi;
        lon = this.SphericalRadius * this.m * lambda * Math.Cos(phi);
        lat = this.SphericalRadius * phi * (1d + (t * this.q3)) * this.rmn;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Urmaev V does not support inverse projection in this wave.");
    }
}
