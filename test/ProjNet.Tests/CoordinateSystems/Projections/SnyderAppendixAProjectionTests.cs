// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Text;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Verifies Snyder Appendix A worked examples against the implemented projection kernels.
/// </summary>
public sealed class SnyderAppendixAProjectionTests
{
    private const string Sphere1 = "SPHEROID[\"Sphere\",1,0]";
    private const string Sphere3 = "SPHEROID[\"Sphere\",3,0]";
    private const string Clarke66 = "SPHEROID[\"Clarke 1866\",6378206.4,294.9786982]";
    private const string International = "SPHEROID[\"International\",6378388,297]";
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Gets the Snyder Appendix A forward-reference cases.
    /// </summary>
    /// <returns>The worked forward vectors.</returns>
    public static IEnumerable<TheoryDataRow<string, string, string, double, double, double, double, double>> GetForwardCases()
    {
        const double sphereTolerance = 5e-7d;
        const double fineSphereTolerance = 3e-8d;
        const double metreTolerance = 0.15d;

        yield return CreateForwardCase(
            "Mercator sphere forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 266-268",
            BuildProjectedWkt(
                "merc",
                Sphere1,
                ("latitude_of_origin", 0d),
                ("central_meridian", -180d),
                ("scale_factor", 1d)),
            -75d,
            35d,
            1.8325957d,
            0.6528366d,
            sphereTolerance);
        yield return CreateForwardCase(
            "Mercator ellipsoidal forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 266-268",
            BuildProjectedWkt(
                "merc",
                Clarke66,
                ("latitude_of_origin", 0d),
                ("central_meridian", -180d),
                ("scale_factor", 1d)),
            -75d,
            35d,
            11688673.70d,
            4139145.60d,
            metreTolerance);

        yield return CreateForwardCase(
            "Transverse Mercator sphere forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 268-271",
            BuildProjectedWkt(
                "tmerc",
                Sphere1,
                ("latitude_of_origin", 0d),
                ("central_meridian", -75d),
                ("scale_factor", 1d)),
            -73.5d,
            40.5d,
            0.0199077d,
            0.7070276d,
            sphereTolerance);
        yield return CreateForwardCase(
            "Transverse Mercator ellipsoidal forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 268-271",
            BuildProjectedWkt(
                "tmerc",
                Clarke66,
                ("latitude_of_origin", 0d),
                ("central_meridian", -75d),
                ("scale_factor", 0.9996d)),
            -73.5d,
            40.5d,
            127106.50d,
            4484124.40d,
            metreTolerance);

        yield return CreateForwardCase(
            "Cylindrical Equal Area sphere forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 272-273",
            BuildProjectedWkt(
                "cea",
                Sphere1,
                ("latitude_of_origin", 0d),
                ("central_meridian", -75d),
                ("standard_parallel_1", 30d)),
            80d,
            35d,
            2.3428242d,
            0.6623090d,
            sphereTolerance);

        // The extracted Appendix A normal-aspect ellipsoidal CEA numbers are internally
        // consistent for phi = 5 degrees, which matches the q/y values and inverse block.
        yield return CreateForwardCase(
            "Cylindrical Equal Area ellipsoidal forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 281-287",
            BuildProjectedWkt(
                "cea",
                Clarke66,
                ("latitude_of_origin", 0d),
                ("central_meridian", -75d),
                ("standard_parallel_1", 5d)),
            -78d,
            5d,
            -332699.80d,
            554248.50d,
            metreTolerance);

        yield return CreateForwardCase(
            "Albers sphere forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 291-294",
            BuildProjectedWkt(
                "aea",
                Sphere1,
                ("latitude_of_origin", 23d),
                ("central_meridian", -96d),
                ("standard_parallel_1", 29.5d),
                ("standard_parallel_2", 45.5d)),
            -75d,
            35d,
            0.2952720d,
            0.2416774d,
            sphereTolerance);
        yield return CreateForwardCase(
            "Albers ellipsoidal forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 291-294",
            BuildProjectedWkt(
                "aea",
                Clarke66,
                ("latitude_of_origin", 23d),
                ("central_meridian", -96d),
                ("standard_parallel_1", 29.5d),
                ("standard_parallel_2", 45.5d)),
            -75d,
            35d,
            1885472.70d,
            1535925.00d,
            metreTolerance);

        yield return CreateForwardCase(
            "Lambert Conformal Conic sphere forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 295-298",
            BuildProjectedWkt(
                "lcc",
                Sphere1,
                ("latitude_of_origin", 23d),
                ("central_meridian", -96d),
                ("standard_parallel_1", 33d),
                ("standard_parallel_2", 45d)),
            -75d,
            35d,
            0.2966785d,
            0.2462112d,
            sphereTolerance);
        yield return CreateForwardCase(
            "Lambert Conformal Conic ellipsoidal forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 296-298",
            BuildProjectedWkt(
                "lcc",
                Clarke66,
                ("latitude_of_origin", 23d),
                ("central_meridian", -96d),
                ("standard_parallel_1", 33d),
                ("standard_parallel_2", 45d)),
            -75d,
            35d,
            1894410.90d,
            1564649.50d,
            metreTolerance);

        yield return CreateForwardCase(
            "Equidistant Conic sphere forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 298-301",
            BuildProjectedWkt(
                "eqdc",
                Sphere1,
                ("latitude_of_origin", 23d),
                ("central_meridian", -96d),
                ("standard_parallel_1", 29.5d),
                ("standard_parallel_2", 45.5d)),
            -75d,
            35d,
            0.2952057d,
            0.2424021d,
            sphereTolerance);
        yield return CreateForwardCase(
            "Equidistant Conic ellipsoidal forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 299-301",
            BuildProjectedWkt(
                "eqdc",
                Clarke66,
                ("latitude_of_origin", 23d),
                ("central_meridian", -96d),
                ("standard_parallel_1", 29.5d),
                ("standard_parallel_2", 45.5d)),
            -75d,
            35d,
            1885051.90d,
            1540507.60d,
            metreTolerance);

        yield return CreateForwardCase(
            "Lambert Azimuthal Equal Area sphere forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 332-337",
            BuildProjectedWkt(
                "laea",
                Sphere3,
                ("latitude_of_origin", 40d),
                ("central_meridian", -100d)),
            100d,
            -20d,
            -4.2339303d,
            4.0257775d,
            sphereTolerance);
        yield return CreateForwardCase(
            "Lambert Azimuthal Equal Area ellipsoidal forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 333-336",
            BuildProjectedWkt(
                "laea",
                Clarke66,
                ("latitude_of_origin", 40d),
                ("central_meridian", -100d)),
            -110d,
            30d,
            -965932.10d,
            -1056814.90d,
            metreTolerance);
        yield return CreateForwardCase(
            "Lambert Azimuthal Equal Area ellipsoidal polar forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 334-337",
            BuildProjectedWkt(
                "laea",
                International,
                ("latitude_of_origin", 90d),
                ("central_meridian", -100d)),
            5d,
            80d,
            1077459.70d,
            288704.50d,
            metreTolerance);

        yield return CreateForwardCase(
            "Van der Grinten sphere forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 363-365",
            BuildProjectedWkt(
                "vandg",
                Sphere1,
                ("latitude_of_origin", 0d),
                ("central_meridian", -85d)),
            -160d,
            -50d,
            -1.1954154d,
            -0.9960733d,
            sphereTolerance);

        yield return CreateForwardCase(
            "Sinusoidal sphere forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 365-366",
            BuildProjectedWkt(
                "sinu",
                Sphere1,
                ("latitude_of_origin", 0d),
                ("central_meridian", -90d)),
            -75d,
            -50d,
            0.1682814d,
            -0.8726646d,
            sphereTolerance);
        yield return CreateForwardCase(
            "Sinusoidal ellipsoidal forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 366-366",
            BuildProjectedWkt(
                "sinu",
                Clarke66,
                ("latitude_of_origin", 0d),
                ("central_meridian", -90d)),
            -75d,
            -50d,
            1075471.50d,
            -5540628.00d,
            metreTolerance);

        yield return CreateForwardCase(
            "Mollweide sphere forward",
            "Snyder (1987), PP 1395, Appendix A, p. 367",
            BuildProjectedWkt(
                "moll",
                Sphere1,
                ("latitude_of_origin", 0d),
                ("central_meridian", -90d)),
            -75d,
            -50d,
            0.1788845d,
            -0.9208758d,
            sphereTolerance);

        yield return CreateForwardCase(
            "Eckert IV sphere forward",
            "Snyder (1987), PP 1395, Appendix A, p. 368",
            BuildProjectedWkt(
                "eck4",
                Sphere1,
                ("latitude_of_origin", 0d),
                ("central_meridian", -90d)),
            -75d,
            -50d,
            0.1875270d,
            -0.9519210d,
            sphereTolerance);

        yield return CreateForwardCase(
            "Eckert VI sphere forward",
            "Snyder (1987), PP 1395, Appendix A, p. 369",
            BuildProjectedWkt(
                "eck6",
                Sphere1,
                ("latitude_of_origin", 0d),
                ("central_meridian", -90d)),
            -75d,
            -50d,
            0.1693623d,
            -0.9570223d,
            sphereTolerance);

        yield return CreateForwardCase(
            "Polyconic sphere forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 303-306",
            BuildProjectedWkt(
                "poly",
                Sphere1,
                ("latitude_of_origin", 30d),
                ("central_meridian", -96d)),
            -75d,
            40d,
            0.2781798d,
            0.2074541d,
            sphereTolerance);
        yield return CreateForwardCase(
            "Polyconic ellipsoidal forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 304-306",
            BuildProjectedWkt(
                "poly",
                Clarke66,
                ("latitude_of_origin", 30d),
                ("central_meridian", -96d)),
            -75d,
            40d,
            1776774.50d,
            1319657.80d,
            metreTolerance);

        yield return CreateForwardCase(
            "Bonne sphere forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 309-311",
            BuildProjectedWkt(
                "bonne",
                Sphere1,
                ("latitude_of_origin", 0d),
                ("central_meridian", -75d),
                ("standard_parallel_1", 40d)),
            -85d,
            30d,
            -0.1508418d,
            -0.1661807d,
            sphereTolerance);
        yield return CreateForwardCase(
            "Bonne ellipsoidal forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 309-311",
            BuildProjectedWkt(
                "bonne",
                Clarke66,
                ("latitude_of_origin", 0d),
                ("central_meridian", -75d),
                ("standard_parallel_1", 40d)),
            -85d,
            30d,
            -962915.10d,
            -1056065.00d,
            metreTolerance);

        yield return CreateForwardCase(
            "Modified stereographic Alaska sphere forward",
            "Snyder (1987), PP 1395, Appendix A, pp. 344-347",
            BuildProjectedWkt(
                "alsk",
                Sphere1,
                ("latitude_of_origin", 64d),
                ("central_meridian", -152d),
                ("scale_factor", 1d / 6370997d)),
            -150d,
            60d,
            0.01739129d,
            -0.06937775d,
            fineSphereTolerance);
    }

