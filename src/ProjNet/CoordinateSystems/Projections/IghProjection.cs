// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Interrupted Goode Homolosine projection (<c>igh</c>).
/// </summary>
/// <remarks>
/// Combines the sinusoidal projection for latitudes within approximately ±40°44′12″ and the
/// Mollweide projection for higher latitudes, with interruptions optimised for the continental
/// landmasses. The projection uses 12 zones: two Mollweide and two sinusoidal zones in the
/// northern hemisphere, and four sinusoidal and four Mollweide zones in the southern hemisphere.
/// </remarks>
internal class IghProjection : MapProjection
{
    private const double EpsLn = 1e-10;
    private const int MollweideIterations = 12;

    private static readonly double Sqrt2 = Math.Sqrt(2d);
    private static readonly double PhiBoundary = DegreesToRadians(40d + (44d / 60d) + (11.8d / 3600d));

    private static readonly double D20 = DegreesToRadians(20d);
    private static readonly double D30 = DegreesToRadians(30d);
    private static readonly double D40 = DegreesToRadians(40d);
    private static readonly double D50 = DegreesToRadians(50d);
    private static readonly double D60 = DegreesToRadians(60d);
    private static readonly double D80 = DegreesToRadians(80d);
    private static readonly double D100 = DegreesToRadians(100d);
    private static readonly double D140 = DegreesToRadians(140d);
    private static readonly double D160 = DegreesToRadians(160d);
    private static readonly double D180 = DegreesToRadians(180d);

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double dy0;
    private readonly ZoneDefinition[] zones;

    /// <summary>
    /// Initializes a new instance of the <see cref="IghProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public IghProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IghProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public IghProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Interrupted_Goode_Homolosine";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        MollweideForwardUnit(0d, PhiBoundary, out _, out double mollweideBoundaryY);
        this.dy0 = PhiBoundary - mollweideBoundaryY;
        this.zones = CreateZones(this.dy0);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new IghProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        int zoneIndex = DetermineForwardZone(lat, lambda);
        ZoneDefinition zone = this.zones[zoneIndex];

        double localLambda = lambda - zone.Lambda0;
        double xUnit;
        double yUnit;
        if (zone.IsMollweide)
        {
            MollweideForwardUnit(localLambda, lat, out xUnit, out yUnit);
        }
        else
        {
            xUnit = localLambda * Math.Cos(lat);
            yUnit = lat;
        }

        lon = this.radius * (zone.X0 + xUnit);
        lat = this.radius * (zone.Y0 + yUnit);
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = y * this.inverseRadius;
        int zoneIndex = DetermineInverseZone(xUnit, yUnit, this.dy0);
        if (zoneIndex < 0)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        ZoneDefinition zone = this.zones[zoneIndex];
        double localX = xUnit - zone.X0;
        double localY = yUnit - zone.Y0;

        double lambdaLocal;
        double phi;
        if (zone.IsMollweide)
        {
            MollweideInverseUnit(localX, localY, out lambdaLocal, out phi);
        }
        else
        {
            phi = localY;
            double cosPhi = Math.Cos(phi);
            lambdaLocal = Math.Abs(cosPhi) <= Eps10 ? 0d : (localX / cosPhi);
        }

