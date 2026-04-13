// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

/// <summary>
/// Implements PROJ's <c>horner</c> polynomial runtime transform.
/// </summary>
/// <remarks>
/// <para>
/// Horner polynomial transforms evaluate either real polynomial coefficient
/// fields or complex polynomial series around configured origins to model local
/// frame distortions. This implementation supports explicit inverse
/// coefficients when provided and otherwise falls back to the reviewed
/// iterative inverse solver.
/// </para>
/// <para>
/// The runtime was independently verified against PROJ's published
/// <c>horner</c> documentation and <c>horner.cpp</c>, including the coefficient
/// ordering, complex-polynomial handling, and the iterative inverse path for
/// cases where only forward coefficients are available.
/// </para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/transformations/horner.html">PROJ: horner.</seealso>
internal sealed class HornerMathTransform : MathTransform
{
    private const int MaximumSupportedDegree = 10000;
    private const int MaxInverseIterations = 32;
    private const double DefaultRange = 500000d;
    private const double DefaultInverseTolerance = 0.001d;
    private const double DeterminantTolerance = 1e-24d;

    private readonly int degree;
    private readonly bool isComplex;
    private readonly bool hasExplicitInverse;
    private readonly bool uneg;
    private readonly bool vneg;
    private readonly double range;
    private readonly double inverseTolerance;

    private readonly double[] fwdU;
    private readonly double[] fwdV;
    private readonly double[] invU;
    private readonly double[] invV;
    private readonly double[] fwdC;
    private readonly double[] invC;

    private readonly double fwdOriginX;
    private readonly double fwdOriginY;
    private readonly double invOriginX;
    private readonly double invOriginY;

    private readonly bool isInverted;
    private MathTransform? inverse;

    private HornerMathTransform(
        int degree,
        bool isComplex,
        bool hasExplicitInverse,
        bool uneg,
        bool vneg,
        double range,
        double inverseTolerance,
        double[] fwdU,
        double[] fwdV,
        double[] invU,
        double[] invV,
        double[] fwdC,
        double[] invC,
        double fwdOriginX,
        double fwdOriginY,
        double invOriginX,
        double invOriginY,
        bool isInverted)
    {
        this.degree = degree;
        this.isComplex = isComplex;
        this.hasExplicitInverse = hasExplicitInverse;
        this.uneg = uneg;
        this.vneg = vneg;
        this.range = range;
        this.inverseTolerance = inverseTolerance;
        this.fwdU = fwdU;
        this.fwdV = fwdV;
        this.invU = invU;
        this.invV = invV;
        this.fwdC = fwdC;
        this.invC = invC;
        this.fwdOriginX = fwdOriginX;
        this.fwdOriginY = fwdOriginY;
        this.invOriginX = invOriginX;
        this.invOriginY = invOriginY;
        this.isInverted = isInverted;
    }

    private HornerMathTransform(HornerMathTransform source, bool isInverted)
    {
        this.degree = source.degree;
        this.isComplex = source.isComplex;
        this.hasExplicitInverse = source.hasExplicitInverse;
        this.uneg = source.uneg;
        this.vneg = source.vneg;
        this.range = source.range;
        this.inverseTolerance = source.inverseTolerance;
        this.fwdU = source.fwdU;
        this.fwdV = source.fwdV;
        this.invU = source.invU;
        this.invV = source.invV;
        this.fwdC = source.fwdC;
        this.invC = source.invC;
        this.fwdOriginX = source.fwdOriginX;
        this.fwdOriginY = source.fwdOriginY;
        this.invOriginX = source.invOriginX;
        this.invOriginY = source.invOriginY;
        this.isInverted = isInverted;
    }

    /// <inheritdoc />
    public override int DimSource => 3;

    /// <inheritdoc />
    public override int DimTarget => 3;

    /// <inheritdoc />
    public override bool Identity()
    {
        return false;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new HornerMathTransform(this, !this.isInverted);

        return this.inverse;
    }

    /// <inheritdoc />
    public override void Invert()
    {
        throw new NotSupportedException("HornerMathTransform is immutable. Use Inverse() to obtain inverted transform.");
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        _ = z;
        if (this.isComplex)
        {
            if (!this.isInverted)
            {
                this.TransformComplexForward(ref x, ref y);
                return;
            }

            if (this.hasExplicitInverse)
            {
                this.TransformComplexInverse(ref x, ref y);
            }
            else
            {
                this.TransformComplexIterativeInverse(ref x, ref y);
            }

            return;
        }

        if (!this.isInverted)
        {
            this.TransformRealForward(ref x, ref y);
            return;
        }

        if (this.hasExplicitInverse)
        {
            this.TransformRealInverse(ref x, ref y);
        }
        else
        {
            this.TransformRealIterativeInverse(ref x, ref y);
        }
    }

