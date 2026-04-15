// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the near-sided and tilted perspective projections (<c>nsper</c>, <c>tpers</c>).
/// </summary>
/// <remarks>
/// NearSidedPerspectiveProjection implements Snyder's near-sided perspective family. The
/// shared code supports polar, equatorial, and oblique viewpoints and optionally applies
/// the additional tilt rotation used by the tilted-perspective variant.
/// </remarks>
/// <seealso>Bugayevskiy &amp; Snyder (1995), "Map Projections: A Reference Manual", Ch. 3, Sect. 3.3.2, pp. 116-121.</seealso>
internal sealed class NearSidedPerspectiveProjection : MapProjection
{
    private readonly double sinph0;
    private readonly double cosph0;
    private readonly double p;
    private readonly double rp;
    private readonly double pn1;
    private readonly double pfact;
    private readonly double h;
    private readonly double cg;
    private readonly double sg;
    private readonly double sw;
    private readonly double cw;
    private readonly Mode mode;
    private readonly bool tilt;

    /// <summary>
    /// Initializes a new instance of the <see cref="NearSidedPerspectiveProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public NearSidedPerspectiveProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NearSidedPerspectiveProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public NearSidedPerspectiveProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        double height = this.Parameters.GetParameterValue("h", "satellite_height");
        this.pn1 = height / this.semiMajor;
        if (this.pn1 <= 0d || this.pn1 > 1e10d)
        {
            ArgumentGuard.ThrowArgument("Invalid value for h.");
        }

        this.p = 1d + this.pn1;
        this.rp = 1d / this.p;
        this.h = 1d / this.pn1;
        this.pfact = (this.p + 1d) * this.h;

        if (Math.Abs(Math.Abs(this.latOrigin) - HalfPi) < Eps10)
        {
            this.mode = this.latOrigin < 0d ? Mode.SPole : Mode.NPole;
        }
        else if (Math.Abs(this.latOrigin) < Eps10)
        {
            this.mode = Mode.Equit;
        }
        else
        {
            this.mode = Mode.Obliq;
            Sincos(this.latOrigin, out this.sinph0, out this.cosph0);
        }

        bool hasTilt = this.Parameters.ContainsKey("tilt");
        bool hasAzi = this.Parameters.ContainsKey("azi") || this.Parameters.ContainsKey("azimuth");
        this.tilt = hasTilt || hasAzi;
        if (this.tilt)
        {
            double omega = DegreesToRadians(this.Parameters.GetOptionalParameterValue("tilt", 0d));
            double gamma = DegreesToRadians(this.Parameters.GetOptionalParameterValue("azi", this.Parameters.GetOptionalParameterValue("azimuth", 0d)));
            this.cg = Math.Cos(gamma);
            this.sg = Math.Sin(gamma);
            this.cw = Math.Cos(omega);
            this.sw = Math.Sin(omega);
            this.Name = "Tilted_Perspective";
        }
        else
        {
            this.Name = "Near_Sided_Perspective";
        }
    }

    private enum Mode
    {
        NPole = 0,
        SPole = 1,
        Equit = 2,
        Obliq = 3,
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new NearSidedPerspectiveProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double sinPhi = Math.Sin(lat);
        double cosPhi = Math.Cos(lat);
        double cosLam = Math.Cos(lambda);

        double yValue = this.mode switch
        {
            Mode.Obliq => (this.sinph0 * sinPhi) + (this.cosph0 * cosPhi * cosLam),
            Mode.Equit => cosPhi * cosLam,
            Mode.SPole => -sinPhi,
            _ => sinPhi,
        };

        if (yValue < this.rp)
        {
            throw new System.InvalidOperationException("Input data outside projection domain.");
        }

        yValue = this.pn1 / (this.p - yValue);
        double xValue = yValue * cosPhi * Math.Sin(lambda);

        yValue *= this.mode switch
        {
            Mode.Obliq => (this.cosph0 * sinPhi) - (this.sinph0 * cosPhi * cosLam),
            Mode.Equit => sinPhi,
            Mode.NPole => -cosPhi * cosLam,
            _ => cosPhi * cosLam,
        };

        if (this.tilt)
        {
            double yt = (yValue * this.cg) + (xValue * this.sg);
            double ba = 1d / ((yt * this.sw * this.h) + this.cw);
            xValue = ((xValue * this.cg) - (yValue * this.sg)) * this.cw * ba;
            yValue = yt * ba;
        }

        lon = this.SphericalRadius * xValue;
        lat = this.SphericalRadius * yValue;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xValue = x * this.InverseSphericalRadius;
        double yValue = y * this.InverseSphericalRadius;

        if (this.tilt)
        {
            double yt = 1d / (this.pn1 - (yValue * this.sw));
            double bm = this.pn1 * xValue * yt;
            double bq = this.pn1 * yValue * this.cw * yt;
            xValue = (bm * this.cg) + (bq * this.sg);
            yValue = (bq * this.cg) - (bm * this.sg);
        }

        double rh = Hypot(xValue, yValue);
        double lambda = 0d;
        double phi = this.latOrigin;
        if (Math.Abs(rh) > Eps10)
        {
            double sinz = 1d - ((rh * rh) * this.pfact);
            if (sinz < 0d)
            {
                throw new System.InvalidOperationException("Input data outside projection domain.");
            }

            sinz = (this.p - Math.Sqrt(sinz)) / ((this.pn1 / rh) + (rh / this.pn1));
            if (Math.Abs(sinz) > 1d + Eps10)
            {
                throw new System.InvalidOperationException("Input data outside projection domain.");
            }

            sinz = Math.Max(-1d, Math.Min(1d, sinz));
            double cosz = Math.Sqrt(Math.Max(0d, 1d - (sinz * sinz)));

            switch (this.mode)
            {
                case Mode.Obliq:
                    phi = Asinz((cosz * this.sinph0) + ((yValue * sinz * this.cosph0) / rh));
                    yValue = (cosz - (this.sinph0 * Math.Sin(phi))) * rh;
                    xValue *= sinz * this.cosph0;
                    break;
                case Mode.Equit:
                    phi = Asinz((yValue * sinz) / rh);
                    yValue = cosz * rh;
                    xValue *= sinz;
                    break;
                case Mode.NPole:
                    phi = Asinz(cosz);
                    yValue = -yValue;
                    break;
                default:
                    phi = -Asinz(cosz);
                    break;
            }

            lambda = Math.Atan2(xValue, yValue);
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
