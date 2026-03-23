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
/// Implements the spherical Winkel II projection (<c>wink2</c>).
/// </summary>
[Serializable]
internal class WinkelIIProjection : MapProjection
{
    private const int MaximumIterations = 10;
    private const double LoopTolerance = 1e-7;

    private readonly double radius;
    private readonly double cosphi1;

    /// <summary>
    /// Initializes a new instance of the <see cref="WinkelIIProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public WinkelIIProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WinkelIIProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public WinkelIIProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Winkel_II";
        this.radius = this.semiMajor * this.scaleFactor;
        double lat1Degrees = this.Parameters.GetOptionalParameterValue("lat_1", RadiansToDegrees(this.latOrigin), "standard_parallel_1");
        this.cosphi1 = Math.Cos(DegreesToRadians(lat1Degrees));
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new WinkelIIProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double yPrime = lat * (2d / PI);
        double k = PI * Math.Sin(lat);
        double phi = 1.8d * lat;
        int i = MaximumIterations;

        for (; i > 0; i--)
        {
            double v = (phi + Math.Sin(phi) - k) / (1d + Math.Cos(phi));
            phi -= v;
            if (Math.Abs(v) < LoopTolerance)
            {
                break;
            }
        }

        phi = i == 0 ? (phi < 0d ? -HalfPi : HalfPi) : (0.5d * phi);
        double x = 0.5d * lambda * (Math.Cos(phi) + this.cosphi1);
        double y = FortPi * (Math.Sin(phi) + yPrime);

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Winkel II does not support inverse projection in this wave.");
    }

    private void ForwardNormalized(double lambda, double phi, out double x, out double y)
    {
        double yPrime = phi * (2d / PI);
        double k = PI * Math.Sin(phi);
        double phiWorking = 1.8d * phi;
        int i = MaximumIterations;

        for (; i > 0; i--)
        {
            double denominator = 1d + Math.Cos(phiWorking);
            if (Math.Abs(denominator) <= Eps10)
            {
                break;
            }

            double v = (phiWorking + Math.Sin(phiWorking) - k) / denominator;
            phiWorking -= v;
            if (Math.Abs(v) < LoopTolerance)
            {
                break;
            }
        }

        phiWorking = i == 0 ? (phiWorking < 0d ? -HalfPi : HalfPi) : (0.5d * phiWorking);
        x = 0.5d * lambda * (Math.Cos(phiWorking) + this.cosphi1);
        y = FortPi * (Math.Sin(phiWorking) + yPrime);
    }
}
