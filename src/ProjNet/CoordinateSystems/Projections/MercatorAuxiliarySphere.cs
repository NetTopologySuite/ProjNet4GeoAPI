// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Mercator Auxiliary Sphere projection (Web Mercator, EPSG:3857).
/// </summary>
/// <remarks>
/// Applies a spherical Mercator formula using the semi-major axis as the sphere radius,
/// without ellipsoidal correction. This is the projection used by most web mapping services.
/// </remarks>
[Serializable]
internal class MercatorAuxiliarySphere : MapProjection
{
    // Scale factor – for the spherical (auxiliary) Mercator this is 1.
    private const double k0 = 1.0;

    /// <summary>
    /// Initializes a new instance of the <see cref="MercatorAuxiliarySphere"/> class.
    /// </summary>
    /// <param name="parameters">List of projection parameters.</param>
    public MercatorAuxiliarySphere(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MercatorAuxiliarySphere"/> class.
    /// </summary>
    /// <param name="parameters">List of projection parameters.</param>
    /// <param name="isInverse">Inverse transform instance when cloning.</param>
    protected MercatorAuxiliarySphere(IEnumerable<ProjectionParameter> parameters, MercatorAuxiliarySphere isInverse)
        : base(parameters, isInverse)
    {
        this.Authority = "EPSG";
        this.Name = "Mercator_Auxiliary_Sphere";
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        if (double.IsNaN(lon) || double.IsNaN(lat))
        {
            lon = double.NaN;
            lat = double.NaN;
            return;
        }

        double dLon = lon;
        double dLat = lat;

        if (Math.Abs(Math.Abs(dLat) - HalfPi) <= Epsln)
        {
            ArgumentGuard.ThrowArgument("Transformation cannot be computed at the poles.");
        }

        // Forward equations for the Spherical (Auxiliary) Mercator Projection:
        // X = semiMajor * k0 * (lon - central_meridian)
        // Y = semiMajor * k0 * ln( tan(PI/4 + lat/2) )
        lon = this.semiMajor * k0 * (dLon - this.centralMeridian);
        lat = this.semiMajor * k0 * Math.Log(Math.Tan((PI * 0.25) + (dLat * 0.5)));

        // Note: false_easting and false_northing can be added here if necessary.
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double dX = x;
        double dY = y;

        // Inverse equations:
        // lon = central_meridian + X / (semiMajor * k0)
        // lat = PI/2 - 2 * atan( exp( -Y / (semiMajor * k0) ) )
        double ts = Math.Exp(-dY / (this.semiMajor * k0));
        double dLat = HalfPi - (2 * Math.Atan(ts));
        double dLon = this.centralMeridian + (dX / (this.semiMajor * k0));

        x = dLon;
        y = dLat;

        // Note: false_easting/false_northing can be subtracted here if provided in the parameter list.
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new MercatorAuxiliarySphere(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }
}
