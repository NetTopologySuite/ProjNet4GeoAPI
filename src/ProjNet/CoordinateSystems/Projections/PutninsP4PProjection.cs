// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Putnins P4' projection (<c>putp4p</c>).
/// </summary>
/// <remarks>
/// Putnins P4' is the equal-area Putnins family member published by Reinholds Putnins in
/// 1934. The implementation uses the characteristic pre- and post-<c>asin</c> scaling with
/// a one-third angle step for the final y coordinate.
/// </remarks>
internal class PutninsP4PProjection : MapProjection
{
    private const double PreAsinFactor = 0.883883476d;
    private const double PostAsinFactor = 1.13137085d;
    private const double DefaultCx = 0.874038744d;
    private const double DefaultCy = 3.883251825d;

    private readonly double cx;
    private readonly double cy;

    /// <summary>
    /// Initializes a new instance of the <see cref="PutninsP4PProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public PutninsP4PProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PutninsP4PProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public PutninsP4PProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Putnins_P4P";
        this.cx = this.Parameters.GetOptionalParameterValue("putp4p_cx", DefaultCx);
        this.cy = this.Parameters.GetOptionalParameterValue("putp4p_cy", DefaultCy);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new PutninsP4PProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = Asinz(PreAsinFactor * Math.Sin(lat));
        double x = this.cx * lambda * Math.Cos(phi);
        double phiThird = phi / 3d;
        double cosPhiThird = Math.Cos(phiThird);
        if (Math.Abs(cosPhiThird) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        x /= cosPhiThird;
        double y = this.cy * Math.Sin(phiThird);

        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;

        double phiThird = Asinz(yy / this.cy);
        double cosPhiThird = Math.Cos(phiThird);
        if (Math.Abs(this.cx) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lambda = (xx * cosPhiThird) / this.cx;
        double phi = 3d * phiThird;
        double cosPhi = Math.Cos(phi);
        if (Math.Abs(cosPhi) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        lambda /= cosPhi;
        phi = Asinz(PostAsinFactor * Math.Sin(phi));

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
