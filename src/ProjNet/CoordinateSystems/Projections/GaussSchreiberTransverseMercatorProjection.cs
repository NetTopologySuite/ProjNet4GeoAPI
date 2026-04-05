// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Gauss-Schreiber Transverse Mercator projection (<c>gstmerc</c>).
/// </summary>
/// <remarks>
/// Supports ellipsoidal computation. The forward transform first maps geodetic coordinates
/// onto a conformal sphere, then applies a transverse Mercator development on that sphere.
/// <para>The conformal-sphere reduction was independently verified against the
/// Gauss-Schreiber/Laborde construction. The implementation matches the intermediate
/// conformal latitude, the <c>n1</c>/<c>n2</c> scale terms, and the final transverse
/// Mercator mapping performed on the conformal sphere.</para>
/// </remarks>
internal class GaussSchreiberTransverseMercatorProjection : MapProjection
{
    private readonly double n1;
    private readonly double c;
    private readonly double n2;
    private readonly double ys;

    /// <summary>
    /// Initializes a new instance of the <see cref="GaussSchreiberTransverseMercatorProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public GaussSchreiberTransverseMercatorProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GaussSchreiberTransverseMercatorProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public GaussSchreiberTransverseMercatorProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Gauss_Schreiber_Transverse_Mercator";

        double cosPhi0 = Math.Cos(this.latOrigin);
        double sinPhi0 = Math.Sin(this.latOrigin);
        double cosPhi0Pow4 = cosPhi0 * cosPhi0;
        cosPhi0Pow4 *= cosPhi0Pow4;

        this.n1 = Math.Sqrt(1d + ((this.es * cosPhi0Pow4) / (1d - this.es)));
        double phic = Asinz(sinPhi0 / this.n1);
        this.c = Math.Log(Tsfnz(0d, -phic, -Math.Sin(phic))) - (this.n1 * Math.Log(Tsfnz(this.e, -this.latOrigin, -sinPhi0)));
        this.n2 = this.scaleFactor * this.semiMajor * Math.Sqrt(1d - this.es) / (1d - (this.es * sinPhi0 * sinPhi0));
        this.ys = -this.n2 * phic;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new GaussSchreiberTransverseMercatorProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double l = this.n1 * lambda;
        double ls = this.c + (this.n1 * Math.Log(Tsfnz(this.e, -lat, -Math.Sin(lat))));
        double sinLs1 = Math.Sin(l) / Math.Cosh(ls);
        double ls1 = Math.Log(Tsfnz(0d, -Asinz(sinLs1), -sinLs1));
        lon = this.n2 * ls1;
        lat = this.ys + (this.n2 * Math.Atan(Math.Sinh(ls) / Math.Cos(l)));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double l = Math.Atan(Math.Sinh(x / this.n2) / Math.Cos((y - this.ys) / this.n2));
        double sinC = Math.Sin((y - this.ys) / this.n2) / Math.Cosh(x / this.n2);
        double lc = Math.Log(Tsfnz(0d, -Asinz(sinC), -sinC));
        double lambda = l / this.n1;

        double phi = -Phi2z(this.e, Math.Exp((lc - this.c) / this.n1), out long flag);
        if (flag != 0)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
