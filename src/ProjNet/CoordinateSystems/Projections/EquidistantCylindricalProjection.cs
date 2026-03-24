// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Equidistant Cylindrical projection (<c>eqc</c>).
/// </summary>
/// <remarks>
/// Maps longitude linearly scaled by the cosine of the standard parallel and latitude
/// linearly from the origin latitude. When the standard parallel is at the equator this
/// is equivalent to the Plate Carrée projection.
/// </remarks>
[Serializable]
internal class EquidistantCylindricalProjection : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double cosStandardParallel;

    /// <summary>
    /// Initializes a new instance of the <see cref="EquidistantCylindricalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public EquidistantCylindricalProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EquidistantCylindricalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public EquidistantCylindricalProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Equidistant_Cylindrical";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        double standardParallel = DegreesToRadians(this.Parameters.GetOptionalParameterValue("standard_parallel_1", 0d, "latitude_of_true_scale"));
        this.cosStandardParallel = Math.Cos(standardParallel);
        if (Math.Abs(this.cosStandardParallel) <= Eps10)
        {
            throw new ArgumentException("The standard parallel cannot be at the poles.");
        }
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new EquidistantCylindricalProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        lon = this.radius * lambda * this.cosStandardParallel;
        lat = this.radius * (lat - this.latOrigin);
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        x = Adjust_lon(this.centralMeridian + ((x * this.inverseRadius) / this.cosStandardParallel));
        y = this.latOrigin + (y * this.inverseRadius);
    }
}
