// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Lambert Azimuthal Equal Area projection for spherical and ellipsoidal models.
/// </summary>
public class LambertAzimuthalEqualAreaProjection : MapProjection
{
    /// <summary>
    /// The delegate to perform forward transformation.
    /// </summary>
    private readonly Transformer radiansToMeters;

    /// <summary>
    /// The delegate to perform reverse transformation.
    /// </summary>
    private readonly Transformer metersToRadians;

    private readonly Mode mode;
    private readonly double qp;
    private readonly double oneEs;
    private readonly double[] apa;

    private readonly double dd;
    private readonly double sinb1;
    private readonly double cosb1;
    private readonly double rq;
    private readonly double xmf;
    private readonly double ymf;

    private readonly double reciprocSemiMajorTimesScaleFactor;

    /// <summary>
    /// Initializes a new instance of the <see cref="LambertAzimuthalEqualAreaProjection"/> class.
    /// </summary>
    /// <param name="parameters">List of parameters to initialize the projection.</param>
    public LambertAzimuthalEqualAreaProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LambertAzimuthalEqualAreaProjection"/> class.
    /// </summary>
    /// <param name="parameters">List of parameters to initialize the projection.</param>
    /// <param name="inverse">The inverse projection instance, or <see langword="null"/> for a forward projection.</param>
    public LambertAzimuthalEqualAreaProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Lambert_Azimuthal_Equal_Area";

        double phi0 = this.latOrigin;

        double t = Math.Abs(phi0);
        if (t > HalfPi + Eps10)
        {
            ArgumentGuard.ThrowArgument("Latitude of origin is outside the valid range.", nameof(parameters));
        }

        if (Math.Abs(t - HalfPi) < Eps10)
        {
            this.mode = phi0 < 0.0 ? Mode.S_POLE : Mode.N_POLE;
        }
        else if (Math.Abs(t) < Eps10)
        {
            this.mode = Mode.EQUIT;
        }
        else
        {
            this.mode = Mode.OBLIQ;
        }

        if (this.es != 0d)
        {
            this.oneEs = 1.0 - this.es;
            this.qp = Qsfn(1, this.e, this.oneEs);

            this.apa = Authset(this.es);
            if (this.apa is null)
            {
                ArgumentGuard.ThrowArgument("Failed to initialize authalic coefficients from projection parameters.", nameof(parameters));
            }

            switch (this.mode)
            {
                case Mode.N_POLE:
                case Mode.S_POLE:
                    this.dd = 1.0;
                    break;
                case Mode.EQUIT:
                    this.dd = 1.0 / (this.rq = Math.Sqrt(0.5 * this.qp));
                    this.xmf = 1.0;
                    this.ymf = 0.5 * this.qp;
                    break;
                case Mode.OBLIQ:
                    this.rq = Math.Sqrt(0.5 * this.qp);
                    double sinphi = Math.Sin(phi0);
                    this.sinb1 = Qsfn(sinphi, this.e, this.oneEs) / this.qp;
                    this.cosb1 = Math.Sqrt(1.0 - (this.sinb1 * this.sinb1));
                    this.dd = Math.Cos(phi0) / (Math.Sqrt(1.0 - (this.es * sinphi * sinphi)) * this.rq * this.cosb1);
                    this.ymf = (this.xmf = this.rq) / this.dd;
                    this.xmf *= this.dd;
                    break;
            }

            this.radiansToMeters = this.EllipsoidalRadiansToMeters;
            this.metersToRadians = this.EllipsoidalMetersToRadians;
        }
        else
        {
            if (this.mode == Mode.OBLIQ)
            {
                this.sinb1 = Math.Sin(phi0);
                this.cosb1 = Math.Cos(phi0);
            }

            this.radiansToMeters = this.SphericalRadiansToMeters;
            this.metersToRadians = this.SphericalMetersToRadians;
        }

