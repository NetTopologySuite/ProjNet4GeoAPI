// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Urmaev Flat-Polar Sinusoidal projection (<c>urmfps</c>).
/// </summary>
/// <remarks>
/// Urmaev Flat-Polar Sinusoidal is a parameterized spherical pseudocylindrical projection
/// associated with Urmaev. The implementation uses the defining relation
/// <c>φ' = asin(n * sin(φ))</c> and then applies the flat-polar sinusoidal scaling
/// constants, making it the verified base for delegated variants such as Wagner I.
/// </remarks>
internal class UrmaevFlatPolarSinusoidalProjection : MapProjection
{
    private const double Cx = 0.8773826753d;
    private const double Cy = 1.139753528477d;

    private readonly double n;
    private readonly double cY;

    /// <summary>
    /// Initializes a new instance of the <see cref="UrmaevFlatPolarSinusoidalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public UrmaevFlatPolarSinusoidalProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UrmaevFlatPolarSinusoidalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public UrmaevFlatPolarSinusoidalProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Urmaev_Flat_Polar_Sinusoidal";
        this.n = this.Parameters.GetParameterValue("n");
        if (this.n <= 0d || this.n > 1d)
        {
            ArgumentGuard.ThrowArgument("Invalid value for n: it should be in ]0,1] range.");
        }

        this.cY = Cy / this.n;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new UrmaevFlatPolarSinusoidalProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = Asinz(this.n * Math.Sin(lat));
        lon = this.SphericalRadius * Cx * lambda * Math.Cos(phi);
        lat = this.SphericalRadius * this.cY * phi;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;
        double phiNormalized = yy / this.cY;
        double phi = Asinz(Math.Sin(phiNormalized) / this.n);
        double denominator = Cx * Math.Cos(phiNormalized);
        if (Math.Abs(denominator) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lambda = xx / denominator;
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
