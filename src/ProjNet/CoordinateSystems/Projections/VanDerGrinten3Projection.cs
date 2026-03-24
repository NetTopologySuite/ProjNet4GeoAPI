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
/// Implements the spherical van der Grinten III projection (<c>vandg3</c>).
/// </summary>
[Serializable]
internal class VanDerGrinten3Projection : MapProjection
{
    private const double Tolerance = 1e-10d;

    private readonly double radius;

    /// <summary>
    /// Initializes a new instance of the <see cref="VanDerGrinten3Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public VanDerGrinten3Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VanDerGrinten3Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public VanDerGrinten3Projection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Van_der_Grinten_III";
        this.radius = this.semiMajor * this.scaleFactor;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new VanDerGrinten3Projection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double bt = Math.Abs((2d / PI) * lat);
        double ct = 1d - (bt * bt);
        if (ct < 0d)
        {
            ct = 0d;
        }
        else
        {
            ct = Math.Sqrt(ct);
        }

        double x;
        double y;
        if (Math.Abs(lambda) < Tolerance)
        {
            x = 0d;
            y = PI * (lat < 0d ? -bt : bt) / (1d + ct);
        }
        else
        {
            double at = 0.5d * Math.Abs((PI / lambda) - (lambda / PI));
            double x1 = bt / (1d + ct);
            x = PI * (Math.Sqrt((at * at) + 1d - (x1 * x1)) - at);
            y = PI * x1;

            if (lambda < 0d)
            {
                x = -x;
            }

            if (lat < 0d)
            {
                y = -y;
            }
        }

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("van der Grinten III does not support inverse projection in this wave.");
    }
}


