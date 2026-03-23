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
/// Implements the spherical Compact Miller projection (<c>comill</c>).
/// </summary>
[Serializable]
internal class CompactMillerProjection : MapProjection
{
    private const double K1 = 0.9902d;
    private const double K2 = 0.1604d;
    private const double K3 = -0.03054d;
    private const double C1 = K1;
    private const double C2 = 3d * K2;
    private const double C3 = 5d * K3;
    private const double Epsilon = 1e-11d;
    private const double MaxYFactor = 0.6000207669862655d;
    private const int MaxIterations = 100;

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double maxY;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompactMillerProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public CompactMillerProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompactMillerProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public CompactMillerProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Compact_Miller";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
        this.maxY = MaxYFactor * PI;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new CompactMillerProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double latSquared = lat * lat;
        double y = lat * (K1 + (latSquared * (K2 + (K3 * latSquared))));
        lon = this.radius * lambda;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;
        if (yy > this.maxY)
        {
            yy = this.maxY;
        }
        else if (yy < -this.maxY)
        {
            yy = -this.maxY;
        }

        double yc = yy;
        bool converged = false;
        for (int i = MaxIterations; i > 0; i--)
        {
            double y2 = yc * yc;
            double f = (yc * (K1 + (y2 * (K2 + (K3 * y2))))) - yy;
            double fDerivative = C1 + (y2 * (C2 + (C3 * y2)));
            double tolerance = f / fDerivative;
            yc -= tolerance;
            if (Math.Abs(tolerance) < Epsilon)
            {
                converged = true;
                break;
            }
        }

        if (!converged)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        x = Adjust_lon(this.centralMeridian + xx);
        y = yc;
    }
}
