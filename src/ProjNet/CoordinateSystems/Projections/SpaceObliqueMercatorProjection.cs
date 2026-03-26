// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Space Oblique Mercator projection family
/// (<c>som</c>, <c>misrsom</c>, <c>lsat</c>).
/// </summary>
[Serializable]
internal sealed class SpaceObliqueMercatorProjection : MapProjection
{
    private const double Tolerance = 1e-7d;

    private readonly double a2;
    private readonly double a4;
    private readonly double b;
    private readonly double c1;
    private readonly double c3;
    private readonly double q;
    private readonly double t;
    private readonly double u;
    private readonly double w;
    private readonly double p22;
    private readonly double sa;
    private readonly double ca;
    private readonly double xj;
    private readonly double rlm;
    private readonly double rlm2;
    private readonly double oneEs;
    private readonly double roneEs;
    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpaceObliqueMercatorProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public SpaceObliqueMercatorProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpaceObliqueMercatorProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public SpaceObliqueMercatorProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(PrepareParameters(parameters), inverse)
    {
        this.Name = "Space_Oblique_Mercator";

        SomSetupParameters setup = ResolveSetupParameters(this.Parameters);
        this.centralMeridian = setup.Lam0;
        this.p22 = setup.P22;
        this.rlm = setup.Rlm;
        this.rlm2 = this.rlm + TwoPi;

        this.sa = Math.Sin(setup.Alf);
        this.ca = Math.Cos(setup.Alf);
        if (Math.Abs(this.ca) < 1e-9d)
        {
            this.ca = 1e-9d;
        }

        this.oneEs = 1d - this.es;
        if (this.oneEs <= 0d)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        this.roneEs = 1d / this.oneEs;
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        double esc = this.es * this.ca * this.ca;
        double ess = this.es * this.sa * this.sa;

        this.w = (1d - esc) * this.roneEs;
        this.w = (this.w * this.w) - 1d;
        this.q = ess * this.roneEs;
        this.t = ess * (2d - this.es) * this.roneEs * this.roneEs;
        this.u = esc * this.roneEs;
        this.xj = this.oneEs * this.oneEs * this.oneEs;

        (this.a2, this.a4, this.b, this.c1, this.c3) = this.ComputeSeriesCoefficients();
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new SpaceObliqueMercatorProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double phi = Math.Max(-HalfPi, Math.Min(HalfPi, lat));
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double tanphi = Math.Tan(phi);

        double lampp = phi >= 0d ? HalfPi : PI + HalfPi;
        double lamdp = 0d;
        double lamt = 0d;
        int l = 0;

        int nn = 0;
        while (true)
        {
            double sav = lampp;
            double lamtp = lambda + (this.p22 * lampp);
            double cl = Math.Cos(lamtp);
            double fac = cl < 0d
                ? lampp + (Math.Sin(lampp) * HalfPi)
                : lampp - (Math.Sin(lampp) * HalfPi);

            for (l = 50; l >= 0; --l)
            {
                lamt = lambda + (this.p22 * sav);
                double c = Math.Cos(lamt);
                if (Math.Abs(c) < Tolerance)
                {
                    lamt -= Tolerance;
                }

                double xlam = ((this.oneEs * tanphi * this.sa) + (Math.Sin(lamt) * this.ca)) / c;
                lamdp = Math.Atan(xlam) + fac;
                if (Math.Abs(Math.Abs(sav) - Math.Abs(lamdp)) < Tolerance)
                {
                    break;
                }

                sav = lamdp;
            }

            if (l == 0 || ++nn >= 3 || (lamdp > this.rlm && lamdp < this.rlm2))
            {
                break;
            }

            if (lamdp <= this.rlm)
            {
                lampp = TwoPi + HalfPi;
            }
            else if (lamdp >= this.rlm2)
            {
                lampp = HalfPi;
            }
        }

        if (l == 0)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double sp = Math.Sin(phi);
        double phidp = Asinz(
            ((this.oneEs * this.ca * sp) - (this.sa * Math.Cos(phi) * Math.Sin(lamt)))
            / Math.Sqrt(1d - (this.es * sp * sp)));
        double tanph = Math.Log(Math.Tan(FortPi + (0.5d * phidp)));

        double sd = Math.Sin(lamdp);
        double sdsq = sd * sd;
        double s = this.p22 * this.sa * Math.Cos(lamdp)
            * Math.Sqrt((1d + (this.t * sdsq)) / ((1d + (this.w * sdsq)) * (1d + (this.q * sdsq))));
        double d = Math.Sqrt((this.xj * this.xj) + (s * s));

        lon = (this.b * lamdp)
            + (this.a2 * Math.Sin(2d * lamdp))
            + (this.a4 * Math.Sin(4d * lamdp))
            - ((tanph * s) / d);
        lat = (this.c1 * sd)
            + (this.c3 * Math.Sin(3d * lamdp))
            + ((tanph * this.xj) / d);

        lon *= this.radius;
        lat *= this.radius;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        x *= this.inverseRadius;
        y *= this.inverseRadius;

        double lamdp = x / this.b;
        double s = 0d;

        int nn = 50;
        do
        {
            double sav = lamdp;
            double sd = Math.Sin(lamdp);
            double sdsq = sd * sd;
            s = this.p22 * this.sa * Math.Cos(lamdp)
                * Math.Sqrt((1d + (this.t * sdsq)) / ((1d + (this.w * sdsq)) * (1d + (this.q * sdsq))));

            lamdp = x
                + ((y * s) / this.xj)
                - (this.a2 * Math.Sin(2d * lamdp))
                - (this.a4 * Math.Sin(4d * lamdp))
                - ((s / this.xj) * ((this.c1 * Math.Sin(lamdp)) + (this.c3 * Math.Sin(3d * lamdp))));
            lamdp /= this.b;

            if (Math.Abs(lamdp - sav) < Tolerance)
            {
                break;
            }
        }
        while (--nn > 0);

        double sl = Math.Sin(lamdp);
        double fac = Math.Exp(Math.Sqrt(1d + ((s * s) / (this.xj * this.xj)))
            * (y - (this.c1 * sl) - (this.c3 * Math.Sin(3d * lamdp))));
        double phidp = 2d * (Math.Atan(fac) - FortPi);

        double dd = sl * sl;
        if (Math.Abs(Math.Cos(lamdp)) < Tolerance)
        {
            lamdp -= Tolerance;
        }

        double spp = Math.Sin(phidp);
        double sppsq = spp * spp;
        double denom = 1d - (sppsq * (1d + this.u));
        if (denom == 0d)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lamt = Math.Atan(
            (((1d - (sppsq * this.roneEs)) * Math.Tan(lamdp) * this.ca)
            - ((spp * this.sa * Math.Sqrt(((1d + (this.q * dd)) * (1d - sppsq)) - (sppsq * this.u))) / Math.Cos(lamdp)))
            / denom);

        double signLam = lamt >= 0d ? 1d : -1d;
        double signCosLamdp = Math.Cos(lamdp) >= 0d ? 1d : -1d;
        lamt -= HalfPi * (1d - signCosLamdp) * signLam;

        double lambda = lamt - (this.p22 * lamdp);
        double phiNumerator = (Math.Tan(lamdp) * Math.Cos(lamt)) - (this.ca * Math.Sin(lamt));
        double phiDenominator = this.oneEs * this.sa;
        double phi = Math.Abs(this.sa) < Tolerance
            ? Asinz(spp / Math.Sqrt((this.oneEs * this.oneEs) + (this.es * sppsq)))
            : Math.Atan(phiNumerator / phiDenominator);

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }

