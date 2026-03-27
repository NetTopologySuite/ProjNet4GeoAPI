// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Central Conic projection (<c>ccon</c>).
/// </summary>
/// <remarks>
/// A perspective conic projection defined by a single standard parallel (<c>lat_1</c>,
/// which must be non-zero). Graticule lines are constructed by central (gnomonic)
/// projection onto the cone. Only spherical input is supported.
/// </remarks>
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
    public CentralConicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Central_Conic";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
        this.phi1 = DegreesToRadians(this.Parameters.GetParameterValue("lat_1", "standard_parallel_1"));
        if (Math.Abs(this.phi1) < Eps10)
        {
            ArgumentGuard.ThrowArgument("Invalid value for lat_1: |lat_1| should be > 0.");
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
