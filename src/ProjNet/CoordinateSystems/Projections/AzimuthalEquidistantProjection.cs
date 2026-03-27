// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Azimuthal Equidistant map projection.
/// </summary>
/// <remarks>
/// <para>The Azimuthal Equidistant projection preserves both distance and direction from
/// the projection centre. All points on the map are at proportionally correct distances
/// from the centre, and the azimuth (bearing) from the centre to any other point is
/// correctly represented. The spherical formulation is used for both forward and inverse
/// transformations.</para>
/// </remarks>
[Serializable]
internal class AzimuthalEquidistantProjection : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double sinPhi0;
    private readonly double cosPhi0;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzimuthalEquidistantProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public AzimuthalEquidistantProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzimuthalEquidistantProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public AzimuthalEquidistantProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Azimuthal_Equidistant";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1.0 / this.radius;
        Sincos(this.latOrigin, out this.sinPhi0, out this.cosPhi0);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new AzimuthalEquidistantProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double sinPhi = Math.Sin(lat);
        double cosPhi = Math.Cos(lat);
        double cosLambda = Math.Cos(lambda);

        double cosC = (this.sinPhi0 * sinPhi) + (this.cosPhi0 * cosPhi * cosLambda);
        double c = Math.Acos(Clamp(cosC, -1d, 1d));
        double sinC = Math.Sin(c);
        double k = Math.Abs(sinC) <= Eps10 ? 1d : c / sinC;

        lon = this.radius * k * cosPhi * Math.Sin(lambda);
        lat = this.radius * k * ((this.cosPhi0 * sinPhi) - (this.sinPhi0 * cosPhi * cosLambda));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double rho = Hypot(x, y);
        if (rho <= Eps10)
        {
            x = this.centralMeridian;
            y = this.latOrigin;
            return;
        }

        double c = rho * this.inverseRadius;
        double sinC = Math.Sin(c);
        double cosC = Math.Cos(c);

        double phi = Math.Asin(Clamp((cosC * this.sinPhi0) + ((y * sinC * this.cosPhi0) / rho), -1d, 1d));
        double lambda = Math.Atan2(x * sinC, (rho * this.cosPhi0 * cosC) - (y * this.sinPhi0 * sinC));

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
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
