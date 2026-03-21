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
/// Represents the documented type.
/// </summary>
[Serializable]
internal class PconicProjection : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double n;
    private readonly double sig;
    private readonly double c1;
    private readonly double c2;
    private readonly double rho0;

    /// <summary>
    /// Initializes a new instance of the <see cref="PconicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public PconicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PconicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public PconicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Perspective_Conic";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        double standardParallel1 = DegreesToRadians(this.Parameters.GetParameterValue("standard_parallel_1", "lat_1"));
        double standardParallel2 = DegreesToRadians(this.Parameters.GetParameterValue("standard_parallel_2", "lat_2"));
        double delta = 0.5d * (standardParallel2 - standardParallel1);
        this.sig = 0.5d * (standardParallel2 + standardParallel1);
        if (Math.Abs(delta) < EPS10 || Math.Abs(this.sig) < EPS10)
        {
            throw new ArgumentException("Illegal value for lat_1 and lat_2: |lat_1 - lat_2| and |lat_1 + lat_2| should be > 0.");
        }

        this.n = Math.Sin(this.sig);
        this.c2 = Math.Cos(delta);
        this.c1 = 1d / Math.Tan(this.sig);

        double latitudeOffset = this.latOrigin - this.sig;
        if ((Math.Abs(latitudeOffset) - EPS10) >= HALFPI)
        {
            throw new ArgumentException("Invalid value for lat_0/lat_1/lat_2: |lat_0 - 0.5 * (lat_1 + lat_2)| should be < 90°.");
        }

        this.rho0 = this.c2 * (this.c1 - Math.Tan(latitudeOffset));
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new PconicProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double rho = this.c2 * (this.c1 - Math.Tan(lat - this.sig));
        double theta = this.n * Adjust_lon(lon - this.centralMeridian);

        lon = this.radius * rho * Math.Sin(theta);
        lat = this.radius * (this.rho0 - (rho * Math.Cos(theta)));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = this.rho0 - (y * this.inverseRadius);
        double rho = Hypot(xUnit, yUnit);

        if (this.n < 0d)
        {
            rho = -rho;
            xUnit = -xUnit;
            yUnit = -yUnit;
        }

        double lambda = Math.Atan2(xUnit, yUnit) / this.n;
        double phi = Math.Atan(this.c1 - (rho / this.c2)) + this.sig;

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
