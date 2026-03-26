// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical McBryde-Thomas Flat-Polar Parabolic projection (<c>mbtfpp</c>).
/// </summary>
[Serializable]
internal class McBrydeThomasFlatPolarParabolicProjection : MapProjection
{
    private const double Csy = 0.95257934441568037152d;
    private const double Fxc = 0.92582009977255146156d;
    private const double Fyc = 3.40168025708304504493d;
    private const double C23 = ProjectionConstants.TwoThirds;
    private const double C13 = ProjectionConstants.OneThird;
    private const double OneEps = ProjectionConstants.OnePlusEps7;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="McBrydeThomasFlatPolarParabolicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public McBrydeThomasFlatPolarParabolicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="McBrydeThomasFlatPolarParabolicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public McBrydeThomasFlatPolarParabolicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "McBryde_Thomas_Flat_Polar_Parabolic";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new McBrydeThomasFlatPolarParabolicProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = Asinz(Csy * Math.Sin(lat));
        double x = Fxc * lambda * ((2d * Math.Cos(C23 * phi)) - 1d);
        double y = Fyc * Math.Sin(C13 * phi);
        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;

        double phi = yy / Fyc;
        if (Math.Abs(phi) >= 1d)
        {
            if (Math.Abs(phi) > OneEps)
            {
                ArgumentGuard.ThrowArgument("Input data outside projection domain.");
            }

            phi = phi < 0d ? -HalfPi : HalfPi;
        }
        else
        {
            phi = Math.Asin(phi);
        }

        phi *= 3d;
        double lambda = xx / (Fxc * ((2d * Math.Cos(C23 * phi)) - 1d));
        phi = Math.Sin(phi) / Csy;
        if (Math.Abs(phi) >= 1d)
        {
            if (Math.Abs(phi) > OneEps)
            {
                ArgumentGuard.ThrowArgument("Input data outside projection domain.");
            }

            phi = phi < 0d ? -HalfPi : HalfPi;
        }
        else
        {
            phi = Math.Asin(phi);
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
