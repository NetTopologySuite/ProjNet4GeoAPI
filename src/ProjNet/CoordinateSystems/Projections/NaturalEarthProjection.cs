// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Natural Earth projection (<c>natearth</c>).
/// </summary>
/// <remarks>
/// The Natural Earth projection is a pseudocylindrical projection with polynomial scaling
/// functions for x and y. The inverse is solved iteratively via Newton–Raphson iteration.
/// </remarks>
[Serializable]
internal class NaturalEarthProjection : MapProjection
{
    private const int Iterations = 12;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="NaturalEarthProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public NaturalEarthProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NaturalEarthProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public NaturalEarthProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Natural_Earth";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1.0 / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new NaturalEarthProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;
        double phi2 = phi * phi;
        double phi4 = phi2 * phi2;
        double phi6 = phi4 * phi2;
        double phi8 = phi4 * phi4;
        double phi10 = phi8 * phi2;
        double phi12 = phi10 * phi2;

        double xScale = 0.8707 - (0.131979 * phi2) - (0.013791 * phi4) + (0.003971 * phi10) - (0.001529 * phi12);
        double yScale = 1.007226 + (0.015085 * phi2) - (0.044475 * phi6) + (0.028874 * phi8) - (0.005916 * phi10);

        lon = this.radius * lambda * xScale;
        lat = this.radius * phi * yScale;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double yy = y * this.inverseRadius;
        double phi = yy;

        for (int i = 0; i < Iterations; i++)
        {
            double phi2 = phi * phi;
            double phi4 = phi2 * phi2;
            double phi6 = phi4 * phi2;
            double phi8 = phi4 * phi4;
            double phi10 = phi8 * phi2;

            double fy = (phi * (1.007226 + (0.015085 * phi2) - (0.044475 * phi6) + (0.028874 * phi8) - (0.005916 * phi10))) - yy;
            double fpy = 1.007226 + (3d * 0.015085 * phi2) - (7d * 0.044475 * phi6) + (9d * 0.028874 * phi8) - (11d * 0.005916 * phi10);

            double delta = fy / fpy;
            phi -= delta;
            if (Math.Abs(delta) < 1e-12)
            {
                break;
            }
        }

        double phi2Final = phi * phi;
        double phi4Final = phi2Final * phi2Final;
        double phi10Final = phi4Final * phi4Final * phi2Final;
        double phi12Final = phi10Final * phi2Final;
        double xScaleFinal = 0.8707 - (0.131979 * phi2Final) - (0.013791 * phi4Final) + (0.003971 * phi10Final) - (0.001529 * phi12Final);

        x = Adjust_lon(this.centralMeridian + ((x * this.inverseRadius) / xScaleFinal));
        y = phi;
    }
}
