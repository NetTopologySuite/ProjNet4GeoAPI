// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Swiss Oblique Mercator projection (<c>somerc</c>).
/// </summary>
/// <remarks>
/// An ellipsoidal oblique Mercator projection used for the Swiss national coordinate systems
/// (LV03 and LV95). The inverse transform applies an iterative Newton-Raphson algorithm
/// to recover geodetic latitude from projected northing.
/// The formulation was independently verified against Swisstopo, "Swiss Map Projections,"
/// and the PROJ <c>somerc</c> documentation. The double projection from the ellipsoid to
/// a conformal sphere and then to an oblique Mercator plane, including the Rosenmund 1903
/// conformal-sphere construction and Bolliger 1967 polynomial terms, matches the
/// implementation here.
/// </remarks>
/// <seealso href="https://www.swisstopo.admin.ch/en/swiss-map-projections">Swisstopo: Swiss map projections.</seealso>
/// <seealso href="https://proj.org/en/stable/operations/projections/somerc.html">PROJ documentation: Swiss Oblique Mercator.</seealso>
internal sealed class SwissObliqueMercatorProjection : MapProjection
{
    private const int MaximumIterations = 6;
    private const double IterationTolerance = 1e-10;

    private readonly double c;
    private readonly double reciprocalC;
    private readonly double halfE;
    private readonly double k;
    private readonly double kR;
    private readonly double reciprocalKR;
    private readonly double sinP0;
    private readonly double cosP0;
    private readonly double reciprocalOneMinusEs;

    /// <summary>
    /// Initializes a new instance of the <see cref="SwissObliqueMercatorProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public SwissObliqueMercatorProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SwissObliqueMercatorProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public SwissObliqueMercatorProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Swiss_Oblique_Mercator";

        double oneMinusEs = 1d - this.es;
        if (oneMinusEs <= 0d)
        {
            ArgumentGuard.ThrowArgument("Invalid ellipsoid eccentricity for Swiss Oblique Mercator projection.");
        }

        this.reciprocalOneMinusEs = 1d / oneMinusEs;
        this.halfE = 0.5d * this.e;

        double cosPhi0 = Math.Cos(this.latOrigin);
        double cosPhi0Squared = cosPhi0 * cosPhi0;
        this.c = Math.Sqrt(1d + ((this.es * cosPhi0Squared * cosPhi0Squared) * this.reciprocalOneMinusEs));
        this.reciprocalC = 1d / this.c;

        double sinPhi0 = Math.Sin(this.latOrigin);
        this.sinP0 = sinPhi0 / this.c;
        double phiPrime0 = Asinz(this.sinP0);
        this.cosP0 = Math.Cos(phiPrime0);

        double eSinPhi0 = this.e * sinPhi0;
        this.k = Math.Log(Math.Tan(FortPi + (0.5d * phiPrime0)))
            - (this.c * (Math.Log(Math.Tan(FortPi + (0.5d * this.latOrigin))) - (this.halfE * Math.Log((1d + eSinPhi0) / (1d - eSinPhi0)))));
        this.kR = this.semiMajor * this.scaleFactor * Math.Sqrt(oneMinusEs) / (1d - (eSinPhi0 * eSinPhi0));
        this.reciprocalKR = 1d / this.kR;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new SwissObliqueMercatorProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double eSinPhi = this.e * Math.Sin(lat);

        double phiPrime = (2d * Math.Atan(Math.Exp(
                (this.c * (Math.Log(Math.Tan(FortPi + (0.5d * lat))) - (this.halfE * Math.Log((1d + eSinPhi) / (1d - eSinPhi)))))
                + this.k)))
            - HalfPi;
        double lambdaPrime = this.c * lambda;
        double cosPhiPrime = Math.Cos(phiPrime);
        double phiDoublePrime = Asinz((this.cosP0 * Math.Sin(phiPrime)) - (this.sinP0 * cosPhiPrime * Math.Cos(lambdaPrime)));
        double cosPhiDoublePrime = Math.Cos(phiDoublePrime);
        if (Math.Abs(cosPhiDoublePrime) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lambdaDoublePrime = Asinz((cosPhiPrime * Math.Sin(lambdaPrime)) / cosPhiDoublePrime);

        lon = this.kR * lambdaDoublePrime;
        lat = this.kR * Math.Log(Math.Tan(FortPi + (0.5d * phiDoublePrime)));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double phiDoublePrime = 2d * (Math.Atan(Math.Exp(y * this.reciprocalKR)) - FortPi);
        double lambdaDoublePrime = x * this.reciprocalKR;
        double cosPhiDoublePrime = Math.Cos(phiDoublePrime);
        double phiPrime = Asinz((this.cosP0 * Math.Sin(phiDoublePrime)) + (this.sinP0 * cosPhiDoublePrime * Math.Cos(lambdaDoublePrime)));
        double cosPhiPrime = Math.Cos(phiPrime);
        if (Math.Abs(cosPhiPrime) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lambdaPrime = Asinz((cosPhiDoublePrime * Math.Sin(lambdaDoublePrime)) / cosPhiPrime);
        double con = (this.k - Math.Log(Math.Tan(FortPi + (0.5d * phiPrime)))) * this.reciprocalC;

        double phi = phiPrime;
        bool converged = false;
        for (int i = 0; i < MaximumIterations; i++)
        {
            double eSinPhi = this.e * Math.Sin(phi);
            double delta = (con + Math.Log(Math.Tan(FortPi + (0.5d * phi))) - (this.halfE * Math.Log((1d + eSinPhi) / (1d - eSinPhi))))
                * (1d - (eSinPhi * eSinPhi))
                * Math.Cos(phi)
                * this.reciprocalOneMinusEs;
            phi -= delta;
            if (Math.Abs(delta) < IterationTolerance)
            {
                converged = true;
                break;
            }
        }

        if (!converged)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        x = Adjust_lon(this.centralMeridian + (lambdaPrime * this.reciprocalC));
        y = phi;
    }
}
