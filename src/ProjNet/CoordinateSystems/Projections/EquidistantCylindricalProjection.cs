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
/// Maps longitude linearly scaled by the standard-parallel factor and latitude by
/// angular or meridional distance from the origin latitude. In the spherical form,
/// <c>E = a * cos(latSP) * λ</c> and <c>N = a * (φ - φ₀)</c>; in the ellipsoidal
/// form, the longitude scale becomes <c>ν₁ * cos(latSP)</c> and northing uses the
/// meridional distance difference <c>M - M₀</c>. When the standard parallel is at
/// the equator in the spherical branch this is equivalent to the Plate Carrée projection.
/// The spherical formulation was independently verified against IOGP, "Geomatics Guidance
/// Note 7, part 2: Coordinate Conversions and Transformations including Formulas"
/// (publication 373-7-2, 2019), EPSG methods 1029 and 1028. The easting and northing
/// equations <c>E = a * cos(latSP) * λ</c>, <c>N = a * (φ - φ₀)</c>,
/// <c>E = a * ν₁ * cos(latSP) * λ</c>, and <c>N = a * (M - M₀)</c> match
/// the implementation here.
/// </remarks>
/// <seealso href="https://epsg.io/1029-method">EPSG method 1029: Equidistant Cylindrical (spherical).</seealso>
/// <seealso href="https://epsg.io/1028-method">EPSG method 1028: Equidistant Cylindrical (ellipsoidal).</seealso>
internal class EquidistantCylindricalProjection : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double longitudeScale;
    private readonly double meridionalDistanceAtOrigin;
    private readonly bool isEllipsoidal;

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
    public EquidistantCylindricalProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Equidistant_Cylindrical";
        this.isEllipsoidal = this.es > 0d;
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        double standardParallel = DegreesToRadians(this.Parameters.GetOptionalParameterValue("standard_parallel_1", 0d, "lat_ts", "latitude_true_scale", "latitude_of_true_scale"));
        double cosStandardParallel = Math.Cos(standardParallel);
        if (Math.Abs(cosStandardParallel) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("The standard parallel cannot be at the poles.");
        }

        if (this.isEllipsoidal)
        {
            double sinStandardParallel = Math.Sin(standardParallel);
            double nu1 = 1d / Math.Sqrt(1d - (this.es * sinStandardParallel * sinStandardParallel));
            this.longitudeScale = nu1 * cosStandardParallel;
            this.meridionalDistanceAtOrigin = this.Mlfn(this.latOrigin, Math.Sin(this.latOrigin), Math.Cos(this.latOrigin));
            return;
        }

        this.longitudeScale = cosStandardParallel;
        this.meridionalDistanceAtOrigin = 0d;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new EquidistantCylindricalProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        lon = this.radius * lambda * this.longitudeScale;

        if (this.isEllipsoidal)
        {
            lat = this.radius * (this.Mlfn(lat, Math.Sin(lat), Math.Cos(lat)) - this.meridionalDistanceAtOrigin);
            return;
        }

        lat = this.radius * (lat - this.latOrigin);
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        x = Adjust_lon(this.centralMeridian + ((x * this.inverseRadius) / this.longitudeScale));

        if (this.isEllipsoidal)
        {
            y = this.Inv_mlfn((y * this.inverseRadius) + this.meridionalDistanceAtOrigin);
            return;
        }

        y = this.latOrigin + (y * this.inverseRadius);
    }
}
