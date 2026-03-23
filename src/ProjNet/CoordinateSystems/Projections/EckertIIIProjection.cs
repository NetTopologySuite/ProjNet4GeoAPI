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
/// Implements the spherical Eckert III projection (<c>eck3</c>).
/// </summary>
[Serializable]
internal class EckertIIIProjection : MapProjection
{
    private const double DefaultCx = 0.42223820031577120149d;
    private const double DefaultCy = 0.84447640063154240298d;
    private const double DefaultA = 1d;
    private const double DefaultB = 0.4052847345693510857755d;

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double cx;
    private readonly double cy;
    private readonly double a;
    private readonly double b;

    /// <summary>
    /// Initializes a new instance of the <see cref="EckertIIIProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public EckertIIIProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EckertIIIProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public EckertIIIProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Eckert_III";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
        this.a = this.Parameters.GetOptionalParameterValue("eck3_a", DefaultA);
        this.b = this.Parameters.GetOptionalParameterValue("eck3_b", DefaultB);
        this.cx = this.Parameters.GetOptionalParameterValue("eck3_cx", DefaultCx);
        this.cy = this.Parameters.GetOptionalParameterValue("eck3_cy", DefaultCy);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new EckertIIIProjection(this.Parameters.ToProjectionParameter(), this);
        }

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
            throw new ArgumentException("Input data outside projection domain.");
        }

        double x = this.cx * lambda * (this.a + Math.Sqrt(underRoot));
        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;
        double phi = yy / this.cy;

        double underRoot = 1d - (this.b * phi * phi);
        if (underRoot < 0d)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        double denominator = this.cx * (this.a + Math.Sqrt(underRoot));
        if (Math.Abs(denominator) <= Eps10)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        double lambda = xx / denominator;
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
