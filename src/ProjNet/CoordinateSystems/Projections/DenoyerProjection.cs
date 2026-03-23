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
/// Implements the spherical Denoyer Semi-Elliptical projection (<c>denoy</c>).
/// </summary>
[Serializable]
internal class DenoyerProjection : MapProjection
{
    private const double C0 = 0.95d;
    private const double C1 = -0.08333333333333333333d;
    private const double C3 = 0.00166666666666666666d;
    private const double D1 = 0.9d;
    private const double D5 = 0.03d;

    private readonly double radius;

    /// <summary>
    /// Initializes a new instance of the <see cref="DenoyerProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public DenoyerProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DenoyerProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public DenoyerProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Denoyer_Semi_Elliptical";
        this.radius = this.semiMajor * this.scaleFactor;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new DenoyerProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double x = lambda;
        double absLambda = Math.Abs(lambda);
        x *= Math.Cos(
            (C0 + (absLambda * (C1 + ((absLambda * absLambda) * C3))))
            * (lat * (D1 + (D5 * (lat * lat * lat * lat)))));

        lon = this.radius * x;
        lat = this.radius * lat;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Denoyer does not support inverse projection in this wave.");
    }
}