        this.reciprocSemiMajorTimesScaleFactor = 1d / (this.scaleFactor * this.semiMajor);
    }

    /// <summary>
    /// A function to perform the actual transformation.
    /// </summary>
    /// <param name="o1">The horizontal ordinate.</param>
    /// <param name="o2">The vertical ordinate.</param>
    private delegate void Transformer(ref double o1, ref double o2);

    /// <summary>
    /// An enumeration of modes.
    /// </summary>
    private enum Mode
    {
        /// <summary>
        /// North pole.
        /// </summary>
        N_POLE,

        /// <summary>
        /// South pole.
        /// </summary>
        S_POLE,

        /// <summary>
        /// Equatorial.
        /// </summary>
        EQUIT,

        /// <summary>
        /// Oblique.
        /// </summary>
        OBLIQ,
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new LambertAzimuthalEqualAreaProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        this.radiansToMeters(ref lon, ref lat);
        lon *= this.scaleFactor * this.semiMajor;
        lat *= this.scaleFactor * this.semiMajor;
    }

    private void EllipsoidalRadiansToMeters(ref double lon, ref double lat)
    {
        double sinb = 0.0, cosb = 0.0, b = 0.0;

        double lam = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;

        double coslam = Math.Cos(lam);
        double sinlam = Math.Sin(lam);
        double sinphi = Math.Sin(phi);
        double q = Qsfn(sinphi, this.e, this.oneEs);

        if (this.mode == Mode.OBLIQ || this.mode == Mode.EQUIT)
        {
            sinb = q / this.qp;
            cosb = Math.Sqrt(1.0 - (sinb * sinb));
        }

        switch (this.mode)
        {
            case Mode.OBLIQ:
                b = 1.0 + (this.sinb1 * sinb) + (this.cosb1 * cosb * coslam);
                break;
            case Mode.EQUIT:
                b = 1.0 + (cosb * coslam);
                break;
            case Mode.N_POLE:
                b = HalfPi + phi;
                q = this.qp - q;
                break;
            case Mode.S_POLE:
                b = phi - HalfPi;
                q = this.qp + q;
                break;
        }

        double x = HugeVal;
        double y = HugeVal;
        if (Math.Abs(b) < Eps10)
        {
            // proj_errno_set(P, PJD_ERR_TOLERANCE_CONDITION);
            return;
        }

        switch (this.mode)
        {
            case Mode.OBLIQ:
                b = Math.Sqrt(2.0 / b);
                y = this.ymf * b * ((this.cosb1 * sinb) - (this.sinb1 * cosb * coslam));
                goto eqcon;
            case Mode.EQUIT:
                b = Math.Sqrt(2.0 / (1.0 + (cosb * coslam)));
                y = b * sinb * this.ymf;
            eqcon:
                x = this.xmf * b * cosb * sinlam;
                break;
            case Mode.N_POLE:
            case Mode.S_POLE:
                if (q >= 1e-15)
                {
                    b = Math.Sqrt(q);
                    x = b * sinlam;
                    y = coslam * (this.mode == Mode.S_POLE ? b : -b);
                }
                else
                {
                    x = y = 0.0;
                }

                break;
        }

        lon = x;
        lat = y;
    }

    private void SphericalRadiansToMeters(ref double lon, ref double lat)
    {
        double lam = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;

        double sinphi = Math.Sin(phi);
        double cosphi = Math.Cos(phi);
        double coslam = Math.Sin(lam);

        double x = HugeVal;
        double y = HugeVal;

        switch (this.mode)
        {
            case Mode.EQUIT:
                y = 1.0 + (cosphi * coslam);
                goto oblcon;
            case Mode.OBLIQ:
                y = 1.0 + (this.sinb1 * sinphi) + (this.cosb1 * cosphi * coslam);
            oblcon:
                if (y <= Eps10)
                {
                    // proj_errno_set(P, PJD_ERR_TOLERANCE_CONDITION);
                    return;
                }

                y = Math.Sqrt(2.0 / y);
                x = y * cosphi * Math.Sin(lam);
                y *= this.mode == Mode.EQUIT ? sinphi :
                    (this.cosb1 * sinphi) - (this.sinb1 * cosphi * coslam);
                break;
            case Mode.N_POLE:
                coslam = -coslam;
                goto continue_S_POLE;

            // -fallthrough
            case Mode.S_POLE:
            continue_S_POLE:
                if (Math.Abs(phi + this.latOrigin) < Eps10)
                {
                    // proj_errno_set(P, PJD_ERR_TOLERANCE_CONDITION);
                    return;
                }

                y = FortPi - (phi * 0.5);
                y = 2.0 * (this.mode == Mode.S_POLE ? Math.Cos(y) : Math.Sin(y));
                x = y * Math.Sin(lam);
                y *= coslam;
                break;
        }

        lon = x;
        lat = y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        x *= this.reciprocSemiMajorTimesScaleFactor;
        y *= this.reciprocSemiMajorTimesScaleFactor;

        this.metersToRadians(ref x, ref y);
    }

    private void EllipsoidalMetersToRadians(ref double x, ref double y)
    {
        double cCe, sCe, q, rho, ab = 0.0;

        switch (this.mode)
        {
            case Mode.EQUIT:
            case Mode.OBLIQ:
                x /= this.dd;
                y *= this.dd;
                rho = Hypot(x, y);
                if (rho < Eps10)
                {
                    x = this.centralMeridian; // lam
                    y = this.latOrigin; // phi
                    return;
                }

                sCe = 2.0 * Math.Asin(0.5 * rho / this.rq);
                cCe = Math.Cos(sCe);
                sCe = Math.Sin(sCe);
                x *= sCe;
                if (this.mode == Mode.OBLIQ)
                {
                    ab = (cCe * this.sinb1) + (y * sCe * this.cosb1 / rho);
                    y = (rho * this.cosb1 * cCe) - (y * this.sinb1 * sCe);
                }
                else
                {
                    ab = y * sCe / rho;
                    y = rho * cCe;
                }

                break;
            case Mode.N_POLE:
                y = -y;
                goto continue_S_POLE;

            // -fallthrough
            case Mode.S_POLE:
            continue_S_POLE:
                q = (x * x) + (y * y);
                if (q == 0.0)
                {
                    x = this.centralMeridian;          // lam
                    y = this.latOrigin;   // phi
                    return;
                }

                ab = 1.0 - (q / this.qp);
                if (this.mode == Mode.S_POLE)
                {
                    ab = -ab;
                }

                break;
        }

        x = x = Adjust_lon(Math.Atan2(x, y) + this.centralMeridian); // lam
        y = Authlat(Math.Asin(ab), this.apa);                      // phi
    }

    private void SphericalMetersToRadians(ref double x, ref double y)
    {
        double cosz = 0.0, rh, sinz = 0.0;

        rh = Hypot(x, y);
        double phi = rh * .5;
        if (phi > 1.0)
        {
            x = 0; // lam
            y = 0; // phi
            return;
        }

        phi = 2.0 * Math.Asin(phi);
        if (this.mode == Mode.OBLIQ || this.mode == Mode.EQUIT)
        {
            sinz = Math.Sin(phi);
            cosz = Math.Cos(phi);
        }

        switch (this.mode)
        {
            case Mode.EQUIT:
                phi = Math.Abs(rh) <= Eps10 ? 0.0 : Math.Asin(y * sinz / rh);
                x *= sinz;
                y = cosz * rh;
                break;
            case Mode.OBLIQ:
                phi = Math.Abs(rh) <= Eps10 ? this.latOrigin :
                    Math.Asin((cosz * this.sinb1) + (y * sinz * this.cosb1 / rh));
                x *= sinz * this.cosb1;
                y = (cosz - (Math.Sin(phi) * this.sinb1)) * rh;
                break;
            case Mode.N_POLE:
                y = -y;
                phi = HalfPi - phi;
                break;
            case Mode.S_POLE:
                phi -= HalfPi;
                break;
        }

        double lam = (y == 0.0 && (this.mode == Mode.EQUIT || this.mode == Mode.OBLIQ)) ?
            0.0 : Math.Atan2(x, y);

        x = Adjust_lon(lam + this.centralMeridian);
        y = phi;
    }
}