        double lambda = lambdaLocal + zone.Lambda0;
        if (!IsPointInZone(zoneIndex, lambda, phi))
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }

    private static ZoneDefinition[] CreateZones(double dy0)
    {
        return
        [
            new ZoneDefinition(true, -D100, -D100, dy0),  // 1
            new ZoneDefinition(true, D30, D30, dy0),      // 2
            new ZoneDefinition(false, -D100, -D100, 0d),  // 3
            new ZoneDefinition(false, D30, D30, 0d),      // 4
            new ZoneDefinition(false, -D160, -D160, 0d),  // 5
            new ZoneDefinition(false, -D60, -D60, 0d),    // 6
            new ZoneDefinition(false, D20, D20, 0d),      // 7
            new ZoneDefinition(false, D140, D140, 0d),    // 8
            new ZoneDefinition(true, -D160, -D160, -dy0), // 9
            new ZoneDefinition(true, -D60, -D60, -dy0),   // 10
            new ZoneDefinition(true, D20, D20, -dy0),     // 11
            new ZoneDefinition(true, D140, D140, -dy0),   // 12
        ];
    }

    private static int DetermineForwardZone(double phi, double lambda)
    {
        if (phi >= PhiBoundary)
        {
            return lambda <= -D40 ? 0 : 1;
        }

        if (phi >= 0d)
        {
            return lambda <= -D40 ? 2 : 3;
        }

        if (phi >= -PhiBoundary)
        {
            if (lambda <= -D100)
            {
                return 4;
            }

            return lambda <= -D20 ? 5 : lambda <= D80 ? 6 : 7;
        }

        if (lambda <= -D100)
        {
            return 8;
        }

        return lambda <= -D20 ? 9 : lambda <= D80 ? 10 : 11;
    }

    private static int DetermineInverseZone(double x, double y, double dy0)
    {
        double y90 = dy0 + Sqrt2;
        if (y > (y90 + EpsLn) || y < (-y90 - EpsLn))
        {
            return -1;
        }

        if (y >= PhiBoundary)
        {
            return x <= -D40 ? 0 : 1;
        }

        if (y >= 0d)
        {
            return x <= -D40 ? 2 : 3;
        }

        if (y >= -PhiBoundary)
        {
            if (x <= -D100)
            {
                return 4;
            }

            return x <= -D20 ? 5 : x <= D80 ? 6 : 7;
        }

        if (x <= -D100)
        {
            return 8;
        }

        return x <= -D20 ? 9 : x <= D80 ? 10 : 11;
    }

    private static bool IsPointInZone(int zoneIndex, double lambda, double phi)
    {
        return zoneIndex switch
        {
            0 => ((lambda >= -D180 - EpsLn) && (lambda <= -D40 + EpsLn))
                                || (((lambda >= -D40 - EpsLn) && (lambda <= -DegreesToRadians(10d) + EpsLn))
                                    && ((phi >= D60 - EpsLn) && (phi <= HalfPi + EpsLn))),
            1 => ((lambda >= -D40 - EpsLn) && (lambda <= D180 + EpsLn))
                                || (((lambda >= -D180 - EpsLn) && (lambda <= -D160 + EpsLn))
                                    && ((phi >= D50 - EpsLn) && (phi <= HalfPi + EpsLn)))
                                || (((lambda >= -DegreesToRadians(50d) - EpsLn) && (lambda <= -D40 + EpsLn))
                                    && ((phi >= D60 - EpsLn) && (phi <= HalfPi + EpsLn))),
            2 => (lambda >= -D180 - EpsLn) && (lambda <= -D40 + EpsLn),
            3 => (lambda >= -D40 - EpsLn) && (lambda <= D180 + EpsLn),
            4 or 8 => (lambda >= -D180 - EpsLn) && (lambda <= -D100 + EpsLn),
            5 or 9 => (lambda >= -D100 - EpsLn) && (lambda <= -D20 + EpsLn),
            6 or 10 => (lambda >= -D20 - EpsLn) && (lambda <= D80 + EpsLn),
            7 or 11 => (lambda >= D80 - EpsLn) && (lambda <= D180 + EpsLn),
            _ => false,
        };
    }

    private static void MollweideForwardUnit(double lambda, double phi, out double x, out double y)
    {
        double theta;
        if (Math.Abs(Math.Abs(phi) - HalfPi) < 1e-12)
        {
            theta = Sign(phi) * HalfPi;
        }
        else
        {
            theta = phi;
            double target = PI * Math.Sin(phi);
            for (int i = 0; i < MollweideIterations; i++)
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

        x = (2d * Sqrt2 / PI) * lambda * Math.Cos(theta);
        y = Sqrt2 * Math.Sin(theta);
    }

    private static void MollweideInverseUnit(double x, double y, out double lambda, out double phi)
    {
        double theta = Math.Asin(ProjectionConstants.Clamp(y / Sqrt2, -1d, 1d));
        double cosTheta = Math.Cos(theta);
        if (Math.Abs(cosTheta) <= Eps10)
        {
            lambda = 0d;
        }
        else
        {
            lambda = x * PI / (2d * Sqrt2 * cosTheta);
        }

        phi = Math.Asin(ProjectionConstants.Clamp(((2d * theta) + Math.Sin(2d * theta)) / PI, -1d, 1d));
    }

    private readonly struct ZoneDefinition(bool isMollweide, double lambda0, double x0, double y0)
    {
        public bool IsMollweide { get; } = isMollweide;

        public double Lambda0 { get; } = lambda0;

        public double X0 { get; } = x0;

        public double Y0 { get; } = y0;
    }
}
