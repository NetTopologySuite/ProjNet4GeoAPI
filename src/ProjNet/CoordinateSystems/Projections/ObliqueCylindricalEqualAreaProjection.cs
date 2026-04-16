// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Oblique Cylindrical Equal Area projection (<c>ocea</c>).
/// </summary>
/// <remarks>
/// The oblique pole can be defined either by an azimuth angle (<c>α</c> or <c>azimuth</c>
/// together with <c>lonc</c>) or by two geographic points via <c>lat_1</c>, <c>lon_1</c>,
/// <c>lat_2</c>, and <c>lon_2</c>.
/// <para>The oblique equal-area construction was independently verified against Snyder's
/// oblique cylindrical equal-area formulation. The implementation matches both parameter
/// initialization paths for the oblique pole and the final equal-area forward/inverse
/// relations in the rotated coordinate system.</para>
/// </remarks>
internal sealed class ObliqueCylindricalEqualAreaProjection : MapProjection
{
    private readonly double rok;
    private readonly double rtk;
    private readonly double sinPhiP;
    private readonly double cosPhiP;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObliqueCylindricalEqualAreaProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public ObliqueCylindricalEqualAreaProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObliqueCylindricalEqualAreaProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public ObliqueCylindricalEqualAreaProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Oblique_Cylindrical_Equal_Area";
        this.rtk = this.scaleFactor;
        this.rok = 1d / this.scaleFactor;

        double lamP = 0d;
        double phiP = 0d;
        if (this.Parameters.ContainsKey("alpha") || this.Parameters.ContainsKey("azimuth"))
        {
            double alpha = PI + DegreesToRadians(this.Parameters.GetOptionalParameterValue("alpha", this.Parameters.GetOptionalParameterValue("azimuth", 0d)));
            double lonc = DegreesToRadians(this.Parameters.GetOptionalParameterValue("lonc", this.Parameters.GetOptionalParameterValue("longitude_of_center", 0d)));
            Sincos(this.latOrigin, out double sinLatitudeOrigin, out double cosLatitudeOrigin);
            lamP = Math.Atan2(-Math.Cos(alpha), -sinLatitudeOrigin * Math.Sin(alpha)) + lonc;
            phiP = Asinz(cosLatitudeOrigin * Math.Sin(alpha));
        }
        else
        {
            double phi1 = DegreesToRadians(this.Parameters.GetParameterValue("lat_1", "standard_parallel_1"));
            double phi2 = DegreesToRadians(this.Parameters.GetParameterValue("lat_2", "standard_parallel_2"));
            double lam1 = DegreesToRadians(this.Parameters.GetOptionalParameterValue("lon_1", 0d));
            double lam2 = DegreesToRadians(this.Parameters.GetOptionalParameterValue("lon_2", 0d));

            lamP = Math.Atan2(
                (Math.Cos(phi1) * Math.Sin(phi2) * Math.Cos(lam1)) - (Math.Sin(phi1) * Math.Cos(phi2) * Math.Cos(lam2)),
                (Math.Sin(phi1) * Math.Cos(phi2) * Math.Sin(lam2)) - (Math.Cos(phi1) * Math.Sin(phi2) * Math.Sin(lam1)));
            if (Math.Abs(lam1 + MapProjection.HalfPi) <= Eps10)
            {
                lamP = -lamP;
            }

            double cosLamDiff = Math.Cos(lamP - lam1);
            double tanPhi1 = Math.Tan(phi1);
            phiP = Math.Abs(tanPhi1) <= Eps10
                ? (cosLamDiff >= 0d ? -MapProjection.HalfPi : MapProjection.HalfPi)
                : Math.Atan(-cosLamDiff / tanPhi1);
        }

        this.centralMeridian = Adjust_lon(lamP + MapProjection.HalfPi);
        this.sinPhiP = Math.Sin(phiP);
        this.cosPhiP = Math.Cos(phiP);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new ObliqueCylindricalEqualAreaProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double sinLam = Math.Sin(lambda);
        double cosLam = Math.Cos(lambda);
        double tanPhi = Math.Tan(lat);

        double xUnit = Math.Atan(((tanPhi * this.cosPhiP) + (this.sinPhiP * sinLam)) / cosLam);
        if (cosLam < 0d)
        {
            xUnit += PI;
        }

        xUnit *= this.rtk;
        double yUnit = this.rok * ((this.sinPhiP * Math.Sin(lat)) - (this.cosPhiP * Math.Cos(lat) * sinLam));

        lon = this.SphericalRadius * xUnit;
        lat = this.SphericalRadius * yUnit;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = (x * this.InverseSphericalRadius) / this.rtk;
        double yUnit = (y * this.InverseSphericalRadius) / this.rok;
        double t = Math.Sqrt(Math.Max(0d, 1d - (yUnit * yUnit)));
        double s = Math.Sin(xUnit);
        double phi = Asinz((yUnit * this.sinPhiP) + (t * this.cosPhiP * s));
        double lambda = Math.Atan2((t * this.sinPhiP * s) - (yUnit * this.cosPhiP), t * Math.Cos(xUnit));

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
