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
/// Implements the spherical Eckert IV projection (<c>eck4</c>).
/// </summary>
[Serializable]
internal class EckertIVProjection : MapProjection
{
    private const double OneTol = 1.00000000000001d;
    private const double Cx = 0.42223820031577120149d;
    private const double Cy = 1.32650042817700232218d;
    private const double RCy = 0.75386330736002178205d;
    private const double Cp = 3.57079632679489661922d;
    private const double RCp = 0.28004957675577868795d;
    private const int Iterations = 6;
    private const double Epsilon = 1e-7;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="EckertIVProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public EckertIVProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EckertIVProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public EckertIVProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Eckert_IV";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new EckertIVProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double p = Cp * Math.Sin(lat);
        double v = lat * lat;
        double theta = lat * (0.895168d + (v * (0.0218849d + (v * 0.00826809d))));
        int i = Iterations;

        for (; i > 0; i--)
        {
            double c = Math.Cos(theta);
            double s = Math.Sin(theta);
            v = (theta + (s * (c + 2d)) - p) / (1d + (c * (c + 2d)) - (s * s));
            theta -= v;
            if (Math.Abs(v) < Epsilon)
            {
                break;
            }
        }

        double x;
        double y;
        if (i == 0)
        {
            x = Cx * lambda;
            y = theta < 0d ? -Cy : Cy;
        }
        else
        {
            x = Cx * lambda * (1d + Math.Cos(theta));
            y = Cy * Math.Sin(theta);
        }

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;
        double sinTheta = yy * RCy;
        double absSinTheta = Math.Abs(sinTheta);
        if (absSinTheta > OneTol)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        double oneMinusAbs = 1d - Math.Abs(sinTheta);
        double lambda;
        double phi;

        if (oneMinusAbs >= 0d && oneMinusAbs <= 1e-12d)
        {
            lambda = xx / Cx;
            phi = sinTheta > 0d ? HalfPi : -HalfPi;
        }
        else
        {
            double theta = Asinz(sinTheta);
            double cosTheta = Math.Cos(theta);
            double denominator = Cx * (1d + cosTheta);
            if (Math.Abs(denominator) <= Eps10)
            {
                throw new ArgumentException("Input data outside projection domain.");
            }

            lambda = xx / denominator;
            double sinPhi = (theta + (sinTheta * (cosTheta + 2d))) * RCp;
            phi = Asinz(sinPhi);
        }

        double absLamMinusPi = Math.Abs(lambda) - PI;
        if (absLamMinusPi > 0d)
        {
            if (absLamMinusPi > 1e-10d)
            {
                throw new ArgumentException("Input data outside projection domain.");
            }

            lambda = lambda > 0d ? PI : -PI;
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
