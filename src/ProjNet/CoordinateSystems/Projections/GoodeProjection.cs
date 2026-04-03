// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Goode Homolosine projection (<c>goode</c>).
/// </summary>
/// <remarks>
/// <para>The Goode Homolosine projection combines the sinusoidal projection for
/// latitudes within approximately ±40.7 degrees and the Mollweide projection for
/// higher latitudes, providing an equal-area representation with interrupted
/// distortion at the seam. Both spherical and ellipsoidal modes are supported.</para>
/// <para>The composite construction was independently verified against the published
/// Goode homolosine transition latitude of 40 degrees 44 minutes 11.8 seconds
/// (0.7109307819 rad) and the
/// PROJ <c>goode</c> documentation. The implementation switches at
/// <c>PhiLim = 0.71093078197902358062</c>, uses a sinusoidal branch below that
/// latitude, and applies the Mollweide branch with the standard <c>YCor = 0.05280</c>
/// seam correction above it, matching the established Goode formulation.</para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/projections/goode.html">PROJ documentation: Goode Homolosine.</seealso>
/// <seealso href="https://en.wikipedia.org/wiki/Goode_homolosine_projection">Wikipedia: Goode homolosine projection.</seealso>
internal class GoodeProjection : MapProjection
{
    private const int MollweideIterations = 12;
    private const double YCor = 0.05280;
    private const double PhiLim = 0.71093078197902358062;
    private static readonly double Sqrt2 = Math.Sqrt(2d);

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly bool isEllipsoidal;
    private readonly double oneEs;
    private readonly double qp;
    private readonly double[] apa;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoodeProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public GoodeProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoodeProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public GoodeProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Goode_Homolosine";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
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
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new GoodeProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = this.isEllipsoidal ? this.GeographicToAuthalic(lat) : lat;

        double xUnit;
        double yUnit;

        if (Math.Abs(phi) <= PhiLim)
        {
            xUnit = lambda * Math.Cos(phi);
            yUnit = phi;
        }
        else
        {
            MollweideForwardUnit(lambda, phi, out xUnit, out yUnit);
            yUnit -= phi >= 0d ? YCor : -YCor;
        }

        lon = this.radius * xUnit;
        lat = this.radius * yUnit;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = y * this.inverseRadius;

        double lambda;
        double phi;
        if (Math.Abs(yUnit) <= PhiLim)
        {
            phi = yUnit;
            double cosPhi = Math.Cos(phi);
            lambda = Math.Abs(cosPhi) <= Eps10 ? 0d : (xUnit / cosPhi);
        }
        else
        {
            double correctedY = yUnit + (yUnit >= 0d ? YCor : -YCor);
            MollweideInverseUnit(xUnit, correctedY, out lambda, out phi);
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = this.isEllipsoidal ? this.AuthalicToGeographic(phi) : phi;
    }

    private static void MollweideForwardUnit(double lambda, double phi, out double x, out double y)
    {
        double theta;
        if (Math.Abs(Math.Abs(phi) - HalfPi) < 1e-12)
        {
            theta = Sign(phi) * HalfPi;
        }
        else
        {
            theta = phi;
            double target = PI * Math.Sin(phi);
            for (int i = 0; i < MollweideIterations; i++)
            {
                double twoTheta = 2d * theta;
                double delta = ((twoTheta + Math.Sin(twoTheta)) - target) / (2d + (2d * Math.Cos(twoTheta)));
                theta -= delta;
                if (Math.Abs(delta) < 1e-12)
                {
                    break;
                }
            }
        }

        x = (2d * Sqrt2 / PI) * lambda * Math.Cos(theta);
        y = Sqrt2 * Math.Sin(theta);
    }

    private static void MollweideInverseUnit(double x, double y, out double lambda, out double phi)
    {
        double theta = Math.Asin(ProjectionConstants.Clamp(y / Sqrt2, -1d, 1d));
        double cosTheta = Math.Cos(theta);

        if (Math.Abs(cosTheta) <= Eps10)
        {
            lambda = 0d;
        }
        else
        {
            lambda = x * PI / (2d * Sqrt2 * cosTheta);
        }

        phi = Math.Asin(ProjectionConstants.Clamp(((2d * theta) + Math.Sin(2d * theta)) / PI, -1d, 1d));
    }

    private double GeographicToAuthalic(double phi)
    {
        double sinPhi = Math.Sin(phi);
        double q = Qsfn(sinPhi, this.e, this.oneEs);
        return Math.Asin(ProjectionConstants.Clamp(q / this.qp, -1d, 1d));
    }

    private double AuthalicToGeographic(double beta)
    {
        return Authlat(beta, this.apa);
    }
}
