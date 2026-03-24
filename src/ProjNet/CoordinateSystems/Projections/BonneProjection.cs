// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Bonne projection (<c>bonne</c>).
/// </summary>
/// <remarks>
/// A pseudoconical equal-area projection in which all parallels are represented as concentric
/// circular arcs with true spacing, and all meridians are equally spaced along each parallel.
/// Both spherical and ellipsoidal forms are supported. The standard parallel <c>lat_1</c>
/// must be non-zero; at ±90° the projection degenerates to a Werner projection.
/// </remarks>
[Serializable]
internal class BonneProjection : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double standardParallel;
    private readonly double sineStandardParallel;
    private readonly double cotStandardParallel;
    private readonly double meridianDistanceAtStandardParallel;
    private readonly double reducedCosphiOverSinphiAtStandardParallel;
    private readonly bool isEllipsoidal;

    /// <summary>
    /// Initializes a new instance of the <see cref="BonneProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public BonneProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BonneProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public BonneProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Bonne";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
        this.standardParallel = DegreesToRadians(this.Parameters.GetOptionalParameterValue("lat_1", RadiansToDegrees(this.latOrigin), "standard_parallel_1"));

        if (Math.Abs(this.standardParallel) <= Eps10)
        {
            throw new ArgumentException("Invalid value for lat_1: |lat_1| should be > 0.");
        }

        this.sineStandardParallel = Math.Sin(this.standardParallel);
        this.isEllipsoidal = this.es > 0d;

        if (this.isEllipsoidal)
        {
            double cosStandardParallel = Math.Cos(this.standardParallel);
            double denominator = Math.Sqrt(1d - (this.es * this.sineStandardParallel * this.sineStandardParallel)) * this.sineStandardParallel;
            this.reducedCosphiOverSinphiAtStandardParallel = cosStandardParallel / denominator;
            this.meridianDistanceAtStandardParallel = this.Mlfn(this.standardParallel, this.sineStandardParallel, cosStandardParallel);
            this.cotStandardParallel = 0d;
            return;
        }

        this.cotStandardParallel = (Math.Abs(Math.Abs(this.standardParallel) - HalfPi) <= Eps10) ? 0d : (1d / Math.Tan(this.standardParallel));
        this.meridianDistanceAtStandardParallel = 0d;
        this.reducedCosphiOverSinphiAtStandardParallel = 0d;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new BonneProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;

        if (!this.isEllipsoidal)
        {
            double rhoSphere = this.cotStandardParallel + this.standardParallel - phi;
            if (Math.Abs(rhoSphere) <= Eps10)
            {
                lon = 0d;
                lat = 0d;
                return;
            }

            double angularTermSphere = lambda * Math.Cos(phi) / rhoSphere;
            lon = this.radius * rhoSphere * Math.Sin(angularTermSphere);
            lat = this.radius * (this.cotStandardParallel - (rhoSphere * Math.Cos(angularTermSphere)));
            return;
        }

        double sinPhi = Math.Sin(phi);
        double cosPhi = Math.Cos(phi);
        double rho = this.reducedCosphiOverSinphiAtStandardParallel + this.meridianDistanceAtStandardParallel - this.Mlfn(phi, sinPhi, cosPhi);
        if (Math.Abs(rho) <= Eps10)
        {
            lon = 0d;
            lat = 0d;
            return;
        }

        double angularDenominatorEllipsoid = rho * Math.Sqrt(1d - (this.es * sinPhi * sinPhi));
        double angularTermEllipsoid = (cosPhi * lambda) / angularDenominatorEllipsoid;
        lon = this.radius * rho * Math.Sin(angularTermEllipsoid);
        lat = this.radius * (this.reducedCosphiOverSinphiAtStandardParallel - (rho * Math.Cos(angularTermEllipsoid)));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = y * this.inverseRadius;

        if (!this.isEllipsoidal)
        {
            double translatedY = this.cotStandardParallel - yUnit;
            double rhoSphere = Sign(this.standardParallel) * Hypot(xUnit, translatedY);
            double phiSphere = this.cotStandardParallel + this.standardParallel - rhoSphere;
            double absPhiSphere = Math.Abs(phiSphere);
            if (absPhiSphere > HalfPi)
            {
                throw new ArgumentException("Input data outside projection domain.");
            }

            double lambdaSphere;
            if (HalfPi - absPhiSphere <= Eps10)
            {
                lambdaSphere = 0d;
            }
            else
            {
                double scale = rhoSphere / Math.Cos(phiSphere);
                lambdaSphere = this.standardParallel > 0d
                    ? scale * Math.Atan2(xUnit, translatedY)
                    : scale * Math.Atan2(-xUnit, -translatedY);
            }

            x = Adjust_lon(this.centralMeridian + lambdaSphere);
            y = phiSphere;
            return;
        }

        double translatedEllipsoidalY = this.reducedCosphiOverSinphiAtStandardParallel - yUnit;
        double rho = Sign(this.standardParallel) * Hypot(xUnit, translatedEllipsoidalY);
        double phi = this.Inv_mlfn(this.reducedCosphiOverSinphiAtStandardParallel + this.meridianDistanceAtStandardParallel - rho);
        double absPhi = Math.Abs(phi);

        double lambda = 0d;
        if (absPhi < HalfPi)
        {
            double sinPhi = Math.Sin(phi);
            double scale = (rho * Math.Sqrt(1d - (this.es * sinPhi * sinPhi))) / Math.Cos(phi);
            lambda = this.standardParallel > 0d
                ? scale * Math.Atan2(xUnit, translatedEllipsoidalY)
                : scale * Math.Atan2(-xUnit, -translatedEllipsoidalY);
        }
        else if ((absPhi - HalfPi) > Eps10)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