    private static List<ProjectionParameter> PrepareParameters(IEnumerable<ProjectionParameter> parameters)
    {
        parameters = ArgumentGuard.ThrowIfNull(parameters, nameof(parameters));

        var merged = CloneParametersList(parameters);
        ProjectionParameterSet input = new(merged);

        bool hasSom = input.ContainsKey("inc_angle") || input.ContainsKey("ps_rev") || input.ContainsKey("asc_lon");
        bool hasLsat = input.ContainsKey("lsat");
        bool hasMisr = input.ContainsKey("path") && !hasLsat && !hasSom;

        if (hasSom)
        {
            // generic som: caller provides inclination / period / ascending longitude
        }
        else if (hasLsat)
        {
            int lsat = ReadPositiveInt(input, "lsat");
            if (lsat < 1 || lsat > 5)
            {
                ArgumentGuard.ThrowArgument("Invalid value for lsat: lsat should be in [1, 5] range");
            }

            int path = ReadPositiveInt(input, "path");
            int maxPath = lsat <= 3 ? 251 : 233;
            if (path < 1 || path > maxPath)
            {
                ArgumentGuard.ThrowArgument($"Invalid value for path: path should be in [1, {maxPath}] range");
            }

            if (lsat <= 3)
            {
                ReplaceParameter(merged, "inc_angle", 99.092d);
                ReplaceParameter(merged, "ps_rev", 103.2669323d / 1440d);
                ReplaceParameter(merged, "asc_lon", 128.87d - ((360d / 251d) * path));
            }
            else
            {
                ReplaceParameter(merged, "inc_angle", 98.2d);
                ReplaceParameter(merged, "ps_rev", 98.8841202d / 1440d);
                ReplaceParameter(merged, "asc_lon", 129.3d - ((360d / 233d) * path));
            }

            ReplaceParameter(merged, "som_rlm_mode", 1d);
        }
        else if (hasMisr)
        {
            int path = ReadPositiveInt(input, "path");
            if (path < 1 || path > 233)
            {
                ArgumentGuard.ThrowArgument("Invalid value for path: path should be in [1, 233] range");
            }

            ReplaceParameter(merged, "inc_angle", 98.30382d);
            ReplaceParameter(merged, "ps_rev", 98.88d / 1440d);
            ReplaceParameter(merged, "asc_lon", 129.3056d - ((360d / 233d) * path));
            ReplaceParameter(merged, "som_rlm_mode", 0d);
        }
        else
        {
            ReplaceParameter(merged, "som_rlm_mode", 0d);
        }

        ProjectionParameterSet resolved = new(merged);
        double ascLon = resolved.GetParameterValue("asc_lon");
        double ascLonRadians = ReadAngleRadians(ascLon, "asc_lon", -TwoPi, TwoPi);
        ReplaceParameter(merged, "central_meridian", RadiansToDegrees(ascLonRadians));

        if (!HasParameter(merged, "inc_angle") || !HasParameter(merged, "ps_rev") || !HasParameter(merged, "asc_lon"))
        {
            ArgumentGuard.ThrowArgument("Missing required SOM parameters: inc_angle, ps_rev, asc_lon.");
        }

        return merged;
    }

