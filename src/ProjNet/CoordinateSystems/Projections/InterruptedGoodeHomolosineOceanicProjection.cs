// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the interrupted Goode Homolosine oceanic projection (<c>igh_o</c>).
/// </summary>
/// <remarks>
/// Uses the same sinusoidal/Mollweide blend as the standard Interrupted Goode Homolosine
/// projection, but with interruptions placed over the continental landmasses so that the
/// oceanic regions appear continuous. The projection uses 12 zones arranged in three
/// longitudinal panels per hemisphere.
/// </remarks>
internal class InterruptedGoodeHomolosineOceanicProjection : MapProjection
{
    private const int MollweideIterations = 12;
    private const double SeamSlack = 1e-10;

    private static readonly double Sqrt2 = Math.Sqrt(2d);
    private static readonly double PhiBoundary = DegreesToRadians(40d + (44d / 60d) + (11.8d / 3600d));

    private static readonly double D10 = DegreesToRadians(10d);
    private static readonly double D20 = DegreesToRadians(20d);
    private static readonly double D40 = DegreesToRadians(40d);
    private static readonly double D50 = DegreesToRadians(50d);
    private static readonly double D60 = DegreesToRadians(60d);
    private static readonly double D90 = DegreesToRadians(90d);
    private static readonly double D100 = DegreesToRadians(100d);
    private static readonly double D110 = DegreesToRadians(110d);
    private static readonly double D130 = DegreesToRadians(130d);
    private static readonly double D140 = DegreesToRadians(140d);
    private static readonly double D150 = DegreesToRadians(150d);
    private static readonly double D160 = DegreesToRadians(160d);
    private static readonly double D180 = DegreesToRadians(180d);

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double dy0;
    private readonly ZoneDefinition[] zones;

    /// <summary>
    /// Initializes a new instance of the <see cref="InterruptedGoodeHomolosineOceanicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public InterruptedGoodeHomolosineOceanicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterruptedGoodeHomolosineOceanicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public InterruptedGoodeHomolosineOceanicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Interrupted_Goode_Homolosine_Oceanic_View";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        MollweideForwardUnit(0d, PhiBoundary, out _, out double mollweideBoundaryY);
        this.dy0 = PhiBoundary - mollweideBoundaryY;

        this.zones =
        [
            new ZoneDefinition(true, -D140, -D140, this.dy0),  // 1
            new ZoneDefinition(true, -D10, -D10, this.dy0),    // 2
            new ZoneDefinition(true, D130, D130, this.dy0),    // 3
            new ZoneDefinition(false, -D140, -D140, 0d),       // 4
            new ZoneDefinition(false, -D10, -D10, 0d),         // 5
            new ZoneDefinition(false, D130, D130, 0d),         // 6
            new ZoneDefinition(false, -D110, -D110, 0d),       // 7
            new ZoneDefinition(false, D20, D20, 0d),           // 8
            new ZoneDefinition(false, D150, D150, 0d),         // 9
            new ZoneDefinition(true, -D110, -D110, -this.dy0), // 10
            new ZoneDefinition(true, D20, D20, -this.dy0),     // 11
            new ZoneDefinition(true, D150, D150, -this.dy0),   // 12
        ];
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new InterruptedGoodeHomolosineOceanicProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        int zone = DetermineForwardZone(lat, lambda);
        ZoneDefinition def = this.zones[zone - 1];

        double localLambda = lambda - def.Lambda0;
        double xUnit;
        double yUnit;
        if (def.IsMollweide)
        {
            MollweideForwardUnit(localLambda, lat, out xUnit, out yUnit);
        }
        else
        {
            xUnit = localLambda * Math.Cos(lat);
            yUnit = lat;
        }

        lon = this.radius * (xUnit + def.X0);
        lat = this.radius * (yUnit + def.Y0);
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = y * this.inverseRadius;
        int zone = DetermineInverseZone(xUnit, yUnit, this.dy0);
        if (zone == 0)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        ZoneDefinition def = this.zones[zone - 1];
        double localX = xUnit - def.X0;
        double localY = yUnit - def.Y0;

