// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Provides the shared implementation of the modified stereographic projection family.
/// </summary>
[Serializable]
internal abstract class ModifiedStereographicProjectionBase : MapProjection
{
    private const int MaximumNewtonIterations = 20;
    private const double NewtonTolerance = 1e-12d;

    private static readonly ComplexNumber[] MilOsCoefficients =
    [
        new ComplexNumber(0.924500d, 0d),
        new ComplexNumber(0d, 0d),
        new ComplexNumber(0.019430d, 0d),
    ];

    private static readonly ComplexNumber[] LeeOsCoefficients =
    [
        new ComplexNumber(0.721316d, 0d),
        new ComplexNumber(0d, 0d),
        new ComplexNumber(-0.0088162d, -0.00617325d),
    ];

    private static readonly ComplexNumber[] Gs48Coefficients =
    [
        new ComplexNumber(0.98879d, 0d),
        new ComplexNumber(0d, 0d),
        new ComplexNumber(-0.050909d, 0d),
        new ComplexNumber(0d, 0d),
        new ComplexNumber(0.075528d, 0d),
    ];

    private static readonly ComplexNumber[] AlskEllipsoidalCoefficients =
    [
        new ComplexNumber(0.9945303d, 0d),
        new ComplexNumber(0.0052083d, -0.0027404d),
        new ComplexNumber(0.0072721d, 0.0048181d),
        new ComplexNumber(-0.0151089d, -0.1932526d),
        new ComplexNumber(0.0642675d, -0.1381226d),
        new ComplexNumber(0.3582802d, -0.2884586d),
    ];

    private static readonly ComplexNumber[] AlskSphericalCoefficients =
    [
        new ComplexNumber(0.9972523d, 0d),
        new ComplexNumber(0.0052513d, -0.0041175d),
        new ComplexNumber(0.0074606d, 0.0048125d),
        new ComplexNumber(-0.0153783d, -0.1968253d),
        new ComplexNumber(0.0636871d, -0.1408027d),
        new ComplexNumber(0.3660976d, -0.2937382d),
    ];

    private static readonly ComplexNumber[] Gs50EllipsoidalCoefficients =
    [
        new ComplexNumber(0.9827497d, 0d),
        new ComplexNumber(0.0210669d, 0.0053804d),
        new ComplexNumber(-0.1031415d, -0.0571664d),
        new ComplexNumber(-0.0323337d, -0.0322847d),
        new ComplexNumber(0.0502303d, 0.1211983d),
        new ComplexNumber(0.0251805d, 0.0895678d),
        new ComplexNumber(-0.0012315d, -0.1416121d),
        new ComplexNumber(0.0072202d, -0.1317091d),
        new ComplexNumber(-0.0194029d, 0.0759677d),
        new ComplexNumber(-0.0210072d, 0.0834037d),
    ];

    private static readonly ComplexNumber[] Gs50SphericalCoefficients =
    [
        new ComplexNumber(0.9842990d, 0d),
        new ComplexNumber(0.0211642d, 0.0037608d),
        new ComplexNumber(-0.1036018d, -0.0575102d),
        new ComplexNumber(-0.0329095d, -0.0320119d),
        new ComplexNumber(0.0499471d, 0.1223335d),
        new ComplexNumber(0.0260460d, 0.0899805d),
        new ComplexNumber(0.0007388d, -0.1435792d),
        new ComplexNumber(0.0075848d, -0.1334108d),
        new ComplexNumber(-0.0216473d, 0.0776645d),
        new ComplexNumber(-0.0225161d, 0.0853673d),
    ];

