// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;

/// <summary>
/// Shared implementation for the spherical STS projection family (<c>kav5</c>, <c>qua_aut</c>, <c>fouc</c>, <c>mbt_s</c>).
/// </summary>
[Serializable]
internal abstract class StsProjectionBase : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double cX;
    private readonly double cY;
    private readonly double cP;
    private readonly bool tanMode;

    /// <summary>
    /// Initializes a new instance of the <see cref="StsProjectionBase"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    /// <param name="name">Projection name.</param>
    /// <param name="p">Projection family p-constant.</param>
    /// <param name="q">Projection family q-constant.</param>
    /// <param name="tanMode">Indicates whether tan-mode equations are active.</param>
    protected StsProjectionBase(
        IEnumerable<ProjectionParameter> parameters,
        MapProjection? inverse,
        string name,
        double p,
        double q,
        bool tanMode)
        : base(parameters, inverse)
    {
        this.Name = name;
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
        this.cX = q / p;
        this.cY = p;
        this.cP = 1d / q;
        this.tanMode = tanMode;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;

        double xUnit = this.cX * lambda * Math.Cos(phi);
        double yUnit = this.cY;
        phi *= this.cP;
        double c = Math.Cos(phi);
        if (this.tanMode)
        {
            xUnit *= c * c;
            yUnit *= Math.Tan(phi);
        }
        else
        {
            if (Math.Abs(c) <= Eps10)
            {
                ArgumentGuard.ThrowArgument("Input data outside projection domain.");
            }

            xUnit /= c;
            yUnit *= Math.Sin(phi);
        }

        lon = this.radius * xUnit;
        lat = this.radius * yUnit;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = (y * this.inverseRadius) / this.cY;

        double phi = this.tanMode ? Math.Atan(yUnit) : Asinz(yUnit);
        double c = Math.Cos(phi);
        double latitude = phi / this.cP;
        double cosLatitude = Math.Cos(latitude);
        if (Math.Abs(cosLatitude) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lambda = xUnit / (this.cX * cosLatitude);
        if (this.tanMode)
        {
            double cSquared = c * c;
            if (Math.Abs(cSquared) <= Eps10)
            {
                ArgumentGuard.ThrowArgument("Input data outside projection domain.");
            }

            lambda /= cSquared;
        }
        else
        {
            lambda *= c;
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = latitude;
    }
}
