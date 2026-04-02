// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Equal Earth projection (<c>eqearth</c>).
/// </summary>
/// <remarks>
/// Equal Earth is an equal-area pseudocylindrical projection with curved parallels and
/// a polynomial forward formula. The inverse is solved iteratively.
/// The coefficient set and authalic-latitude relation were independently verified against
/// Savric, Patterson, and Jenny (2018), including <c>A1</c> through <c>A4</c> and
/// <c>sin(theta) = (sqrt(3) / 2) * sin(phi)</c>.
/// </remarks>
internal class EqualEarthProjection : MapProjection
{
    private const double A1 = 1.340264;
    private const double A2 = -0.081106;
    private const double A3 = 0.000893;
    private const double A4 = 0.003796;
    private const int Iterations = 12;
    private const double MaxY = 1.3173627591574d;

    private static readonly double M = Math.Sqrt(3.0) * 0.5;

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly bool isEllipsoidal;
    private readonly double oneEs;
    private readonly double qp;
    private readonly double[] apa;

    /// <summary>
    /// Initializes a new instance of the <see cref="EqualEarthProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public EqualEarthProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EqualEarthProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public EqualEarthProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Equal_Earth";
        this.isEllipsoidal = this.es > 0d;
        if (this.isEllipsoidal)
        {
            this.oneEs = 1d - this.es;
            this.qp = Qsfn(1d, this.e, this.oneEs);
            this.apa = Authset(this.es);
        }
        else
        {
            this.oneEs = 0d;
            this.qp = 0d;
            this.apa = [];
        }

        double authalicScale = this.isEllipsoidal ? Math.Sqrt(0.5d * this.qp) : 1d;
        this.radius = this.semiMajor * this.scaleFactor * authalicScale;
        this.inverseRadius = 1.0 / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new EqualEarthProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double sinPhi = Math.Sin(lat);
        if (this.isEllipsoidal)
        {
            double q = Qsfn(sinPhi, this.e, this.oneEs);
            sinPhi = ProjectionConstants.Clamp(q / this.qp, -1d, 1d);
        }

        double theta = Math.Asin(ProjectionConstants.Clamp(M * sinPhi, -1d, 1d));

        double theta2 = theta * theta;
        double theta6 = theta2 * theta2 * theta2;
        double denominator = A1 + (3d * A2 * theta2) + (theta6 * ((7d * A3) + (9d * A4 * theta2)));

        lon = this.radius * lambda * Math.Cos(theta) / (M * denominator);
        lat = this.radius * theta * (A1 + (A2 * theta2) + (theta6 * (A3 + (A4 * theta2))));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double theta = y * this.inverseRadius;
        if (theta > MaxY)
        {
            theta = MaxY;
        }
        else if (theta < -MaxY)
        {
            theta = -MaxY;
        }

        bool converged = false;
        for (int i = 0; i < Iterations; i++)
        {
            double theta2 = theta * theta;
            double theta6 = theta2 * theta2 * theta2;
            double value = (theta * (A1 + (A2 * theta2) + (theta6 * (A3 + (A4 * theta2))))) - (y * this.inverseRadius);
            double derivative = A1 + (3d * A2 * theta2) + (theta6 * ((7d * A3) + (9d * A4 * theta2)));
            double delta = value / derivative;
            theta -= delta;
            if (Math.Abs(delta) < 1e-12)
            {
                converged = true;
                break;
            }
        }

        if (!converged)
        {
            ArgumentGuard.ThrowArgumentOutOfRange(nameof(y), "Equal Earth inverse did not converge.");
        }

        double theta2Final = theta * theta;
        double theta6Final = theta2Final * theta2Final * theta2Final;
        double denominatorFinal = A1 + (3d * A2 * theta2Final) + (theta6Final * ((7d * A3) + (9d * A4 * theta2Final)));
        double cosTheta = Math.Cos(theta);

        if (Math.Abs(cosTheta) <= Eps10)
        {
            x = this.centralMeridian;
        }
        else
        {
            x = Adjust_lon(this.centralMeridian + ((x * this.inverseRadius) * M * denominatorFinal / cosTheta));
        }

        double beta = Math.Asin(ProjectionConstants.Clamp(Math.Sin(theta) / M, -1d, 1d));
        y = this.isEllipsoidal ? Authlat(beta, this.apa) : beta;
    }
}
