// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Lambert Conformal Conic Alternative projection (<c>lcca</c>).
/// </summary>
[Serializable]
internal sealed class LambertConformalConicAlternativeProjection : MapProjection
{
    private const int MaximumIterations = 10;
    private const double DeltaTolerance = 1e-12;

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double l;
    private readonly double m0;
    private readonly double r0;
    private readonly double c;

    /// <summary>
    /// Initializes a new instance of the <see cref="LambertConformalConicAlternativeProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public LambertConformalConicAlternativeProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LambertConformalConicAlternativeProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public LambertConformalConicAlternativeProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Lambert_Conformal_Conic_Alternative";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        if (Math.Abs(this.latOrigin) < Eps10)
        {
            throw new ArgumentException("Invalid value for lat_0: it should be different from 0.");
        }

        this.l = Math.Sin(this.latOrigin);
        this.m0 = this.Mlfn(this.latOrigin, Math.Sin(this.latOrigin), Math.Cos(this.latOrigin));

        double s2p0 = this.l * this.l;
        double r = 1d / (1d - (this.es * s2p0));
        double n0 = Math.Sqrt(r);
        r *= (1d - this.es) * n0;
        double tan0 = Math.Tan(this.latOrigin);
        this.r0 = n0 / tan0;
        this.c = 1d / (6d * r * n0);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new LambertConformalConicAlternativeProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double s = this.Mlfn(lat, Math.Sin(lat), Math.Cos(lat)) - this.m0;
        double dr = Fs(s, this.c);
        double r = this.r0 - dr;
        double theta = lambda * this.l;

        lon = this.radius * (r * Math.Sin(theta));
        lat = this.radius * (this.r0 - (r * Math.Cos(theta)));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = y * this.inverseRadius;
        double theta = Math.Atan2(xUnit, this.r0 - yUnit);
        double dr = yUnit - (xUnit * Math.Tan(0.5d * theta));
        double lambda = theta / this.l;

        double s = dr;
        int i;
        for (i = 0; i < MaximumIterations; i++)
        {
            double diff = (Fs(s, this.c) - dr) / Fsp(s, this.c);
            s -= diff;
            if (Math.Abs(diff) < DeltaTolerance)
            {
                break;
            }
        }

        if (i == MaximumIterations)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        double phi = this.Inv_mlfn(s + this.m0);

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }

    private static double Fs(double s, double c)
    {
        return s * (1d + ((s * s) * c));
    }

    private static double Fsp(double s, double c)
    {
        return 1d + (3d * s * s * c);
    }

}

