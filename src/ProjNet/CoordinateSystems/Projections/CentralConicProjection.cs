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
/// Implements the spherical Central Conic projection (<c>ccon</c>).
/// </summary>
[Serializable]
internal sealed class CentralConicProjection : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double phi1;
    private readonly double sinPhi1;
    private readonly double ctgPhi1;

    /// <summary>
    /// Initializes a new instance of the <see cref="CentralConicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public CentralConicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CentralConicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public CentralConicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Central_Conic";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
        this.phi1 = DegreesToRadians(this.Parameters.GetParameterValue("lat_1", "standard_parallel_1"));
        if (Math.Abs(this.phi1) < Eps10)
        {
            throw new ArgumentException("Invalid value for lat_1: |lat_1| should be > 0.");
        }

        this.sinPhi1 = Math.Sin(this.phi1);
        this.ctgPhi1 = Math.Cos(this.phi1) / this.sinPhi1;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new CentralConicProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double r = this.ctgPhi1 - Math.Tan(lat - this.phi1);
        double xUnit = r * Math.Sin(lambda * this.sinPhi1);
        double yUnit = this.ctgPhi1 - (r * Math.Cos(lambda * this.sinPhi1));

        lon = this.radius * xUnit;
        lat = this.radius * yUnit;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = this.ctgPhi1 - (y * this.inverseRadius);
        double lambda = Math.Atan2(xUnit, yUnit) / this.sinPhi1;
        double phi = this.phi1 - Math.Atan(Hypot(xUnit, yUnit) - this.ctgPhi1);

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}

