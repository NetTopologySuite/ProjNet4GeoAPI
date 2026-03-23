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
/// Implements the van der Grinten I projection (<c>vandg</c>).
/// </summary>
[Serializable]
internal class VanDerGrintenProjection : MapProjection
{
    private const double Tolerance = 1e-10;
    private const double Third = 1d / 3d;
    private const double TwoTwentySevenths = 2d / 27d;
    private const double FourPiOverThree = 4.18879020478639098458d;
    private const double PiSquared = PI * PI;
    private const double TwoPiSquared = 2d * PiSquared;
    private const double HalfPiSquared = 0.5d * PiSquared;
    private const double InverseDomainEpsilon = 1e-16;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="VanDerGrintenProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public VanDerGrintenProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VanDerGrintenProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public VanDerGrintenProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "VanDerGrinten";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new VanDerGrintenProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double p2 = Math.Abs(lat / HalfPi);
        if ((p2 - Tolerance) > 1d)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        if (p2 > 1d)
        {
            p2 = 1d;
        }

        double x;
        double y;

        if (Math.Abs(lat) <= Tolerance)
        {
            x = lambda;
            y = 0d;
        }
        else if (Math.Abs(lambda) <= Tolerance || Math.Abs(p2 - 1d) < Tolerance)
        {
            x = 0d;
            y = PI * Math.Tan(0.5d * Math.Asin(p2));
            if (lat < 0d)
            {
                y = -y;
            }
        }
        else
        {
            double al = 0.5d * Math.Abs((PI / lambda) - (lambda / PI));
            double al2 = al * al;
            double g = Math.Sqrt(1d - (p2 * p2));
            g /= p2 + g - 1d;
            double g2 = g * g;
            double p = g * ((2d / p2) - 1d);
            double pSquared = p * p;

            double diff = g - pSquared;
            double sum = pSquared + al2;
            double radicand = (al2 * diff * diff) - (sum * (g2 - pSquared));
            if (radicand < -Tolerance)
            {
                throw new ArgumentException("Input data outside projection domain.");
            }

            if (radicand < 0d)
            {
                radicand = 0d;
            }

            x = PI * Math.Abs((al * diff) + Math.Sqrt(radicand)) / sum;
            if (lambda < 0d)
            {
                x = -x;
            }

            y = Math.Abs(x / PI);
            y = 1d - (y * (y + (2d * al)));
            if (y < -Tolerance)
            {
                throw new ArgumentException("Input data outside projection domain.");
            }

            if (y < 0d)
            {
                y = 0d;
            }
            else
            {
                y = Math.Sqrt(y) * (lat < 0d ? -PI : PI);
            }
        }

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;
        double x2 = xx * xx;

        if (Math.Abs(yy) < Tolerance)
        {
            y = 0d;
            double t = (x2 * x2) + (TwoPiSquared * (x2 + HalfPiSquared));
            double lambdaEquator = Math.Abs(xx) <= Tolerance ? 0d : (0.5d * ((x2 - PiSquared) + Math.Sqrt(t)) / xx);
            x = Adjust_lon(this.centralMeridian + lambdaEquator);
            return;
        }

        double ay = Math.Abs(yy);
        double y2 = yy * yy;
        double r = x2 + y2;
        double r2 = r * r;
        double c1 = -PI * ay * (r + PiSquared);
        double ayr = ay * r;
        double piTerm = PI * (y2 + (PI * (ay + HalfPi)));
        double c3 = r2 + (TwoPi * (ayr + piTerm));
        double c2 = c1 + (PiSquared * (r - (3d * y2)));
        double c0 = PI * ay;

        c2 /= c3;
        double al = (c1 / c3) - (Third * c2 * c2);
        double m = 2d * Math.Sqrt(-Third * al);
        double c2Cubed = c2 * c2 * c2;
        double d = (TwoTwentySevenths * c2Cubed) + (((c0 * c0) - (Third * c2 * c1)) / c3);
        double alMulM = al * m;
        if (Math.Abs(alMulM) < InverseDomainEpsilon)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        d = 3d * d / alMulM;
        double ad = Math.Abs(d);
        if ((ad - Tolerance) > 1d)
        {
            throw new ArgumentException("Input data outside projection domain.");
        }

        d = ad > 1d ? (d > 0d ? 0d : PI) : Math.Acos(d);
        if (r > PiSquared)
        {
            d = TwoPi - d;
        }

        double phi = PI * ((m * Math.Cos((d * Third) + FourPiOverThree)) - (Third * c2));
        if (yy < 0d)
        {
            phi = -phi;
        }

        double t2 = r2 + (TwoPiSquared * (x2 - y2 + HalfPiSquared));
        double lambdaDenominator = Math.Abs(xx) <= Tolerance ? 0d : xx;
        double lambda = Math.Abs(lambdaDenominator) <= Tolerance
            ? 0d
            : (0.5d * (r - PiSquared + (t2 <= 0d ? 0d : Math.Sqrt(t2))) / lambdaDenominator);

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