        double lambdaLocal;
        double phi;
        if (def.IsMollweide)
        {
            MollweideInverseUnit(localX, localY, out lambdaLocal, out phi);
        }
        else
        {
            phi = localY;
            double cosPhi = Math.Cos(phi);
            lambdaLocal = Math.Abs(cosPhi) <= Eps10 ? 0d : (localX / cosPhi);
        }

        double lambda = lambdaLocal + def.Lambda0;
        if (!IsInZone(zone, lambda, phi))
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }

    private static int DetermineForwardZone(double phi, double lambda)
    {
        if (phi >= PhiBoundary)
        {
            return lambda <= -D90 ? 1 : lambda >= D60 ? 3 : 2;
        }

        if (phi >= 0d)
        {
            return lambda <= -D90 ? 4 : lambda >= D60 ? 6 : 5;
        }

        if (phi >= -PhiBoundary)
        {
            return lambda <= -D60 ? 7 : lambda >= D90 ? 9 : 8;
        }

        return lambda <= -D60 ? 10 : lambda >= D90 ? 12 : 11;
    }

    private static int DetermineInverseZone(double x, double y, double dy0)
    {
        double y90 = dy0 + Sqrt2;
        if (y > y90 + SeamSlack || y < -y90 - SeamSlack)
        {
            return 0;
        }

        if (y >= PhiBoundary)
        {
            return x <= -D90 ? 1 : x >= D60 ? 3 : 2;
        }

        if (y >= 0d)
        {
            return x <= -D90 ? 4 : x >= D60 ? 6 : 5;
        }

        if (y >= -PhiBoundary)
        {
            return x <= -D60 ? 7 : x >= D90 ? 9 : 8;
        }

        return x <= -D60 ? 10 : x >= D90 ? 12 : 11;
    }

    private static bool IsInZone(int zone, double lambda, double phi)
    {
        return zone switch
        {
            1 => (lambda >= -D180 - SeamSlack && lambda <= -D90 + SeamSlack)
                                || (lambda >= D160 - SeamSlack && lambda <= D180 + SeamSlack && phi >= D50 - SeamSlack && phi <= D90 + SeamSlack),
            2 => lambda >= -D90 - SeamSlack && lambda <= D60 + SeamSlack,
            3 => (lambda >= D60 - SeamSlack && lambda <= D180 + SeamSlack)
                                || (lambda >= -D180 - SeamSlack && lambda <= -D160 + SeamSlack && phi >= D50 - SeamSlack && phi <= D90 + SeamSlack),
            4 => lambda >= -D180 - SeamSlack && lambda <= -D90 + SeamSlack,
            5 => lambda >= -D90 - SeamSlack && lambda <= D60 + SeamSlack,
            6 => lambda >= D60 - SeamSlack && lambda <= D180 + SeamSlack,
            7 => lambda >= -D180 - SeamSlack && lambda <= -D60 + SeamSlack,
            8 => lambda >= -D60 - SeamSlack && lambda <= D90 + SeamSlack,
            9 => lambda >= D90 - SeamSlack && lambda <= D180 + SeamSlack,
            10 => lambda >= -D180 - SeamSlack && lambda <= -D60 + SeamSlack,
            11 => (lambda >= -D60 - SeamSlack && lambda <= D90 + SeamSlack)
                                || (lambda >= D90 - SeamSlack && lambda <= D100 + SeamSlack && phi >= -D90 - SeamSlack && phi <= -D40 + SeamSlack),
            12 => lambda >= D90 - SeamSlack && lambda <= D180 + SeamSlack,
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
        lambda = Math.Abs(cosTheta) <= Eps10 ? 0d : (x * PI / (2d * Sqrt2 * cosTheta));
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
