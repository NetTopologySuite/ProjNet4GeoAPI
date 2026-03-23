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
/// Implements the spherical Laskowski projection (<c>lask</c>).
/// </summary>
[Serializable]
internal class LaskowskiProjection : MapProjection
{
    private const double A10 = 0.975534d;
    private const double A12 = -0.119161d;
    private const double A32 = -0.0143059d;
    private const double A14 = -0.0547009d;
    private const double B01 = 1.00384d;
    private const double B21 = 0.0802894d;
    private const double B03 = 0.0998909d;
    private const double B41 = 0.000199025d;
    private const double B23 = -0.02855d;
    private const double B05 = -0.0491032d;

    private readonly double radius;

    /// <summary>
    /// Initializes a new instance of the <see cref="LaskowskiProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public LaskowskiProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LaskowskiProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public LaskowskiProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Laskowski";
        this.radius = this.semiMajor * this.scaleFactor;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new LaskowskiProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double l2 = lambda * lambda;
        double p2 = lat * lat;
        double x = lambda * (A10 + (p2 * (A12 + (l2 * A32) + (p2 * A14))));
        double y = lat * (B01 + (l2 * (B21 + (p2 * B23) + (l2 * B41))) + (p2 * (B03 + (p2 * B05))));
        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Laskowski does not support inverse projection in this wave.");
    }
}
