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
/// Implements the spherical Collignon projection (<c>collg</c>).
/// </summary>
[Serializable]
internal class CollignonProjection : MapProjection
{
    private const double Fxc = 1.12837916709551257390d;
    private const double Fyc = 1.77245385090551602729d;
    private const double OneEps = ProjectionConstants.OnePlusEps7;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollignonProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public CollignonProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CollignonProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public CollignonProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Collignon";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new CollignonProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double yRoot = 1d - Math.Sin(lat);
        if (yRoot <= 0d)
        {
            yRoot = 0d;
        }
        else
        {
            yRoot = Math.Sqrt(yRoot);
        }

        double x = Fxc * lambda * yRoot;
        double y = Fyc * (1d - yRoot);

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;

        double phi = (yy / Fyc) - 1d;
        phi = 1d - (phi * phi);
        double absPhi = Math.Abs(phi);
        if (absPhi < 1d)
        {
            phi = Math.Asin(phi);
        }
        else if (absPhi > OneEps)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }
        else
        {
            phi = phi < 0d ? -HalfPi : HalfPi;
        }

        double lamFactor = 1d - Math.Sin(phi);
        double lambda = lamFactor <= 0d ? 0d : xx / (Fxc * Math.Sqrt(lamFactor));
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
