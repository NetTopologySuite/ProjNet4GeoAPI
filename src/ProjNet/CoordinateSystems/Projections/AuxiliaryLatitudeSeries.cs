// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;

/// <summary>
/// Provides the subset of PROJ auxiliary-latitude series needed by the exact ETMERC kernel.
/// </summary>
/// <remarks>
/// <para>The series are sixth-order expansions in the third flattening
/// <c>n = (a - b) / (a + b)</c>, following PROJ's implementation from
/// <c>latitudes.cpp</c>. Only the conformal/geographic and conformal/rectifying
/// conversions used by ETMERC are included here.</para>
/// </remarks>
internal static class AuxiliaryLatitudeSeries
{
    private const int Order = 6;

    private static readonly double[] ConformalToGeographicSeries =
    [
        2.0d, -2.0d / 3.0d, -2.0d, 116.0d / 45.0d, 26.0d / 45.0d, -2854.0d / 675.0d,
        7.0d / 3.0d, -8.0d / 5.0d, -227.0d / 45.0d, 2704.0d / 315.0d, 2323.0d / 945.0d,
        56.0d / 15.0d, -136.0d / 35.0d, -1262.0d / 105.0d, 73814.0d / 2835.0d,
        4279.0d / 630.0d, -332.0d / 35.0d, -399572.0d / 14175.0d,
        4174.0d / 315.0d, -144838.0d / 6237.0d,
        601676.0d / 22275.0d,
    ];

    private static readonly double[] GeographicToConformalSeries =
    [
        -2.0d, 2.0d / 3.0d, 4.0d / 3.0d, -82.0d / 45.0d, 32.0d / 45.0d, 4642.0d / 4725.0d,
        5.0d / 3.0d, -16.0d / 15.0d, -13.0d / 9.0d, 904.0d / 315.0d, -1522.0d / 945.0d,
        -26.0d / 15.0d, 34.0d / 21.0d, 8.0d / 5.0d, -12686.0d / 2835.0d,
        1237.0d / 630.0d, -12.0d / 5.0d, -24832.0d / 14175.0d,
        -734.0d / 315.0d, 109598.0d / 31185.0d,
        444337.0d / 155925.0d,
    ];

    private static readonly double[] ConformalToRectifyingSeries =
    [
        1.0d / 2.0d, -2.0d / 3.0d, 5.0d / 16.0d, 41.0d / 180.0d, -127.0d / 288.0d, 7891.0d / 37800.0d,
        13.0d / 48.0d, -3.0d / 5.0d, 557.0d / 1440.0d, 281.0d / 630.0d, -1983433.0d / 1935360.0d,
        61.0d / 240.0d, -103.0d / 140.0d, 15061.0d / 26880.0d, 167603.0d / 181440.0d,
        49561.0d / 161280.0d, -179.0d / 168.0d, 6601661.0d / 7257600.0d,
        34729.0d / 80640.0d, -3418889.0d / 1995840.0d,
        212378941.0d / 319334400.0d,
    ];

    private static readonly double[] RectifyingToConformalSeries =
    [
        -1.0d / 2.0d, 2.0d / 3.0d, -37.0d / 96.0d, 1.0d / 360.0d, 81.0d / 512.0d, -96199.0d / 604800.0d,
        -1.0d / 48.0d, -1.0d / 15.0d, 437.0d / 1440.0d, -46.0d / 105.0d, 1118711.0d / 3870720.0d,
        -17.0d / 480.0d, 37.0d / 840.0d, 209.0d / 4480.0d, -5569.0d / 90720.0d,
        -4397.0d / 161280.0d, 11.0d / 504.0d, 830251.0d / 7257600.0d,
        -4583.0d / 161280.0d, 108847.0d / 3991680.0d,
        -20648693.0d / 638668800.0d,
    ];

    private static readonly double[] RectifyingRadiusSeries =
    [
        1.0d,
        1.0d / 4.0d,
        1.0d / 64.0d,
        1.0d / 256.0d,
    ];

    /// <summary>
    /// Builds the conformal-to-geographic conversion coefficients for the supplied third flattening.
    /// </summary>
    /// <param name="thirdFlattening">The third flattening <c>(a - b) / (a + b)</c>.</param>
    /// <returns>An array of six Fourier coefficients.</returns>
    internal static double[] BuildConformalToGeographicCoefficients(double thirdFlattening)
    {
        return BuildCoefficients(thirdFlattening, ConformalToGeographicSeries);
    }

