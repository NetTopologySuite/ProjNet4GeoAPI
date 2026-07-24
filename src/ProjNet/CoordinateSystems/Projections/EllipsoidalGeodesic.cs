// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;

/// <summary>
/// Provides reusable ellipsoidal geodesic helpers for projection kernels.
/// </summary>
internal static class EllipsoidalGeodesic
{
    /// <summary>
    /// Gets the maximum number of Vincenty iterations used by the shared geodesic helpers.
    /// </summary>
    internal const int MaxIterations = 100;

    /// <summary>
    /// Gets the convergence tolerance used by the shared geodesic helpers.
    /// </summary>
    internal const double Tolerance = 1e-12d;

    private const int MinJacobiSteps = 512;
    private const int MaxJacobiSteps = 16384;

    /// <summary>
    /// Computes the Vincenty delta-sigma correction term.
    /// </summary>
    /// <param name="bCoeff">The series coefficient.</param>
    /// <param name="sinSigma">The sine of sigma.</param>
    /// <param name="cosSigma">The cosine of sigma.</param>
    /// <param name="cos2SigmaM">The cosine of twice sigma sub m.</param>
    /// <returns>The computed delta-sigma correction.</returns>
    internal static double ComputeDeltaSigma(double bCoeff, double sinSigma, double cosSigma, double cos2SigmaM)
    {
        double cos2SigmaMSquared = cos2SigmaM * cos2SigmaM;
        double sinSigmaSquared = sinSigma * sinSigma;
        double firstTerm = cosSigma * (-1d + (2d * cos2SigmaMSquared));
        double secondTerm = (bCoeff / 6d) * cos2SigmaM * (-3d + (4d * sinSigmaSquared)) * (-3d + (4d * cos2SigmaMSquared));
        double bracket = cos2SigmaM + ((bCoeff / 4d) * (firstTerm - secondTerm));
        return bCoeff * sinSigma * bracket;
    }

    /// <summary>
    /// Normalizes a longitude to the principal interval.
    /// </summary>
    /// <param name="longitude">The longitude to normalize.</param>
    /// <returns>The normalized longitude.</returns>
    internal static double NormalizeLongitude(double longitude)
    {
        return Math.Atan2(Math.Sin(longitude), Math.Cos(longitude));
    }

