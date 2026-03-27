// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical McBryde-Thomas Flat-Polar Quartic projection (<c>mbtfpq</c>).
/// </summary>
[Serializable]
internal class McBrydeThomasFlatPolarQuarticProjection : MapProjection
{
    private const int Iterations = 20;
    private const double IterationTolerance = 1e-7d;
    private const double OneTol = ProjectionConstants.OnePlusEps6;
    private const double C = 1.70710678118654752440d;
    private const double Rc = 0.58578643762690495119d;
    private const double Fyc = 1.87475828462269495505d;
    private const double Ryc = 0.53340209679417701685d;
    private const double Fxc = 0.31245971410378249250d;
    private const double Rxc = 3.20041258076506210122d;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="McBrydeThomasFlatPolarQuarticProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public McBrydeThomasFlatPolarQuarticProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="McBrydeThomasFlatPolarQuarticProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public McBrydeThomasFlatPolarQuarticProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "McBryde_Thomas_Flat_Polar_Quartic";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new McBrydeThomasFlatPolarQuarticProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;
        double c = C * Math.Sin(phi);
        for (int i = Iterations; i > 0; i--)
        {
            double deltaNumerator = (Math.Sin(0.5d * phi) + Math.Sin(phi)) - c;
            double deltaDenominator = (0.5d * Math.Cos(0.5d * phi)) + Math.Cos(phi);
            double delta = deltaNumerator / deltaDenominator;
            phi -= delta;
            if (Math.Abs(delta) < IterationTolerance)
            {
                break;
            }
        }

        double x = Fxc * lambda * (1d + ((2d * Math.Cos(phi)) / Math.Cos(0.5d * phi)));
        double y = Fyc * Math.Sin(0.5d * phi);
        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;

        double phi = Ryc * yy;
        double t;
        if (Math.Abs(phi) > 1d)
        {
            if (Math.Abs(phi) > OneTol)
            {
                ArgumentGuard.ThrowArgument("Input data outside projection domain.");
            }

            if (phi < 0d)
            {
                t = -1d;
                phi = -PI;
            }
            else
            {
                t = 1d;
                phi = PI;
            }
        }
        else
        {
            t = phi;
            phi = 2d * Math.Asin(phi);
        }

        double lambda = Rxc * xx / (1d + ((2d * Math.Cos(phi)) / Math.Cos(0.5d * phi)));
        phi = Rc * (t + Math.Sin(phi));
        if (Math.Abs(phi) > 1d)
        {
            if (Math.Abs(phi) > OneTol)
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
