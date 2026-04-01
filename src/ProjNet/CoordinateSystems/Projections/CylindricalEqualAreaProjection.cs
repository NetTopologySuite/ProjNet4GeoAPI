// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

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
internal class CylindricalEqualAreaProjection : MapProjection
{
    private const double Epsilon = 1e-10d;

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double cosStandardParallel;
    private readonly bool isEllipsoidal;
    private readonly double oneEs;
    private readonly double qp;

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
        this.isEllipsoidal = this.es > 0d;

        double standardParallel = DegreesToRadians(this.Parameters.GetOptionalParameterValue("standard_parallel_1", 0d, "lat_ts"));
        this.cosStandardParallel = Math.Cos(standardParallel);
        if (Math.Abs(this.cosStandardParallel) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("The standard parallel cannot be at the poles.");
        }

        if (this.isEllipsoidal)
        {
            this.oneEs = 1d - this.es;
            double sinStandardParallel = Math.Sin(standardParallel);
            this.cosStandardParallel /= Math.Sqrt(1d - (this.es * sinStandardParallel * sinStandardParallel));
            this.qp = Qsfn(1d, this.e, this.oneEs);
        }
        else
        {
            this.oneEs = 0d;
            this.qp = 0d;
        }

        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new CylindricalEqualAreaProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        lon = this.radius * lambda * this.cosStandardParallel;

        if (this.isEllipsoidal)
        {
            lat = this.radius * (0.5d * Qsfn(Math.Sin(lat), this.e, this.oneEs)) / this.cosStandardParallel;
        }
        else
        {
            lat = this.radius * Math.Sin(lat) / this.cosStandardParallel;
        }
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        x = Adjust_lon(this.centralMeridian + ((x * this.inverseRadius) / this.cosStandardParallel));

        double normalized = (y * this.cosStandardParallel) * this.inverseRadius;
        if (this.isEllipsoidal)
        {
            double q = ProjectionConstants.Clamp(2d * normalized, -this.qp, this.qp);
            y = Phi1z(this.e, q, out long _);
            return;
        }

        if (Math.Abs(normalized) - Epsilon <= 1d)
        {
            if (Math.Abs(normalized) >= 1d)
            {
                y = normalized < 0d ? -HalfPi : HalfPi;
            }
            else
            {
                y = Math.Asin(normalized);
            }
        }
        else
        {
            ArgumentGuard.ThrowArgumentOutOfRange(nameof(y), "Coordinate is outside the valid Cylindrical Equal Area domain.");
        }
    }
}
