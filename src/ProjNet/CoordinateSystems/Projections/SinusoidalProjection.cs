// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Sinusoidal projection (<c>sinu</c>).
/// </summary>
/// <remarks>
/// The Sinusoidal projection is an equal-area pseudocylindrical projection in which parallels
/// are evenly spaced straight lines and meridians are sinusoidal curves. Both spherical and
/// ellipsoidal modes are supported.
/// <para>The formulation was independently verified against the Wikipedia article
/// "Sinusoidal projection". The spherical equations <c>x = λ * cos(φ)</c>,
/// <c>y = φ</c> and the ellipsoidal branch that combines the meridian arc
/// <c>Mlfn(φ)</c> with the longitude scaling
/// <c>cos(φ) / sqrt(1 - e² * sin²(φ))</c> match the implementation here.</para>
/// <para>See also John P. Snyder, "Map Projections - A Working Manual",
/// U.S. Geological Survey Professional Paper 1395, 1987, Ch. 30, pp. 243-248,
/// for the sinusoidal projection in both its spherical and ellipsoidal forms.</para>
/// </remarks>
/// <seealso href="https://en.wikipedia.org/wiki/Sinusoidal_projection">Wikipedia: Sinusoidal projection.</seealso>
internal class SinusoidalProjection : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly bool isEllipsoidal;

    /// <summary>
    /// Initializes a new instance of the <see cref="SinusoidalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public SinusoidalProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SinusoidalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public SinusoidalProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Sinusoidal";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
        this.isEllipsoidal = this.es > 0d;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new SinusoidalProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;

        if (this.isEllipsoidal)
        {
            double sinPhi = Math.Sin(phi);
            double cosPhi = Math.Cos(phi);
            lat = this.radius * this.Mlfn(phi, sinPhi, cosPhi);
            lon = this.radius * lambda * cosPhi / Math.Sqrt(1d - (this.es * sinPhi * sinPhi));
            return;
        }

        lon = this.radius * lambda * Math.Cos(phi);
        lat = this.radius * phi;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = y * this.inverseRadius;

        if (this.isEllipsoidal)
        {
            double phiEllipsoid = this.Inv_mlfn(yUnit);
            double absPhi = Math.Abs(phiEllipsoid);
            double lambdaEllipsoid = 0d;

            if (absPhi < HalfPi)
            {
                double sinPhi = Math.Sin(phiEllipsoid);
                lambdaEllipsoid = xUnit * Math.Sqrt(1d - (this.es * sinPhi * sinPhi)) / Math.Cos(phiEllipsoid);
            }
            else if ((absPhi - Eps10) >= HalfPi)
            {
                ArgumentGuard.ThrowArgument("Input data outside projection domain.");
            }

            x = Adjust_lon(this.centralMeridian + lambdaEllipsoid);
            y = phiEllipsoid;
            return;
        }

        double phiSphere = yUnit;
        double cosPhiSphere = Math.Cos(phiSphere);
        double lambdaSphere = Math.Abs(cosPhiSphere) <= Eps10 ? 0d : (xUnit / cosPhiSphere);

        x = Adjust_lon(this.centralMeridian + lambdaSphere);
        y = phiSphere;
    }
}
