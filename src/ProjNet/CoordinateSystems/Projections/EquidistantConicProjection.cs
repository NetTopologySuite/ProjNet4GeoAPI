// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Equidistant Conic projection (<c>eqdc</c>).
/// </summary>
/// <remarks>
/// <para>Distances along all meridians and along the two standard parallels are
/// preserved. Supports both one-standard-parallel and two-standard-parallel forms;
/// when a single parallel is specified via <c>standard_parallel_1</c>, the cone
/// constant is set to the sine of that parallel.</para>
/// <para>The spherical and ellipsoidal formulations were independently verified against
/// Snyder, "Map Projections - A Working Manual" (USGS Professional Paper 1395, 1987),
/// section 16, Equidistant Conic, and the PROJ <c>eqdc</c> documentation. The
/// ellipsoidal branch matches the published use of meridional distances and parallel
/// scale factors through <c>Mlfn</c> and <c>Msfnz</c>, which corrects the earlier
/// spherical-only implementation.</para>
/// </remarks>
/// <seealso href="https://epsg.io/1119-method">EPSG method 1119: Equidistant Conic.</seealso>
/// <seealso href="https://pubs.usgs.gov/publication/pp1395">USGS Professional Paper 1395: Map Projections - A Working Manual.</seealso>
/// <seealso href="https://proj.org/en/stable/operations/projections/eqdc.html">PROJ documentation: Equidistant Conic.</seealso>
/// <seealso href="https://en.wikipedia.org/wiki/Equidistant_conic_projection">Wikipedia: Equidistant conic projection.</seealso>
/// <seealso>Bugayevskiy &amp; Snyder (1995), "Map Projections: A Reference Manual", Ch. 3, Sect. 3.1.4, pp. 95-98.</seealso>
internal class EquidistantConicProjection : MapProjection
{
    private const double Epsilon = 1e-10d;

    private readonly bool ellipsoidal;
    private readonly double n;
    private readonly double g;
    private readonly double rho0;

    /// <summary>
    /// Initializes a new instance of the <see cref="EquidistantConicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public EquidistantConicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EquidistantConicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public EquidistantConicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Equidistant_Conic";
        this.ellipsoidal = this.es > 0d;

        double standardParallel1 = DegreesToRadians(this.Parameters.GetParameterValue("standard_parallel_1", "lat_1"));
        double standardParallel2 = DegreesToRadians(this.Parameters.GetOptionalParameterValue("standard_parallel_2", RadiansToDegrees(standardParallel1), "lat_2"));

        bool secant = Math.Abs(standardParallel1 - standardParallel2) >= Epsilon;
        double sinParallel1 = Math.Sin(standardParallel1);
        this.n = sinParallel1;
        if (this.ellipsoidal)
        {
            double cosParallel1 = Math.Cos(standardParallel1);
            double m1 = Msfnz(this.e, sinParallel1, cosParallel1);
            double ml1 = this.Mlfn(standardParallel1, sinParallel1, cosParallel1);
            if (secant)
            {
                double sinParallel2 = Math.Sin(standardParallel2);
                double cosParallel2 = Math.Cos(standardParallel2);
                double ml2 = this.Mlfn(standardParallel2, sinParallel2, cosParallel2);
                if (ml1 == ml2)
                {
                    ArgumentGuard.ThrowArgument("Invalid standard parallels for equidistant conic projection.");
                }

                this.n = (m1 - Msfnz(this.e, sinParallel2, cosParallel2)) / (ml2 - ml1);
            }

            if (Math.Abs(this.n) <= Eps10)
            {
                ArgumentGuard.ThrowArgument("Invalid standard parallels for equidistant conic projection.");
            }

            this.g = ml1 + (m1 / this.n);
            this.rho0 = this.g - this.Mlfn(this.latOrigin, Math.Sin(this.latOrigin), Math.Cos(this.latOrigin));
        }
        else
        {
            if (secant)
            {
                this.n = (Math.Cos(standardParallel1) - Math.Cos(standardParallel2)) / (standardParallel2 - standardParallel1);
            }

            if (Math.Abs(this.n) <= Eps10)
            {
                ArgumentGuard.ThrowArgument("Invalid standard parallels for equidistant conic projection.");
            }

            this.g = (Math.Cos(standardParallel1) / this.n) + standardParallel1;
            this.rho0 = this.g - this.latOrigin;
        }
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new EquidistantConicProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double theta = this.n * Adjust_lon(lon - this.centralMeridian);
        double rho = this.g
            - (this.ellipsoidal
                ? this.Mlfn(lat, Math.Sin(lat), Math.Cos(lat))
                : lat);

        lon = this.SphericalRadius * rho * Math.Sin(theta);
        lat = this.SphericalRadius * (this.rho0 - (rho * Math.Cos(theta)));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.InverseSphericalRadius;
        double yUnit = y * this.InverseSphericalRadius;
        double rhoPrime = this.rho0 - yUnit;
        double rho = Sign(this.n) * Math.Sqrt((xUnit * xUnit) + (rhoPrime * rhoPrime));

        double theta = 0d;
        if (Math.Abs(rho) > Eps10)
        {
            theta = Math.Atan2(xUnit, rhoPrime);
        }

        x = Adjust_lon(this.centralMeridian + (theta / this.n));
        y = this.ellipsoidal ? this.Inv_mlfn(this.g - rho) : this.g - rho;
    }
}
