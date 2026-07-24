// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Hotine Oblique Mercator map projection (EPSG method 9812).
/// </summary>
/// <remarks>
/// <para>The Hotine Oblique Mercator projects a region along a central oblique line
/// defined by an azimuth at the projection centre. The cylinder axis is tilted with
/// respect to the Earth's axis, making it suitable for regions with a predominant
/// oblique extent. It is a conformal projection. False easting and northing are
/// applied relative to the centre of the initial line.</para>
/// <para>The formulation was independently verified against IOGP, "Geomatics Guidance
/// Note 7, part 2: Coordinate Conversions and Transformations including Formulas"
/// (publication 373-7-2, 2019), EPSG method 9812, Hotine Oblique Mercator (variant A).
/// The <c>u0</c> offset at the intersection of the central line and aposphere equator
/// and the rectified-skew rotation by <c>γ</c> match the implementation here.</para>
/// </remarks>
/// <seealso href="https://epsg.io/9812-method">EPSG method 9812: Hotine Oblique Mercator (variant A).</seealso>
/// <seealso>Bugayevskiy &amp; Snyder (1995), "Map Projections: A Reference Manual", Ch. 2, Sect. 2.1.7, pp. 63-66.</seealso>
internal class HotineObliqueMercatorProjection : MapProjection
{
    private readonly bool noRotation;
    private readonly double sinP20;
    private readonly double cosP20;
    private readonly double bl;
    private readonly double al;
    private readonly double d;
    private readonly double el;
    private readonly double singrid;
    private readonly double cosgrid;
    private readonly double singam;
    private readonly double cosgam;
    private readonly double u;
    private readonly double vPoleNorth;
    private readonly double vPoleSouth;

