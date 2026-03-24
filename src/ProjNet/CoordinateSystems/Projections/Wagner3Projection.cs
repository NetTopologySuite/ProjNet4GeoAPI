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
/// Implements the spherical Wagner III projection (<c>wag3</c>).
/// </summary>
[Serializable]
internal class Wagner3Projection : MapProjection
{
    private const double TwoThird = 0.6666666666666666666667d;

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double cx;

    /// <summary>
    /// Initializes a new instance of the <see cref="Wagner3Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Wagner3Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Wagner3Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Wagner3Projection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Wagner_III";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        double latTsDeg = this.Parameters.GetOptionalParameterValue("lat_ts", 0d, "latitude_true_scale");
        double ts = DegreesToRadians(latTsDeg);
        double denominator = Math.Cos((2d * ts) / 3d);
        if (Math.Abs(denominator) <= Eps10)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        this.cx = Math.Cos(ts) / denominator;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new Wagner3Projection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double x = this.cx * lambda * Math.Cos(TwoThird * lat);
        double y = lat;

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;

        double phi = yy;
        double denominator = this.cx * Math.Cos(TwoThird * phi);
        if (Math.Abs(denominator) <= Eps10)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        double lambda = xx / denominator;
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}

