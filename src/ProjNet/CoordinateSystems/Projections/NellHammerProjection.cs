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
/// Implements the spherical Nell-Hammer projection (<c>nell_h</c>).
/// </summary>
[Serializable]
internal class NellHammerProjection : MapProjection
{
    private const int Iterations = 9;
    private const double Epsilon = 1e-7d;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="NellHammerProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public NellHammerProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NellHammerProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public NellHammerProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Nell_Hammer";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new NellHammerProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double x = 0.5d * lambda * (1d + Math.Cos(lat));
        double y = 2d * (lat - Math.Tan(0.5d * lat));
        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;

        double p = 0.5d * yy;
        double phi = 0d;
        int i = Iterations;
        for (; i > 0; i--)
        {
            double c = Math.Cos(0.5d * phi);
            double denominator = 1d - (0.5d / (c * c));
            if (Math.Abs(denominator) <= Eps10)
            {
                break;
            }

            double v = (phi - Math.Tan(phi / 2d) - p) / denominator;
            phi -= v;
            if (Math.Abs(v) < Epsilon)
            {
                break;
            }
        }

        double lambda;
        if (i == 0)
        {
            phi = p < 0d ? -HalfPi : HalfPi;
            lambda = 2d * xx;
        }
        else
        {
            double denominator = 1d + Math.Cos(phi);
            if (Math.Abs(denominator) <= Eps10)
            {
                throw new ArgumentException("Input data outside projection domain.");
            }

            lambda = (2d * xx) / denominator;
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