    /// <summary>
    /// Gets the Snyder Appendix A inverse-reference cases.
    /// </summary>
    /// <returns>The worked inverse vectors.</returns>
    public static IEnumerable<TheoryDataRow<string, string, string, double, double, double, double, double>> GetInverseCases()
    {
        const double sphereTolerance = 2e-6d;
        const double relaxedSphereTolerance = 3e-6d;
        const double alaskaInverseTolerance = 3e-7d;
        const double degreeTolerance = 1e-5d;

        yield return CreateInverseCase(
            "Mercator sphere inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 266-268",
            BuildProjectedWkt(
                "merc",
                Sphere1,
                ("latitude_of_origin", 0d),
                ("central_meridian", -180d),
                ("scale_factor", 1d)),
            1.8325957d,
            0.6528366d,
            -75d,
            35d,
            sphereTolerance);
        yield return CreateInverseCase(
            "Mercator ellipsoidal inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 266-268",
            BuildProjectedWkt(
                "merc",
                Clarke66,
                ("latitude_of_origin", 0d),
                ("central_meridian", -180d),
                ("scale_factor", 1d)),
            11688673.70d,
            4139145.60d,
            -75d,
            35d,
            degreeTolerance);

        yield return CreateInverseCase(
            "Transverse Mercator sphere inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 268-271",
            BuildProjectedWkt(
                "tmerc",
                Sphere1,
                ("latitude_of_origin", 0d),
                ("central_meridian", -75d),
                ("scale_factor", 1d)),
            0.0199077d,
            0.7070276d,
            -73.5d,
            40.5d,
            relaxedSphereTolerance);
        yield return CreateInverseCase(
            "Transverse Mercator ellipsoidal inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 268-271",
            BuildProjectedWkt(
                "tmerc",
                Clarke66,
                ("latitude_of_origin", 0d),
                ("central_meridian", -75d),
                ("scale_factor", 0.9996d)),
            127106.50d,
            4484124.40d,
            -73.5d,
            40.5d,
            degreeTolerance);

        yield return CreateInverseCase(
            "Cylindrical Equal Area sphere inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 272-273",
            BuildProjectedWkt(
                "cea",
                Sphere1,
                ("latitude_of_origin", 0d),
                ("central_meridian", -75d),
                ("standard_parallel_1", 30d)),
            2.3428242d,
            0.6623090d,
            80d,
            35d,
            sphereTolerance);
        yield return CreateInverseCase(
            "Cylindrical Equal Area ellipsoidal inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 281-287",
            BuildProjectedWkt(
                "cea",
                Clarke66,
                ("latitude_of_origin", 0d),
                ("central_meridian", -75d),
                ("standard_parallel_1", 5d)),
            -332699.80d,
            554248.50d,
            -78d,
            5d,
            degreeTolerance);

        yield return CreateInverseCase(
            "Albers sphere inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 291-294",
            BuildProjectedWkt(
                "aea",
                Sphere1,
                ("latitude_of_origin", 23d),
                ("central_meridian", -96d),
                ("standard_parallel_1", 29.5d),
                ("standard_parallel_2", 45.5d)),
            0.2952720d,
            0.2416774d,
            -75d,
            35d,
            relaxedSphereTolerance);
        yield return CreateInverseCase(
            "Albers ellipsoidal inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 291-294",
            BuildProjectedWkt(
                "aea",
                Clarke66,
                ("latitude_of_origin", 23d),
                ("central_meridian", -96d),
                ("standard_parallel_1", 29.5d),
                ("standard_parallel_2", 45.5d)),
            1885472.70d,
            1535925.00d,
            -75d,
            35d,
            degreeTolerance);

        yield return CreateInverseCase(
            "Lambert Conformal Conic sphere inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 295-298",
            BuildProjectedWkt(
                "lcc",
                Sphere1,
                ("latitude_of_origin", 23d),
                ("central_meridian", -96d),
                ("standard_parallel_1", 33d),
                ("standard_parallel_2", 45d)),
            0.2966785d,
            0.2462112d,
            -75d,
            35d,
            relaxedSphereTolerance);
        yield return CreateInverseCase(
            "Lambert Conformal Conic ellipsoidal inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 296-298",
            BuildProjectedWkt(
                "lcc",
                Clarke66,
                ("latitude_of_origin", 23d),
                ("central_meridian", -96d),
                ("standard_parallel_1", 33d),
                ("standard_parallel_2", 45d)),
            1894410.90d,
            1564649.50d,
            -75d,
            35d,
            degreeTolerance);

        yield return CreateInverseCase(
            "Equidistant Conic sphere inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 298-301",
            BuildProjectedWkt(
                "eqdc",
                Sphere1,
                ("latitude_of_origin", 23d),
                ("central_meridian", -96d),
                ("standard_parallel_1", 29.5d),
                ("standard_parallel_2", 45.5d)),
            0.2952057d,
            0.2424021d,
            -75d,
            35d,
            relaxedSphereTolerance);
        yield return CreateInverseCase(
            "Equidistant Conic ellipsoidal inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 299-301",
            BuildProjectedWkt(
                "eqdc",
                Clarke66,
                ("latitude_of_origin", 23d),
                ("central_meridian", -96d),
                ("standard_parallel_1", 29.5d),
                ("standard_parallel_2", 45.5d)),
            1885051.90d,
            1540507.60d,
            -75d,
            35d,
            degreeTolerance);

        yield return CreateInverseCase(
            "Lambert Azimuthal Equal Area sphere inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 332-337",
            BuildProjectedWkt(
                "laea",
                Sphere3,
                ("latitude_of_origin", 40d),
                ("central_meridian", -100d)),
            -4.2339303d,
            4.0257775d,
            100d,
            -20d,
            sphereTolerance);
        yield return CreateInverseCase(
            "Lambert Azimuthal Equal Area ellipsoidal inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 333-336",
            BuildProjectedWkt(
                "laea",
                Clarke66,
                ("latitude_of_origin", 40d),
                ("central_meridian", -100d)),
            -965932.10d,
            -1056814.90d,
            -110d,
            30d,
            degreeTolerance);
        yield return CreateInverseCase(
            "Lambert Azimuthal Equal Area ellipsoidal polar inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 334-337",
            BuildProjectedWkt(
                "laea",
                International,
                ("latitude_of_origin", 90d),
                ("central_meridian", -100d)),
            1077459.70d,
            288704.50d,
            5d,
            80d,
            degreeTolerance);

        yield return CreateInverseCase(
            "Van der Grinten sphere inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 363-365",
            BuildProjectedWkt(
                "vandg",
                Sphere1,
                ("latitude_of_origin", 0d),
                ("central_meridian", -85d)),
            -1.1954154d,
            -0.9960733d,
            -160d,
            -50d,
            sphereTolerance);

        yield return CreateInverseCase(
            "Sinusoidal sphere inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 365-366",
            BuildProjectedWkt(
                "sinu",
                Sphere1,
                ("latitude_of_origin", 0d),
                ("central_meridian", -90d)),
            0.1682814d,
            -0.8726646d,
            -75d,
            -50d,
            sphereTolerance);
        yield return CreateInverseCase(
            "Sinusoidal ellipsoidal inverse",
            "Snyder (1987), PP 1395, Appendix A, p. 366",
            BuildProjectedWkt(
                "sinu",
                Clarke66,
                ("latitude_of_origin", 0d),
                ("central_meridian", -90d)),
            1075471.50d,
            -5540628.00d,
            -75d,
            -50d,
            degreeTolerance);

        yield return CreateInverseCase(
            "Mollweide sphere inverse",
            "Snyder (1987), PP 1395, Appendix A, p. 367",
            BuildProjectedWkt(
                "moll",
                Sphere1,
                ("latitude_of_origin", 0d),
                ("central_meridian", -90d)),
            0.1788845d,
            -0.9208758d,
            -75d,
            -50d,
            sphereTolerance);

        yield return CreateInverseCase(
            "Eckert IV sphere inverse",
            "Snyder (1987), PP 1395, Appendix A, p. 368",
            BuildProjectedWkt(
                "eck4",
                Sphere1,
                ("latitude_of_origin", 0d),
                ("central_meridian", -90d)),
            0.1875270d,
            -0.9519210d,
            -75d,
            -50d,
            sphereTolerance);

        yield return CreateInverseCase(
            "Eckert VI sphere inverse",
            "Snyder (1987), PP 1395, Appendix A, p. 369",
            BuildProjectedWkt(
                "eck6",
                Sphere1,
                ("latitude_of_origin", 0d),
                ("central_meridian", -90d)),
            0.1693623d,
            -0.9570223d,
            -75d,
            -50d,
            relaxedSphereTolerance);

        yield return CreateInverseCase(
            "Polyconic sphere inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 303-306",
            BuildProjectedWkt(
                "poly",
                Sphere1,
                ("latitude_of_origin", 30d),
                ("central_meridian", -96d)),
            0.2781798d,
            0.2074541d,
            -75d,
            40d,
            sphereTolerance);
        yield return CreateInverseCase(
            "Polyconic ellipsoidal inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 304-306",
            BuildProjectedWkt(
                "poly",
                Clarke66,
                ("latitude_of_origin", 30d),
                ("central_meridian", -96d)),
            1776774.50d,
            1319657.80d,
            -75d,
            40d,
            degreeTolerance);

        yield return CreateInverseCase(
            "Bonne sphere inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 309-311",
            BuildProjectedWkt(
                "bonne",
                Sphere1,
                ("latitude_of_origin", 0d),
                ("central_meridian", -75d),
                ("standard_parallel_1", 40d)),
            -0.1508418d,
            -0.1661807d,
            -85d,
            30d,
            sphereTolerance);
        yield return CreateInverseCase(
            "Bonne ellipsoidal inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 309-311",
            BuildProjectedWkt(
                "bonne",
                Clarke66,
                ("latitude_of_origin", 0d),
                ("central_meridian", -75d),
                ("standard_parallel_1", 40d)),
            -962915.10d,
            -1056065.00d,
            -85d,
            30d,
            degreeTolerance);

        yield return CreateInverseCase(
            "Modified stereographic Alaska sphere inverse",
            "Snyder (1987), PP 1395, Appendix A, pp. 344-347",
            BuildProjectedWkt(
                "alsk",
                Sphere1,
                ("latitude_of_origin", 64d),
                ("central_meridian", -152d),
                ("scale_factor", 1d / 6370997d)),
            0.01739129d,
            -0.06937775d,
            -150d,
            60d,
            alaskaInverseTolerance);
    }

