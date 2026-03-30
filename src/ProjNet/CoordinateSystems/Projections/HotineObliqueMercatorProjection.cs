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
/// </remarks>
[Serializable]
internal class HotineObliqueMercatorProjection : MapProjection
{
    private readonly double azimuth;
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
    private readonly double sinaz;
    private readonly double cosaz;
    private readonly double u;

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

        this.azimuth = DegreesToRadians(this.Parameters.GetParameterValue("azimuth"));
        double rectifiedGridAngle = DegreesToRadians(this.Parameters.GetParameterValue("rectified_grid_angle"));

        Sincos(this.latOrigin, out this.sinP20, out this.cosP20);
        double con = 1.0 - (this.es * Math.Pow(this.sinP20, 2));
        double com = Math.Sqrt(1.0 - this.es);
        this.bl = Math.Sqrt(1.0 + (this.es * Math.Pow(this.cosP20, 4.0) / (1.0 - this.es)));
        this.al = this.semiMajor * this.bl * this.scaleFactor * com / con;

        double f;
        if (Math.Abs(this.latOrigin) < Epsln)
        {
            // ts = 1.0;
            this.d = 1.0;
            this.el = 1.0;
            f = 1.0;
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
                    f = this.d + Math.Sqrt((this.d * this.d) - 1.0);
                }
                else
                {
                    f = this.d - Math.Sqrt((this.d * this.d) - 1.0);
                }
            }
            else
            {
                f = this.d;
            }

            this.el = f * Math.Pow(ts, this.bl);
        }

        double g = .5 * (f - (1.0 / f));
        double gama = Asinz(Math.Sin(this.azimuth) / this.d);
        this.Lon_origin -= Asinz(g * Math.Tan(gama)) / this.bl;

        con = Math.Abs(this.latOrigin);
        if ((con > Epsln) && (Math.Abs(con - HalfPi) > Epsln))
        {
            Sincos(gama, out this.singam, out this.cosgam);
            Sincos(this.azimuth, out this.sinaz, out this.cosaz);
            if (this.latOrigin >= 0)
            {
                this.u = (this.al / this.bl) * Math.Atan(Math.Sqrt((this.d * this.d) - 1.0) / this.cosaz);
            }
            else
            {
                this.u = -(this.al / this.bl) * Math.Atan(Math.Sqrt((this.d * this.d) - 1.0) / this.cosaz);
            }
        }
        else
        {
            ArgumentGuard.ThrowArgument("Input data error");
        }

        Sincos(rectifiedGridAngle, out this.singrid, out this.cosgrid);
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

            ArgumentGuard.ThrowArgument("AuthorityCode");
            return false;
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
        double us, ul;

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
        }
        else
        {
            if (lat >= 0)
            {
                ul = this.singam;
            }
            else
            {
                ul = -this.singam;
            }

            us = this.al * lat / this.bl;
        }

        if (Math.Abs(Math.Abs(ul) - 1.0) <= Epsln)
        {
            throw new InvalidOperationException("Point projects into infinity");
        }

        double vs = .5 * this.al * Math.Log((1.0 - ul) / (1.0 + ul)) / this.bl;
        if (!this.NaturalOriginOffsets)
        {
            us -= this.u;
        }

        lon = (vs * this.cosgrid) + (us * this.singrid);
        lat = (us * this.cosgrid) - (vs * this.singrid);
    }

    /// <inheritdoc/>
    protected override void MetersToRadians(ref double x, ref double y)
    {
        // Inverse equations
        // -----------------
        double vs = (x * this.cosgrid) - (y * this.singrid);
        double us = (y * this.cosgrid) + (x * this.singrid);
        if (!this.NaturalOriginOffsets)
        {
            us += this.u;
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
