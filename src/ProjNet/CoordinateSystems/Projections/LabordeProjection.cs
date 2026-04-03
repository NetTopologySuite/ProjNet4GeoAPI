// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Laborde projection (<c>labrd</c>).
/// </summary>
/// <remarks>
/// Laborde is the oblique conformal projection introduced for Madagascar by Jean Laborde in
/// 1928. The implementation follows the classical conformal-sphere setup and then applies
/// the Laborde polynomial correction terms that distinguish this projection from standard
/// oblique Mercator forms.
/// </remarks>
internal class LabordeProjection : MapProjection
{
    private const int MaximumIterations = 20;
    private const double IterationTolerance = 1e-10;

    private readonly double kRg;
    private readonly double p0s;
    private readonly double a;
    private readonly double c;
    private readonly double ca;
    private readonly double cb;
    private readonly double cc;
    private readonly double cd;

    /// <summary>
    /// Initializes a new instance of the <see cref="LabordeProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public LabordeProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LabordeProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public LabordeProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Laborde";

        if (Math.Abs(this.latOrigin) < Eps10)
        {
            ArgumentGuard.ThrowArgument("Invalid value for lat_0: lat_0 should be different from 0.");
        }

        double azimuth = DegreesToRadians(this.Parameters.GetOptionalParameterValue("azi", this.Parameters.GetOptionalParameterValue("azimuth", 0d)));
        double sinPhi0 = Math.Sin(this.latOrigin);
        double t = 1d - (this.es * sinPhi0 * sinPhi0);
        double n = 1d / Math.Sqrt(t);
        double r = (1d - this.es) * n / t;
        this.kRg = this.scaleFactor * Math.Sqrt(n * r);
        this.p0s = Math.Atan(Math.Sqrt(r / n) * Math.Tan(this.latOrigin));
        this.a = sinPhi0 / Math.Sin(this.p0s);
        t = this.e * sinPhi0;
        this.c = (0.5d * this.e * this.a * Math.Log((1d + t) / (1d - t)))
            - (this.a * Math.Log(Math.Tan(FortPi + (0.5d * this.latOrigin))))
            + Math.Log(Math.Tan(FortPi + (0.5d * this.p0s)));
        t = azimuth + azimuth;
        double cbTmp = 1d / (12d * this.kRg * this.kRg);
        this.ca = (1d - Math.Cos(t)) * cbTmp;
        this.cb = cbTmp * Math.Sin(t);
        this.cc = 3d * ((this.ca * this.ca) - (this.cb * this.cb));
        this.cd = 6d * this.ca * this.cb;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new LabordeProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);

        double v1 = this.a * Math.Log(Math.Tan(FortPi + (0.5d * lat)));
        double t = this.e * Math.Sin(lat);
        double v2 = 0.5d * this.e * this.a * Math.Log((1d + t) / (1d - t));
        double ps = 2d * (Math.Atan(Math.Exp(v1 - v2 + this.c)) - FortPi);
        double i1 = ps - this.p0s;

        double cosps = Math.Cos(ps);
        double cosps2 = cosps * cosps;
        double sinps = Math.Sin(ps);
        double sinps2 = sinps * sinps;
        double i4 = this.a * cosps;
        double i2 = 0.5d * this.a * i4 * sinps;
        double i3 = i2 * this.a * this.a * ((5d * cosps2) - sinps2) / 12d;
        double i6 = i4 * this.a * this.a;
        double i5 = i6 * (cosps2 - sinps2) / 6d;
        i6 *= this.a * this.a * ((5d * cosps2 * cosps2) + (sinps2 * (sinps2 - (18d * cosps2)))) / 120d;

        t = lambda * lambda;
        double x = this.kRg * lambda * (i4 + (t * (i5 + (t * i6))));
        double y = this.kRg * (i1 + (t * (i2 + (t * i3))));
        double x2 = x * x;
        double y2 = y * y;
        v1 = (3d * x * y2) - (x * x2);
        v2 = (y * y2) - (3d * x2 * y);
        x += (this.ca * v1) + (this.cb * v2);
        y += (this.ca * v2) - (this.cb * v1);

        lon = this.semiMajor * x;
        lat = this.semiMajor * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x / this.semiMajor;
        double yy = y / this.semiMajor;

        double x2 = xx * xx;
        double y2 = yy * yy;
        double v1 = (3d * xx * y2) - (xx * x2);
        double v2 = (yy * y2) - (3d * x2 * yy);
        double v3 = xx * ((5d * y2 * y2) + (x2 * ((-10d * y2) + x2)));
        double v4 = yy * ((5d * x2 * x2) + (y2 * ((-10d * x2) + y2)));
        xx += (-this.ca * v1) - (this.cb * v2) + (this.cc * v3) + (this.cd * v4);
        yy += (this.cb * v1) - (this.ca * v2) - (this.cd * v3) + (this.cc * v4);

        double ps = this.p0s + (yy / this.kRg);
        double pe = ps + this.latOrigin - this.p0s;

        bool converged = false;
        for (int i = 0; i < MaximumIterations; i++)
        {
            v1 = this.a * Math.Log(Math.Tan(FortPi + (0.5d * pe)));
            double tpe = this.e * Math.Sin(pe);
            v2 = 0.5d * this.e * this.a * Math.Log((1d + tpe) / (1d - tpe));
            double delta = ps - (2d * (Math.Atan(Math.Exp(v1 - v2 + this.c)) - FortPi));
            pe += delta;
            if (Math.Abs(delta) < IterationTolerance)
            {
                converged = true;
                break;
            }
        }

        if (!converged)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double t = this.e * Math.Sin(pe);
        t = 1d - (t * t);
        double re = (1d - this.es) / (t * Math.Sqrt(t));
        t = Math.Tan(ps);
        double t2 = t * t;
        double s = this.kRg * this.kRg;
        double d = re * this.scaleFactor * this.kRg;
        double i7 = t / (2d * d);
        double i8 = t * (5d + (3d * t2)) / (24d * d * s);
        d = Math.Cos(ps) * this.kRg * this.a;
        double i9 = 1d / d;
        d *= s;
        double i10 = (1d + (2d * t2)) / (6d * d);
        double i11 = (5d + (t2 * (28d + (24d * t2)))) / (120d * d * s);
        x2 = xx * xx;
        double phi = pe + (x2 * (-i7 + (i8 * x2)));
        double lambda = xx * (i9 + (x2 * (-i10 + (x2 * i11))));

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
