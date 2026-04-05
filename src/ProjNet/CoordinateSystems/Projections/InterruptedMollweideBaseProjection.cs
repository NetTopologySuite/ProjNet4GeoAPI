// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides shared Mollweide helper logic for interrupted Mollweide-family projections.
/// </summary>
internal abstract class InterruptedMollweideBaseProjection : MapProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InterruptedMollweideBaseProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    protected InterruptedMollweideBaseProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
    }

    /// <summary>
    /// Determines whether a longitude/latitude pair is valid for the specified interrupted zone.
    /// </summary>
    /// <param name="zone">1-based zone index.</param>
    /// <param name="lambda">Longitude in radians in the projection's local domain.</param>
    /// <param name="phi">Latitude in radians in the projection's local domain.</param>
    /// <param name="zoneEnvelopes">Zone longitude ranges and hemisphere constraints.</param>
    /// <returns><see langword="true"/> when the coordinate is inside the zone envelope; otherwise <see langword="false"/>.</returns>
    protected static bool IsInZone(int zone, double lambda, double phi, (double MinLambda, double MaxLambda, bool IsNorthernHemisphere)[] zoneEnvelopes)
    {
        const double seamSlack = 1e-10;

        if (zone < 1 || zone > zoneEnvelopes.Length)
        {
            return false;
        }

        (double minLambda, double maxLambda, bool isNorthernHemisphere) = zoneEnvelopes[zone - 1];
        bool latitudeInRange = isNorthernHemisphere
            ? phi >= 0d - seamSlack
            : phi <= 0d + seamSlack;

        return latitudeInRange
            && lambda >= minLambda - seamSlack
            && lambda <= maxLambda + seamSlack;
    }

    /// <summary>
    /// Projects local Mollweide input coordinates into unit-space coordinates.
    /// </summary>
    /// <param name="lambda">Longitude offset from zone central meridian, in radians.</param>
    /// <param name="phi">Latitude in radians.</param>
    /// <param name="x">Projected unit-space x coordinate.</param>
    /// <param name="y">Projected unit-space y coordinate.</param>
    protected static void MollweideForwardUnit(double lambda, double phi, out double x, out double y)
    {
        const int mollweideIterations = 12;
        double sqrt2 = Math.Sqrt(2d);

        double theta = Sign(phi) * HalfPi;
        if (Math.Abs(Math.Abs(phi) - HalfPi) >= 1e-12)
        {
            theta = phi;
            double target = PI * Math.Sin(phi);
            for (int i = 0; i < mollweideIterations; i++)
            {
                double twoTheta = 2d * theta;
                double delta = ((twoTheta + Math.Sin(twoTheta)) - target) / (2d + (2d * Math.Cos(twoTheta)));
                theta -= delta;
                if (Math.Abs(delta) < 1e-12)
                {
                    break;
                }
            }
        }

        x = (2d * sqrt2 / PI) * lambda * Math.Cos(theta);
        y = sqrt2 * Math.Sin(theta);
    }

    /// <summary>
    /// Inverts unit-space Mollweide coordinates to local longitude/latitude.
    /// </summary>
    /// <param name="x">Projected unit-space x coordinate.</param>
    /// <param name="y">Projected unit-space y coordinate.</param>
    /// <param name="lambda">Recovered local longitude offset from zone central meridian, in radians.</param>
    /// <param name="phi">Recovered latitude in radians.</param>
    protected static void MollweideInverseUnit(double x, double y, out double lambda, out double phi)
    {
        double sqrt2 = Math.Sqrt(2d);
        double theta = Math.Asin(ProjectionConstants.Clamp(y / sqrt2, -1d, 1d));
        double cosTheta = Math.Cos(theta);
        lambda = Math.Abs(cosTheta) <= Eps10 ? 0d : (x * PI / (2d * sqrt2 * cosTheta));
        phi = Math.Asin(ProjectionConstants.Clamp(((2d * theta) + Math.Sin(2d * theta)) / PI, -1d, 1d));
    }

    /// <summary>
    /// Computes zone-local Mollweide forward coordinates including per-zone offsets.
    /// </summary>
    /// <param name="zones">Zone definition array.</param>
    /// <param name="zone">1-based zone index.</param>
    /// <param name="lambda">Longitude in radians.</param>
    /// <param name="phi">Latitude in radians.</param>
    /// <param name="x">Resulting zone-relative x coordinate.</param>
    /// <param name="y">Resulting zone-relative y coordinate.</param>
    protected static void MollweideForward(IReadOnlyList<MollweideZoneDefinition> zones, int zone, double lambda, double phi, out double x, out double y)
    {
        MollweideZoneDefinition def = zones[zone - 1];
        MollweideForwardUnit(lambda - def.Lambda0, phi, out double xUnit, out double yUnit);
        x = xUnit + def.X0;
        y = yUnit + def.Y0;
    }

    /// <summary>
    /// Computes the seam boundary x-position between adjacent zones.
    /// </summary>
    /// <param name="zones">Zone definition array.</param>
    /// <param name="determineForwardZone">Forward zone selection function.</param>
    /// <param name="lambda">Seam longitude in radians.</param>
    /// <param name="phi">Latitude in radians.</param>
    /// <returns>Average x-position across both seam sides.</returns>
    protected static double ComputeZoneBoundaryX(IReadOnlyList<MollweideZoneDefinition> zones, Func<double, double, int> determineForwardZone, double lambda, double phi)
    {
        const double seamSlack = 1e-10;

        MollweideForward(zones, determineForwardZone(phi, lambda - seamSlack), lambda - seamSlack, phi, out double x1, out _);
        MollweideForward(zones, determineForwardZone(phi, lambda + seamSlack), lambda + seamSlack, phi, out double x2, out _);
        return (x1 + x2) * 0.5d;
    }

    /// <summary>
    /// Computes x-offset alignment between two zones at the given seam sample points.
    /// </summary>
    /// <param name="zones">Zone definition array.</param>
    /// <param name="zone1">First 1-based zone index.</param>
    /// <param name="zone2">Second 1-based zone index.</param>
    /// <param name="lambda">Seam longitude sample in radians.</param>
    /// <param name="phi1">Latitude sample for zone 1 in radians.</param>
    /// <param name="phi2">Latitude sample for zone 2 in radians.</param>
    /// <returns>The x-offset δ that aligns zone 1 with zone 2 at the seam.</returns>
    protected static double ComputeZoneOffset(IReadOnlyList<MollweideZoneDefinition> zones, int zone1, int zone2, double lambda, double phi1, double phi2)
    {
        MollweideForward(zones, zone1, lambda, phi1, out double x1, out _);
        MollweideForward(zones, zone2, lambda, phi2, out double x2, out _);
        return x2 - x1;
    }

    /// <summary>
    /// Represents one interrupted Mollweide zone definition.
    /// </summary>
    protected sealed class MollweideZoneDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MollweideZoneDefinition"/> class.
        /// </summary>
        /// <param name="x0">Zone x-offset in unit-space coordinates.</param>
        /// <param name="lambda0">Zone central meridian in radians.</param>
        /// <param name="y0">Zone y-offset in unit-space coordinates.</param>
        public MollweideZoneDefinition(double x0, double lambda0, double y0)
        {
            this.X0 = x0;
            this.Lambda0 = lambda0;
            this.Y0 = y0;
        }

        /// <summary>
        /// Gets or sets the zone x-offset in unit-space coordinates.
        /// </summary>
        public double X0 { get; set; }

        /// <summary>
        /// Gets the zone central meridian in radians.
        /// </summary>
        public double Lambda0 { get; }

        /// <summary>
        /// Gets the zone y-offset in unit-space coordinates.
        /// </summary>
        public double Y0 { get; }
    }
}
