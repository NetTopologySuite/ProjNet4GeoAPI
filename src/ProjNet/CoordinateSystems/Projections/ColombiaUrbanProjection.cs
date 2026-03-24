// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Colombia Urban projection (<c>col_urban</c>).
/// </summary>
[Serializable]
internal class ColombiaUrbanProjection : MapProjection
{
    private readonly double h0;
    private readonly double rho0;
    private readonly double a;
    private readonly double b;
    private readonly double c;
    private readonly double d;
    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="ColombiaUrbanProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public ColombiaUrbanProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ColombiaUrbanProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public ColombiaUrbanProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(MergeParameters(parameters), inverse)
    {
        this.Name = "Colombia_Urban";
        double unscaledH0 = this.Parameters.GetParameterValue("h_0");
        this.h0 = unscaledH0 / this.semiMajor;
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        double sinPhi0 = Math.Sin(this.latOrigin);
        double nu0 = 1d / Math.Sqrt(1d - (this.es * sinPhi0 * sinPhi0));
        this.a = 1d + (this.h0 / nu0);
        this.rho0 = (1d - this.es) / Math.Pow(1d - (this.es * sinPhi0 * sinPhi0), 1.5d);
        this.b = Math.Tan(this.latOrigin) / (2d * this.rho0 * nu0);
        this.c = 1d + this.h0;
        this.d = this.rho0 * (1d + (this.h0 / (1d - this.es)));
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new ColombiaUrbanProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double cosPhi = Math.Cos(lat);
        double sinPhi = Math.Sin(lat);
        double nu = 1d / Math.Sqrt(1d - (this.es * sinPhi * sinPhi));
        double lambdaNuCosPhi = lambda * nu * cosPhi;
        double x = this.a * lambdaNuCosPhi;
        double sinPhiM = Math.Sin(0.5d * (lat + this.latOrigin));
        double rhoM = (1d - this.es) / Math.Pow(1d - (this.es * sinPhiM * sinPhiM), 1.5d);
        double g = 1d + (this.h0 / rhoM);
        double y = g * this.rho0 * ((lat - this.latOrigin) + (this.b * lambdaNuCosPhi * lambdaNuCosPhi));
        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;

        double phi = this.latOrigin + (yy / this.d) - (this.b * (xx / this.c) * (xx / this.c));
        double sinPhi = Math.Sin(phi);
        double nu = 1d / Math.Sqrt(1d - (this.es * sinPhi * sinPhi));
        double lambda = xx / (this.c * nu * Math.Cos(phi));

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }

    private static List<ProjectionParameter> MergeParameters(IEnumerable<ProjectionParameter> parameters)
    {
        var merged = CloneParametersList(parameters);
        if (!HasParameter(merged, "h_0"))
        {
            throw new ArgumentException("Missing mandatory projection parameter 'h_0'.", nameof(parameters));
        }

        return merged;
    }

    private static bool HasParameter(List<ProjectionParameter> parameters, string name)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
