// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ProjNet.CoordinateSystems;

/// <summary>
/// Implements PROJ's (abridged) <c>molodensky</c> runtime transform.
/// </summary>
/// <remarks>
/// The abridged branch was independently verified against IOGP, "Geomatics Guidance
/// Note 7, part 2: Coordinate Conversions and Transformations including Formulas"
/// (publication 373-7-2, 2019), EPSG method 9605, Abridged Molodensky. In particular,
/// the combined ellipsoid-difference term follows <c>a * df + f * da</c> in the
/// latitude and height corrections, matching the published method.
/// </remarks>
/// <seealso href="https://epsg.io/9605-method">EPSG method 9605: Abridged Molodensky.</seealso>
internal sealed class MolodenskyMathTransform : MathTransform
{
    private readonly double semiMajor;
    private readonly double flattening;
    private readonly double eccentricitySquared;
    private readonly double dx;
    private readonly double dy;
    private readonly double dz;
    private readonly double da;
    private readonly double df;
    private readonly bool abridged;

    private readonly bool isInverted;
    private MathTransform? inverse;

    private MolodenskyMathTransform(
        double semiMajor,
        double semiMinor,
        double dx,
        double dy,
        double dz,
        double da,
        double df,
        bool abridged,
        bool isInverted)
    {
        if (semiMajor <= 0d || double.IsNaN(semiMajor) || double.IsInfinity(semiMajor))
        {
            ArgumentGuard.ThrowArgument("Molodensky requires a positive finite semi-major axis.", nameof(semiMajor));
        }

        if (semiMinor <= 0d || double.IsNaN(semiMinor) || double.IsInfinity(semiMinor))
        {
            ArgumentGuard.ThrowArgument("Molodensky requires a positive finite semi-minor axis.", nameof(semiMinor));
        }

        this.semiMajor = semiMajor;
        this.flattening = (semiMajor - semiMinor) / semiMajor;
        this.eccentricitySquared = (2d * this.flattening) - (this.flattening * this.flattening);
        this.dx = dx;
        this.dy = dy;
        this.dz = dz;
        this.da = da;
        this.df = df;
        this.abridged = abridged;
        this.isInverted = isInverted;
    }

    private MolodenskyMathTransform(MolodenskyMathTransform source, bool isInverted)
    {
        this.semiMajor = source.semiMajor;
        this.flattening = source.flattening;
        this.eccentricitySquared = source.eccentricitySquared;
        this.dx = source.dx;
        this.dy = source.dy;
        this.dz = source.dz;
        this.da = source.da;
        this.df = source.df;
        this.abridged = source.abridged;
        this.isInverted = isInverted;
    }

    /// <inheritdoc />
    public override int DimSource => 3;

    /// <inheritdoc />
    public override int DimTarget => 3;

    /// <inheritdoc />
    public override string WKT => throw new NotImplementedException();

    /// <inheritdoc />
    public override string XML => throw new NotImplementedException();

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new MolodenskyMathTransform(this, !this.isInverted);

