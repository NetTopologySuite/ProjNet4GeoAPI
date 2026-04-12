// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the International Map of the World Polyconic projection (<c>imw_p</c>).
/// </summary>
/// <remarks>
/// <para>The International Map of the World Polyconic projection is the modified
/// polyconic sheet projection adopted for the 1:1,000,000 International Map of the
/// World series. This implementation supports spherical and ellipsoidal forward and
/// inverse forms for the standard IMW quadrangle layout.</para>
/// <para>This implementation matches PROJ's <c>imw_p</c> formulation and Snyder's
/// summary of the Charles Lallemand 1909 modification approved for the IMW program
/// proposed by Albrecht Penck in 1891. The constructor reproduces the IMW sheet
/// logic with two standard parallels and latitude-zone-dependent standard meridians
/// at ±2, ±4, or ±8 degrees, while the forward and inverse paths use meridian-arc
/// evaluation with iterative longitude and latitude recovery.</para>
/// </remarks>
/// <seealso>Bugayevskiy &amp; Snyder (1995), "Map Projections: A Reference Manual", Ch. 4, Sect. 4.3.2, pp. 151-153; see also Ch. 5, Sect. 5.2.1, p. 165.</seealso>
/// <seealso href="https://proj.org/en/stable/operations/projections/imw_p.html">PROJ documentation: International Map of the World Polyconic.</seealso>
internal sealed class InternationalMapWorldPolyconicProjection : MapProjection
{
    private const int MaximumIterations = 1000;

    private readonly double phi1;
    private readonly double phi2;
    private readonly double sinPhi1;
    private readonly double sinPhi2;
    private readonly double r1;
    private readonly double r2;
    private readonly double c2;
    private readonly double p;
    private readonly double pp;
    private readonly double q;
    private readonly double qp;

    /// <summary>
    /// Initializes a new instance of the <see cref="InternationalMapWorldPolyconicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public InternationalMapWorldPolyconicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InternationalMapWorldPolyconicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public InternationalMapWorldPolyconicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "International_Map_of_the_World_Polyconic";

        this.phi1 = DegreesToRadians(this.Parameters.GetParameterValue("lat_1", "standard_parallel_1"));
        this.phi2 = DegreesToRadians(this.Parameters.GetParameterValue("lat_2", "standard_parallel_2"));

        double del = 0.5d * (this.phi2 - this.phi1);
        double sig = 0.5d * (this.phi2 + this.phi1);
        if (Math.Abs(del) < Eps10 || Math.Abs(sig) < Eps10)
        {
            ArgumentGuard.ThrowArgument("Illegal value for lat_1 and lat_2: |lat_1 - lat_2| and |lat_1 + lat_2| should be > 0.");
        }

        double lam1 = this.Parameters.ContainsKey("lon_1")
            ? DegreesToRadians(this.Parameters.GetParameterValue("lon_1"))
            : (Math.Abs(sig * 180d / PI) <= 60d ? 2d * PI / 180d : (Math.Abs(sig * 180d / PI) <= 76d ? 4d * PI / 180d : 8d * PI / 180d));

        this.sinPhi1 = Math.Sin(this.phi1);
        this.sinPhi2 = Math.Sin(this.phi2);
        this.r1 = this.phi1 == 0d ? 0d : 1d / (Math.Tan(this.phi1) * Math.Sqrt(1d - (this.es * this.sinPhi1 * this.sinPhi1)));
        this.r2 = this.phi2 == 0d ? 0d : 1d / (Math.Tan(this.phi2) * Math.Sqrt(1d - (this.es * this.sinPhi2 * this.sinPhi2)));
        double x1 = this.phi1 == 0d ? lam1 : this.r1 * Math.Sin(lam1 * this.sinPhi1);
        double y1 = this.phi1 == 0d ? 0d : this.r1 * (1d - Math.Cos(lam1 * this.sinPhi1));
        double x2 = this.phi2 == 0d ? lam1 : this.r2 * Math.Sin(lam1 * this.sinPhi2);
        double t2 = this.phi2 == 0d ? 0d : this.r2 * (1d - Math.Cos(lam1 * this.sinPhi2));
        double m1 = this.Mlfn(this.phi1, this.sinPhi1, Math.Cos(this.phi1));
        double m2 = this.Mlfn(this.phi2, this.sinPhi2, Math.Cos(this.phi2));
        double t = m2 - m1;
        double s = x2 - x1;
        double y2 = Math.Sqrt(Math.Max(0d, (t * t) - (s * s))) + y1;
        this.c2 = y2 - t2;
        double invT = 1d / t;
        this.p = ((m2 * y1) - (m1 * y2)) * invT;
        this.q = (y2 - y1) * invT;
        this.pp = ((m2 * x1) - (m1 * x2)) * invT;
        this.qp = (x2 - x1) * invT;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new InternationalMapWorldPolyconicProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        this.ComputeLocalForward(lon, lat, out double xUnit, out double yUnit, out _);
        lon = this.SphericalRadius * xUnit;
        lat = this.SphericalRadius * yUnit;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.InverseSphericalRadius;
        double yUnit = y * this.InverseSphericalRadius;

