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
/// Implements the spherical van der Grinten IV projection (<c>vandg4</c>).
/// </summary>
[Serializable]
internal class VanDerGrintenIVProjection : MapProjection
{
    private const double Tolerance = 1e-10d;

    private readonly double radius;

    /// <summary>
    /// Initializes a new instance of the <see cref="VanDerGrintenIVProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public VanDerGrintenIVProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VanDerGrintenIVProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public VanDerGrintenIVProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Van_der_Grinten_IV";
        this.radius = this.semiMajor * this.scaleFactor;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new VanDerGrintenIVProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;
        double x;
        double y;

        if (Math.Abs(phi) < Tolerance)
        {
            x = lambda;
            y = 0d;
        }
        else if (Math.Abs(lambda) < Tolerance || Math.Abs(Math.Abs(phi) - HalfPi) < Tolerance)
        {
            x = 0d;
            y = phi;
        }
        else
        {
            double bt = Math.Abs((2d / PI) * phi);
            double bt2 = bt * bt;
            double ct = 0.5d * ((bt * (8d - (bt * (2d + bt2)))) - 5d) / (bt2 * (bt - 1d));
            double ct2 = ct * ct;
            double dt = (2d / PI) * lambda;
            dt = dt + (1d / dt);
            dt = Math.Sqrt((dt * dt) - 4d);
            if ((Math.Abs(lambda) - HalfPi) < 0d)
            {
                dt = -dt;
            }

            double dt2 = dt * dt;
            double x1 = bt + ct;
            x1 *= x1;
            double t = bt + (3d * ct);
            double ft = (x1 * (bt2 + (ct2 * dt2) - 1d))
                + ((1d - bt2) * ((bt2 * ((t * t) + (4d * ct2))) + (ct2 * ((12d * bt * ct) + (4d * ct2)))));
            x1 = ((dt * (x1 + ct2 - 1d)) + (2d * Math.Sqrt(ft))) / ((4d * x1) + dt2);
            x = HalfPi * x1;
            y = HalfPi * Math.Sqrt(1d + (dt * Math.Abs(x1)) - (x1 * x1));

            if (lambda < 0d)
            {
                x = -x;
            }

            if (phi < 0d)
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
        throw new InvalidOperationException("van der Grinten IV does not support inverse projection in this wave.");
    }
}