    private readonly double lambda0;
    private readonly double phi0;
    private readonly double effectiveSemiMajor;
    private readonly double effectiveScale;
    private readonly double inverseEffectiveScale;
    private readonly double effectiveEs;
    private readonly double effectiveE;
    private readonly ComplexNumber[] coefficients;
    private readonly int polynomialOrder;
    private readonly double schio;
    private readonly double cchio;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModifiedStereographicProjectionBase"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    /// <param name="name">Projection name.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2214:Do not call overridable methods in constructors",
        Justification = "Variant-specific constants must be provided by derived projection types during initialization.")]
    protected ModifiedStereographicProjectionBase(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse, string name)
        : base(parameters, inverse)
    {
        this.Name = name;

        this.ConfigureVariant(
            out this.lambda0,
            out this.phi0,
            out this.effectiveSemiMajor,
            out this.effectiveEs,
            out this.coefficients,
            out this.polynomialOrder);

        this.effectiveE = Math.Sqrt(this.effectiveEs);
        this.effectiveScale = this.effectiveSemiMajor * this.scaleFactor;
        if (Math.Abs(this.effectiveScale) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Scale factor must be non-zero for modified stereographic projection.");
        }

        this.inverseEffectiveScale = 1d / this.effectiveScale;
        double chi0 = this.effectiveEs != 0d
            ? this.ComputeConformalLatitude(this.phi0)
            : this.phi0;

        this.schio = Math.Sin(chi0);
        this.cchio = Math.Cos(chi0);
    }

    /// <summary>
    /// Gets the coefficient set used by <c>mil_os</c>.
    /// </summary>
    /// <returns>The coefficient array.</returns>
    protected static ComplexNumber[] GetMilOsCoefficients() => MilOsCoefficients;

    /// <summary>
    /// Gets the coefficient set used by <c>lee_os</c>.
    /// </summary>
    /// <returns>The coefficient array.</returns>
    protected static ComplexNumber[] GetLeeOsCoefficients() => LeeOsCoefficients;

    /// <summary>
    /// Gets the coefficient set used by <c>gs48</c>.
    /// </summary>
    /// <returns>The coefficient array.</returns>
    protected static ComplexNumber[] GetGs48Coefficients() => Gs48Coefficients;

    /// <summary>
    /// Gets the coefficient set used by ellipsoidal <c>alsk</c>.
    /// </summary>
    /// <returns>The coefficient array.</returns>
    protected static ComplexNumber[] GetAlskEllipsoidalCoefficients() => AlskEllipsoidalCoefficients;

    /// <summary>
    /// Gets the coefficient set used by spherical <c>alsk</c>.
    /// </summary>
    /// <returns>The coefficient array.</returns>
    protected static ComplexNumber[] GetAlskSphericalCoefficients() => AlskSphericalCoefficients;

    /// <summary>
    /// Gets the coefficient set used by ellipsoidal <c>gs50</c>.
    /// </summary>
    /// <returns>The coefficient array.</returns>
    protected static ComplexNumber[] GetGs50EllipsoidalCoefficients() => Gs50EllipsoidalCoefficients;

    /// <summary>
    /// Gets the coefficient set used by spherical <c>gs50</c>.
    /// </summary>
    /// <returns>The coefficient array.</returns>
    protected static ComplexNumber[] GetGs50SphericalCoefficients() => Gs50SphericalCoefficients;

    /// <summary>
    /// Configures variant-specific constants.
    /// </summary>
    /// <param name="lambda0">Variant central meridian.</param>
    /// <param name="phi0">Variant latitude of origin.</param>
    /// <param name="semiMajor">Variant semi-major axis.</param>
    /// <param name="es">Variant eccentricity squared.</param>
    /// <param name="coefficients">Complex polynomial coefficients.</param>
    /// <param name="polynomialOrder">Polynomial order used by PROJ.</param>
    protected abstract void ConfigureVariant(
        out double lambda0,
        out double phi0,
        out double semiMajor,
        out double es,
        out ComplexNumber[] coefficients,
        out int polynomialOrder);

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.lambda0);
        double chi = this.ComputeConformalLatitude(lat);
        double schi = Math.Sin(chi);
        double cchi = Math.Cos(chi);
        double sinLambda = Math.Sin(lambda);
        double cosLambda = Math.Cos(lambda);

        double denominator = 1d + (this.schio * schi) + (this.cchio * cchi * cosLambda);
        if (Math.Abs(denominator) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double s = 2d / denominator;
        ComplexNumber p = new(
            s * cchi * sinLambda,
            s * ((this.cchio * schi) - (this.schio * cchi * cosLambda)));

        p = EvaluateComplexPolynomial(p, this.coefficients, this.polynomialOrder);
        lon = this.effectiveScale * p.Real;
        lat = this.effectiveScale * p.Imaginary;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double targetX = x * this.inverseEffectiveScale;
        double targetY = y * this.inverseEffectiveScale;

        ComplexNumber p = new(targetX, targetY);
        bool converged = false;
        for (int i = 0; i < MaximumNewtonIterations; i++)
        {
            ComplexNumber f = EvaluateComplexPolynomialAndDerivative(p, this.coefficients, this.polynomialOrder, out ComplexNumber derivative);
            f = new ComplexNumber(f.Real - targetX, f.Imaginary - targetY);

            double denominator = (derivative.Real * derivative.Real) + (derivative.Imaginary * derivative.Imaginary);
            if (Math.Abs(denominator) <= Eps10)
            {
                break;
            }

            ComplexNumber delta = new(
                -((f.Real * derivative.Real) + (f.Imaginary * derivative.Imaginary)) / denominator,
                -((f.Imaginary * derivative.Real) - (f.Real * derivative.Imaginary)) / denominator);

            p = new ComplexNumber(p.Real + delta.Real, p.Imaginary + delta.Imaginary);
            if (Math.Abs(delta.Real) + Math.Abs(delta.Imaginary) <= NewtonTolerance)
            {
                converged = true;
                break;
            }
        }

        if (!converged)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double rh = Hypot(p.Real, p.Imaginary);
        if (rh <= NewtonTolerance)
        {
            x = Adjust_lon(this.lambda0);
            y = this.phi0;
            return;
        }

        double z = 2d * Math.Atan(0.5d * rh);
        double sinZ = Math.Sin(z);
        double cosZ = Math.Cos(z);
        double chi = Asinz((cosZ * this.schio) + ((p.Imaginary * sinZ * this.cchio) / rh));
        double phi = this.ComputeGeodeticLatitude(chi);
        double lambda = Math.Atan2(
            p.Real * sinZ,
            (rh * this.cchio * cosZ) - (p.Imaginary * this.schio * sinZ));

        x = Adjust_lon(this.lambda0 + lambda);
        y = phi;
    }

    private static ComplexNumber EvaluateComplexPolynomial(ComplexNumber z, IReadOnlyList<ComplexNumber> coefficients, int order)
    {
        int index = order;
        ComplexNumber value = coefficients[index];
        while (index > 0)
        {
            index--;
            double previousReal = value.Real;
            value = new ComplexNumber(
                coefficients[index].Real + (z.Real * previousReal) - (z.Imaginary * value.Imaginary),
                coefficients[index].Imaginary + (z.Real * value.Imaginary) + (z.Imaginary * previousReal));
        }

        double tailReal = value.Real;
        return new ComplexNumber(
            (z.Real * tailReal) - (z.Imaginary * value.Imaginary),
            (z.Real * value.Imaginary) + (z.Imaginary * tailReal));
    }

    private static ComplexNumber EvaluateComplexPolynomialAndDerivative(
        ComplexNumber z,
        IReadOnlyList<ComplexNumber> coefficients,
        int order,
        out ComplexNumber derivative)
    {
        int index = order;
        ComplexNumber value = coefficients[index];
        ComplexNumber derivativeValue = value;
        bool first = true;

        while (index > 0)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                double derivativeReal = derivativeValue.Real;
                derivativeValue = new ComplexNumber(
                    value.Real + (z.Real * derivativeReal) - (z.Imaginary * derivativeValue.Imaginary),
                    value.Imaginary + (z.Real * derivativeValue.Imaginary) + (z.Imaginary * derivativeReal));
            }

            index--;
            double valueReal = value.Real;
            value = new ComplexNumber(
                coefficients[index].Real + (z.Real * valueReal) - (z.Imaginary * value.Imaginary),
                coefficients[index].Imaginary + (z.Real * value.Imaginary) + (z.Imaginary * valueReal));
        }

        double derivativeTail = derivativeValue.Real;
        derivativeValue = new ComplexNumber(
            value.Real + (z.Real * derivativeTail) - (z.Imaginary * derivativeValue.Imaginary),
            value.Imaginary + (z.Real * derivativeValue.Imaginary) + (z.Imaginary * derivativeTail));

        double valueTail = value.Real;
        value = new ComplexNumber(
            (z.Real * valueTail) - (z.Imaginary * value.Imaginary),
            (z.Real * value.Imaginary) + (z.Imaginary * valueTail));

        derivative = derivativeValue;
        return value;
    }

    private double ComputeConformalLatitude(double geodeticLatitude)
    {
        if (this.effectiveEs == 0d)
        {
            return geodeticLatitude;
        }

        double eSinPhi = this.effectiveE * Math.Sin(geodeticLatitude);
        return (2d * Math.Atan(
                Math.Tan((HalfPi + geodeticLatitude) * 0.5d) *
                Math.Pow((1d - eSinPhi) / (1d + eSinPhi), this.effectiveE * 0.5d)))
            - HalfPi;
    }

    private double ComputeGeodeticLatitude(double conformalLatitude)
    {
        if (this.effectiveEs == 0d)
        {
            return conformalLatitude;
        }

        double phi = conformalLatitude;
        for (int i = 0; i < MaximumNewtonIterations; i++)
        {
            double eSinPhi = this.effectiveE * Math.Sin(phi);
            double deltaPhi = (2d * Math.Atan(
                    Math.Tan((HalfPi + conformalLatitude) * 0.5d) *
                    Math.Pow((1d + eSinPhi) / (1d - eSinPhi), this.effectiveE * 0.5d)))
                - HalfPi
                - phi;

            phi += deltaPhi;
            if (Math.Abs(deltaPhi) <= NewtonTolerance)
            {
                return phi;
            }
        }

        ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        return conformalLatitude;
    }

    /// <summary>
    /// Represents an immutable complex number used by the PROJ complex polynomial functions.
    /// </summary>
    /// <param name="real">Real component.</param>
    /// <param name="imaginary">Imaginary component.</param>
    protected readonly struct ComplexNumber(double real, double imaginary)
    {
        /// <summary>
        /// Gets the real component.
        /// </summary>
        public double Real { get; } = real;

        /// <summary>
        /// Gets the imaginary component.
        /// </summary>
        public double Imaginary { get; } = imaginary;
    }
}
