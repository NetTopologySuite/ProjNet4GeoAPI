// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical bipolar conic projection of the western hemisphere (<c>bipc</c>).
/// </summary>
[Serializable]
internal class BipolarConicProjection : MapProjection
{
    private const double Epsilon = 1e-10d;
    private const double Epsilon10 = 1e-10d;
    private const double OneEpsilon = 1.000000001d;
    private const int Iterations = 10;
    private const double LamB = -0.34894976726250681539d;
    private const double N = 0.63055844881274687180d;
    private const double F = 1.89724742567461030582d;
    private const double Azab = 0.81650043674686363166d;
    private const double Azba = 1.82261843856185925133d;
    private const double T = 1.27246578267089012270d;
    private const double Rhoc = 1.20709121521568721927d;
    private const double CosAzc = 0.69691523038678375519d;
    private const double SinAzc = 0.71715351331143607555d;
    private const double Cos45 = 0.70710678118654752469d;
    private const double Sin45 = 0.70710678118654752410d;
    private const double Cos20 = 0.93969262078590838411d;
    private const double Sin20 = -0.34202014332566873287d;
    private const double R110 = 1.91986217719376253360d;
    private const double R104 = 1.81514242207410275904d;

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly bool noSkew;

    /// <summary>
    /// Initializes a new instance of the <see cref="BipolarConicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public BipolarConicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BipolarConicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public BipolarConicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Bipolar_Conic";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
        this.noSkew = this.Parameters.ContainsKey("ns") || this.Parameters.ContainsKey("noskew");
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new BipolarConicProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double cphi = Math.Cos(lat);
        double sphi = Math.Sin(lat);
        double sdlam = LamB - lambda;
        double cdlam = Math.Cos(sdlam);
        sdlam = Math.Sin(sdlam);

        double az;
        bool atPole = Math.Abs(Math.Abs(lat) - HalfPi) < Epsilon10;
        if (atPole)
        {
            az = lat < 0d ? PI : 0d;
        }
        else
        {
            double tphi = sphi / cphi;
            az = Math.Atan2(sdlam, Cos45 * (tphi - cdlam));
        }

        bool tag = az > Azba;
        double y;
        double z;
        double av;
        if (tag)
        {
            sdlam = lambda + R110;
            cdlam = Math.Cos(sdlam);
            sdlam = Math.Sin(sdlam);
            z = (Sin20 * sphi) + (Cos20 * cphi * cdlam);
            if (Math.Abs(z) > 1d)
            {
                if (Math.Abs(z) > OneEpsilon)
                {
                    ArgumentGuard.ThrowArgument("Input data outside projection domain.");
                }

                z = z < 0d ? -1d : 1d;
            }
            else
            {
                z = Math.Acos(z);
            }

            if (!atPole)
            {
                double tphi = sphi / cphi;
                az = Math.Atan2(sdlam, (Cos20 * tphi) - (Sin20 * cdlam));
            }

            av = Azab;
            y = Rhoc;
        }
        else
        {
            z = Sin45 * (sphi + (cphi * cdlam));
            if (Math.Abs(z) > 1d)
            {
                if (Math.Abs(z) > OneEpsilon)
                {
                    ArgumentGuard.ThrowArgument("Input data outside projection domain.");
                }

                z = z < 0d ? -1d : 1d;
            }
            else
            {
                z = Math.Acos(z);
            }

            av = Azba;
            y = -Rhoc;
        }

        if (z < 0d)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double t = Math.Pow(Math.Tan(0.5d * z), N);
        double r = F * t;
        double al = 0.5d * (R104 - z);
        if (al < 0d)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        al = (t + Math.Pow(al, N)) / T;
        if (Math.Abs(al) > 1d)
        {
            if (Math.Abs(al) > OneEpsilon)
            {
                ArgumentGuard.ThrowArgument("Input data outside projection domain.");
            }

            al = al < 0d ? -1d : 1d;
        }
        else
        {
            al = Math.Acos(al);
        }

        t = N * (av - az);
        if (Math.Abs(t) < al)
        {
            double denominator = Math.Cos(al + (tag ? t : -t));
            if (Math.Abs(denominator) <= Eps10)
            {
                ArgumentGuard.ThrowArgument("Input data outside projection domain.");
            }

            r /= denominator;
        }

        double x = r * Math.Sin(t);
        y += (tag ? -r : r) * Math.Cos(t);
        if (this.noSkew)
        {
            double xt = x;
            x = (-x * CosAzc) - (y * SinAzc);
            y = (-y * CosAzc) + (xt * SinAzc);
        }

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;

        if (this.noSkew)
        {
            double t = xx;
            xx = (-xx * CosAzc) + (yy * SinAzc);
            yy = (-yy * CosAzc) - (t * SinAzc);
        }

        bool neg = xx < 0d;
        double s;
        double c;
        double av;
        if (neg)
        {
            yy = Rhoc - yy;
            s = Sin20;
            c = Cos20;
            av = Azab;
        }
        else
        {
            yy += Rhoc;
            s = Sin45;
            c = Cos45;
            av = Azba;
        }

        double r = Hypot(xx, yy);
        double rl = r;
        double rp = r;
        double az = Math.Atan2(xx, yy);
        double absAz = Math.Abs(az);
        double z = 0d;

        int i = Iterations;
        for (; i > 0; i--)
        {
            z = 2d * Math.Atan(Math.Pow(r / F, 1d / N));
            double alCosArg = (Math.Pow(Math.Tan(0.5d * z), N) + Math.Pow(Math.Tan(0.5d * (R104 - z)), N)) / T;
            alCosArg = ClampToUnit(alCosArg);
            double al = Math.Acos(alCosArg);
            if (absAz < al)
            {
                r = rp * Math.Cos(al + (neg ? az : -az));
            }

            if (Math.Abs(rl - r) < Epsilon)
            {
                break;
            }

            rl = r;
        }

        if (i == 0)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        az = av - (az / N);
        double phi = Asinz((s * Math.Cos(z)) + (c * Math.Sin(z) * Math.Cos(az)));
        double lambda = Math.Atan2(Math.Sin(az), (c / Math.Tan(z)) - (s * Math.Cos(az)));
        if (neg)
        {
            lambda -= R110;
        }
        else
        {
            lambda = LamB - lambda;
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }

    private static double ClampToUnit(double value)
    {
        if (value > 1d)
        {
            return 1d;
        }

        if (value < -1d)
        {
            return -1d;
        }

        return value;
    }
}
