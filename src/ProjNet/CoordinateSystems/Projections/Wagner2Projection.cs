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
/// Implements the spherical Wagner II projection (<c>wag2</c>).
/// </summary>
[Serializable]
internal class Wagner2Projection : MapProjection
{
    private const double Cx = 0.92483d;
    private const double Cy = 1.38725d;
    private const double Cp1 = 0.88022d;
    private const double Cp2 = 0.88550d;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="Wagner2Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Wagner2Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Wagner2Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Wagner2Projection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Wagner_II";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new Wagner2Projection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = Asinz(Cp1 * Math.Sin(Cp2 * lat));
        double x = Cx * lambda * Math.Cos(phi);
        double y = Cy * phi;

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;

        double phi = yy / Cy;
        double denominator = Cx * Math.Cos(phi);
        if (Math.Abs(denominator) <= Eps10)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        double lambda = xx / denominator;
        phi = Asinz(Math.Sin(phi) / Cp1) / Cp2;

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}

