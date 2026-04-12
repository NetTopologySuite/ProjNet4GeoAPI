// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the interrupted Mollweide Oceanic projection (<c>imoll_o</c>).
/// </summary>
/// <remarks>
/// <para>The interrupted Mollweide Oceanic projection is a six-lobe equal-area
/// Mollweide variant arranged to keep the major ocean basins visually continuous. In
/// contrast with the interrupted Goode homolosine oceanic projection, it keeps the
/// Mollweide construction at all latitudes and therefore omits the sinusoidal transition
/// latitude.</para>
/// <para>This implementation matches PROJ's <c>imoll_o</c> definition for the
/// ocean-centered interrupted Mollweide arrangement, also attributed to J. P. Goode's
/// 1919 interrupted homolographic work. The six zone definitions encode the standard
/// oceanic interruption pattern recommended for central longitude near -160 degrees.</para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/projections/imoll_o.html">PROJ documentation: Interrupted Mollweide Oceanic View.</seealso>
/// <seealso href="https://en.wikipedia.org/wiki/Mollweide_projection">Wikipedia: Mollweide projection.</seealso>
internal sealed class InterruptedMollweideOceanicProjection : InterruptedMollweideBaseProjection
{
    private const double SeamSlack = 1e-10d;

    private static readonly double D10 = DegreesToRadians(10d);
    private static readonly double D20 = DegreesToRadians(20d);
    private static readonly double D60 = DegreesToRadians(60d);
    private static readonly double D90 = DegreesToRadians(90d);
    private static readonly double D110 = DegreesToRadians(110d);
    private static readonly double D130 = DegreesToRadians(130d);
    private static readonly double D140 = DegreesToRadians(140d);
    private static readonly double D150 = DegreesToRadians(150d);
    private static readonly double D180 = DegreesToRadians(180d);

    private readonly MollweideZoneDefinition[] zones;
    private readonly double boundary12;
    private readonly double boundary23;
    private readonly double boundary45;
    private readonly double boundary56;

    /// <summary>
    /// Initializes a new instance of the <see cref="InterruptedMollweideOceanicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public InterruptedMollweideOceanicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterruptedMollweideOceanicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public InterruptedMollweideOceanicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Interrupted_Mollweide_Oceanic_View";

        this.zones =
        [
            new MollweideZoneDefinition(-D140, -D140, 0d), // 1
            new MollweideZoneDefinition(-D10, -D10, 0d),   // 2
            new MollweideZoneDefinition(D130, D130, 0d),   // 3
            new MollweideZoneDefinition(-D110, -D110, 0d), // 4
            new MollweideZoneDefinition(D20, D20, 0d),     // 5
            new MollweideZoneDefinition(D150, D150, 0d),   // 6
        ];

        this.zones[1].X0 += ComputeZoneOffset(this.zones, 2, 1, -D90, 0d + SeamSlack, 0d + SeamSlack);
        this.zones[2].X0 += ComputeZoneOffset(this.zones, 3, 2, D60, 0d + SeamSlack, 0d + SeamSlack);
        this.zones[3].X0 += ComputeZoneOffset(this.zones, 4, 1, -D180, 0d - SeamSlack, 0d + SeamSlack);
        this.zones[4].X0 += ComputeZoneOffset(this.zones, 5, 2, -D60, 0d - SeamSlack, 0d + SeamSlack);
        this.zones[5].X0 += ComputeZoneOffset(this.zones, 6, 3, D90, 0d - SeamSlack, 0d + SeamSlack);

        this.boundary12 = ComputeZoneBoundaryX(this.zones, DetermineForwardZone, -D90, 0d + SeamSlack);
        this.boundary23 = ComputeZoneBoundaryX(this.zones, DetermineForwardZone, D60, 0d + SeamSlack);
        this.boundary45 = ComputeZoneBoundaryX(this.zones, DetermineForwardZone, -D60, 0d - SeamSlack);
        this.boundary56 = ComputeZoneBoundaryX(this.zones, DetermineForwardZone, D90, 0d - SeamSlack);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new InterruptedMollweideOceanicProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        int zone = DetermineForwardZone(lat, lambda);
        MollweideZoneDefinition def = this.zones[zone - 1];
        MollweideForwardUnit(lambda - def.Lambda0, lat, out double xUnit, out double yUnit);
        lon = this.SphericalRadius * (xUnit + def.X0);
        lat = this.SphericalRadius * (yUnit + def.Y0);
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.InverseSphericalRadius;
        double yUnit = y * this.InverseSphericalRadius;
        int zone = DetermineInverseZone(xUnit, yUnit, this.boundary12, this.boundary23, this.boundary45, this.boundary56);
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
                (-D180, -D90, true),
                (-D90, D60, true),
                (D60, D180, true),
                (-D180, -D60, false),
                (-D60, D90, false),
                (D90, D180, false),
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
            return lambda <= -D90 ? 1 : lambda >= D60 ? 3 : 2;
        }

        return lambda <= -D60 ? 4 : lambda >= D90 ? 6 : 5;
    }

    private static int DetermineInverseZone(
        double x,
        double y,
        double seam12,
        double seam23,
        double seam45,
        double seam56)
    {
        double y90 = ProjectionConstants.Sqrt2;
        if (y > y90 + SeamSlack || y < -y90 - SeamSlack)
        {
            return 0;
        }

        if (y >= 0d)
        {
            return x <= seam12 ? 1 : x >= seam23 ? 3 : 2;
        }

        return x <= seam45 ? 4 : x >= seam56 ? 6 : 5;
    }
}
