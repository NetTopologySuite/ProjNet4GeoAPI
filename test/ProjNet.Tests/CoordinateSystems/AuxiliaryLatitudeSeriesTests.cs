// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;
using Xunit;

/// <summary>
/// Verifies the auxiliary-latitude series coefficients used by the exact ETMERC helpers.
/// </summary>
public class AuxiliaryLatitudeSeriesTests
{
    /// <summary>
    /// Verifies the geographic-to-conformal coefficient set against WGS84 reference values.
    /// </summary>
    [Fact]
    public void BuildGeographicToConformalCoefficients_WithWgs84ThirdFlattening_ReturnsReferenceValues()
    {
        double thirdFlattening = CreateWgs84ThirdFlattening();
        double[] actual = AuxiliaryLatitudeSeries.BuildGeographicToConformalCoefficients(thirdFlattening);
        double[] expected =
        [
            -3.356554619797427665e-03,
            4.694573027162596873e-06,
            -8.194497547212908632e-09,
            1.557996682859191760e-11,
            -3.103292241538314634e-14,
            6.389147500821738608e-17,
        ];

        AssertCoefficientsEqual(expected, actual);
    }

    /// <summary>
    /// Verifies the conformal-to-geographic coefficient set against WGS84 reference values.
    /// </summary>
    [Fact]
    public void BuildConformalToGeographicCoefficients_WithWgs84ThirdFlattening_ReturnsReferenceValues()
    {
        double thirdFlattening = CreateWgs84ThirdFlattening();
        double[] actual = AuxiliaryLatitudeSeries.BuildConformalToGeographicCoefficients(thirdFlattening);
        double[] expected =
        [
            3.356551469132832127e-03,
            6.571873198628443494e-06,
            1.764640411308580812e-08,
            5.387753784255921739e-11,
            1.764007472632213096e-13,
            6.056073876794177066e-16,
        ];

        AssertCoefficientsEqual(expected, actual);
    }

    /// <summary>
    /// Verifies the conformal/rectifying coefficient sets and normalized radius against WGS84 reference values.
    /// </summary>
    [Fact]
    public void RectifyingCoefficientFamilies_WithWgs84ThirdFlattening_ReturnReferenceValues()
    {
        double thirdFlattening = CreateWgs84ThirdFlattening();
        double[] conformalToRectifying = AuxiliaryLatitudeSeries.BuildConformalToRectifyingCoefficients(thirdFlattening);
        double[] rectifyingToConformal = AuxiliaryLatitudeSeries.BuildRectifyingToConformalCoefficients(thirdFlattening);

        double[] expectedConformalToRectifying =
        [
            8.377318206244698320e-04,
            7.608527773572307478e-07,
            1.197645503329452535e-09,
            2.429170607201358663e-12,
            5.711757677865803845e-15,
            1.491117731258389510e-17,
        ];

        double[] expectedRectifyingToConformal =
        [
            -8.377321640579486446e-04,
            -5.905870152220203267e-08,
            -1.673482665283996826e-10,
            -2.164798040062705858e-13,
            -3.787978046168604770e-16,
            -7.248748890694154495e-19,
        ];

        AssertCoefficientsEqual(expectedConformalToRectifying, conformalToRectifying);
        AssertCoefficientsEqual(expectedRectifyingToConformal, rectifyingToConformal);
        Assert.Equal(9.983242984312526991e-01, AuxiliaryLatitudeSeries.RectifyingRadius(thirdFlattening), 15);
    }

    /// <summary>
    /// Verifies that the geographic/conformal coefficient pair round-trips a representative latitude.
    /// </summary>
    [Fact]
    public void Convert_WithInverseCoefficientPair_RoundTripsRepresentativeLatitude()
    {
        double thirdFlattening = CreateWgs84ThirdFlattening();
        double geographicLatitude = DegreesToRadians(40d);
        double[] geographicToConformal = AuxiliaryLatitudeSeries.BuildGeographicToConformalCoefficients(thirdFlattening);
        double[] conformalToGeographic = AuxiliaryLatitudeSeries.BuildConformalToGeographicCoefficients(thirdFlattening);

        double conformalLatitude = AuxiliaryLatitudeSeries.Convert(geographicLatitude, geographicToConformal);
        double roundTrippedLatitude = AuxiliaryLatitudeSeries.Convert(conformalLatitude, conformalToGeographic);

        Assert.Equal(6.948277525098944807e-01, conformalLatitude, 15);
        Assert.Equal(geographicLatitude, roundTrippedLatitude, 15);
    }

    private static double DegreesToRadians(double degrees)
        => degrees * (Math.PI / 180d);

    private static void AssertCoefficientsEqual(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], 15);
        }
    }

    private static double CreateWgs84ThirdFlattening()
    {
        Ellipsoid ellipsoid = Ellipsoid.WGS84;
        double flattening = 1d / ellipsoid.InverseFlattening;
        return flattening / (2d - flattening);
    }
}
