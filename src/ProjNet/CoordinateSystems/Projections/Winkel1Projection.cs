// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Winkel I projection (<c>wink1</c>).
/// </summary>
/// <remarks>
/// <para>Winkel I is the 1914 projection introduced by Oswald Winkel. The
/// implementation applies the classic averaged longitude scale
/// <c>0.5 * λ * (cos(phi1) + cos(φ))</c> with the configurable true-scale
/// latitude parameter.</para>
/// <para>This implementation matches PROJ's <c>wink1</c> formulation for the
/// arithmetic mean of the sinusoidal and equidistant cylindrical projections, with
/// the configurable true-scale latitude preserving Winkel's standard-parallel
/// variant.</para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/projections/wink1.html">PROJ documentation: Winkel I.</seealso>
/// <seealso href="https://desktop.arcgis.com/en/arcmap/latest/map/projections/winkel-i.htm">ArcGIS projection reference: Winkel I.</seealso>
internal sealed class Winkel1Projection : MapProjection
{
    private readonly double cosphi1;

    /// <summary>
    /// Initializes a new instance of the <see cref="Winkel1Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Winkel1Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Winkel1Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Winkel1Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Winkel_I";

        double latTs = DegreesToRadians(this.Parameters.GetOptionalParameterValue("lat_ts", RadiansToDegrees(this.latOrigin), "latitude_true_scale"));
        this.cosphi1 = Math.Cos(latTs);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new Winkel1Projection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double x = 0.5d * lambda * (this.cosphi1 + Math.Cos(lat));
        double y = lat;

        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;
        double denominator = this.cosphi1 + Math.Cos(yy);
        if (Math.Abs(denominator) <= Eps10)
        {
            ProjectionThrowHelper.ThrowOutsideProjectionDomain();
        }

        double lambda = 2d * xx / denominator;
        x = Adjust_lon(this.centralMeridian + lambda);
        y = yy;
    }
}