    /// <summary>
    /// Creates a <see cref="HornerMathTransform"/> from parsed PROJ arguments.
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
        if (args is null)
        {
            skipReason = "horner arguments were null.";
            return false;
        }

        if (!TryParseDegree(args, out int degree, out skipReason))
        {
            return false;
        }

        bool isComplex = args.ContainsKey("fwd_c") || args.ContainsKey("inv_c");
        bool hasExplicitInverse = isComplex
            ? args.ContainsKey("inv_c") || args.ContainsKey("inv_origin")
            : args.ContainsKey("inv_u") || args.ContainsKey("inv_v") || args.ContainsKey("inv_origin");
        int coefficientCount = isComplex
            ? GetComplexCoefficientCount(degree)
            : GetRealCoefficientCount(degree);

        double[] fwdU = [];
        double[] fwdV = [];
        double[] invU = [];
        double[] invV = [];
        double[] fwdC = [];
        double[] invC = [];

        if (isComplex)
        {
            if (!TryParseCoefficientList(args, "fwd_c", coefficientCount, out double[]? parsedFwdC, out skipReason))
            {
                return false;
            }

            fwdC = ArgumentGuard.ThrowIfNull(parsedFwdC, nameof(parsedFwdC));

            if (hasExplicitInverse)
            {
                if (!TryParseCoefficientList(args, "inv_c", coefficientCount, out double[]? parsedInvC, out skipReason))
                {
                    return false;
                }

                invC = ArgumentGuard.ThrowIfNull(parsedInvC, nameof(parsedInvC));
            }
        }
        else
        {
            if (!TryParseCoefficientList(args, "fwd_u", coefficientCount, out double[]? parsedFwdU, out skipReason)
                || !TryParseCoefficientList(args, "fwd_v", coefficientCount, out double[]? parsedFwdV, out skipReason))
            {
                return false;
            }

            fwdU = ArgumentGuard.ThrowIfNull(parsedFwdU, nameof(parsedFwdU));
            fwdV = ArgumentGuard.ThrowIfNull(parsedFwdV, nameof(parsedFwdV));

            if (hasExplicitInverse)
            {
                if (!TryParseCoefficientList(args, "inv_u", coefficientCount, out double[]? parsedInvU, out skipReason)
                    || !TryParseCoefficientList(args, "inv_v", coefficientCount, out double[]? parsedInvV, out skipReason))
                {
                    return false;
                }

                invU = ArgumentGuard.ThrowIfNull(parsedInvU, nameof(parsedInvU));
                invV = ArgumentGuard.ThrowIfNull(parsedInvV, nameof(parsedInvV));
            }
        }

        if (!TryParseOrigin(args, "fwd_origin", out double fwdOriginX, out double fwdOriginY, out skipReason))
        {
            return false;
        }

        double invOriginX = 0d;
        double invOriginY = 0d;
        if (hasExplicitInverse
            && !TryParseOrigin(args, "inv_origin", out invOriginX, out invOriginY, out skipReason))
        {
            return false;
        }

        if (!TryParseOptionalSingle(args, "range", DefaultRange, out double range, out skipReason))
        {
            return false;
        }

        if (range <= 0d)
        {
            skipReason = "Invalid value for +range.";
            return false;
        }

        if (!TryParseOptionalSingle(args, "inv_tolerance", DefaultInverseTolerance, out double inverseTolerance, out skipReason))
        {
            return false;
        }

        if (!args.ContainsKey("inv_tolerance") && args.ContainsKey("tolerance"))
        {
            if (!TryParseOptionalSingle(args, "tolerance", DefaultInverseTolerance, out inverseTolerance, out skipReason))
            {
                return false;
            }
        }

        if (inverseTolerance <= 0d)
        {
            skipReason = "Invalid value for +inv_tolerance.";
            return false;
        }

        transform = new HornerMathTransform(
            degree,
            isComplex,
            hasExplicitInverse,
            args.ContainsKey("uneg"),
            args.ContainsKey("vneg"),
            range,
            inverseTolerance,
            fwdU,
            fwdV,
            invU,
            invV,
            fwdC,
            invC,
            fwdOriginX,
            fwdOriginY,
            invOriginX,
            invOriginY,
            false);

        if (args.ContainsKey("inv"))
        {
            transform = transform.Inverse();
        }