    /// <summary>
    /// Solves the ellipsoidal inverse geodesic by Vincenty's method.
    /// </summary>
    /// <param name="semiMinor">The semi-minor axis.</param>
    /// <param name="flattening">The ellipsoid flattening.</param>
    /// <param name="eccentricityPrimeSquared">The second eccentricity squared.</param>
    /// <param name="latitude1">The start latitude in radians.</param>
    /// <param name="longitude1">The start longitude in radians.</param>
    /// <param name="latitude2">The end latitude in radians.</param>
    /// <param name="longitude2">The end longitude in radians.</param>
    /// <param name="distance">The solved geodesic distance in meters.</param>
    /// <param name="azimuth">The forward azimuth in radians.</param>
    /// <returns><see langword="true"/> when the iteration converged; otherwise <see langword="false"/>.</returns>
    internal static bool TryVincentyInverse(
        double semiMinor,
        double flattening,
        double eccentricityPrimeSquared,
        double latitude1,
        double longitude1,
        double latitude2,
        double longitude2,
        out double distance,
        out double azimuth)
    {
        distance = 0d;
        azimuth = 0d;

        if (Math.Abs(latitude1 - latitude2) < Tolerance && Math.Abs(longitude1 - longitude2) < Tolerance)
        {
            return true;
        }

        double oneMinusF = 1d - flattening;
        double tanU1 = oneMinusF * Math.Tan(latitude1);
        double tanU2 = oneMinusF * Math.Tan(latitude2);
        double u1 = Math.Atan(tanU1);
        double u2 = Math.Atan(tanU2);
        double sinU1 = Math.Sin(u1);
        double cosU1 = Math.Cos(u1);
        double sinU2 = Math.Sin(u2);
        double cosU2 = Math.Cos(u2);

        double l = NormalizeLongitude(longitude2 - longitude1);
        double lambda = l;

        for (int iteration = 0; iteration < MaxIterations; iteration++)
        {
            double sinLambda = Math.Sin(lambda);
            double cosLambda = Math.Cos(lambda);

            double term1 = cosU2 * sinLambda;
            double term2 = (cosU1 * sinU2) - (sinU1 * cosU2 * cosLambda);
            double sinSigma = Math.Sqrt((term1 * term1) + (term2 * term2));
            if (sinSigma < Tolerance)
            {
                return true;
            }

            double cosSigma = (sinU1 * sinU2) + (cosU1 * cosU2 * cosLambda);
            double sigma = Math.Atan2(sinSigma, cosSigma);
            double sinAlpha = (cosU1 * cosU2 * sinLambda) / sinSigma;
            double cosSqAlpha = 1d - (sinAlpha * sinAlpha);
            double cos2SigmaM = cosSqAlpha < Tolerance
                ? 0d
                : cosSigma - ((2d * sinU1 * sinU2) / cosSqAlpha);

            double c = (flattening / 16d) * cosSqAlpha * (4d + (flattening * (4d - (3d * cosSqAlpha))));
            double lambdaPrevious = lambda;
            lambda = l + ((1d - c) * flattening * sinAlpha * (sigma + (c * sinSigma * (cos2SigmaM + (c * cosSigma * (-1d + (2d * cos2SigmaM * cos2SigmaM)))))));
            if (Math.Abs(lambda - lambdaPrevious) <= Tolerance)
            {
                double uSq = cosSqAlpha * eccentricityPrimeSquared;
                double aCoeff = 1d + ((uSq / 16384d) * (4096d + (uSq * (-768d + (uSq * (320d - (175d * uSq)))))));
                double bCoeff = (uSq / 1024d) * (256d + (uSq * (-128d + (uSq * (74d - (47d * uSq))))));
                double deltaSigma = ComputeDeltaSigma(bCoeff, sinSigma, cosSigma, cos2SigmaM);

                distance = semiMinor * aCoeff * (sigma - deltaSigma);
                azimuth = Math.Atan2(
                    cosU2 * Math.Sin(lambda),
                    (cosU1 * sinU2) - (sinU1 * cosU2 * Math.Cos(lambda)));
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Solves the ellipsoidal direct geodesic by Vincenty's method.
    /// </summary>
    /// <param name="semiMinor">The semi-minor axis.</param>
    /// <param name="flattening">The ellipsoid flattening.</param>
    /// <param name="eccentricityPrimeSquared">The second eccentricity squared.</param>
    /// <param name="latitude1">The start latitude in radians.</param>
    /// <param name="longitude1">The start longitude in radians.</param>
    /// <param name="azimuth1">The forward azimuth in radians.</param>
    /// <param name="distance">The geodesic distance in meters.</param>
    /// <param name="latitude2">The solved end latitude in radians.</param>
    /// <param name="longitude2">The solved end longitude in radians.</param>
    /// <returns><see langword="true"/> when the iteration converged; otherwise <see langword="false"/>.</returns>
    internal static bool TryVincentyDirect(
        double semiMinor,
        double flattening,
        double eccentricityPrimeSquared,
        double latitude1,
        double longitude1,
        double azimuth1,
        double distance,
        out double latitude2,
        out double longitude2)
    {
        latitude2 = latitude1;
        longitude2 = longitude1;

        double oneMinusF = 1d - flattening;
        double tanU1 = oneMinusF * Math.Tan(latitude1);
        double u1 = Math.Atan(tanU1);
        double sinU1 = Math.Sin(u1);
        double cosU1 = Math.Cos(u1);
        double sinAlpha1 = Math.Sin(azimuth1);
        double cosAlpha1 = Math.Cos(azimuth1);

        double sigma1 = Math.Atan2(tanU1, cosAlpha1);
        double sinAlpha = cosU1 * sinAlpha1;
        double cosSqAlpha = 1d - (sinAlpha * sinAlpha);
        double uSq = cosSqAlpha * eccentricityPrimeSquared;
        double aCoeff = 1d + ((uSq / 16384d) * (4096d + (uSq * (-768d + (uSq * (320d - (175d * uSq)))))));
        double bCoeff = (uSq / 1024d) * (256d + (uSq * (-128d + (uSq * (74d - (47d * uSq))))));

        double sigma = distance / (semiMinor * aCoeff);
        for (int iteration = 0; iteration < MaxIterations; iteration++)
        {
            double cos2SigmaM = Math.Cos((2d * sigma1) + sigma);
            double sinSigma = Math.Sin(sigma);
            double cosSigma = Math.Cos(sigma);
            double deltaSigma = ComputeDeltaSigma(bCoeff, sinSigma, cosSigma, cos2SigmaM);
            double sigmaPrevious = sigma;
            sigma = (distance / (semiMinor * aCoeff)) + deltaSigma;
            if (Math.Abs(sigma - sigmaPrevious) <= Tolerance)
            {
                double sinSigmaFinal = Math.Sin(sigma);
                double cosSigmaFinal = Math.Cos(sigma);
                double tmp = (sinU1 * sinSigmaFinal) - (cosU1 * cosSigmaFinal * cosAlpha1);

                latitude2 = Math.Atan2(
                    (sinU1 * cosSigmaFinal) + (cosU1 * sinSigmaFinal * cosAlpha1),
                    oneMinusF * Math.Sqrt((sinAlpha * sinAlpha) + (tmp * tmp)));

                double lambda = Math.Atan2(
                    sinSigmaFinal * sinAlpha1,
                    (cosU1 * cosSigmaFinal) - (sinU1 * sinSigmaFinal * cosAlpha1));

                double c = (flattening / 16d) * cosSqAlpha * (4d + (flattening * (4d - (3d * cosSqAlpha))));
                double l = lambda - ((1d - c) * flattening * sinAlpha * (sigma + (c * sinSigmaFinal * (cos2SigmaM + (c * cosSigmaFinal * (-1d + (2d * cos2SigmaM * cos2SigmaM)))))));
                longitude2 = NormalizeLongitude(longitude1 + l);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Solves the end position together with reduced length and geodesic scale for the given geodesic.
    /// </summary>
    /// <param name="semiMajor">The semi-major axis.</param>
    /// <param name="semiMinor">The semi-minor axis.</param>
    /// <param name="flattening">The ellipsoid flattening.</param>
    /// <param name="eccentricitySquared">The first eccentricity squared.</param>
    /// <param name="eccentricityPrimeSquared">The second eccentricity squared.</param>
    /// <param name="latitude1">The start latitude in radians.</param>
    /// <param name="longitude1">The start longitude in radians.</param>
    /// <param name="azimuth1">The forward azimuth in radians.</param>
    /// <param name="distance">The geodesic distance in meters.</param>
    /// <param name="latitude2">The solved end latitude in radians.</param>
    /// <param name="longitude2">The solved end longitude in radians.</param>
    /// <param name="reducedLength">The reduced length <c>m12</c>.</param>
    /// <param name="geodesicScale">The geodesic scale <c>M12</c>.</param>
    /// <returns><see langword="true"/> when the solve succeeded; otherwise <see langword="false"/>.</returns>
    internal static bool TrySolvePositionReducedLengthAndScale(
        double semiMajor,
        double semiMinor,
        double flattening,
        double eccentricitySquared,
        double eccentricityPrimeSquared,
        double latitude1,
        double longitude1,
        double azimuth1,
        double distance,
        out double latitude2,
        out double longitude2,
        out double reducedLength,
        out double geodesicScale)
    {
        latitude2 = latitude1;
        longitude2 = longitude1;
        reducedLength = 0d;
        geodesicScale = 1d;

        if (Math.Abs(distance) <= Tolerance)
        {
            return true;
        }

        if (Math.Abs(Math.Sin(azimuth1)) <= 1e-15d)
        {
            return TrySolveMeridionalPositionReducedLengthAndScale(
                semiMajor,
                eccentricitySquared,
                latitude1,
                longitude1,
                azimuth1,
                distance,
                out latitude2,
                out longitude2,
                out reducedLength,
                out geodesicScale);
        }

        int stepCount = DetermineJacobiStepCount(semiMajor, distance);
        double stepSize = distance / stepCount;
        double jacobiReducedLength = 0d;
        double jacobiReducedLengthDerivative = 1d;
        double jacobiScale = 1d;
        double jacobiScaleDerivative = 0d;

        for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
        {
            double baseDistance = stepIndex * stepSize;
            if (!TrySampleGaussianCurvature(
                semiMajor,
                semiMinor,
                flattening,
                eccentricitySquared,
                eccentricityPrimeSquared,
                latitude1,
                longitude1,
                azimuth1,
                baseDistance,
                out double curvatureStart))
            {
                return false;
            }

            if (!TrySampleGaussianCurvature(
                semiMajor,
                semiMinor,
                flattening,
                eccentricitySquared,
                eccentricityPrimeSquared,
                latitude1,
                longitude1,
                azimuth1,
                baseDistance + (0.5d * stepSize),
                out double curvatureMid))
            {
                return false;
            }

            if (!TrySampleGaussianCurvature(
                semiMajor,
                semiMinor,
                flattening,
                eccentricitySquared,
                eccentricityPrimeSquared,
                latitude1,
                longitude1,
                azimuth1,
                baseDistance + stepSize,
                out double curvatureEnd))
            {
                return false;
            }

            double k1ReducedLength = stepSize * jacobiReducedLengthDerivative;
            double k1ReducedLengthDerivative = stepSize * (-curvatureStart * jacobiReducedLength);
            double k1Scale = stepSize * jacobiScaleDerivative;
            double k1ScaleDerivative = stepSize * (-curvatureStart * jacobiScale);

            double reducedLengthMid1 = jacobiReducedLength + (0.5d * k1ReducedLength);
            double reducedLengthDerivativeMid1 = jacobiReducedLengthDerivative + (0.5d * k1ReducedLengthDerivative);
            double scaleMid1 = jacobiScale + (0.5d * k1Scale);
            double scaleDerivativeMid1 = jacobiScaleDerivative + (0.5d * k1ScaleDerivative);

            double k2ReducedLength = stepSize * reducedLengthDerivativeMid1;
            double k2ReducedLengthDerivative = stepSize * (-curvatureMid * reducedLengthMid1);
            double k2Scale = stepSize * scaleDerivativeMid1;
            double k2ScaleDerivative = stepSize * (-curvatureMid * scaleMid1);

            double reducedLengthMid2 = jacobiReducedLength + (0.5d * k2ReducedLength);
            double reducedLengthDerivativeMid2 = jacobiReducedLengthDerivative + (0.5d * k2ReducedLengthDerivative);
            double scaleMid2 = jacobiScale + (0.5d * k2Scale);
            double scaleDerivativeMid2 = jacobiScaleDerivative + (0.5d * k2ScaleDerivative);

            double k3ReducedLength = stepSize * reducedLengthDerivativeMid2;
            double k3ReducedLengthDerivative = stepSize * (-curvatureMid * reducedLengthMid2);
            double k3Scale = stepSize * scaleDerivativeMid2;
            double k3ScaleDerivative = stepSize * (-curvatureMid * scaleMid2);

            double reducedLengthEnd = jacobiReducedLength + k3ReducedLength;
            double reducedLengthDerivativeEnd = jacobiReducedLengthDerivative + k3ReducedLengthDerivative;
            double scaleEnd = jacobiScale + k3Scale;
            double scaleDerivativeEnd = jacobiScaleDerivative + k3ScaleDerivative;

            double k4ReducedLength = stepSize * reducedLengthDerivativeEnd;
            double k4ReducedLengthDerivative = stepSize * (-curvatureEnd * reducedLengthEnd);
            double k4Scale = stepSize * scaleDerivativeEnd;
            double k4ScaleDerivative = stepSize * (-curvatureEnd * scaleEnd);

            jacobiReducedLength += (k1ReducedLength + (2d * (k2ReducedLength + k3ReducedLength)) + k4ReducedLength) / 6d;
            jacobiReducedLengthDerivative += (k1ReducedLengthDerivative + (2d * (k2ReducedLengthDerivative + k3ReducedLengthDerivative)) + k4ReducedLengthDerivative) / 6d;
            jacobiScale += (k1Scale + (2d * (k2Scale + k3Scale)) + k4Scale) / 6d;
            jacobiScaleDerivative += (k1ScaleDerivative + (2d * (k2ScaleDerivative + k3ScaleDerivative)) + k4ScaleDerivative) / 6d;
        }

        if (!TryVincentyDirect(
            semiMinor,
            flattening,
            eccentricityPrimeSquared,
            latitude1,
            longitude1,
            azimuth1,
            distance,
            out latitude2,
            out longitude2))
        {
            return false;
        }

        reducedLength = jacobiReducedLength;
        geodesicScale = jacobiScale;
        return true;
    }

    private static bool TrySolveMeridionalPositionReducedLengthAndScale(
        double semiMajor,
        double eccentricitySquared,
        double latitude1,
        double longitude1,
        double azimuth1,
        double distance,
        out double latitude2,
        out double longitude2,
        out double reducedLength,
        out double geodesicScale)
    {
        latitude2 = latitude1;
        longitude2 = longitude1;
        reducedLength = 0d;
        geodesicScale = 1d;

        int stepCount = DetermineMeridionalStepCount(semiMajor, distance);
        double stepSize = distance / stepCount;
        double direction = Math.Cos(azimuth1) >= 0d ? 1d : -1d;

        double latitude = latitude1;
        double jacobiReducedLength = 0d;
        double jacobiReducedLengthDerivative = 1d;
        double jacobiScale = 1d;
        double jacobiScaleDerivative = 0d;

        for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
        {
            EvaluateMeridionalDerivatives(
                semiMajor,
                eccentricitySquared,
                direction,
                latitude,
                jacobiReducedLength,
                jacobiReducedLengthDerivative,
                jacobiScale,
                jacobiScaleDerivative,
                out double latitudeDerivative1,
                out double reducedLengthDerivative1,
                out double reducedLengthSecondDerivative1,
                out double scaleDerivative1,
                out double scaleSecondDerivative1);

            double latitudeMid1 = latitude + (0.5d * stepSize * latitudeDerivative1);
            double reducedLengthMid1 = jacobiReducedLength + (0.5d * stepSize * reducedLengthDerivative1);
            double reducedLengthDerivativeMid1 = jacobiReducedLengthDerivative + (0.5d * stepSize * reducedLengthSecondDerivative1);
            double scaleMid1 = jacobiScale + (0.5d * stepSize * scaleDerivative1);
            double scaleDerivativeMid1 = jacobiScaleDerivative + (0.5d * stepSize * scaleSecondDerivative1);

            EvaluateMeridionalDerivatives(
                semiMajor,
                eccentricitySquared,
                direction,
                latitudeMid1,
                reducedLengthMid1,
                reducedLengthDerivativeMid1,
                scaleMid1,
                scaleDerivativeMid1,
                out double latitudeDerivative2,
                out double reducedLengthDerivative2,
                out double reducedLengthSecondDerivative2,
                out double scaleDerivative2,
                out double scaleSecondDerivative2);

            double latitudeMid2 = latitude + (0.5d * stepSize * latitudeDerivative2);
            double reducedLengthMid2 = jacobiReducedLength + (0.5d * stepSize * reducedLengthDerivative2);
            double reducedLengthDerivativeMid2 = jacobiReducedLengthDerivative + (0.5d * stepSize * reducedLengthSecondDerivative2);
            double scaleMid2 = jacobiScale + (0.5d * stepSize * scaleDerivative2);
            double scaleDerivativeMid2 = jacobiScaleDerivative + (0.5d * stepSize * scaleSecondDerivative2);

            EvaluateMeridionalDerivatives(
                semiMajor,
                eccentricitySquared,
                direction,
                latitudeMid2,
                reducedLengthMid2,
                reducedLengthDerivativeMid2,
                scaleMid2,
                scaleDerivativeMid2,
                out double latitudeDerivative3,
                out double reducedLengthDerivative3,
                out double reducedLengthSecondDerivative3,
                out double scaleDerivative3,
                out double scaleSecondDerivative3);

            double latitudeEnd = latitude + (stepSize * latitudeDerivative3);
            double reducedLengthEnd = jacobiReducedLength + (stepSize * reducedLengthDerivative3);
            double reducedLengthDerivativeEnd = jacobiReducedLengthDerivative + (stepSize * reducedLengthSecondDerivative3);
            double scaleEnd = jacobiScale + (stepSize * scaleDerivative3);
            double scaleDerivativeEnd = jacobiScaleDerivative + (stepSize * scaleSecondDerivative3);

            EvaluateMeridionalDerivatives(
                semiMajor,
                eccentricitySquared,
                direction,
                latitudeEnd,
                reducedLengthEnd,
                reducedLengthDerivativeEnd,
                scaleEnd,
                scaleDerivativeEnd,
                out double latitudeDerivative4,
                out double reducedLengthDerivative4,
                out double reducedLengthSecondDerivative4,
                out double scaleDerivative4,
                out double scaleSecondDerivative4);

            latitude += (stepSize / 6d) * (latitudeDerivative1 + (2d * (latitudeDerivative2 + latitudeDerivative3)) + latitudeDerivative4);
            jacobiReducedLength += (stepSize / 6d) * (reducedLengthDerivative1 + (2d * (reducedLengthDerivative2 + reducedLengthDerivative3)) + reducedLengthDerivative4);
            jacobiReducedLengthDerivative += (stepSize / 6d) * (reducedLengthSecondDerivative1 + (2d * (reducedLengthSecondDerivative2 + reducedLengthSecondDerivative3)) + reducedLengthSecondDerivative4);
            jacobiScale += (stepSize / 6d) * (scaleDerivative1 + (2d * (scaleDerivative2 + scaleDerivative3)) + scaleDerivative4);
            jacobiScaleDerivative += (stepSize / 6d) * (scaleSecondDerivative1 + (2d * (scaleSecondDerivative2 + scaleSecondDerivative3)) + scaleSecondDerivative4);
        }

        latitude2 = Math.Max(-Math.PI / 2d, Math.Min(Math.PI / 2d, latitude));
        longitude2 = longitude1;
        reducedLength = jacobiReducedLength;
        geodesicScale = jacobiScale;
        return true;
    }

    private static int DetermineJacobiStepCount(double semiMajor, double distance)
    {
        double angularDistance = semiMajor <= 0d ? 0d : Math.Abs(distance) / semiMajor;
        double normalized = angularDistance / (Math.PI / 2d);

        if (normalized >= 0.995d)
        {
            return MaxJacobiSteps;
        }

        if (normalized >= 0.95d)
        {
            return 8192;
        }

        if (normalized >= 0.8d)
        {
            return 4096;
        }

        if (normalized >= 0.5d)
        {
            return 2048;
        }

        int stepCount = (int)Math.Ceiling(2048d * normalized);
        return stepCount < MinJacobiSteps ? MinJacobiSteps : stepCount;
    }

    private static int DetermineMeridionalStepCount(double semiMajor, double distance)
    {
        double angularDistance = semiMajor <= 0d ? 0d : Math.Abs(distance) / semiMajor;
        double normalized = angularDistance / (Math.PI / 2d);

        if (normalized >= 0.995d)
        {
            return 131072;
        }

        if (normalized >= 0.9d)
        {
            return 65536;
        }

        if (normalized >= 0.5d)
        {
            return 32768;
        }

        return 16384;
    }

    private static bool TrySampleGaussianCurvature(
        double semiMajor,
        double semiMinor,
        double flattening,
        double eccentricitySquared,
        double eccentricityPrimeSquared,
        double latitude1,
        double longitude1,
        double azimuth1,
        double distance,
        out double curvature)
    {
        curvature = 0d;
        double latitude = latitude1;
        if (Math.Abs(distance) > Tolerance)
        {
            if (!TryVincentyDirect(
                semiMinor,
                flattening,
                eccentricityPrimeSquared,
                latitude1,
                longitude1,
                azimuth1,
                distance,
                out latitude,
                out _))
            {
                return false;
            }
        }

        curvature = ComputeGaussianCurvature(semiMajor, eccentricitySquared, latitude);
        return true;
    }

    private static double ComputeGaussianCurvature(double semiMajor, double eccentricitySquared, double latitude)
    {
        double sinLatitude = Math.Sin(latitude);
        double oneMinusEsSinSquared = 1d - (eccentricitySquared * sinLatitude * sinLatitude);
        double sqrtOneMinusEsSinSquared = Math.Sqrt(oneMinusEsSinSquared);
        double meridianRadius = (semiMajor * (1d - eccentricitySquared)) / (oneMinusEsSinSquared * sqrtOneMinusEsSinSquared);
        double primeVerticalRadius = semiMajor / sqrtOneMinusEsSinSquared;
        return 1d / (meridianRadius * primeVerticalRadius);
    }

    private static void EvaluateMeridionalDerivatives(
        double semiMajor,
        double eccentricitySquared,
        double direction,
        double latitude,
        double reducedLength,
        double reducedLengthDerivative,
        double geodesicScale,
        double geodesicScaleDerivative,
        out double latitudeDerivative,
        out double reducedLengthFirstDerivative,
        out double reducedLengthSecondDerivative,
        out double geodesicScaleFirstDerivative,
        out double geodesicScaleSecondDerivative)
    {
        double sinLatitude = Math.Sin(latitude);
        double oneMinusEsSinSquared = 1d - (eccentricitySquared * sinLatitude * sinLatitude);
        double sqrtOneMinusEsSinSquared = Math.Sqrt(oneMinusEsSinSquared);
        double meridianRadius = (semiMajor * (1d - eccentricitySquared)) / (oneMinusEsSinSquared * sqrtOneMinusEsSinSquared);
        double curvature = ComputeGaussianCurvature(semiMajor, eccentricitySquared, latitude);

        latitudeDerivative = direction / meridianRadius;
        reducedLengthFirstDerivative = reducedLengthDerivative;
        reducedLengthSecondDerivative = -curvature * reducedLength;
        geodesicScaleFirstDerivative = geodesicScaleDerivative;
        geodesicScaleSecondDerivative = -curvature * geodesicScale;
    }
}