    /// <summary>
    /// Builds the geographic-to-conformal conversion coefficients for the supplied third flattening.
    /// </summary>
    /// <param name="thirdFlattening">The third flattening <c>(a - b) / (a + b)</c>.</param>
    /// <returns>An array of six Fourier coefficients.</returns>
    internal static double[] BuildGeographicToConformalCoefficients(double thirdFlattening)
    {
        return BuildCoefficients(thirdFlattening, GeographicToConformalSeries);
    }

    /// <summary>
    /// Builds the conformal-to-rectifying conversion coefficients for the supplied third flattening.
    /// </summary>
    /// <param name="thirdFlattening">The third flattening <c>(a - b) / (a + b)</c>.</param>
    /// <returns>An array of six Fourier coefficients.</returns>
    internal static double[] BuildConformalToRectifyingCoefficients(double thirdFlattening)
    {
        return BuildCoefficients(thirdFlattening, ConformalToRectifyingSeries);
    }

    /// <summary>
    /// Builds the rectifying-to-conformal conversion coefficients for the supplied third flattening.
    /// </summary>
    /// <param name="thirdFlattening">The third flattening <c>(a - b) / (a + b)</c>.</param>
    /// <returns>An array of six Fourier coefficients.</returns>
    internal static double[] BuildRectifyingToConformalCoefficients(double thirdFlattening)
    {
        return BuildCoefficients(thirdFlattening, RectifyingToConformalSeries);
    }

    /// <summary>
    /// Converts an auxiliary latitude angle using a precomputed Fourier series.
    /// </summary>
    /// <param name="latitude">The source auxiliary latitude in radians.</param>
    /// <param name="coefficients">The Fourier coefficients for the desired conversion.</param>
    /// <returns>The converted auxiliary latitude in radians.</returns>
    internal static double Convert(double latitude, double[] coefficients)
    {
        return Convert(latitude, Math.Sin(latitude), Math.Cos(latitude), coefficients);
    }

    /// <summary>
    /// Converts an auxiliary latitude angle using a precomputed Fourier series and supplied trigonometric terms.
    /// </summary>
    /// <param name="latitude">The source auxiliary latitude in radians.</param>
    /// <param name="sinLatitude">The sine of <paramref name="latitude"/>.</param>
    /// <param name="cosLatitude">The cosine of <paramref name="latitude"/>.</param>
    /// <param name="coefficients">The Fourier coefficients for the desired conversion.</param>
    /// <returns>The converted auxiliary latitude in radians.</returns>
    internal static double Convert(double latitude, double sinLatitude, double cosLatitude, double[] coefficients)
    {
        coefficients = ArgumentGuard.ThrowIfNull(coefficients, nameof(coefficients));
        return latitude + Clenshaw(sinLatitude, cosLatitude, coefficients);
    }

    /// <summary>
    /// Computes the rectifying radius used by PROJ's exact ETMERC kernel.
    /// </summary>
    /// <param name="thirdFlattening">The third flattening <c>(a - b) / (a + b)</c>.</param>
    /// <returns>The rectifying radius normalized by the semi-major axis.</returns>
    internal static double RectifyingRadius(double thirdFlattening)
    {
        return Polyval(thirdFlattening * thirdFlattening, RectifyingRadiusSeries.AsSpan()) / (1d + thirdFlattening);
    }

    private static double[] BuildCoefficients(double thirdFlattening, double[] seriesCoefficients)
    {
        double[] coefficients = new double[Order];
        double power = thirdFlattening;
        int offset = 0;

        for (int coefficientIndex = 0; coefficientIndex < Order; coefficientIndex++)
        {
            int polynomialOrder = Order - coefficientIndex - 1;
            coefficients[coefficientIndex] = power * Polyval(thirdFlattening, seriesCoefficients.AsSpan(offset, polynomialOrder + 1));
            offset += polynomialOrder + 1;
            power *= thirdFlattening;
        }

        return coefficients;
    }

    private static double Polyval(double x, ReadOnlySpan<double> coefficients)
    {
        double value = coefficients[coefficients.Length - 1];
        for (int i = coefficients.Length - 2; i >= 0; i--)
        {
            value = (value * x) + coefficients[i];
        }

        return value;
    }

    private static double Clenshaw(double sinLatitude, double cosLatitude, double[] coefficients)
    {
        double accumulator0 = 0d;
        double accumulator1 = 0d;
        double x = 2d * (cosLatitude - sinLatitude) * (cosLatitude + sinLatitude);

        for (int i = coefficients.Length - 1; i >= 0; i--)
        {
            double next = (x * accumulator0) - accumulator1 + coefficients[i];
            accumulator1 = accumulator0;
            accumulator0 = next;
        }

        return 2d * sinLatitude * cosLatitude * accumulator0;
    }
}