        return true;
    }

    private static int GetRealCoefficientCount(int order)
    {
        return ((order + 1) * (order + 2)) / 2;
    }

    private static int GetComplexCoefficientCount(int order)
    {
        return (2 * order) + 2;
    }

    private static bool TryParseDegree(
        Dictionary<string, string> args,
        out int degree,
        out string? skipReason)
    {
        degree = 0;
        skipReason = null;

        if (!args.TryGetValue("deg", out string? degreeToken) || string.IsNullOrWhiteSpace(degreeToken))
        {
            skipReason = "Must specify polynomial degree, (+deg=n)";
            return false;
        }

        if (!int.TryParse(degreeToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out degree))
        {
            skipReason = "Invalid value for +deg.";
            return false;
        }

        if (degree is < 0 or > MaximumSupportedDegree)
        {
            skipReason = $"Degree is unreasonable: {degree.ToString(CultureInfo.InvariantCulture)}";
            return false;
        }

        return true;
    }

    private static bool TryParseCoefficientList(
        Dictionary<string, string> args,
        string key,
        int expectedCount,
        [NotNullWhen(true)] out double[]? coefficients,
        out string? skipReason)
    {
        coefficients = null;
        skipReason = null;

        if (!args.TryGetValue(key, out string? token) || string.IsNullOrWhiteSpace(token))
        {
            skipReason = $"missing {key}";
            return false;
        }

        double[] values = new double[expectedCount];
        CsvParseStatus parseStatus = SpanParseUtility.TryParseCsvValues(token.AsSpan(), values, out int parsedCount);
        if (parseStatus != CsvParseStatus.Success || parsedCount != expectedCount)
        {
            skipReason = $"Malformed polynomium set {key}. need {expectedCount.ToString(CultureInfo.InvariantCulture)} coefs";
            return false;
        }

        coefficients = values;
        return true;
    }

    private static bool TryParseOrigin(
        Dictionary<string, string> args,
        string key,
        out double x,
        out double y,
        out string? skipReason)
    {
        x = 0d;
        y = 0d;
        if (!TryParseCoefficientList(args, key, 2, out double[]? values, out skipReason))
        {
            return false;
        }

        x = values[0];
        y = values[1];
        return true;
    }

    private static bool TryParseOptionalSingle(
        Dictionary<string, string> args,
        string key,
        double defaultValue,
        out double value,
        out string? skipReason)
    {
        value = defaultValue;
        skipReason = null;

        if (!args.TryGetValue(key, out string? token) || string.IsNullOrWhiteSpace(token))
        {
            return true;
        }

        Span<double> parsed = stackalloc double[1];
        CsvParseStatus parseStatus = SpanParseUtility.TryParseCsvValues(token.AsSpan(), parsed, out int parsedCount);
        if (parseStatus != CsvParseStatus.Success || parsedCount != 1)
        {
            skipReason = $"Invalid value for +{key}.";
            return false;
        }

        value = parsed[0];
        return true;
    }

    private static (double E, double N) EvaluateReal(
        int degree,
        double[] cx,
        double[] cy,
        double e,
        double n,
        int orderOffset)
    {
        int size = GetRealCoefficientCount(degree);
        int xIndex = size - 1;
        int yIndex = size - 1;

        double northing = cy[yIndex--];
        double easting = cx[xIndex--];
        for (int r = degree; r > orderOffset; r--)
        {
            double u = cy[yIndex--];
            double v = cx[xIndex--];
            for (int c = degree; c >= r; c--)
            {
                u = (n * u) + cy[yIndex--];
                v = (e * v) + cx[xIndex--];
            }

            northing = (e * northing) + u;
            easting = (n * easting) + v;
        }

        return (easting, northing);
    }

    private static double EvaluateSingleReal(int degree, double[] coefficients, double value, int orderOffset)
    {
        int index = degree;
        double result = coefficients[index--];
        for (int r = degree; r > orderOffset; r--)
        {
            result = (value * result) + coefficients[index--];
        }

        return result;
    }

    private static (double E, double N) EvaluateComplex(
        int degree,
        double[] coefficients,
        double e,
        double n,
        int orderOffset)
    {
        int size = GetComplexCoefficientCount(degree);
        int begin = orderOffset * 2;
        int index = size - 1;

        double outE = coefficients[index--];
        double outN = coefficients[index--];
        while (index >= begin)
        {
            double intermediate = (n * outE) + (e * outN) + coefficients[index--];
            outN = (n * outN) - (e * outE) + coefficients[index--];
            outE = intermediate;
        }

        return (outE, outN);
    }

    private void TransformRealForward(ref double x, ref double y)
    {
        double e = x - this.fwdOriginX;
        double n = y - this.fwdOriginY;
        this.ValidateRange(n, e);
        (x, y) = EvaluateReal(this.degree, this.fwdU, this.fwdV, e, n, 0);
    }

    private void TransformRealInverse(ref double x, ref double y)
    {
        double e = x - this.invOriginX;
        double n = y - this.invOriginY;
        this.ValidateRange(n, e);
        (x, y) = EvaluateReal(this.degree, this.invU, this.invV, e, n, 0);
    }

    private void TransformRealIterativeInverse(ref double x, ref double y)
    {
        double e = x;
        double n = y;
        this.ValidateRange(n, e);

        double deltaE = e - this.fwdU[0];
        double deltaN = n - this.fwdV[0];
        double x0 = 0d;
        double y0 = 0d;
        for (int i = 0; i < MaxInverseIterations; i++)
        {
            (double mb, double mc) = EvaluateReal(this.degree, this.fwdU, this.fwdV, x0, y0, 1);
            double ma = EvaluateSingleReal(this.degree, this.fwdU, x0, 1);
            double md = EvaluateSingleReal(this.degree, this.fwdV, y0, 1);

            double determinant = (ma * md) - (mb * mc);
            if (Math.Abs(determinant) <= DeterminantTolerance)
            {
                break;
            }

            double inverseDeterminant = 1d / determinant;
            double nextX = inverseDeterminant * ((md * deltaE) - (mb * deltaN));
            double nextY = inverseDeterminant * ((ma * deltaN) - (mc * deltaE));
            bool converged = Math.Abs(nextX - x0) < this.inverseTolerance
        && Math.Abs(nextY - y0) < this.inverseTolerance;
            x0 = nextX;
            y0 = nextY;
            if (converged)
            {
                x = x0 + this.fwdOriginX;
                y = y0 + this.fwdOriginY;
                return;
            }
        }

        ArgumentGuard.ThrowArgument("horner inverse iteration did not converge.");
    }

    private void TransformComplexForward(ref double x, ref double y)
    {
        this.TransformComplexDefault(ref x, ref y, true);
    }

    private void TransformComplexInverse(ref double x, ref double y)
    {
        this.TransformComplexDefault(ref x, ref y, false);
    }

    private void TransformComplexDefault(ref double x, ref double y, bool forward)
    {
        double e = forward ? x - this.fwdOriginX : x - this.invOriginX;
        double n = forward ? y - this.fwdOriginY : y - this.invOriginY;

        if (this.uneg)
        {
            e = -e;
        }

        if (this.vneg)
        {
            n = -n;
        }

        this.ValidateRange(n, e);
        (x, y) = EvaluateComplex(this.degree, forward ? this.fwdC : this.invC, e, n, 0);
    }

    private void TransformComplexIterativeInverse(ref double x, ref double y)
    {
        double e = x;
        double n = y;
        this.ValidateRange(n, e);

        // Real component corresponds to northing, imaginary component to easting.
        double dzReal = n - this.fwdC[0];
        double dzImaginary = e - this.fwdC[1];
        double w0Real = 0d;
        double w0Imaginary = 0d;
        for (int i = 0; i < MaxInverseIterations; i++)
        {
            (double derivativeE, double derivativeN) = EvaluateComplex(this.degree, this.fwdC, w0Imaginary, w0Real, 1);
            double detReal = derivativeN;
            double detImaginary = derivativeE;
            double denominator = (detReal * detReal) + (detImaginary * detImaginary);
            if (denominator <= DeterminantTolerance)
            {
                break;
            }

            double nextReal = ((dzReal * detReal) + (dzImaginary * detImaginary)) / denominator;
            double nextImaginary = ((dzImaginary * detReal) - (dzReal * detImaginary)) / denominator;

            bool converged = Math.Abs(nextReal - w0Real) < this.inverseTolerance
        && Math.Abs(nextImaginary - w0Imaginary) < this.inverseTolerance;
            w0Real = nextReal;
            w0Imaginary = nextImaginary;
            if (converged)
            {
                double outE = w0Imaginary;
                double outN = w0Real;
                if (this.uneg)
                {
                    outE = -outE;
                }

                if (this.vneg)
                {
                    outN = -outN;
                }

                x = outE + this.fwdOriginX;
                y = outN + this.fwdOriginY;
                return;
            }
        }

        ArgumentGuard.ThrowArgument("horner inverse iteration did not converge.");
    }

    private void ValidateRange(double n, double e)
    {
        if (Math.Abs(n) > this.range || Math.Abs(e) > this.range)
        {
            ArgumentGuard.ThrowArgument("Coordinate is outside horner operation range.");
        }
    }
}
