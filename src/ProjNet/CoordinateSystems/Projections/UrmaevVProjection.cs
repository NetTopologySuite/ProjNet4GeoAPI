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
/// Implements the spherical Urmaev V projection (<c>urm5</c>, no inverse).
/// </summary>
[Serializable]
internal class UrmaevVProjection : MapProjection
{
    private readonly double radius;
    private readonly double n;
    private readonly double m;
    private readonly double rmn;
    private readonly double q3;

    /// <summary>
    /// Initializes a new instance of the <see cref="UrmaevVProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public UrmaevVProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UrmaevVProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public UrmaevVProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Urmaev_V";
        this.radius = this.semiMajor * this.scaleFactor;
        this.n = this.Parameters.GetParameterValue("n");
        if (this.n <= 0d || this.n > 1d)
        {
            throw new ArgumentException("Invalid value for n: it should be in ]0,1] range.");
        }

        double q = this.Parameters.GetOptionalParameterValue("q", 0d);
        this.q3 = q / 3d;

        double alpha = DegreesToRadians(this.Parameters.GetOptionalParameterValue("alpha", 0d));
        double t = this.n * Math.Sin(alpha);
        double denom = Math.Sqrt(1d - (t * t));
        if (denom == 0d)
        {
            throw new ArgumentException("Invalid value for n / alpha: n * sin(|alpha|) should be < 1.");
        }

        this.m = Math.Cos(alpha) / denom;
        this.rmn = 1d / (this.m * this.n);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new UrmaevVProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = Asinz(this.n * Math.Sin(lat));
        double t = phi * phi;
        lon = this.radius * this.m * lambda * Math.Cos(phi);
        lat = this.radius * phi * (1d + (t * this.q3)) * this.rmn;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Urmaev V does not support inverse projection in this wave.");
    }
}