    /// <summary>
    /// Initializes a new instance of the <see cref="HotineObliqueMercatorProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public HotineObliqueMercatorProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HotineObliqueMercatorProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public HotineObliqueMercatorProjection(IEnumerable<ProjectionParameter> parameters, HotineObliqueMercatorProjection? inverse)
        : base(parameters, inverse)
    {
        this.Authority = "EPSG";
        this.AuthorityCode = 9812;
        this.Name = "Hotine_Oblique_Mercator";
        this.noRotation = this.Parameters.ContainsKey("no_rot");

        double alpha = 0d;
        double gamma0;
        double rotationAngle;
        Sincos(this.latOrigin, out this.sinP20, out this.cosP20);
        double con = 1.0 - (this.es * Math.Pow(this.sinP20, 2));
        double com = Math.Sqrt(1.0 - this.es);
        this.bl = Math.Sqrt(1.0 + (this.es * Math.Pow(this.cosP20, 4.0) / (1.0 - this.es)));
        this.al = this.semiMajor * this.bl * this.scaleFactor * com / con;

        double fValue = 1.0;
        if (Math.Abs(this.latOrigin) < Epsln)
        {
            this.d = 1.0;
            this.el = 1.0;
        }
        else
        {
            double ts = Tsfnz(this.e, this.latOrigin, this.sinP20);
            con = Math.Sqrt(con);
            this.d = this.bl * com / (this.cosP20 * con);
            if (((this.d * this.d) - 1.0) > 0.0)
            {
                if (this.latOrigin >= 0.0)
                {
                    fValue = this.d + Math.Sqrt((this.d * this.d) - 1.0);
                }
                else
                {
                    fValue = this.d - Math.Sqrt((this.d * this.d) - 1.0);
                }
            }
            else
            {
                fValue = this.d;
            }

            this.el = fValue * Math.Pow(ts, this.bl);
        }

        bool hasAzimuth = this.Parameters.ContainsKey("alpha") || this.Parameters.ContainsKey("azimuth");
        bool hasRotationAngle = this.Parameters.ContainsKey("gamma") || this.Parameters.ContainsKey("rectified_grid_angle");
        if (hasAzimuth || hasRotationAngle)
        {
            alpha = DegreesToRadians(this.Parameters.GetOptionalParameterValue("alpha", this.Parameters.GetOptionalParameterValue("azimuth", 0d)));
            rotationAngle = DegreesToRadians(this.Parameters.GetOptionalParameterValue("gamma", this.Parameters.GetOptionalParameterValue("rectified_grid_angle", RadiansToDegrees(alpha))));
            if (Math.Abs(Math.Abs(this.latOrigin) - HalfPi) <= Eps7)
            {
                ArgumentGuard.ThrowArgument("Invalid value for lat_0: |lat_0| should be < 90°", nameof(parameters));
            }

            if (hasAzimuth)
            {
                gamma0 = Asinz(Math.Sin(alpha) / this.d);
                if (!hasRotationAngle)
                {
                    rotationAngle = alpha;
                }
            }
            else
            {
                gamma0 = rotationAngle;
                alpha = Asinz(this.d * Math.Sin(gamma0));
            }

            double g = 0.5 * (fValue - (1.0 / fValue));
            this.Lon_origin -= Asinz(g * Math.Tan(gamma0)) / this.bl;
        }
        else
        {
            double phi1 = DegreesToRadians(this.Parameters.GetParameterValue("lat_1", "standard_parallel_1"));
            double phi2 = DegreesToRadians(this.Parameters.GetParameterValue("lat_2", "standard_parallel_2"));
            double lam1 = DegreesToRadians(this.Parameters.GetOptionalParameterValue("lon_1", 0d));
            double lam2 = DegreesToRadians(this.Parameters.GetOptionalParameterValue("lon_2", 0d));

            if (Math.Abs(phi1) > HalfPi - Eps7)
            {
                ArgumentGuard.ThrowArgument("Invalid value for lat_1: |lat_1| should be < 90°", nameof(parameters));
            }

            if (Math.Abs(phi2) > HalfPi - Eps7)
            {
                ArgumentGuard.ThrowArgument("Invalid value for lat_2: |lat_2| should be < 90°", nameof(parameters));
            }

            if (Math.Abs(phi1 - phi2) <= Eps7)
            {
                ArgumentGuard.ThrowArgument("Invalid value for lat_1/lat_2: lat_1 should be different from lat_2", nameof(parameters));
            }

            if (Math.Abs(phi1) <= Eps7)
            {
                ArgumentGuard.ThrowArgument("Invalid value for lat_1: lat_1 should be different from 0", nameof(parameters));
            }

            if (Math.Abs(Math.Abs(this.latOrigin) - HalfPi) <= Eps7)
            {
                ArgumentGuard.ThrowArgument("Invalid value for lat_0: |lat_0| should be < 90°", nameof(parameters));
            }

            double h = Math.Pow(Tsfnz(this.e, phi1, Math.Sin(phi1)), this.bl);
            double l = Math.Pow(Tsfnz(this.e, phi2, Math.Sin(phi2)), this.bl);
            double f = this.el / h;
            double p = (l - h) / (l + h);
            if (Math.Abs(p) <= Epsln)
            {
                ArgumentGuard.ThrowArgument("Invalid value for eccentricity", nameof(parameters));
            }

            double j = this.el * this.el;
            j = (j - (l * h)) / (j + (l * h));
            double lamDifference = lam1 - lam2;
            if (lamDifference < -PI)
            {
                lam2 -= TwoPi;
            }
            else if (lamDifference > PI)
            {
                lam2 += TwoPi;
            }

            this.Lon_origin = Adjust_lon((0.5 * (lam1 + lam2)) - (Math.Atan(j * Math.Tan(0.5 * this.bl * (lam1 - lam2)) / p) / this.bl));
            double denominator = f - (1.0 / f);
            if (Math.Abs(denominator) <= Epsln)
            {
                ArgumentGuard.ThrowArgument("Invalid value for eccentricity", nameof(parameters));
            }

            gamma0 = Math.Atan(2.0 * Math.Sin(this.bl * Adjust_lon(lam1 - this.Lon_origin)) / denominator);
            rotationAngle = alpha = Asinz(this.d * Math.Sin(gamma0));
        }

        Sincos(gamma0, out this.singam, out this.cosgam);
        Sincos(rotationAngle, out this.singrid, out this.cosgrid);
        double arB = this.al / this.bl;
        if (!this.NaturalOriginOffsets)
        {
            this.u = Math.Abs(arB * Math.Atan(Math.Sqrt(Math.Max(0d, (this.d * this.d) - 1.0)) / Math.Cos(alpha)));
            if (this.latOrigin < 0.0)
            {
                this.u = -this.u;
            }
        }
        else
        {
            this.u = 0d;
        }

        double halfGamma = 0.5 * gamma0;
        this.vPoleNorth = arB * Math.Log(Math.Tan(FortPi - halfGamma));
        this.vPoleSouth = arB * Math.Log(Math.Tan(FortPi + halfGamma));
    }