        double phi = this.phi2;
        double lambda = xUnit / Math.Cos(phi);
        for (int i = 0; i < MaximumIterations; i++)
        {
            this.ComputeLocalForward(lambda, phi, out double tx, out double ty, out double yc);

            double denominator = ty - yc;
            if (denominator == 0d && Math.Abs(ty - yUnit) > Eps10)
            {
                ArgumentGuard.ThrowArgument("Input data outside projection domain.");
            }

            if (denominator != 0d || Math.Abs(ty - yUnit) <= Eps10)
            {
                if (denominator != 0d)
                {
                    phi = ((phi - this.phi1) * (yUnit - yc) / denominator) + this.phi1;
                }
            }

            if (tx != 0d && Math.Abs(tx - xUnit) > Eps10)
            {
                lambda = lambda * xUnit / tx;
            }

            if (Math.Abs(tx - xUnit) <= Eps10 && Math.Abs(ty - yUnit) <= Eps10)
            {
                x = Adjust_lon(this.centralMeridian + lambda);
                y = phi;
                return;
            }
        }

        ArgumentGuard.ThrowArgument("Input data outside projection domain.");
    }

    private void ComputeLocalForward(double lambda, double phi, out double x, out double y, out double yc)
    {
        if (phi == 0d)
        {
            x = lambda;
            y = 0d;
            yc = 0d;
            return;
        }

        double sinPhi = Math.Sin(phi);
        double m = this.Mlfn(phi, sinPhi, Math.Cos(phi));
        double xa = this.pp + (this.qp * m);
        double ya = this.p + (this.q * m);
        double r = 1d / (Math.Tan(phi) * Math.Sqrt(1d - (this.es * sinPhi * sinPhi)));
        double cTerm = (r * r) - (xa * xa);
        if (cTerm < 0d)
        {
            cTerm = 0d;
        }

        double c = Math.Sqrt(cTerm);
        if (phi < 0d)
        {
            c = -c;
        }

        c += ya - r;

        double xb = lambda;
        double yb = this.c2;
        if (this.phi2 != 0d)
        {
            double t = lambda * this.sinPhi2;
            xb = this.r2 * Math.Sin(t);
            yb = this.c2 + (this.r2 * (1d - Math.Cos(t)));
        }

        double xc = lambda;
        yc = 0d;
        if (this.phi1 != 0d)
        {
            double t = lambda * this.sinPhi1;
            xc = this.r1 * Math.Sin(t);
            yc = this.r1 * (1d - Math.Cos(t));
        }

        double d = (xb - xc) / (yb - yc);
        double b = xc + (d * (c + r - yc));
        double root = (r * r * (1d + (d * d))) - (b * b);
        if (root < 0d)
        {
            root = 0d;
        }

        x = d * Math.Sqrt(root);
        if (phi > 0d)
        {
            x = -x;
        }

        x = (b + x) / (1d + (d * d));
        double yRoot = (r * r) - (x * x);
        if (yRoot < 0d)
        {
            yRoot = 0d;
        }

        y = Math.Sqrt(yRoot);
        if (phi > 0d)
        {
            y = -y;
        }

        y += c + r;
    }
}