        return this.inverse;
    }

    /// <inheritdoc />
    public override void Invert()
    {
        throw new NotSupportedException("MolodenskyMathTransform is immutable. Use Inverse() to obtain inverted transform.");
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        if (double.IsNaN(z))
        {
            z = 0d;
        }

        double lam = DegreesToRadians(x);
        double phi = DegreesToRadians(y);
        double h = z;

        (double dLam, double dPhi, double dH) = this.abridged
            ? this.CalculateAbridgedDelta(lam, phi, h)
            : this.CalculateStandardDelta(lam, phi, h);

        if (this.isInverted)
        {
            lam -= dLam;
            phi -= dPhi;
            h -= dH;
        }
        else
        {
            lam += dLam;
            phi += dPhi;
            h += dH;
        }

        x = RadiansToDegrees(lam);
        y = RadiansToDegrees(phi);
        z = h;
    }

    /// <summary>
    /// Creates a <see cref="MolodenskyMathTransform"/> from parsed PROJ arguments.
    /// </summary>
    /// <param name="args">Parsed PROJ argument dictionary.</param>
    /// <param name="transform">Created transform instance on success.</param>
    /// <param name="skipReason">Failure reason when creation is not possible.</param>
    /// <returns><see langword="true"/> when a transform was created.</returns>
    internal static bool TryCreate(
        Dictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (args is null)
        {
            skipReason = "molodensky arguments were null.";
            return false;
        }

        if (!TryGetRequiredDouble(args, "dx", out double dx))
        {
            skipReason = "missing dx";
            return false;
        }

        if (!TryGetRequiredDouble(args, "dy", out double dy))
        {
            skipReason = "missing dy";
            return false;
        }

        if (!TryGetRequiredDouble(args, "dz", out double dz))
        {
            skipReason = "missing dz";
            return false;
        }

        if (!TryGetRequiredDouble(args, "da", out double da))
        {
            skipReason = "missing da";
            return false;
        }

        if (!TryGetRequiredDouble(args, "df", out double df))
        {
            skipReason = "missing df";
            return false;
        }

        if (!ProjEllipsoidResolver.TryResolveEllipsoidOrDefault(
            args,
            includeDatumToken: true,
            allowClarke1880Ign: false,
            allowBessel: false,
            out double semiMajor,
            out double semiMinor))
        {
            skipReason = "Unable to resolve ellipsoid for molodensky.";
            return false;
        }

        try
        {
            transform = new MolodenskyMathTransform(
                semiMajor,
                semiMinor,
                dx,
                dy,
                dz,
                da,
                df,
                args.ContainsKey("abridged"),
                false);
            if (args.ContainsKey("inv"))
            {
                transform = transform.Inverse();
            }

            return true;
        }
        catch (ArgumentException exception)
        {
            skipReason = exception.Message;
            return false;
        }
    }

    private static bool TryGetRequiredDouble(Dictionary<string, string> args, string key, out double value)
    {
        value = 0d;
        return args.TryGetValue(key, out string? token)
            && SpanParseUtility.TryParseFiniteDouble(token, out value);
    }

    private static bool TryGetOptionalDouble(Dictionary<string, string> args, string key, out double value)
    {
        value = 0d;
        return args.TryGetValue(key, out string? token)
            && SpanParseUtility.TryParseFiniteDouble(token, out value);
    }

    private (double DeltaLam, double DeltaPhi, double DeltaH) CalculateStandardDelta(double lam, double phi, double h)
    {
        double sinLam = Math.Sin(lam);
        double cosLam = Math.Cos(lam);
        double sinPhi = Math.Sin(phi);
        double cosPhi = Math.Cos(phi);

        double rho = this.ComputeRm(phi);
        double nu = this.ComputeRn(phi);

        double dPhi = (-this.dx * sinPhi * cosLam)
            - (this.dy * sinPhi * sinLam)
            + (this.dz * cosPhi)
            + ((nu * this.eccentricitySquared * sinPhi * cosPhi * this.da) / this.semiMajor)
            + (sinPhi * cosPhi * ((rho / (1d - this.flattening)) + (nu * (1d - this.flattening))) * this.df);
        double dPhiDenominator = rho + h;
        if (dPhiDenominator == 0d)
        {
            ArgumentGuard.ThrowArgument("Molodensky standard produced invalid denominator for dphi.");
        }

        dPhi /= dPhiDenominator;

        double dLamDenominator = (nu + h) * cosPhi;
        if (dLamDenominator == 0d)
        {
            ArgumentGuard.ThrowArgument("Molodensky standard produced invalid denominator for dlam.");
        }

        double dLam = ((-this.dx * sinLam) + (this.dy * cosLam)) / dLamDenominator;
        double dH = (this.dx * cosPhi * cosLam)
            + (this.dy * cosPhi * sinLam)
            + (this.dz * sinPhi)
            - ((this.semiMajor / nu) * this.da)
            + (nu * (1d - this.flattening) * sinPhi * sinPhi * this.df);
        return (dLam, dPhi, dH);
    }

    private (double DeltaLam, double DeltaPhi, double DeltaH) CalculateAbridgedDelta(double lam, double phi, double h)
    {
        _ = h;
        double sinLam = Math.Sin(lam);
        double cosLam = Math.Cos(lam);
        double sinPhi = Math.Sin(phi);
        double cosPhi = Math.Cos(phi);
        double adffda = (this.semiMajor * this.df) + (this.flattening * this.da);

        double dPhi = (-this.dx * sinPhi * cosLam)
            - (this.dy * sinPhi * sinLam)
            + (this.dz * cosPhi)
            + (adffda * Math.Sin(2d * phi));
        double dPhiDenominator = this.ComputeRm(phi);
        if (dPhiDenominator == 0d)
        {
            ArgumentGuard.ThrowArgument("Molodensky abridged produced invalid denominator for dphi.");
        }

        dPhi /= dPhiDenominator;

        double dLamDenominator = this.ComputeRn(phi) * cosPhi;
        if (dLamDenominator == 0d)
        {
            ArgumentGuard.ThrowArgument("Molodensky abridged produced invalid denominator for dlam.");
        }

        double dLam = ((-this.dx * sinLam) + (this.dy * cosLam)) / dLamDenominator;
        double dH = (this.dx * cosPhi * cosLam)
            + (this.dy * cosPhi * sinLam)
            + (this.dz * sinPhi)
            - this.da
            + (adffda * sinPhi * sinPhi);
        return (dLam, dPhi, dH);
    }

    private double ComputeRn(double phi)
    {
        double sinPhi = Math.Sin(phi);
        return this.eccentricitySquared == 0d
            ? this.semiMajor
            : this.semiMajor / Math.Sqrt(1d - (this.eccentricitySquared * sinPhi * sinPhi));
    }

    private double ComputeRm(double phi)
    {
        double sinPhi = Math.Sin(phi);
        if (this.eccentricitySquared == 0d)
        {
            return this.semiMajor;
        }

        if (phi == 0d)
        {
            return this.semiMajor * (1d - this.eccentricitySquared);
        }

        return Math.Abs(phi) == (Math.PI * 0.5d)
            ? this.semiMajor / Math.Sqrt(1d - this.eccentricitySquared)
            : (this.semiMajor * (1d - this.eccentricitySquared))
            / Math.Pow(1d - (this.eccentricitySquared * sinPhi * sinPhi), 1.5d);
    }
}
