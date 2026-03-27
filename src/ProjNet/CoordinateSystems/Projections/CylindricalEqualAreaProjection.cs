// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Cylindrical Equal Area projection (<c>cea</c>).
/// </summary>
/// <remarks>
/// Preserves area by mapping latitude to y = R·sin(φ) / cos(φ₁), where φ₁ is the
/// standard parallel. When the standard parallel is at the equator this is equivalent
/// to the Lambert Cylindrical Equal Area projection.
/// </remarks>
[Serializable]
internal class CylindricalEqualAreaProjection : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double cosStandardParallel;

    /// <summary>
    /// Initializes a new instance of the <see cref="CylindricalEqualAreaProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public CylindricalEqualAreaProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CylindricalEqualAreaProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public CylindricalEqualAreaProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Cylindrical_Equal_Area";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        double standardParallel = DegreesToRadians(this.Parameters.GetOptionalParameterValue("standard_parallel_1", 0d, "lat_ts"));
        this.cosStandardParallel = Math.Cos(standardParallel);
        if (Math.Abs(this.cosStandardParallel) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("The standard parallel cannot be at the poles.");
        }
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new CylindricalEqualAreaProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        lon = this.radius * lambda * this.cosStandardParallel;
        lat = this.radius * Math.Sin(lat) / this.cosStandardParallel;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        x = Adjust_lon(this.centralMeridian + ((x * this.inverseRadius) / this.cosStandardParallel));
        y = Math.Asin(Clamp((y * this.cosStandardParallel) * this.inverseRadius, -1d, 1d));
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }
}
