// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the New Zealand Map Grid projection (<c>nzmg</c>).
/// </summary>
/// <remarks>
/// The projection was independently verified against IOGP, "Geomatics Guidance Note 7,
/// part 2: Coordinate Conversions and Transformations including Formulas" (publication
/// 373-7-2, 2019), EPSG method 9811, and LINZ's New Zealand Map Grid specification,
/// including Technical Report TR04 on conversion between latitude/longitude and NZMG.
/// NZMG is implemented as a sixth-order complex polynomial with the standard
/// <c>lat0</c>, <c>lon0</c>, <c>false easting</c>, and <c>false northing</c> parameters,
/// and the coefficient sets used here match the published formulation.
/// </remarks>
/// <seealso href="https://www.linz.govt.nz/guidance/geodetic-system/coordinate-systems-used-new-zealand/projections/new-zealand-map-grid-nzmg">LINZ: New Zealand Map Grid specification.</seealso>
/// <seealso href="https://www.linz.govt.nz/sites/default/files/cust/tr04-conversion-between-latitude-longitude-nzmg-2003.pdf">LINZ TR04: Conversion between latitude/longitude and NZMG.</seealso>
internal class NewZealandMapGridProjection : MapProjection
{
    private const double ProjectionSemiMajor = 6378388d;
    private const int Nbf = 5;
    private const int Ntpsi = 9;
    private const int Ntphi = 8;
    private const int NewtonIterations = 20;
    private const double Epsilon = 1e-10;
    private const double Sec5ToRad = 0.4848136811095359935899141023d;
    private const double RadToSec5 = 2.062648062470963551564733573d;

    private static readonly ComplexNumber[] Bf =
    [
        new ComplexNumber(0.7557853228d, 0d),
        new ComplexNumber(0.249204646d, 0.003371507d),
        new ComplexNumber(-0.001541739d, 0.041058560d),
        new ComplexNumber(-0.10162907d, 0.01727609d),
        new ComplexNumber(-0.26623489d, -0.36249218d),
        new ComplexNumber(-0.6870983d, -1.1651967d),
    ];

    private static readonly double[] Tpsi =
    [
        0.6399175073d,
        -0.1358797613d,
        0.063294409d,
        -0.02526853d,
        0.0117879d,
        -0.0055161d,
        0.0026906d,
        -0.001333d,
        0.00067d,
        -0.00034d,
    ];

    private static readonly double[] Tphi =
    [
        1.5627014243d,
        0.5185406398d,
        -0.03333098d,
        -0.1052906d,
        -0.0368594d,
        0.007317d,
        0.01220d,
        0.00394d,
        -0.0013d,
    ];

    private readonly double latitudeOfOrigin;
    private readonly double centralMeridianNz;
    private readonly double projectionRadius;
    private readonly double inverseProjectionRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewZealandMapGridProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public NewZealandMapGridProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NewZealandMapGridProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public NewZealandMapGridProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "New_Zealand_Map_Grid";
        this.centralMeridianNz = DegreesToRadians(this.Parameters.GetOptionalParameterValue("central_meridian", 173d, "longitude_of_center"));
        this.latitudeOfOrigin = DegreesToRadians(this.Parameters.GetOptionalParameterValue("latitude_of_origin", -41d, "latitude_of_center"));
        this.projectionRadius = ProjectionSemiMajor * this.scaleFactor;
        this.inverseProjectionRadius = 1d / this.projectionRadius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new NewZealandMapGridProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double phi = (lat - this.latitudeOfOrigin) * RadToSec5;
        double pReal = Tpsi[Ntpsi];
        for (int i = Ntpsi; i > 0; i--)
        {
            pReal = Tpsi[i - 1] + (phi * pReal);
        }

        pReal *= phi;
        var p = new ComplexNumber(pReal, Adjust_lon(lon - this.centralMeridianNz));
        p = EvaluateComplexPolynomial(p, Bf, Nbf);

