// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Equidistant Conic projection (<c>eqdc</c>).
/// </summary>
/// <remarks>
/// Distances along all meridians and along the two standard parallels are preserved.
/// Supports both one-standard-parallel and two-standard-parallel forms; when a single
/// parallel is specified via <c>standard_parallel_1</c>, the cone constant is set to
/// the sine of that parallel.
/// </remarks>
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
    public EquidistantConicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
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
            ArgumentGuard.ThrowArgument("Invalid standard parallels for equidistant conic projection.");
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
