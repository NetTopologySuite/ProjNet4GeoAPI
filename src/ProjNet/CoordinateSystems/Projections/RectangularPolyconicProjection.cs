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
/// Implements the spherical Rectangular Polyconic projection (<c>rpoly</c>).
/// </summary>
[Serializable]
internal sealed class RectangularPolyconicProjection : MapProjection
{
    private readonly double radius;
    private readonly double modeFxa;
    private readonly double modeFxb;
    private readonly bool mode;

    /// <summary>
    /// Initializes a new instance of the <see cref="RectangularPolyconicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public RectangularPolyconicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RectangularPolyconicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public RectangularPolyconicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Rectangular_Polyconic";
        this.radius = this.semiMajor * this.scaleFactor;

        double phi1 = Math.Abs(DegreesToRadians(this.Parameters.GetOptionalParameterValue("lat_ts", 0d)));
        this.mode = phi1 > Eps10;
        if (this.mode)
        {
            this.modeFxb = 0.5d * Math.Sin(phi1);
            this.modeFxa = 0.5d / this.modeFxb;
        }
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        throw new InvalidOperationException("Rectangular Polyconic does not support inverse projection in this wave.");
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;
        double fa = this.mode ? Math.Tan(lambda * this.modeFxb) * this.modeFxa : 0.5d * lambda;
        double xUnit;
        double yUnit;

        if (Math.Abs(phi) < 1e-9d)
        {
            xUnit = fa + fa;
            yUnit = -this.latOrigin;
        }
        else
        {
            yUnit = 1d / Math.Tan(phi);
            fa = 2d * Math.Atan(fa * Math.Sin(phi));
            xUnit = Math.Sin(fa) * yUnit;
            yUnit = (phi - this.latOrigin) + ((1d - Math.Cos(fa)) * yUnit);
        }

        lon = this.radius * xUnit;
        lat = this.radius * yUnit;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Rectangular Polyconic does not support inverse projection in this wave.");
    }
}

