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
/// Implements the spherical Urmaev Flat-Polar Sinusoidal projection (<c>urmfps</c>).
/// </summary>
[Serializable]
internal class UrmaevFlatPolarSinusoidalProjection : MapProjection
{
    private const double Cx = 0.8773826753d;
    private const double Cy = 1.139753528477d;

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double n;
    private readonly double cY;

    /// <summary>
    /// Initializes a new instance of the <see cref="UrmaevFlatPolarSinusoidalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public UrmaevFlatPolarSinusoidalProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UrmaevFlatPolarSinusoidalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public UrmaevFlatPolarSinusoidalProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Urmaev_Flat_Polar_Sinusoidal";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
        this.n = this.Parameters.GetParameterValue("n");
        if (this.n <= 0d || this.n > 1d)
        {
            throw new ArgumentException("Invalid value for n: it should be in ]0,1] range.");
        }

        this.cY = Cy / this.n;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new UrmaevFlatPolarSinusoidalProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = Asinz(this.n * Math.Sin(lat));
        lon = this.radius * Cx * lambda * Math.Cos(phi);
        lat = this.radius * this.cY * phi;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;
        double phiNormalized = yy / this.cY;
        double phi = Asinz(Math.Sin(phiNormalized) / this.n);
        double denominator = Cx * Math.Cos(phiNormalized);
        if (Math.Abs(denominator) <= Eps10)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        double lambda = xx / denominator;
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
