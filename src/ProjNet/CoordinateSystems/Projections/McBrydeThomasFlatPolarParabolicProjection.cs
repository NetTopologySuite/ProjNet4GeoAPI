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
/// Implements the spherical McBryde-Thomas Flat-Polar Parabolic projection (<c>mbtfpp</c>).
/// </summary>
[Serializable]
internal class McBrydeThomasFlatPolarParabolicProjection : MapProjection
{
    private const double Csy = 0.95257934441568037152d;
    private const double Fxc = 0.92582009977255146156d;
    private const double Fyc = 3.40168025708304504493d;
    private const double C23 = 0.66666666666666666666d;
    private const double C13 = 0.33333333333333333333d;
    private const double OneEps = 1.0000001d;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="McBrydeThomasFlatPolarParabolicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public McBrydeThomasFlatPolarParabolicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="McBrydeThomasFlatPolarParabolicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public McBrydeThomasFlatPolarParabolicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "McBryde_Thomas_Flat_Polar_Parabolic";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new McBrydeThomasFlatPolarParabolicProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = Asinz(Csy * Math.Sin(lat));
        double x = Fxc * lambda * ((2d * Math.Cos(C23 * phi)) - 1d);
        double y = Fyc * Math.Sin(C13 * phi);
        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;

        double phi = yy / Fyc;
        if (Math.Abs(phi) >= 1d)
        {
            if (Math.Abs(phi) > OneEps)
            {
                throw new ArgumentException("Input data outside projection domain.");
            }

            phi = phi < 0d ? -HalfPi : HalfPi;
        }
        else
        {
            phi = Math.Asin(phi);
        }

        phi *= 3d;
        double lambda = xx / (Fxc * ((2d * Math.Cos(C23 * phi)) - 1d));
        phi = Math.Sin(phi) / Csy;
        if (Math.Abs(phi) >= 1d)
        {
            if (Math.Abs(phi) > OneEps)
            {
                throw new ArgumentException("Input data outside projection domain.");
            }

            phi = phi < 0d ? -HalfPi : HalfPi;
        }
        else
        {
            phi = Math.Asin(phi);
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
