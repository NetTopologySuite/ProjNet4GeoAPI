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
internal class InterruptedMollweideProjection : InterruptedMollweideBaseProjection
{
    private const double SeamSlack = 1e-10;

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
    private readonly MollweideZoneDefinition[] zones;
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
    public InterruptedMollweideProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Interrupted_Mollweide";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        this.zones =
        [
            new MollweideZoneDefinition(-D100, -D100, 0d), // 1
            new MollweideZoneDefinition(D30, D30, 0d),     // 2
            new MollweideZoneDefinition(-D160, -D160, 0d), // 3
            new MollweideZoneDefinition(-D60, -D60, 0d),   // 4
            new MollweideZoneDefinition(D20, D20, 0d),     // 5
            new MollweideZoneDefinition(D140, D140, 0d),   // 6
        ];

        this.zones[2].X0 += ComputeZoneOffset(this.zones, 3, 1, -D160, 0d - SeamSlack, 0d + SeamSlack);
        this.zones[1].X0 += ComputeZoneOffset(this.zones, 2, 1, -D40, 0d + SeamSlack, 0d + SeamSlack);
        this.zones[3].X0 += ComputeZoneOffset(this.zones, 4, 1, -D100, 0d - SeamSlack, 0d + SeamSlack);
        this.zones[4].X0 += ComputeZoneOffset(this.zones, 5, 2, -D20, 0d - SeamSlack, 0d + SeamSlack);
        this.zones[5].X0 += ComputeZoneOffset(this.zones, 6, 2, D80, 0d - SeamSlack, 0d + SeamSlack);

        this.boundary12 = ComputeZoneBoundaryX(this.zones, DetermineForwardZone, -D40, 0d + SeamSlack);
        this.boundary34 = ComputeZoneBoundaryX(this.zones, DetermineForwardZone, -D100, 0d - SeamSlack);
        this.boundary45 = ComputeZoneBoundaryX(this.zones, DetermineForwardZone, -D20, 0d - SeamSlack);
        this.boundary56 = ComputeZoneBoundaryX(this.zones, DetermineForwardZone, D80, 0d - SeamSlack);
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
        MollweideZoneDefinition def = this.zones[zone - 1];
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

        MollweideZoneDefinition def = this.zones[zone - 1];
        MollweideInverseUnit(xUnit - def.X0, yUnit - def.Y0, out double lambdaLocal, out double phi);
        double lambda = lambdaLocal + def.Lambda0;
        if (!IsInZone(
            zone,
            lambda,
            phi,
            [
                (-D180, -D40, true),
                (-D40, D180, true),
                (-D180, -D100, false),
                (-D100, -D20, false),
                (-D20, D80, false),
                (D80, D180, false),
            ]))
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
        double y90 = Math.Sqrt(2d);
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
}
