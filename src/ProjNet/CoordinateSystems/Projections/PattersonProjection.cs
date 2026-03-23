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
/// Represents the documented type.
/// </summary>
[Serializable]
internal class PattersonProjection : MapProjection
{
    private const double K1 = 1.0148d;
    private const double K2 = 0.23185d;
    private const double K3 = -0.14499d;
    private const double K4 = 0.02406d;
    private const int Iterations = 12;

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double maxY;

    /// <summary>
    /// Initializes a new instance of the <see cref="PattersonProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public PattersonProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PattersonProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public PattersonProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Patterson";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
        this.maxY = ForwardPolynomial(HalfPi);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new PattersonProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        lon = this.radius * lambda;
        lat = this.radius * ForwardPolynomial(lat);
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        x = Adjust_lon(this.centralMeridian + (x * this.inverseRadius));

        double targetY = Clamp(y * this.inverseRadius, -this.maxY, this.maxY);
        double phi = targetY / K1;

        for (int i = 0; i < Iterations; i++)
        {
            double f = ForwardPolynomial(phi) - targetY;
            double df = ForwardPolynomialDerivative(phi);
            double delta = f / df;
            phi -= delta;
            if (Math.Abs(delta) <= 1e-12d)
            {
                break;
            }
        }

        y = Clamp(phi, -HalfPi, HalfPi);
    }

    private static double ForwardPolynomial(double phi)
    {
        double phi2 = phi * phi;
        double phi4 = phi2 * phi2;
        double phi6 = phi4 * phi2;
        double phi8 = phi4 * phi4;

        return (K1 * phi) + (K2 * phi * phi4) + (K3 * phi * phi6) + (K4 * phi * phi8);
    }

    private static double ForwardPolynomialDerivative(double phi)
    {
        double phi2 = phi * phi;
        double phi4 = phi2 * phi2;
        double phi6 = phi4 * phi2;
        double phi8 = phi4 * phi4;

        return K1 + (5d * K2 * phi4) + (7d * K3 * phi6) + (9d * K4 * phi8);
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }
}
