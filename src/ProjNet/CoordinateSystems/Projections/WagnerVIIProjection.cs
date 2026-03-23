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
/// Implements the spherical Wagner VII projection (<c>wag7</c>).
/// </summary>
[Serializable]
internal class WagnerVIIProjection : MapProjection
{
    private const double YPreFactor = 0.90630778703664996d;
    private const double XFactor = 2.66723d;
    private const double YFactor = 1.24104d;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="WagnerVIIProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public WagnerVIIProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WagnerVIIProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public WagnerVIIProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Wagner_VII";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new WagnerVIIProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double yTemp = YPreFactor * Math.Sin(lat);
        double theta = Asinz(yTemp);
        double cosTheta = Math.Cos(theta);
        double lambdaThird = lambda / 3d;

        double x = XFactor * cosTheta * Math.Sin(lambdaThird);
        double denominator = Math.Sqrt(0.5d * (1d + (cosTheta * Math.Cos(lambdaThird))));
        if (denominator <= Eps10)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        double scale = 1d / denominator;
        x *= scale;
        double y = yTemp * YFactor * scale;

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Wagner VII does not support inverse projection in this wave.");
    }
}
