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
/// Implements the spherical Bacon / Apian / Ortelius globular projections.
/// </summary>
[Serializable]
internal class BaconProjection : MapProjection
{
    private const double HalfPiSquared = 2.46740110027233965467d;
    private const double Epsilon = 1e-10d;

    private readonly double radius;
    private readonly bool bacon;
    private readonly bool ortelius;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaconProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public BaconProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaconProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public BaconProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : this(parameters, inverse, bacon: true, ortelius: false, "Bacon_Globular")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaconProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    /// <param name="bacon">Whether to use Bacon latitude scaling.</param>
    /// <param name="ortelius">Whether to use Ortelius branch for wide longitudes.</param>
    /// <param name="name">Projection display name.</param>
    protected BaconProjection(
        IEnumerable<ProjectionParameter> parameters,
        MapProjection inverse,
        bool bacon,
        bool ortelius,
        string name)
        : base(parameters, inverse)
    {
        this.Name = name;
        this.radius = this.semiMajor * this.scaleFactor;
        this.bacon = bacon;
        this.ortelius = ortelius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new BaconProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double y = this.bacon ? HalfPi * Math.Sin(lat) : lat;
        double absLambda = Math.Abs(lambda);
        double x = 0d;

        if (absLambda >= Epsilon)
        {
            if (this.ortelius && absLambda >= HalfPi)
            {
                x = Math.Sqrt(HalfPiSquared - (lat * lat) + Epsilon) + absLambda - HalfPi;
            }
            else
            {
                double f = 0.5d * ((HalfPiSquared / absLambda) + absLambda);
                x = absLambda - f + Math.Sqrt((f * f) - (y * y));
            }

            if (lambda < 0d)
            {
                x = -x;
            }
        }

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Bacon globular family does not support inverse projection in this wave.");
    }
}
