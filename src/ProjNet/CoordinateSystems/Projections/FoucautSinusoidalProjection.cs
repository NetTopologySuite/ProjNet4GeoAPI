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
/// Implements the spherical Foucaut Sinusoidal projection (<c>fouc_s</c>).
/// </summary>
[Serializable]
internal class FoucautSinusoidalProjection : MapProjection
{
    private const int MaximumIterations = 10;
    private const double LoopTolerance = 1e-7d;

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double n;
    private readonly double n1;

    /// <summary>
    /// Initializes a new instance of the <see cref="FoucautSinusoidalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public FoucautSinusoidalProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FoucautSinusoidalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public FoucautSinusoidalProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Foucaut_Sinusoidal";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
        this.n = this.Parameters.GetOptionalParameterValue("n", 0d);
        if (this.n < 0d || this.n > 1d)
        {
            throw new ArgumentException("Invalid value for n: it should be in [0,1] range.");
        }

        this.n1 = 1d - this.n;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new FoucautSinusoidalProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double t = Math.Cos(lat);
        double denominator = this.n + (this.n1 * t);
        if (Math.Abs(denominator) <= Eps10)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        lon = this.radius * (lambda * t / denominator);
        lat = this.radius * ((this.n * lat) + (this.n1 * Math.Sin(lat)));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;
        double phi;
        if (this.n != 0d)
        {
            phi = yy;
            int i = MaximumIterations;
            for (; i > 0; i--)
            {
                double sinPhi = Math.Sin(phi);
                double cosPhi = Math.Cos(phi);
                double denominator = this.n + (this.n1 * cosPhi);
                if (Math.Abs(denominator) <= Eps10)
                {
                    break;
                }

                double v = ((this.n * phi) + (this.n1 * sinPhi) - yy) / denominator;
                phi -= v;
                if (Math.Abs(v) < LoopTolerance)
                {
                    break;
                }
            }

            if (i == 0)
            {
                phi = yy < 0d ? -HalfPi : HalfPi;
            }
        }
        else
        {
            phi = Asinz(yy);
        }

        double cos = Math.Cos(phi);
        if (Math.Abs(cos) <= Eps10)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        double lambda = xx * (this.n + (this.n1 * cos)) / cos;
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
