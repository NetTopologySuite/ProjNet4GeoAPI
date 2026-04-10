// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;

/// <summary>
/// Shared implementation for simple spherical conic projections in <c>sconics.cpp</c>.
/// </summary>
/// <remarks>
/// All variants are spherical-only and share a common polar-conic forward and inverse
/// transform. The cone constant <c>n</c> and the reference radius <c>rhoC</c> are computed
/// differently for each variant. The Murdoch II variant uses a tangent-based radial
/// distance rather than the linear distance used by the other variants.
/// <para>The shared formulation was independently verified against the classical simple-conic
/// family used for the Euler, Murdoch, Tissot, and Vitkovsky variants. The implementation
/// matches the common polar-conic structure <c>x = ρ * sin(n * λ)</c>,
/// <c>y = rho0 - ρ * cos(n * λ)</c> with variant-specific definitions of
/// <c>n</c>, <c>rhoC</c>, and <c>rho0</c>.</para>
/// </remarks>
internal abstract class SimpleConicProjectionBase : MapProjection
{
    private readonly SimpleConicType type;
    private readonly double n;
    private readonly double rhoC;
    private readonly double rho0;
    private readonly double sig;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleConicProjectionBase"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    /// <param name="type">Simple conic variant.</param>
    /// <param name="name">Projection name.</param>
    protected SimpleConicProjectionBase(
        IEnumerable<ProjectionParameter> parameters,
        MapProjection? inverse,
        SimpleConicType type,
        string name)
        : base(parameters, inverse)
    {
        this.type = type;
        this.Name = name;

        double phi1 = DegreesToRadians(this.Parameters.GetParameterValue("lat_1", "standard_parallel_1"));
        double phi2 = DegreesToRadians(this.Parameters.GetParameterValue("lat_2", "standard_parallel_2"));
        double delta = 0.5d * (phi2 - phi1);
        this.sig = 0.5d * (phi2 + phi1);

        if (Math.Abs(delta) < Eps10 || Math.Abs(this.sig) < Eps10)
        {
            ArgumentGuard.ThrowArgument("Illegal value for lat_1 and lat_2: |lat_1 - lat_2| and |lat_1 + lat_2| should be > 0.");
        }

        switch (type)
        {
            case SimpleConicType.Tissot:
            {
                this.n = Math.Sin(this.sig);
                double cs = Math.Cos(delta);
                this.rhoC = (this.n / cs) + (cs / this.n);
                double tissotDomain = (this.rhoC - (2d * Math.Sin(this.latOrigin))) / this.n;
                if (tissotDomain < 0d)
                {
                    ArgumentGuard.ThrowArgument("Input data outside projection domain.");
                }

                this.rho0 = Math.Sqrt(tissotDomain);
                break;
            }

            case SimpleConicType.Murdoch1:
                this.rhoC = (Math.Sin(delta) / (delta * Math.Tan(this.sig))) + this.sig;
                this.rho0 = this.rhoC - this.latOrigin;
                this.n = Math.Sin(this.sig);
                break;

            case SimpleConicType.Murdoch2:
            {
                double cosDelta = Math.Cos(delta);
                if (cosDelta < 0d)
                {
                    ArgumentGuard.ThrowArgument("Input data outside projection domain.");
                }

                double cs = Math.Sqrt(cosDelta);
                this.rhoC = cs / Math.Tan(this.sig);
                this.rho0 = this.rhoC + Math.Tan(this.sig - this.latOrigin);
                this.n = Math.Sin(this.sig) * cs;
                break;
            }

            case SimpleConicType.Murdoch3:
                this.rhoC = (delta / (Math.Tan(this.sig) * Math.Tan(delta))) + this.sig;
                this.rho0 = this.rhoC - this.latOrigin;
                this.n = Math.Sin(this.sig) * Math.Sin(delta) * Math.Tan(delta) / (delta * delta);
                break;

            case SimpleConicType.Euler:
                this.n = Math.Sin(this.sig) * Math.Sin(delta) / delta;
                delta *= 0.5d;
                this.rhoC = (delta / (Math.Tan(delta) * Math.Tan(this.sig))) + this.sig;
                this.rho0 = this.rhoC - this.latOrigin;
                break;

            case SimpleConicType.Vitkovsky1:
            {
                double cs = Math.Tan(delta);
                this.n = cs * Math.Sin(this.sig) / delta;
                this.rhoC = (delta / (cs * Math.Tan(this.sig))) + this.sig;
                this.rho0 = this.rhoC - this.latOrigin;
                break;
            }

            default:
                ArgumentGuard.ThrowArgumentOutOfRange(nameof(type));
                break;
        }
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double rho = this.type == SimpleConicType.Murdoch2
            ? this.rhoC + Math.Tan(this.sig - lat)
            : this.rhoC - lat;
        double theta = lambda * this.n;

        lon = this.SphericalRadius * rho * Math.Sin(theta);
        lat = this.SphericalRadius * (this.rho0 - (rho * Math.Cos(theta)));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.InverseSphericalRadius;
        double yUnit = this.rho0 - (y * this.InverseSphericalRadius);
        double rho = Hypot(xUnit, yUnit);
        if (this.n < 0d)
        {
            rho = -rho;
            xUnit = -xUnit;
            yUnit = -yUnit;
        }

        double lambda = Math.Atan2(xUnit, yUnit) / this.n;
        double phi = this.type == SimpleConicType.Murdoch2
            ? this.sig - Math.Atan(rho - this.rhoC)
            : this.rhoC - rho;

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