        lon = this.projectionRadius * p.Imaginary;
        lat = this.projectionRadius * p.Real;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double yNormalized = y * this.inverseProjectionRadius;
        double xNormalized = x * this.inverseProjectionRadius;
        var p = new ComplexNumber(yNormalized, xNormalized);
        bool converged = false;

        for (int i = 0; i < NewtonIterations; i++)
        {
            ComplexNumber f = EvaluateComplexPolynomialAndDerivative(p, Bf, Nbf, out ComplexNumber derivative);
            f.Real -= yNormalized;
            f.Imaginary -= xNormalized;

            double denominator = (derivative.Real * derivative.Real) + (derivative.Imaginary * derivative.Imaginary);
            if (Math.Abs(denominator) <= Eps10)
            {
                break;
            }

            var delta = new ComplexNumber(
                -((f.Real * derivative.Real) + (f.Imaginary * derivative.Imaginary)) / denominator,
                -((f.Imaginary * derivative.Real) - (f.Real * derivative.Imaginary)) / denominator);

            p.Real += delta.Real;
            p.Imaginary += delta.Imaginary;

            if (Math.Abs(delta.Real) + Math.Abs(delta.Imaginary) <= Epsilon)
            {
                converged = true;
                break;
            }
        }

        if (!converged)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double phi = Tphi[Ntphi];
        for (int i = Ntphi; i > 0; i--)
        {
            phi = Tphi[i - 1] + (p.Real * phi);
        }

        x = Adjust_lon(this.centralMeridianNz + p.Imaginary);
        y = this.latitudeOfOrigin + (p.Real * phi * Sec5ToRad);
    }

    private static ComplexNumber EvaluateComplexPolynomial(ComplexNumber z, IReadOnlyList<ComplexNumber> coefficients, int order)
    {
        int index = order;
        ComplexNumber value = coefficients[index];
        for (int i = order; i > 0; i--)
        {
            index--;
            double previousReal = value.Real;
            value.Real = coefficients[index].Real + (z.Real * previousReal) - (z.Imaginary * value.Imaginary);
            value.Imaginary = coefficients[index].Imaginary + (z.Real * value.Imaginary) + (z.Imaginary * previousReal);
        }

        double finalReal = value.Real;
        value.Real = (z.Real * finalReal) - (z.Imaginary * value.Imaginary);
        value.Imaginary = (z.Real * value.Imaginary) + (z.Imaginary * finalReal);
        return value;
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

        for (int i = order; i > 0; i--)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                double derivativeReal = derivativeValue.Real;
                derivativeValue.Real = value.Real + (z.Real * derivativeReal) - (z.Imaginary * derivativeValue.Imaginary);
                derivativeValue.Imaginary = value.Imaginary + (z.Real * derivativeValue.Imaginary) + (z.Imaginary * derivativeReal);
            }

            index--;
            double valueReal = value.Real;
            value.Real = coefficients[index].Real + (z.Real * valueReal) - (z.Imaginary * value.Imaginary);
            value.Imaginary = coefficients[index].Imaginary + (z.Real * value.Imaginary) + (z.Imaginary * valueReal);
        }

        double derivativeTailReal = derivativeValue.Real;
        derivativeValue.Real = value.Real + (z.Real * derivativeTailReal) - (z.Imaginary * derivativeValue.Imaginary);
        derivativeValue.Imaginary = value.Imaginary + (z.Real * derivativeValue.Imaginary) + (z.Imaginary * derivativeTailReal);

        double valueTailReal = value.Real;
        value.Real = (z.Real * valueTailReal) - (z.Imaginary * value.Imaginary);
        value.Imaginary = (z.Real * value.Imaginary) + (z.Imaginary * valueTailReal);

        derivative = derivativeValue;
        return value;
    }

    private struct ComplexNumber(double real, double imaginary)
    {
        public double Real = real;
        public double Imaginary = imaginary;
    }
}
