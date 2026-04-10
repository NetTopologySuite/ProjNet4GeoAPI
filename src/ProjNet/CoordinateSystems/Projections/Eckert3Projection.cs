// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Eckert III projection (<c>eck3</c>).
/// </summary>
/// <remarks>
/// Eckert III is a spherical pseudocylindrical projection with elliptical meridians.
/// The formulation was independently verified against John P. Snyder,
/// <i>Map Projections - A Working Manual</i> (USGS Professional Paper 1395, 1987),
/// section 32, and Max Eckert's 1906 description of the family. The forward relation
/// <c>x = Cx * λ * (a + sqrt(1 - b * φ²))</c> together with the linear
/// <c>y = Cy * φ</c> term matches the implementation here.
/// </remarks>
/// <seealso href="https://neacsu.net/geodesy/snyder/7-pseudocylindrical/sect_32/">Snyder section 32: pseudocylindrical projections.</seealso>
internal class Eckert3Projection : MapProjection
{
    private const double DefaultCx = 0.42223820031577120149d;
    private const double DefaultCy = 0.84447640063154240298d;
    private const double DefaultA = 1d;
    private const double DefaultB = 0.4052847345693510857755d;

    private readonly double cx;
    private readonly double cy;
    private readonly double a;
    private readonly double b;

    /// <summary>
    /// Initializes a new instance of the <see cref="Eckert3Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Eckert3Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Eckert3Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Eckert3Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Eckert_III";
        this.a = this.Parameters.GetOptionalParameterValue("eck3_a", DefaultA);
        this.b = this.Parameters.GetOptionalParameterValue("eck3_b", DefaultB);
        this.cx = this.Parameters.GetOptionalParameterValue("eck3_cx", DefaultCx);
        this.cy = this.Parameters.GetOptionalParameterValue("eck3_cy", DefaultCy);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new Eckert3Projection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double y = this.cy * lat;
        double underRoot = 1d - (this.b * lat * lat);
        if (underRoot < 0d)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double x = this.cx * lambda * (this.a + Math.Sqrt(underRoot));
        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;
        double phi = yy / this.cy;

        double underRoot = 1d - (this.b * phi * phi);
        if (underRoot < 0d)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double denominator = this.cx * (this.a + Math.Sqrt(underRoot));
        if (Math.Abs(denominator) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lambda = xx / denominator;
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