    private bool NaturalOriginOffsets
    {
        get
        {
            if (this.AuthorityCode == 9812)
            {
                return false;
            }

            if (this.AuthorityCode == 9815)
            {
                return true;
            }

            return ProjectionThrowHelper.ThrowInvalidOperation<bool>($"Unexpected Hotine Oblique Mercator authority code {this.AuthorityCode}.");
        }
    }

    /// <inheritdoc/>
    public override MathTransform Inverse()
    {
        this.inverse ??= new HotineObliqueMercatorProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc/>
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double us;
        double ul;
        double vs;

        // Forward equations
        // -----------------
        double sin_phi = Math.Sin(lat);
        double dlon = Adjust_lon(lon - this.Lon_origin);
        double vl = Math.Sin(this.bl * dlon);
        if (Math.Abs(Math.Abs(lat) - HalfPi) > Epsln)
        {
            double ts1 = Tsfnz(this.e, lat, sin_phi);
            double q = this.el / Math.Pow(ts1, this.bl);
            double s = .5 * (q - (1.0 / q));
            double t = .5 * (q + (1.0 / q));
            ul = ((s * this.singam) - (vl * this.cosgam)) / t;
            double con = Math.Cos(this.bl * dlon);
            if (Math.Abs(con) < .0000001)
            {
                us = this.al * this.bl * dlon;
            }
            else
            {
                us = this.al * Math.Atan(((s * this.cosgam) + (vl * this.singam)) / con) / this.bl;
                if (con < 0)
                {
                    us += PI * this.al / this.bl;
                }
            }

            vs = .5 * this.al * Math.Log((1.0 - ul) / (1.0 + ul)) / this.bl;
        }
        else
        {
            if (lat >= 0)
            {
                ul = this.singam;
                vs = this.vPoleNorth;
            }
            else
            {
                ul = -this.singam;
                vs = this.vPoleSouth;
            }

            us = this.al * lat / this.bl;
        }

        if (Math.Abs(Math.Abs(ul) - 1.0) <= Epsln)
        {
            throw new InvalidOperationException("Point projects into infinity");
        }

        if (this.noRotation)
        {
            lon = us;
            lat = vs;
        }
        else
        {
            if (!this.NaturalOriginOffsets)
            {
                us -= this.u;
            }

            lon = (vs * this.cosgrid) + (us * this.singrid);
            lat = (us * this.cosgrid) - (vs * this.singrid);
        }
    }

    /// <inheritdoc/>
    protected override void MetersToRadians(ref double x, ref double y)
    {
        // Inverse equations
        // -----------------
        double vs;
        double us;
        if (this.noRotation)
        {
            vs = y;
            us = x;
        }
        else
        {
            vs = (x * this.cosgrid) - (y * this.singrid);
            us = (y * this.cosgrid) + (x * this.singrid);
            if (!this.NaturalOriginOffsets)
            {
                us += this.u;
            }
        }

        double q = Math.Exp(-this.bl * vs / this.al);
        double s = .5 * (q - (1.0 / q));
        double t = .5 * (q + (1.0 / q));
        double vl = Math.Sin(this.bl * us / this.al);
        double ul = ((vl * this.cosgam) + (s * this.singam)) / t;
        if (Math.Abs(Math.Abs(ul) - 1.0) <= Epsln)
        {
            x = this.Lon_origin;
            y = Sign(ul) * HalfPi;
        }
        else
        {
            double con = 1.0 / this.bl;
            double ts1 = Math.Pow(this.el / Math.Sqrt((1.0 + ul) / (1.0 - ul)), con);
            y = Phi2z(this.e, ts1, out _);
            con = Math.Cos(this.bl * us / this.al);
            double theta = this.Lon_origin - (Math.Atan2((s * this.cosgam) - (vl * this.singam), con) / this.bl);
            x = Adjust_lon(theta);
        }
    }
}
