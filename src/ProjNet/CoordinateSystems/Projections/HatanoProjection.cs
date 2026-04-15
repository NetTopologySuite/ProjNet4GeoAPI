// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Hatano Asymmetrical Equal Area projection (<c>hatano</c>).
/// </summary>
/// <remarks>
/// Hatano Asymmetrical Equal Area is a spherical equal-area pseudocylindrical projection
/// introduced by Masataka Hatano in 1972. The northern and southern hemispheres use
/// distinct constants, and the implementation solves <c>φ + sin(φ) = c * sin(lat)</c>
/// iteratively before applying the final half-angle scaling.
/// </remarks>
internal sealed class HatanoProjection : MapProjection
{
    private const int Iterations = 20;
    private const double OneTol = ProjectionConstants.OnePlusEps6;
    private const double Cn = 2.67595d;
    private const double Csz = 2.43763d;
    private const double Rcn = 0.37369906014686373063d;
    private const double Rcs = 0.41023453108141924738d;
    private const double Fycn = 1.75859d;
    private const double Fycs = 1.93052d;
    private const double Rycn = 0.56863737426006061674d;
    private const double Rycs = 0.51799515156538134803d;
    private const double Fxc = 0.85d;
    private const double Rxc = 1.17647058823529411764d;

    /// <summary>
    /// Initializes a new instance of the <see cref="HatanoProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public HatanoProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HatanoProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public HatanoProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Hatano";
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new HatanoProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;
        double c = Math.Sin(phi) * (phi < 0d ? Csz : Cn);
        for (int i = Iterations; i > 0; i--)
        {
            double denominator = 1d + Math.Cos(phi);
            if (Math.Abs(denominator) <= Eps10)
            {
                break;
            }

            double th1 = (phi + Math.Sin(phi) - c) / denominator;
            phi -= th1;
            if (Math.Abs(th1) < Eps7)
            {
                break;
            }
        }

        phi *= 0.5d;
        double x = Fxc * lambda * Math.Cos(phi);
        double y = Math.Sin(phi) * (phi < 0d ? Fycs : Fycn);

        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;
        double th = yy * (yy < 0d ? Rycs : Rycn);
        double absTh = Math.Abs(th);
        if (absTh > 1d)
        {
            if (absTh > OneTol)
            {
                ProjectionThrowHelper.ThrowOutsideProjectionDomain();
            }

            th = th > 0d ? HalfPi : -HalfPi;
        }
        else
        {
            th = Math.Asin(th);
        }

        double cosTh = Math.Cos(th);
        if (Math.Abs(cosTh) <= Eps10)
        {
            ProjectionThrowHelper.ThrowOutsideProjectionDomain();
        }

        double lambda = (Rxc * xx) / cosTh;
        double thetaDouble = th + th;
        double phi = (thetaDouble + Math.Sin(thetaDouble)) * (yy < 0d ? Rcs : Rcn);
        double absPhi = Math.Abs(phi);
        if (absPhi > 1d)
        {
            if (absPhi > OneTol)
            {
                ProjectionThrowHelper.ThrowOutsideProjectionDomain();
            }

            phi = phi > 0d ? HalfPi : -HalfPi;
        }
        else
        {
            phi = Math.Asin(phi);
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