    /// <summary>
    /// Verifies Snyder Appendix A forward vectors.
    /// </summary>
    /// <param name="caseLabel">Human-readable case label.</param>
    /// <param name="citation">Appendix citation.</param>
    /// <param name="wkt">Projection WKT.</param>
    /// <param name="longitude">Source longitude degrees.</param>
    /// <param name="latitude">Source latitude degrees.</param>
    /// <param name="expectedX">Expected projected x.</param>
    /// <param name="expectedY">Expected projected y.</param>
    /// <param name="tolerance">Absolute tolerance.</param>
    [Theory]
    [MemberData(nameof(GetForwardCases))]
    public void MatchesSnyderAppendixAForwardVectors(
        string caseLabel,
        string citation,
        string wkt,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY,
        double tolerance)
    {
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        AssertCoordinateWithinTolerance(caseLabel, citation, "X", projectedPoint[0], expectedX, tolerance);
        AssertCoordinateWithinTolerance(caseLabel, citation, "Y", projectedPoint[1], expectedY, tolerance);
    }

    /// <summary>
    /// Verifies Snyder Appendix A inverse vectors.
    /// </summary>
    /// <param name="caseLabel">Human-readable case label.</param>
    /// <param name="citation">Appendix citation.</param>
    /// <param name="wkt">Projection WKT.</param>
    /// <param name="x">Source projected x.</param>
    /// <param name="y">Source projected y.</param>
    /// <param name="expectedLongitude">Expected longitude degrees.</param>
    /// <param name="expectedLatitude">Expected latitude degrees.</param>
    /// <param name="tolerance">Absolute tolerance.</param>
    [Theory]
    [MemberData(nameof(GetInverseCases))]
    public void MatchesSnyderAppendixAInverseVectors(
        string caseLabel,
        string citation,
        string wkt,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude,
        double tolerance)
    {
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        AssertCoordinateWithinTolerance(caseLabel, citation, "longitude", geographicPoint[0], expectedLongitude, tolerance);
        AssertCoordinateWithinTolerance(caseLabel, citation, "latitude", geographicPoint[1], expectedLatitude, tolerance);
    }

