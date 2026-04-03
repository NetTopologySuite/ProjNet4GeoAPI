// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Eckert II projection (<c>eck2</c>).
/// </summary>
/// <remarks>
/// Eckert II is a spherical equal-area pseudocylindrical projection with shortened, broken
/// meridians. The formulation was independently verified against the Wikipedia article
/// "Eckert II projection" and Max Eckert's 1906 description of the family. The auxiliary
/// term <c>sqrt(4 - 3 * sin(|phi|))</c> used in both the forward equations and the inverse
/// recovery of <c>phi</c> matches the implementation here.
/// </remarks>
/// <seealso href="https://en.wikipedia.org/wiki/Eckert_II_projection">Wikipedia: Eckert II projection.</seealso>
internal class Eckert2Projection : MapProjection
{
    private const double Fxc = 0.46065886596178063902d;
    private const double Fyc = 1.44720250911653531871d;
    private const double C13 = ProjectionConstants.OneThird;
    private const double OneEps = ProjectionConstants.OnePlusEps7;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="Eckert2Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Eckert2Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Eckert2Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Eckert2Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Eckert_II";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new Eckert2Projection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double yTmp = Math.Sqrt(4d - (3d * Math.Sin(Math.Abs(lat))));
        double x = Fxc * lambda * yTmp;
        double y = Fyc * (2d - yTmp);
        if (lat < 0d)
        {
            y = -y;
        }

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;

        double phiTmp = 2d - (Math.Abs(yy) / Fyc);
        double denominator = Fxc * phiTmp;
        if (Math.Abs(denominator) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lambda = xx / denominator;
        double phi = (4d - (phiTmp * phiTmp)) * C13;
        double absPhi = Math.Abs(phi);
        if (absPhi >= 1d)
        {
            if (absPhi > OneEps)
            {
                ArgumentGuard.ThrowArgument("Input data outside projection domain.");
            }

            phi = phi < 0d ? -HalfPi : HalfPi;
        }
        else
        {
            phi = Math.Asin(phi);
        }

        if (yy < 0d)
        {
            phi = -phi;
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
