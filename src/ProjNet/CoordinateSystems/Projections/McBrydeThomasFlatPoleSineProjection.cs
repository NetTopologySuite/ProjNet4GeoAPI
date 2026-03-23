// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical McBryde-Thomas Flat-Pole Sine (No. 2) projection (<c>mbt_fps</c>).
/// </summary>
[Serializable]
internal sealed class McBrydeThomasFlatPoleSineProjection : MapProjection
{
    private const int MaximumIterations = 10;
    private const double LoopTolerance = 1e-7;
    private const double C1 = 0.45503d;
    private const double C2 = 1.36509d;
    private const double C3 = 1.41546d;
    private const double CX = 0.22248d;
    private const double CY = 1.44492d;
    private const double C1_2 = 0.33333333333333333333333333d;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="McBrydeThomasFlatPoleSineProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public McBrydeThomasFlatPoleSineProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="McBrydeThomasFlatPoleSineProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public McBrydeThomasFlatPoleSineProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "McBryde_Thomas_Flat_Pole_Sine";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new McBrydeThomasFlatPoleSineProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;
        double k = C3 * Math.Sin(phi);
        for (int i = 0; i < MaximumIterations; i++)
        {
            double t = phi / C2;
            double denominator = (C1_2 * Math.Cos(t)) + Math.Cos(phi);
            if (Math.Abs(denominator) <= Eps10)
            {
                throw new ArgumentException("Input data outside projection domain.");
            }

            double v = ((C1 * Math.Sin(t)) + Math.Sin(phi) - k) / denominator;
            phi -= v;
            if (Math.Abs(v) < LoopTolerance)
            {
                break;
            }
        }

        double tt = phi / C2;
        lon = this.radius * (CX * lambda * (1d + ((3d * Math.Cos(phi)) / Math.Cos(tt))));
        lat = this.radius * (CY * Math.Sin(tt));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = y * this.inverseRadius;
        double t = Asinz(yUnit / CY);
        double phi = C2 * t;
        double denominator = CX * (1d + ((3d * Math.Cos(phi)) / Math.Cos(t)));
        if (Math.Abs(denominator) <= Eps10)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        double lambda = xUnit / denominator;
        phi = Asinz(((C1 * Math.Sin(t)) + Math.Sin(phi)) / C3);

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}

