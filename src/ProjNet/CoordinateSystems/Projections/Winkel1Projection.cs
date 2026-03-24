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
/// Implements the spherical Winkel I projection (<c>wink1</c>).
/// </summary>
[Serializable]
internal class Winkel1Projection : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double cosphi1;

    /// <summary>
    /// Initializes a new instance of the <see cref="Winkel1Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Winkel1Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Winkel1Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Winkel1Projection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Winkel_I";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        double latTs = DegreesToRadians(this.Parameters.GetOptionalParameterValue("lat_ts", RadiansToDegrees(this.latOrigin), "latitude_true_scale"));
        this.cosphi1 = Math.Cos(latTs);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new Winkel1Projection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double x = 0.5d * lambda * (this.cosphi1 + Math.Cos(lat));
        double y = lat;

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;
        double denominator = this.cosphi1 + Math.Cos(yy);
        if (Math.Abs(denominator) <= Eps10)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        double lambda = 2d * xx / denominator;
        x = Adjust_lon(this.centralMeridian + lambda);
        y = yy;
    }
}

