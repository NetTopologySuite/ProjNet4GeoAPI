// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Represents the documented type.
/// </summary>
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
    public HotineObliqueMercatorProjection(IEnumerable<ProjectionParameter> parameters, HotineObliqueMercatorProjection inverse)
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
        if (Math.Abs(this.latOrigin) < EPSLN)
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
        this.Lon_origin = this.Lon_origin - (Asinz(g * Math.Tan(gama)) / this.bl);

        con = Math.Abs(this.latOrigin);
        if ((con > EPSLN) && (Math.Abs(con - HALFPI) > EPSLN))
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
            throw new ArgumentException("Input data error");
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

            throw new ArgumentException("AuthorityCode");
        }
    }

    /// <inheritdoc/>
    public override MathTransform Inverse()
    {
        if (this.inverse == null)
        {
            this.inverse = new HotineObliqueMercatorProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    // protected override double[] RadiansToMeters(double[] lonlat)
    // {
    //    var lon = lonlat[0];
    //    var lat = lonlat[1];

    // Double us, ul;

    // // Forward equations
    //    // -----------------
    //    var sin_phi = Math.Sin(lat);
    //    var dlon = adjust_lon(lon - lon_origin);
    //    var vl = Math.Sin(_bl * dlon);
    //    if (Math.Abs(Math.Abs(lat) - HALF_PI) > EPSLN)
    //    {
    //        var ts1 = tsfnz(_e, lat, sin_phi);
    //        var q = _el / (Math.Pow(ts1, _bl));
    //        var s = .5 * (q - 1.0 / q);
    //        var t = .5 * (q + 1.0 / q);
    //        ul = (s * _singam - vl * _cosgam) / t;
    //        var con = Math.Cos(_bl * dlon);
    //        if (Math.Abs(con) < .0000001)
    //        {
    //            us = _al * _bl * dlon;
    //        }
    //        else
    //        {
    //            us = _al * Math.Atan((s * _cosgam + vl * _singam) / con) / _bl;
    //            if (con < 0)
    //                us = us + PI * _al / _bl;
    //        }
    //    }
    //    else
    //    {
    //        if (lat >= 0)
    //            ul = _singam;
    //        else
    //            ul = -_singam;
    //        us = _al * lat / _bl;
    //    }
    //    if (Math.Abs(Math.Abs(ul) - 1.0) <= EPSLN)
    //    {
    //        throw new InvalidOperationException("Point projects into infinity");
    //    }

    // var vs = .5 * _al * Math.Log((1.0 - ul) / (1.0 + ul)) / _bl;
    //    if (!NaturalOriginOffsets) us = us - _u;
    //    var x = vs * _cosgrid + us * _singrid;
    //    var y = us * _cosgrid - vs * _singrid;

    // return lonlat.Length == 2
    //        ? new [] {x, y} :
    //          new [] {x, y, lonlat[2]};
    // }

    /// <inheritdoc/>
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double us, ul;

        // Forward equations
        // -----------------
        double sin_phi = Math.Sin(lat);
        double dlon = Adjust_lon(lon - this.Lon_origin);
        double vl = Math.Sin(this.bl * dlon);
        if (Math.Abs(Math.Abs(lat) - HALFPI) > EPSLN)
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
                    us = us + (PI * this.al / this.bl);
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

        if (Math.Abs(Math.Abs(ul) - 1.0) <= EPSLN)
        {
            throw new InvalidOperationException("Point projects into infinity");
        }

        double vs = .5 * this.al * Math.Log((1.0 - ul) / (1.0 + ul)) / this.bl;
        if (!this.NaturalOriginOffsets)
        {
            us = us - this.u;
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
            us = us + this.u;
        }

        double q = Math.Exp(-this.bl * vs / this.al);
        double s = .5 * (q - (1.0 / q));
        double t = .5 * (q + (1.0 / q));
        double vl = Math.Sin(this.bl * us / this.al);
        double ul = ((vl * this.cosgam) + (s * this.singam)) / t;
        if (Math.Abs(Math.Abs(ul) - 1.0) <= EPSLN)
        {
            x = this.Lon_origin;
            y = Sign(ul) * HALFPI;
        }
        else
        {
            double con = 1.0 / this.bl;
            double ts1 = Math.Pow(this.el / Math.Sqrt((1.0 + ul) / (1.0 - ul)), con);
            long flag;
            y = Phi2z(this.e, ts1, out flag);
            con = Math.Cos(this.bl * us / this.al);
            double theta = this.Lon_origin - (Math.Atan2((s * this.cosgam) - (vl * this.singam), con) / this.bl);
            x = Adjust_lon(theta);
        }
    }
}
