// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Roussilhe stereographic projection (<c>rouss</c>).
/// </summary>
[Serializable]
internal sealed class RoussilheStereographicProjection : MapProjection
{
    private readonly double s0;
    private readonly double a1;
    private readonly double a2;
    private readonly double a3;
    private readonly double a4;
    private readonly double a5;
    private readonly double a6;
    private readonly double b1;
    private readonly double b2;
    private readonly double b3;
    private readonly double b4;
    private readonly double b5;
    private readonly double b6;
    private readonly double b7;
    private readonly double b8;
    private readonly double c1;
    private readonly double c2;
    private readonly double c3;
    private readonly double c4;
    private readonly double c5;
    private readonly double c6;
    private readonly double c7;
    private readonly double c8;
    private readonly double d1;
    private readonly double d2;
    private readonly double d3;
    private readonly double d4;
    private readonly double d5;
    private readonly double d6;
    private readonly double d7;
    private readonly double d8;
    private readonly double d9;
    private readonly double d10;
    private readonly double d11;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoussilheStereographicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public RoussilheStereographicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoussilheStereographicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public RoussilheStereographicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Roussilhe_Stereographic";

        double sinPhi0 = Math.Sin(this.latOrigin);
        this.s0 = this.Mlfn(this.latOrigin, sinPhi0, Math.Cos(this.latOrigin));
        double es2 = this.es * sinPhi0 * sinPhi0;
        double t = 1d - es2;
        double n0 = 1d / Math.Sqrt(t);
        double oneMinusEs = 1d - this.es;
        double rOverR0Squared = (t * t) / oneMinusEs;
        double rOverR0Fourth = rOverR0Squared * rOverR0Squared;
        double tanPhi0 = Math.Tan(this.latOrigin);
        double tanPhi0Squared = tanPhi0 * tanPhi0;

        this.c1 = this.a1 = rOverR0Squared / 4d;
        this.c2 = this.a2 = rOverR0Squared * ((2d * tanPhi0Squared) - 1d - (2d * es2)) / 12d;
        this.a3 = rOverR0Squared * tanPhi0 * (1d + (4d * tanPhi0Squared)) / (12d * n0);
        this.a4 = rOverR0Fourth / 24d;
        this.a5 = rOverR0Fourth * (-1d + (tanPhi0Squared * (11d + (12d * tanPhi0Squared)))) / 24d;
        this.a6 = rOverR0Fourth * (-2d + (tanPhi0Squared * (11d - (2d * tanPhi0Squared)))) / 240d;
        this.b1 = tanPhi0 / (2d * n0);
        this.b2 = rOverR0Squared / 12d;
        this.b3 = rOverR0Squared * (1d + (2d * tanPhi0Squared) - (2d * es2)) / 4d;
        this.b4 = rOverR0Squared * tanPhi0 * (2d - tanPhi0Squared) / (24d * n0);
        this.b5 = rOverR0Squared * tanPhi0 * (5d + (4d * tanPhi0Squared)) / (8d * n0);
        this.b6 = rOverR0Fourth * (-2d + (tanPhi0Squared * (-5d + (6d * tanPhi0Squared)))) / 48d;
        this.b7 = rOverR0Fourth * (5d + (tanPhi0Squared * (19d + (12d * tanPhi0Squared)))) / 24d;
        this.b8 = rOverR0Fourth / 120d;
        this.c3 = rOverR0Squared * tanPhi0 * (1d + tanPhi0Squared) / (3d * n0);
        this.c4 = rOverR0Fourth * (-3d + (tanPhi0Squared * (34d + (22d * tanPhi0Squared)))) / 240d;
        this.c5 = rOverR0Fourth * (4d + (tanPhi0Squared * (13d + (12d * tanPhi0Squared)))) / 24d;
        this.c6 = rOverR0Fourth / 16d;
        this.c7 = rOverR0Fourth * tanPhi0 * (11d + (tanPhi0Squared * (33d + (tanPhi0Squared * 16d)))) / (48d * n0);
        this.c8 = rOverR0Fourth * tanPhi0 * (1d + (tanPhi0Squared * 4d)) / (36d * n0);
        this.d1 = tanPhi0 / (2d * n0);
        this.d2 = rOverR0Squared / 12d;
        this.d3 = rOverR0Squared * ((2d * tanPhi0Squared) + 1d - (2d * es2)) / 4d;
        this.d4 = rOverR0Squared * tanPhi0 * (1d + tanPhi0Squared) / (8d * n0);
        this.d5 = rOverR0Squared * tanPhi0 * (1d + (tanPhi0Squared * 2d)) / (4d * n0);
        this.d6 = rOverR0Fourth * (1d + (tanPhi0Squared * (6d + (tanPhi0Squared * 6d)))) / 16d;
        this.d7 = rOverR0Fourth * tanPhi0Squared * (3d + (tanPhi0Squared * 4d)) / 8d;
        this.d8 = rOverR0Fourth / 80d;
        this.d9 = rOverR0Fourth * tanPhi0 * (-21d + (tanPhi0Squared * (178d - (tanPhi0Squared * 26d)))) / 720d;
        this.d10 = rOverR0Fourth * tanPhi0 * (29d + (tanPhi0Squared * (86d + (tanPhi0Squared * 48d)))) / (96d * n0);
        this.d11 = rOverR0Fourth * tanPhi0 * (37d + (tanPhi0Squared * 44d)) / (96d * n0);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new RoussilheStereographicProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double cp = Math.Cos(lat);
        double sp = Math.Sin(lat);
        double s = this.Mlfn(lat, sp, cp) - this.s0;
        double s2 = s * s;
        double denominator = Math.Sqrt(1d - (this.es * sp * sp));
        if (Math.Abs(denominator) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double al = lon * cp / denominator;
        double al2 = al * al;
        double x = this.scaleFactor * al *
            (1d + (s2 * (this.a1 + (s2 * this.a4))) - (al2 * (this.a2 + (s * this.a3) + (s2 * this.a5) + (al2 * this.a6))));
        double y = this.scaleFactor *
            ((al2 * (this.b1 + (al2 * this.b4))) +
            (s * (1d + (al2 * (this.b3 - (al2 * this.b6))) + (s2 * (this.b2 + (s2 * this.b8))) + (s * al2 * (this.b5 + (s * this.b7))))));

        lon = this.semiMajor * x;
        lat = this.semiMajor * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x / (this.semiMajor * this.scaleFactor);
        double yy = y / (this.semiMajor * this.scaleFactor);
        double x2 = xx * xx;
        double y2 = yy * yy;

        double al = xx * (1d - (this.c1 * y2) +
            (x2 * (this.c2 + (this.c3 * yy) - (this.c4 * x2) + (this.c5 * y2) - (this.c7 * x2 * yy))) +
            (y2 * ((this.c6 * y2) - (this.c8 * x2 * yy))));

        double s = this.s0 + (yy * (1d + (y2 * (-this.d2 + (this.d8 * y2))))) +
            (x2 * (-this.d1 + (yy * (-this.d3 + (yy * (-this.d5 + (yy * (-this.d7 + (yy * this.d11))))))) +
            (x2 * (this.d4 + (yy * (this.d6 + (yy * this.d10))) - (x2 * this.d9)))));

        double phi = this.Inv_mlfn(s);
        double sinPhi = Math.Sin(phi);
        double cosPhi = Math.Cos(phi);
        if (Math.Abs(cosPhi) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lam = al * Math.Sqrt(1d - (this.es * sinPhi * sinPhi)) / cosPhi;
        x = Adjust_lon(this.centralMeridian + lam);
        y = phi;
    }
}