    private static TheoryDataRow<string, string, string, double, double, double, double, double> CreateForwardCase(
        string caseLabel,
        string citation,
        string wkt,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY,
        double tolerance)
    {
        return new TheoryDataRow<string, string, string, double, double, double, double, double>(
            caseLabel,
            citation,
            wkt,
            longitude,
            latitude,
            expectedX,
            expectedY,
            tolerance);
    }

    private static TheoryDataRow<string, string, string, double, double, double, double, double> CreateInverseCase(
        string caseLabel,
        string citation,
        string wkt,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude,
        double tolerance)
    {
        return new TheoryDataRow<string, string, string, double, double, double, double, double>(
            caseLabel,
            citation,
            wkt,
            x,
            y,
            expectedLongitude,
            expectedLatitude,
            tolerance);
    }

    private static void AssertCoordinateWithinTolerance(
        string caseLabel,
        string citation,
        string axis,
        double actual,
        double expected,
        double tolerance)
    {
        double delta = Math.Abs(actual - expected);
        Assert.True(
            delta <= tolerance,
            FormattableString.Invariant($"{caseLabel}: expected {axis} {expected:R}, actual {actual:R}, delta {delta:R}, tolerance {tolerance:R}. {citation}."));
    }

    private static string BuildProjectedWkt(
        string projectionName,
        string spheroidClause,
        params (string Name, double Value)[] parameters)
    {
        StringBuilder builder = new();
        builder.Append(FormattableString.Invariant(
            $"PROJCS[\"Snyder-AppendixA-{projectionName}\",GEOGCS[\"Snyder\",DATUM[\"Snyder_Datum\",{spheroidClause}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{projectionName}\"]"));
        foreach ((string name, double value) in parameters)
        {
            builder.Append(FormattableString.Invariant($",PARAMETER[\"{name}\",{value:R}]"));
        }

        if (!HasParameter(parameters, "false_easting"))
        {
            builder.Append(",PARAMETER[\"false_easting\",0]");
        }

        if (!HasParameter(parameters, "false_northing"))
        {
            builder.Append(",PARAMETER[\"false_northing\",0]");
        }

        builder.Append(",UNIT[\"metre\",1]]");
        return builder.ToString();
    }

    private static bool HasParameter((string Name, double Value)[] parameters, string name)
    {
        foreach ((string parameterName, _) in parameters)
        {
            if (string.Equals(parameterName, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
