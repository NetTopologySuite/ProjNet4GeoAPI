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
/// Implements the spherical Nell projection (<c>nell</c>).
/// </summary>
[Serializable]
internal class NellProjection : MapProjection
{
    private const int Iterations = 10;
    private const double LoopTolerance = 1e-7d;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="NellProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public NellProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NellProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public NellProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Nell";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new NellProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double k = 2d * Math.Sin(lat);
        double phiSquared = lat * lat;
        double phi = lat * (1.00371d + (phiSquared * (-0.0935382d + (phiSquared * -0.011412d))));
        for (int i = Iterations; i > 0; i--)
        {
            double denominator = 1d + Math.Cos(phi);
            if (Math.Abs(denominator) <= Eps10)
            {
                break;
            }

            double v = (phi + Math.Sin(phi) - k) / denominator;
            phi -= v;
            if (Math.Abs(v) < LoopTolerance)
            {
                break;
            }
        }

        double x = 0.5d * lambda * (1d + Math.Cos(phi));
        double y = phi;
        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;
        double denominator = 1d + Math.Cos(yy);
        if (Math.Abs(denominator) <= Eps10)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        double lambda = (2d * xx) / denominator;
        double phi = Asinz(0.5d * (yy + Math.Sin(yy)));
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
