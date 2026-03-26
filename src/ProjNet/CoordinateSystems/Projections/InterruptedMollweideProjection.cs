// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the interrupted Mollweide projection (<c>imoll</c>).
/// </summary>
[Serializable]
internal class InterruptedMollweideProjection : MapProjection
{
    private const int MollweideIterations = 12;
    private const double SeamSlack = 1e-10;

    private static readonly double Sqrt2 = Math.Sqrt(2d);

    private static readonly double D20 = DegreesToRadians(20d);
    private static readonly double D30 = DegreesToRadians(30d);
    private static readonly double D40 = DegreesToRadians(40d);
    private static readonly double D60 = DegreesToRadians(60d);
    private static readonly double D80 = DegreesToRadians(80d);
    private static readonly double D100 = DegreesToRadians(100d);
    private static readonly double D140 = DegreesToRadians(140d);
    private static readonly double D160 = DegreesToRadians(160d);
    private static readonly double D180 = DegreesToRadians(180d);

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly ZoneDefinition[] zones;
    private readonly double boundary12;
    private readonly double boundary34;
    private readonly double boundary45;
    private readonly double boundary56;

    /// <summary>
    /// Initializes a new instance of the <see cref="InterruptedMollweideProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public InterruptedMollweideProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterruptedMollweideProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public InterruptedMollweideProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Interrupted_Mollweide";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        this.zones =
        [
            new ZoneDefinition(-D100, -D100, 0d), // 1
            new ZoneDefinition(D30, D30, 0d),     // 2
            new ZoneDefinition(-D160, -D160, 0d), // 3
            new ZoneDefinition(-D60, -D60, 0d),   // 4
            new ZoneDefinition(D20, D20, 0d),     // 5
            new ZoneDefinition(D140, D140, 0d),   // 6
        ];

        this.zones[2].X0 += this.ComputeZoneOffset(3, 1, -D160, 0d - SeamSlack, 0d + SeamSlack);
        this.zones[1].X0 += this.ComputeZoneOffset(2, 1, -D40, 0d + SeamSlack, 0d + SeamSlack);
        this.zones[3].X0 += this.ComputeZoneOffset(4, 1, -D100, 0d - SeamSlack, 0d + SeamSlack);
        this.zones[4].X0 += this.ComputeZoneOffset(5, 2, -D20, 0d - SeamSlack, 0d + SeamSlack);
        this.zones[5].X0 += this.ComputeZoneOffset(6, 2, D80, 0d - SeamSlack, 0d + SeamSlack);

        this.boundary12 = this.ComputeZoneBoundaryX(-D40, 0d + SeamSlack);
        this.boundary34 = this.ComputeZoneBoundaryX(-D100, 0d - SeamSlack);
        this.boundary45 = this.ComputeZoneBoundaryX(-D20, 0d - SeamSlack);
        this.boundary56 = this.ComputeZoneBoundaryX(D80, 0d - SeamSlack);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new InterruptedMollweideProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        int zone = DetermineForwardZone(lat, lambda);
        ZoneDefinition def = this.zones[zone - 1];
        MollweideForwardUnit(lambda - def.Lambda0, lat, out double xUnit, out double yUnit);
        lon = this.radius * (xUnit + def.X0);
        lat = this.radius * (yUnit + def.Y0);
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = y * this.inverseRadius;
        int zone = DetermineInverseZone(xUnit, yUnit, this.boundary12, this.boundary34, this.boundary45, this.boundary56);
        if (zone == 0)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        ZoneDefinition def = this.zones[zone - 1];
        MollweideInverseUnit(xUnit - def.X0, yUnit - def.Y0, out double lambdaLocal, out double phi);
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
        if (phi >= 0d)
        {
            return lambda <= -D40 ? 1 : 2;
        }

        if (lambda <= -D100)
        {
            return 3;
        }

        if (lambda <= -D20)
        {
            return 4;
        }

        return lambda <= D80 ? 5 : 6;
    }

    private static int DetermineInverseZone(
        double x,
        double y,
        double seam12,
        double seam34,
        double seam45,
        double seam56)
    {
        double y90 = Sqrt2;
        if (y > y90 + SeamSlack || y < -y90 - SeamSlack)
        {
            return 0;
        }

        if (y >= 0d)
        {
            return x <= seam12 ? 1 : 2;
        }

        if (x <= seam34)
        {
            return 3;
        }

        if (x <= seam45)
        {
            return 4;
        }

        return x <= seam56 ? 5 : 6;
    }

    private static bool IsInZone(int zone, double lambda, double phi)
    {
        switch (zone)
        {
            case 1:
                return lambda >= -D180 - SeamSlack && lambda <= -D40 + SeamSlack && phi >= 0d - SeamSlack;
            case 2:
                return lambda >= -D40 - SeamSlack && lambda <= D180 + SeamSlack && phi >= 0d - SeamSlack;
            case 3:
                return lambda >= -D180 - SeamSlack && lambda <= -D100 + SeamSlack && phi <= 0d + SeamSlack;
            case 4:
                return lambda >= -D100 - SeamSlack && lambda <= -D20 + SeamSlack && phi <= 0d + SeamSlack;
            case 5:
                return lambda >= -D20 - SeamSlack && lambda <= D80 + SeamSlack && phi <= 0d + SeamSlack;
            case 6:
                return lambda >= D80 - SeamSlack && lambda <= D180 + SeamSlack && phi <= 0d + SeamSlack;
            default:
                return false;
        }
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
        double theta = Math.Asin(Clamp(y / Sqrt2, -1d, 1d));
        double cosTheta = Math.Cos(theta);
        lambda = Math.Abs(cosTheta) <= Eps10 ? 0d : (x * PI / (2d * Sqrt2 * cosTheta));
        phi = Math.Asin(Clamp(((2d * theta) + Math.Sin(2d * theta)) / PI, -1d, 1d));
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }

    private void MollweideForward(int zone, double lambda, double phi, out double x, out double y)
    {
        ZoneDefinition def = this.zones[zone - 1];
        MollweideForwardUnit(lambda - def.Lambda0, phi, out double xUnit, out double yUnit);
        x = xUnit + def.X0;
        y = yUnit + def.Y0;
    }

    private double ComputeZoneOffset(int zone1, int zone2, double lambda, double phi1, double phi2)
    {
        this.MollweideForward(zone1, lambda, phi1, out double x1, out _);
        this.MollweideForward(zone2, lambda, phi2, out double x2, out _);
        return x2 - x1;
    }

    private double ComputeZoneBoundaryX(double lambda, double phi)
    {
        this.MollweideForward(DetermineForwardZone(phi, lambda - SeamSlack), lambda - SeamSlack, phi, out double x1, out _);
        this.MollweideForward(DetermineForwardZone(phi, lambda + SeamSlack), lambda + SeamSlack, phi, out double x2, out _);
        return (x1 + x2) * 0.5d;
    }

    private sealed class ZoneDefinition
    {
        public ZoneDefinition(double x0, double lambda0, double y0)
        {
            this.X0 = x0;
            this.Lambda0 = lambda0;
            this.Y0 = y0;
        }

        public double X0 { get; set; }

        public double Lambda0 { get; }

        public double Y0 { get; }
    }
}