    private static SomSetupParameters ResolveSetupParameters(ProjectionParameterSet parameters)
    {
        double lam0 = ReadAngleRadians(parameters.GetParameterValue("asc_lon"), "asc_lon", -TwoPi, TwoPi);
        double alf = ReadAngleRadians(parameters.GetParameterValue("inc_angle"), "inc_angle", 0d, PI);
        double p22 = parameters.GetParameterValue("ps_rev");
        if (p22 < 0d)
        {
            ArgumentGuard.ThrowArgument("Number of days per rotation should be positive");
        }

        bool lsatMode = Math.Abs(parameters.GetOptionalParameterValue("som_rlm_mode", 0d)) > 0.5d;
        double rlm = lsatMode ? PI * ((1d / 248d) + 0.5161290322580645d) : 0d;
        return new SomSetupParameters(lam0, alf, p22, rlm);
    }

    private static double ReadAngleRadians(double raw, string name, double minRadians, double maxRadians)
    {
        if (double.IsNaN(raw) || double.IsInfinity(raw))
        {
            ArgumentGuard.ThrowArgument($"Invalid value for {name}.");
        }

        double radians = DegreesToRadians(raw);
        if (radians < minRadians || radians > maxRadians)
        {
            ArgumentGuard.ThrowArgument($"Invalid value for {name}.");
        }

        return radians;
    }

    private static int ReadPositiveInt(ProjectionParameterSet parameters, string name)
    {
        double value = parameters.GetParameterValue(name);
        int rounded = (int)Math.Round(value);
        if (Math.Abs(value - rounded) > Eps10 || rounded <= 0)
        {
            ArgumentGuard.ThrowArgument($"Invalid value for {name}.");
        }

        return rounded;
    }

    private static bool HasParameter(List<ProjectionParameter> parameters, string name)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void ReplaceParameter(List<ProjectionParameter> parameters, string name, double value)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                parameters[i] = new ProjectionParameter(name, value);
                return;
            }
        }

        parameters.Add(new ProjectionParameter(name, value));
    }

    private (double A2, double A4, double B, double C1, double C3) ComputeSeriesCoefficients()
    {
        double a2 = 0d;
        double a4 = 0d;
        double b = 0d;
        double c1 = 0d;
        double c3 = 0d;

        this.AddSerazContribution(0d, 1d, ref a2, ref a4, ref b, ref c1, ref c3);
        for (double lam = 9d; lam <= 81.0001d; lam += 18d)
        {
            this.AddSerazContribution(lam, 4d, ref a2, ref a4, ref b, ref c1, ref c3);
        }

        for (double lam = 18d; lam <= 72.0001d; lam += 18d)
        {
            this.AddSerazContribution(lam, 2d, ref a2, ref a4, ref b, ref c1, ref c3);
        }

        this.AddSerazContribution(90d, 1d, ref a2, ref a4, ref b, ref c1, ref c3);

        return (a2 / 30d, a4 / 60d, b / 30d, c1 / 15d, c3 / 45d);
    }

    private void AddSerazContribution(double lamDegrees, double multiplier, ref double a2, ref double a4, ref double b, ref double c1, ref double c3)
    {
        double lam = DegreesToRadians(lamDegrees);
        double sd = Math.Sin(lam);
        double sdsq = sd * sd;

        double s = this.p22 * this.sa * Math.Cos(lam)
            * Math.Sqrt((1d + (this.t * sdsq)) / ((1d + (this.w * sdsq)) * (1d + (this.q * sdsq))));

        double d1 = 1d + (this.q * sdsq);
        double h = Math.Sqrt((1d + (this.q * sdsq)) / (1d + (this.w * sdsq)))
            * (((1d + (this.w * sdsq)) / (d1 * d1)) - (this.p22 * this.ca));

        double sq = Math.Sqrt((this.xj * this.xj) + (s * s));
        double fc = multiplier * ((h * this.xj) - (s * s)) / sq;

        b += fc;
        a2 += fc * Math.Cos(2d * lam);
        a4 += fc * Math.Cos(4d * lam);

        fc = multiplier * s * (h + this.xj) / sq;
        c1 += fc * Math.Cos(lam);
        c3 += fc * Math.Cos(3d * lam);
    }

    private readonly struct SomSetupParameters
    {
        public SomSetupParameters(double lam0, double alf, double p22, double rlm)
        {
            this.Lam0 = lam0;
            this.Alf = alf;
            this.P22 = p22;
            this.Rlm = rlm;
        }

        public double Lam0 { get; }

        public double Alf { get; }

        public double P22 { get; }

        public double Rlm { get; }
    }
}
