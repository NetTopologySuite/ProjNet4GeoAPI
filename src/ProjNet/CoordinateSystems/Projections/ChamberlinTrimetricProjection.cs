// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Chamberlin Trimetric projection (<c>chamb</c>).
/// </summary>
/// <remarks>
/// Inverse projection is not supported in this implementation.
/// <para>The forward construction was independently verified against Chamberlin's trimetric
/// method. The implementation matches the three-control-point setup, the law-of-cosines
/// angle recovery, and the mean-point blending used to place interior points.</para>
/// </remarks>
internal sealed class ChamberlinTrimetricProjection : MapProjection
{
    private const double Third = 0.333333333333333333d;
    private const double Tolerance = 1e-9d;

    private readonly ControlPoint[] control = [new(), new(), new()];
    private readonly Point meanPoint = new();
    private readonly double beta1;
    private readonly double beta2;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChamberlinTrimetricProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public ChamberlinTrimetricProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChamberlinTrimetricProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public ChamberlinTrimetricProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Chamberlin_Trimetric";

        for (int i = 0; i < 3; i++)
        {
            int index = i + 1;
            double phi = DegreesToRadians(this.Parameters.GetOptionalParameterValue($"lat_{index}", 0d, $"latitude_{index}"));
            double lambda = DegreesToRadians(this.Parameters.GetOptionalParameterValue($"lon_{index}", 0d, $"longitude_{index}"));
            lambda = Adjust_lon(lambda - this.centralMeridian);

            this.control[i].Phi = phi;
            this.control[i].Lambda = lambda;
            this.control[i].CosPhi = Math.Cos(phi);
            this.control[i].SinPhi = Math.Sin(phi);
        }

        for (int i = 0; i < 3; i++)
        {
            int j = i == 2 ? 0 : i + 1;
            this.control[i].Arc = Vect(
                this.control[j].Phi - this.control[i].Phi,
                this.control[i].CosPhi,
                this.control[i].SinPhi,
                this.control[j].CosPhi,
                this.control[j].SinPhi,
                this.control[j].Lambda - this.control[i].Lambda);

            if (Math.Abs(this.control[i].Arc.R) <= Tolerance)
            {
                ArgumentGuard.ThrowArgument("Invalid value for control points: they should be distinct.");
            }
        }

        double beta0 = LawOfCosines(this.control[0].Arc.R, this.control[2].Arc.R, this.control[1].Arc.R);
        this.beta1 = LawOfCosines(this.control[0].Arc.R, this.control[1].Arc.R, this.control[2].Arc.R);
        this.beta2 = PI - beta0;

        this.control[0].Projected.Y = this.control[2].Arc.R * Math.Sin(beta0);
        this.control[1].Projected.Y = this.control[0].Projected.Y;
        this.meanPoint.Y = this.control[0].Projected.Y + this.control[0].Projected.Y;
        this.control[2].Projected.Y = 0d;

        this.control[1].Projected.X = 0.5d * this.control[0].Arc.R;
        this.control[0].Projected.X = -this.control[1].Projected.X;
        this.control[2].Projected.X = this.control[0].Projected.X + (this.control[2].Arc.R * Math.Cos(beta0));
        this.meanPoint.X = this.control[2].Projected.X;
    }

    /// <inheritdoc />
    protected override bool HasInverseSupport => false;

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return this.GetOrCreateInverse(() => new ChamberlinTrimetricProjection(this.Parameters.ToProjectionParameter(), this));
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double sinPhi = Math.Sin(lat);
        double cosPhi = Math.Cos(lat);
        var v = new Arc[3];
        int hitControl = -1;

        for (int i = 0; i < 3; i++)
        {
            v[i] = Vect(
                lat - this.control[i].Phi,
                this.control[i].CosPhi,
                this.control[i].SinPhi,
                cosPhi,
                sinPhi,
                lambda - this.control[i].Lambda);

            if (Math.Abs(v[i].R) <= Tolerance)
            {
                hitControl = i;
                break;
            }

            v[i] = new Arc(v[i].R, Adjust_lon(v[i].Az - this.control[i].Arc.Az));
        }

        double x = hitControl >= 0 ? this.control[hitControl].Projected.X : this.meanPoint.X;
        double y = hitControl >= 0 ? this.control[hitControl].Projected.Y : this.meanPoint.Y;
        if (hitControl < 0)
        {
            for (int i = 0; i < 3; i++)
            {
                int j = i == 2 ? 0 : i + 1;
                double a = LawOfCosines(this.control[i].Arc.R, v[i].R, v[j].R);
                if (v[i].Az < 0d)
                {
                    a = -a;
                }

                if (i == 0)
                {
                    x += v[i].R * Math.Cos(a);
                    y -= v[i].R * Math.Sin(a);
                }
                else if (i == 1)
                {
                    a = this.beta1 - a;
                    x -= v[i].R * Math.Cos(a);
                    y -= v[i].R * Math.Sin(a);
                }
                else
                {
                    a = this.beta2 - a;
                    x += v[i].R * Math.Cos(a);
                    y += v[i].R * Math.Sin(a);
                }
            }

            x *= Third;
            y *= Third;
        }

        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Chamberlin Trimetric does not support inverse projection in this wave.");
    }

    private static Arc Vect(double dphi, double c1, double s1, double c2, double s2, double dlam)
    {
        double cdl = Math.Cos(dlam);
        double r;
        if (Math.Abs(dphi) > 1d || Math.Abs(dlam) > 1d)
        {
            r = Math.Acos(ProjectionConstants.ClampToUnit((s1 * s2) + (c1 * c2 * cdl)));
        }
        else
        {
            double dp = Math.Sin(0.5d * dphi);
            double dl = Math.Sin(0.5d * dlam);
            r = 2d * Asinz(Math.Sqrt((dp * dp) + (c1 * c2 * dl * dl)));
        }

        if (Math.Abs(r) <= Tolerance)
        {
            return new Arc(0d, 0d);
        }

        double az = Math.Atan2(c2 * Math.Sin(dlam), (c1 * s2) - (s1 * c2 * cdl));
        return new Arc(r, az);
    }

    private static double LawOfCosines(double b, double c, double a)
    {
        double value = 0.5d * ((b * b) + (c * c) - (a * a)) / (b * c);
        return Math.Acos(ProjectionConstants.ClampToUnit(value));
    }

    private readonly struct Arc(double r, double az)
    {
        public double R { get; } = r;

        public double Az { get; } = az;
    }

    private sealed class ControlPoint
    {
        public double Phi { get; set; }

        public double Lambda { get; set; }

        public double CosPhi { get; set; }

        public double SinPhi { get; set; }

        public Arc Arc { get; set; } = new(0d, 0d);

        public Point Projected { get; } = new();
    }

    private sealed class Point
    {
        public double X { get; set; }

        public double Y { get; set; }
    }
}
