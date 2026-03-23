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
internal class EquidistantConicProjection : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double n;
    private readonly double g;
    private readonly double rho0;

    /// <summary>
    /// Initializes a new instance of the <see cref="EquidistantConicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public EquidistantConicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EquidistantConicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public EquidistantConicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Equidistant_Conic";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        double standardParallel1 = DegreesToRadians(this.Parameters.GetParameterValue("standard_parallel_1", "lat_1"));
        double standardParallel2 = DegreesToRadians(this.Parameters.GetOptionalParameterValue("standard_parallel_2", RadiansToDegrees(standardParallel1), "lat_2"));

        if (Math.Abs(standardParallel1 - standardParallel2) <= Eps10)
        {
            this.n = Math.Sin(standardParallel1);
        }
        else
        {
            this.n = (Math.Cos(standardParallel1) - Math.Cos(standardParallel2)) / (standardParallel2 - standardParallel1);
        }

        if (Math.Abs(this.n) <= Eps10)
        {
            throw new ArgumentException("Invalid standard parallels for equidistant conic projection.");
        }

        this.g = (Math.Cos(standardParallel1) / this.n) + standardParallel1;
        this.rho0 = this.g - this.latOrigin;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new EquidistantConicProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double theta = this.n * Adjust_lon(lon - this.centralMeridian);
        double rho = this.g - lat;

        lon = this.radius * rho * Math.Sin(theta);
        lat = this.radius * (this.rho0 - (rho * Math.Cos(theta)));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = y * this.inverseRadius;
        double rhoPrime = this.rho0 - yUnit;
        double rho = Sign(this.n) * Math.Sqrt((xUnit * xUnit) + (rhoPrime * rhoPrime));

        double theta = 0d;
        if (Math.Abs(rho) > Eps10)
        {
            theta = Math.Atan2(xUnit, rhoPrime);
        }

        x = Adjust_lon(this.centralMeridian + (theta / this.n));
        y = this.g - rho;
    }
}
