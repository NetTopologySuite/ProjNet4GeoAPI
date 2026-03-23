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
/// Implements the spherical Airy projection (<c>airy</c>).
/// </summary>
[Serializable]
internal class AiryProjection : MapProjection
{
    private const double Epsilon = 1e-10d;

    private readonly double radius;
    private readonly double cb;
    private readonly double sinPhi0;
    private readonly double cosPhi0;
    private readonly double pHalfPi;
    private readonly bool noCut;
    private readonly Mode mode;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiryProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public AiryProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AiryProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public AiryProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Airy";
        this.radius = this.semiMajor * this.scaleFactor;
        this.noCut = this.Parameters.ContainsKey("no_cut");

        double beta = 0.5d * (HalfPi - DegreesToRadians(this.Parameters.GetOptionalParameterValue("lat_b", 0d)));
        if (Math.Abs(beta) < Epsilon)
        {
            this.cb = -0.5d;
        }
        else
        {
            double cotBeta = 1d / Math.Tan(beta);
            this.cb = (cotBeta * cotBeta) * Math.Log(Math.Cos(beta));
        }

        if (Math.Abs(Math.Abs(this.latOrigin) - HalfPi) < Epsilon)
        {
            this.mode = this.latOrigin < 0d ? Mode.SouthPole : Mode.NorthPole;
            this.pHalfPi = this.latOrigin < 0d ? -HalfPi : HalfPi;
        }
        else if (Math.Abs(this.latOrigin) < Epsilon)
        {
            this.mode = Mode.Equatorial;
            this.pHalfPi = 0d;
        }
        else
        {
            this.mode = Mode.Oblique;
            this.sinPhi0 = Math.Sin(this.latOrigin);
            this.cosPhi0 = Math.Cos(this.latOrigin);
            this.pHalfPi = 0d;
        }
    }

    private enum Mode
    {
        NorthPole = 0,
        SouthPole = 1,
        Equatorial = 2,
        Oblique = 3,
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new AiryProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double x;
        double y;
        double sinLam = Math.Sin(lambda);
        double cosLam = Math.Cos(lambda);

        switch (this.mode)
        {
            case Mode.Equatorial:
            case Mode.Oblique:
            {
                double sinPhi = Math.Sin(lat);
                double cosPhi = Math.Cos(lat);
                double cosz = cosPhi * cosLam;
                if (this.mode == Mode.Oblique)
                {
                    cosz = (this.sinPhi0 * sinPhi) + (this.cosPhi0 * cosz);
                }

                if (!this.noCut && cosz < -Epsilon)
                {
                    throw new ArgumentException("Input data outside projection domain.");
                }

                double s = 1d - cosz;
                double kRho;
                if (Math.Abs(s) > Epsilon)
                {
                    double t = 0.5d * (1d + cosz);
                    if (Math.Abs(t) <= Eps10)
                    {
                        throw new ArgumentException("Input data outside projection domain.");
                    }

                    kRho = (-Math.Log(t) / s) - (this.cb / t);
                }
                else
                {
                    kRho = 0.5d - this.cb;
                }

                x = kRho * cosPhi * sinLam;
                y = this.mode == Mode.Oblique
                    ? kRho * ((this.cosPhi0 * sinPhi) - (this.sinPhi0 * cosPhi * cosLam))
                    : kRho * sinPhi;
                break;
            }

            case Mode.NorthPole:
            case Mode.SouthPole:
            default:
            {
                double phi = Math.Abs(this.pHalfPi - lat);
                if (!this.noCut && (phi - Epsilon) > HalfPi)
                {
                    throw new ArgumentException("Input data outside projection domain.");
                }

                phi *= 0.5d;
                if (phi > Epsilon)
                {
                    double t = Math.Tan(phi);
                    double kRho = -2d * ((Math.Log(Math.Cos(phi)) / t) + (t * this.cb));
                    x = kRho * sinLam;
                    y = kRho * cosLam;
                    if (this.mode == Mode.NorthPole)
                    {
                        y = -y;
                    }
                }
                else
                {
                    x = 0d;
                    y = 0d;
                }

                break;
            }
        }

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Airy does not support inverse projection in this wave.");
    }
}
