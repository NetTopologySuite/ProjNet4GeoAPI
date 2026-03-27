// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Gnomonic map projection (<c>gnom</c>).
/// </summary>
/// <remarks>
/// The Gnomonic projection is a perspective azimuthal projection from the center of the
/// sphere onto a tangent plane. All great circles (geodesics) project as straight lines.
/// Points at or beyond 90° angular distance from the projection center cannot be projected
/// and produce <see cref="double.NaN"/> output coordinates.
/// </remarks>
[Serializable]
internal class GnomonicProjection : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double sinPhi0;
    private readonly double cosPhi0;

    /// <summary>
    /// Initializes a new instance of the <see cref="GnomonicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public GnomonicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GnomonicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public GnomonicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Gnomonic";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1.0 / this.radius;
        Sincos(this.latOrigin, out this.sinPhi0, out this.cosPhi0);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new GnomonicProjection(this.Parameters.ToProjectionParameter(), this);

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
        if (cosC <= Eps10)
        {
            lon = double.NaN;
            lat = double.NaN;
            return;
        }

        double k = 1d / cosC;
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

        double c = Math.Atan(rho * this.inverseRadius);
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
