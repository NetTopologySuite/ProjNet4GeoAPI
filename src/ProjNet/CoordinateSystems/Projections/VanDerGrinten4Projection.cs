// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical van der Grinten IV projection (<c>vandg4</c>).
/// </summary>
/// <remarks>
/// Inverse projection is not supported.
/// <para>The forward formulation was independently verified against the van der Grinten IV
/// construction. The implementation matches the special-case branches and the general
/// auxiliary <c>bt</c>/<c>ct</c>/<c>dt</c> expressions used for interior points.</para>
/// </remarks>
internal class VanDerGrinten4Projection : MapProjection
{
    private const double Tolerance = 1e-10d;

    private readonly double radius;

    /// <summary>
    /// Initializes a new instance of the <see cref="VanDerGrinten4Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public VanDerGrinten4Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VanDerGrinten4Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public VanDerGrinten4Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Van_der_Grinten_IV";
        this.radius = this.semiMajor * this.scaleFactor;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new VanDerGrinten4Projection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;
        double x;
        double y;

        if (Math.Abs(phi) < Tolerance)
        {
            x = lambda;
            y = 0d;
        }
        else if (Math.Abs(lambda) < Tolerance || Math.Abs(Math.Abs(phi) - HalfPi) < Tolerance)
        {
            x = 0d;
            y = phi;
        }
        else
        {
            double bt = Math.Abs((2d / PI) * phi);
            double bt2 = bt * bt;
            double ct = 0.5d * ((bt * (8d - (bt * (2d + bt2)))) - 5d) / (bt2 * (bt - 1d));
            double ct2 = ct * ct;
            double dt = (2d / PI) * lambda;
            dt += 1d / dt;
            dt = Math.Sqrt((dt * dt) - 4d);
            if ((Math.Abs(lambda) - HalfPi) < 0d)
            {
                dt = -dt;
            }

            double dt2 = dt * dt;
            double x1 = bt + ct;
            x1 *= x1;
            double t = bt + (3d * ct);
            double ft = (x1 * (bt2 + (ct2 * dt2) - 1d))
                + ((1d - bt2) * ((bt2 * ((t * t) + (4d * ct2))) + (ct2 * ((12d * bt * ct) + (4d * ct2)))));
            x1 = ((dt * (x1 + ct2 - 1d)) + (2d * Math.Sqrt(ft))) / ((4d * x1) + dt2);
            x = HalfPi * x1;
            y = HalfPi * Math.Sqrt(1d + (dt * Math.Abs(x1)) - (x1 * x1));

            if (lambda < 0d)
            {
                x = -x;
            }

            if (phi < 0d)
            {
                y = -y;
            }
        }

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("van der Grinten IV does not support inverse projection in this wave.");
    }
}
