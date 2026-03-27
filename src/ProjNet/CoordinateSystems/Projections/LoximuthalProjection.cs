// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Loximuthal projection (<c>loxim</c>).
/// </summary>
/// <remarks>
/// The Loximuthal projection preserves the shape of loxodromes (rhumb lines) as straight lines
/// emanating from a user-defined reference latitude (<c>lat_1</c>). The reference latitude must
/// not be at the poles.
/// </remarks>
[Serializable]
internal class LoximuthalProjection : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double referenceLatitude;
    private readonly double referenceMercatorTerm;
    private readonly double cosReferenceLatitude;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoximuthalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public LoximuthalProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoximuthalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public LoximuthalProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Loximuthal";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        this.referenceLatitude = DegreesToRadians(this.Parameters.GetOptionalParameterValue("lat_1", RadiansToDegrees(this.latOrigin), "latitude_of_origin"));
        this.cosReferenceLatitude = Math.Cos(this.referenceLatitude);
        if (Math.Abs(Math.Abs(this.referenceLatitude) - HalfPi) <= Epsln)
        {
            ArgumentGuard.ThrowArgument("The reference latitude cannot be at the poles.");
        }

        this.referenceMercatorTerm = Math.Log(Math.Tan(FortPi + (0.5d * this.referenceLatitude)));
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new LoximuthalProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;
        double deltaPhi = phi - this.referenceLatitude;
        lat = this.radius * deltaPhi;

        if (Math.Abs(deltaPhi) <= Eps10)
        {
            lon = this.radius * lambda * this.cosReferenceLatitude;
            return;
        }

        double mercatorTerm = Math.Log(Math.Tan(FortPi + (0.5d * phi)));
        double denominator = mercatorTerm - this.referenceMercatorTerm;
        if (Math.Abs(denominator) <= Eps10)
        {
            lon = this.radius * lambda * this.cosReferenceLatitude;
            return;
        }

        lon = this.radius * lambda * deltaPhi / denominator;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double lat = this.referenceLatitude + (y * this.inverseRadius);
        double deltaPhi = lat - this.referenceLatitude;

        double lambda;
        if (Math.Abs(deltaPhi) <= Eps10)
        {
            lambda = x * this.inverseRadius / this.cosReferenceLatitude;
        }
        else
        {
            double mercatorTerm = Math.Log(Math.Tan(FortPi + (0.5d * lat)));
            double numerator = mercatorTerm - this.referenceMercatorTerm;
            if (Math.Abs(numerator) <= Eps10)
            {
                lambda = x * this.inverseRadius / this.cosReferenceLatitude;
            }
            else
            {
                lambda = (x * this.inverseRadius) * numerator / deltaPhi;
            }
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = lat;
    }
}
