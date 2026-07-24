// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Extended Transverse Mercator projection (<c>etmerc</c>).
/// </summary>
/// <remarks>
/// <para>This implementation ports the exact Poder/Engsager ETMERC kernel used by modern PROJ.
/// It is limited to ellipsoidal inputs and now backs the default ellipsoidal
/// <c>tmerc</c>/<c>transverse_mercator</c>/<c>utm</c> aliases in addition to the
/// explicit <c>etmerc</c> / <c>extended_transverse_mercator</c> names. Spherical
/// and explicit <c>+approx</c> routes remain on the classic Snyder-style
/// <see cref="TransverseMercator"/> implementation.</para>
/// </remarks>
internal sealed class ExtendedTransverseMercator : MapProjection
{
    private const double DomainLimit = 2.623395162778d;

    private readonly double[] conformalToGeographic;
    private readonly double[] geographicToConformal;
    private readonly double[] conformalToRectifying;
    private readonly double[] rectifyingToConformal;
    private readonly double meridianQuadrantScale;
    private readonly double originNorthingOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtendedTransverseMercator"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public ExtendedTransverseMercator(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    private ExtendedTransverseMercator(IEnumerable<ProjectionParameter> parameters, ExtendedTransverseMercator? inverse)
        : base(parameters, inverse)
    {
        if (this.es <= 0d)
        {
            ProjectionThrowHelper.ThrowNotSupported("Extended Transverse Mercator requires an ellipsoidal model.");
        }

        this.Name = "Extended_Transverse_Mercator";

        double thirdFlattening = (this.semiMajor - this.semiMinor) / (this.semiMajor + this.semiMinor);
        this.conformalToGeographic = AuxiliaryLatitudeSeries.BuildConformalToGeographicCoefficients(thirdFlattening);
        this.geographicToConformal = AuxiliaryLatitudeSeries.BuildGeographicToConformalCoefficients(thirdFlattening);
        this.conformalToRectifying = AuxiliaryLatitudeSeries.BuildConformalToRectifyingCoefficients(thirdFlattening);
        this.rectifyingToConformal = AuxiliaryLatitudeSeries.BuildRectifyingToConformalCoefficients(thirdFlattening);

        this.meridianQuadrantScale = this.scaleFactor * this.semiMajor * AuxiliaryLatitudeSeries.RectifyingRadius(thirdFlattening);
        double originConformalLatitude = AuxiliaryLatitudeSeries.Convert(this.latOrigin, this.geographicToConformal);
        this.originNorthingOffset = -this.meridianQuadrantScale * AuxiliaryLatitudeSeries.Convert(originConformalLatitude, this.conformalToRectifying);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new ExtendedTransverseMercator(this.Parameters.ToProjectionParameter(), this);
        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double conformalLatitude = AuxiliaryLatitudeSeries.Convert(lat, this.geographicToConformal);
        double sinConformalLatitude = Math.Sin(conformalLatitude);
        double cosConformalLatitude = Math.Cos(conformalLatitude);
        double sinLambda = Math.Sin(lambda);
        double cosLambda = Math.Cos(lambda);

        double cosLatitudeLongitude = cosConformalLatitude * cosLambda;
        double normalizedNorthing = Math.Atan2(sinConformalLatitude, cosLatitudeLongitude);
        double inverseDenominator = 1d / Hypot(sinConformalLatitude, cosLatitudeLongitude);
        double tangentEasting = sinLambda * cosConformalLatitude * inverseDenominator;
        double normalizedEasting = Asinh(tangentEasting);

        double twiceInverseDenominator = 2d * inverseDenominator;
        double twiceInverseDenominatorSquared = twiceInverseDenominator * inverseDenominator;
        double realFactor = cosLatitudeLongitude * twiceInverseDenominatorSquared;
        double sinArgumentReal = sinConformalLatitude * realFactor;
        double cosArgumentReal = (cosLatitudeLongitude * realFactor) - 1d;
        double sinhArgumentImaginary = tangentEasting * twiceInverseDenominator;
        double coshArgumentImaginary = twiceInverseDenominatorSquared - 1d;

        ComplexClenshaw(
            this.conformalToRectifying,
            sinArgumentReal,
            cosArgumentReal,
            sinhArgumentImaginary,
            coshArgumentImaginary,
            out double deltaNorthing,
            out double deltaEasting);

        normalizedNorthing += deltaNorthing;
        normalizedEasting += deltaEasting;

        if (Math.Abs(normalizedEasting) > DomainLimit)
        {
            ProjectionThrowHelper.ThrowOutsideProjectionDomain();
        }

        lon = this.meridianQuadrantScale * normalizedEasting;
        lat = (this.meridianQuadrantScale * normalizedNorthing) + this.originNorthingOffset;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double normalizedNorthing = (y - this.originNorthingOffset) / this.meridianQuadrantScale;
        double normalizedEasting = x / this.meridianQuadrantScale;

        if (Math.Abs(normalizedEasting) > DomainLimit)
        {
            ProjectionThrowHelper.ThrowOutsideProjectionDomain();
        }

        double sinArgumentReal = Math.Sin(2d * normalizedNorthing);
        double cosArgumentReal = Math.Cos(2d * normalizedNorthing);
        double expDoubleEasting = Math.Exp(2d * normalizedEasting);
        double halfInverseExpDoubleEasting = 0.5d / expDoubleEasting;
        double sinhArgumentImaginary = (0.5d * expDoubleEasting) - halfInverseExpDoubleEasting;
        double coshArgumentImaginary = (0.5d * expDoubleEasting) + halfInverseExpDoubleEasting;

        ComplexClenshaw(
            this.rectifyingToConformal,
            sinArgumentReal,
            cosArgumentReal,
            sinhArgumentImaginary,
            coshArgumentImaginary,
            out double deltaNorthing,
            out double deltaEasting);

        normalizedNorthing += deltaNorthing;
        normalizedEasting += deltaEasting;

        double sinConformalLatitude = Math.Sin(normalizedNorthing);
        double cosConformalLatitude = Math.Cos(normalizedNorthing);
        double sinhNormalizedEasting = Math.Sinh(normalizedEasting);
        double lambda = Math.Atan2(sinhNormalizedEasting, cosConformalLatitude);
        double modulusEasting = Hypot(sinhNormalizedEasting, cosConformalLatitude);
        double normalization = Hypot(sinConformalLatitude, modulusEasting);
        double conformalLatitude = Math.Atan2(sinConformalLatitude, modulusEasting);
        double phi = AuxiliaryLatitudeSeries.Convert(
            conformalLatitude,
            sinConformalLatitude / normalization,
            modulusEasting / normalization,
            this.conformalToGeographic);

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }

    private static double Asinh(double value)
    {
        return Math.Log(value + Hypot(1d, value));
    }

    private static void ComplexClenshaw(
        double[] coefficients,
        double sinArgumentReal,
        double cosArgumentReal,
        double sinhArgumentImaginary,
        double coshArgumentImaginary,
        out double real,
        out double imaginary)
    {
        coefficients = ArgumentGuard.ThrowIfNull(coefficients, nameof(coefficients));

        double recurrenceReal = 2d * cosArgumentReal * coshArgumentImaginary;
        double recurrenceImaginary = -2d * sinArgumentReal * sinhArgumentImaginary;
        double previousImaginary = 0d;
        double previousReal = 0d;
        double currentImaginary = 0d;
        double currentReal = coefficients[coefficients.Length - 1];

        for (int i = coefficients.Length - 2; i >= 0; i--)
        {
            double previousPreviousReal = previousReal;
            double previousPreviousImaginary = previousImaginary;
            previousReal = currentReal;
            previousImaginary = currentImaginary;
            currentReal = -previousPreviousReal + (recurrenceReal * previousReal) - (recurrenceImaginary * previousImaginary) + coefficients[i];
            currentImaginary = -previousPreviousImaginary + (recurrenceImaginary * previousReal) + (recurrenceReal * previousImaginary);
        }

        double baseReal = sinArgumentReal * coshArgumentImaginary;
        double baseImaginary = cosArgumentReal * sinhArgumentImaginary;
        real = (baseReal * currentReal) - (baseImaginary * currentImaginary);
        imaginary = (baseReal * currentImaginary) + (baseImaginary * currentReal);
    }
}
