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
/// Represents the documented type.
/// </summary>
[Serializable]
internal class MollweideProjection : MapProjection
{
    private const int Iterations = 30;
    private const double LoopTolerance = 1e-7d;
    private const double DefaultP = 90d;

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double cx;
    private readonly double cy;
    private readonly double cp;

    /// <summary>
    /// Initializes a new instance of the <see cref="MollweideProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public MollweideProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MollweideProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public MollweideProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Mollweide";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        double p = DegreesToRadians(this.Parameters.GetOptionalParameterValue("moll_p", DefaultP));
        double sp = Math.Sin(p);
        double p2 = p + p;
        double denominator = p2 + Math.Sin(p2);
        if (Math.Abs(sp) <= Eps10 || Math.Abs(denominator) <= Eps10)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        double r = Math.Sqrt(TwoPi * sp / denominator);
        this.cx = this.Parameters.GetOptionalParameterValue("moll_cx", 2d * r / PI);
        this.cy = this.Parameters.GetOptionalParameterValue("moll_cy", r / sp);
        this.cp = this.Parameters.GetOptionalParameterValue("moll_cp", denominator);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new MollweideProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;
        double k = this.cp * Math.Sin(phi);
        int i = Iterations;
        for (; i > 0; i--)
        {
            double v = (phi + Math.Sin(phi) - k) / (1d + Math.Cos(phi));
            phi -= v;
            if (Math.Abs(v) < LoopTolerance)
            {
                break;
            }
        }

        if (i == 0)
        {
            phi = phi < 0d ? -HalfPi : HalfPi;
        }
        else
        {
            phi *= 0.5d;
        }

        lon = this.radius * this.cx * lambda * Math.Cos(phi);
        lat = this.radius * this.cy * Math.Sin(phi);
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;
        double phi = Asinz(yy / this.cy);
        double cosPhi = Math.Cos(phi);
        if (Math.Abs(cosPhi) <= Eps10)
        {
            x = this.centralMeridian;
            y = phi >= 0d ? HalfPi : -HalfPi;
            return;
        }

        double lambda = xx / (this.cx * cosPhi);
        if (Math.Abs(lambda) < PI)
        {
            phi += phi;
            phi = Asinz((phi + Math.Sin(phi)) / this.cp);
        }
        else
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
