// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2002 Urban Science Applications, Inc.
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from GeoTools.NET.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the American Polyconic map projection.
/// </summary>
[Serializable]
internal class PolyconicProjection : MapProjection
{
    /// <summary>
    /// Maximum difference allowed when comparing real numbers.
    /// </summary>
    private const double Epsilon = 1E-10;

    /// <summary>
    /// Maximum number of iterations for iterative computations.
    /// </summary>
    private const int MaximumIterations = 20;

    /// <summary>
    /// Difference allowed in iterative computations.
    /// </summary>
    private const double IterationTolerance = ProjectionConstants.Tolerance1E12;

    /// <summary>
    /// Meridian distance at the latitude of origin.
    /// Used for calculations for the ellipsoid.
    /// </summary>
    private readonly double ml0;

    private readonly double reciprocSemiMajorTimesScaleFactor;

    /// <summary>
    /// Initializes a new instance of the <see cref="PolyconicProjection"/> class.
    /// </summary>
    /// <param name="parameters">The parameter values in standard units.</param>
    public PolyconicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PolyconicProjection"/> class.
    /// </summary>
    /// <param name="parameters">The parameter values in standard units.</param>
    /// <param name="inverse">The inverse projection instance, or <see langword="null"/> for a forward projection.</param>
    protected PolyconicProjection(IEnumerable<ProjectionParameter> parameters, PolyconicProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Polyconic";

        this.ml0 = this.Mlfn(this.latOrigin, Math.Sin(this.latOrigin), Math.Cos(this.latOrigin));
        this.reciprocSemiMajorTimesScaleFactor = 1 / (this.semiMajor * this.scaleFactor);
    }

    /// <inheritdoc/>
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lam = lon;
        double phi = lat;

        double delta_lam = Adjust_lon(lam - this.centralMeridian);

        double x, y;

        if (Math.Abs(phi) <= Epsilon)
        {
            x = delta_lam; // lam;
            y = -this.ml0;
        }
        else
        {
            double sp = Math.Sin(phi);
            double cp;
            double ms = Math.Abs(cp = Math.Cos(phi)) > Epsilon ? this.Msfn(sp, cp) / sp : 0.0;
            // lam =
            delta_lam *= sp;
            x = ms * Math.Sin(delta_lam);
            y = (this.Mlfn(phi, sp, cp) - this.ml0) + (ms * (1.0 - Math.Cos(delta_lam)));
        }

        lon = this.scaleFactor * this.semiMajor * x;
        lat = this.scaleFactor * this.semiMajor * y;
    }

    /// <inheritdoc/>
    protected override void MetersToRadians(ref double x, ref double y)
    {
        x *= this.reciprocSemiMajorTimesScaleFactor;
        y *= this.reciprocSemiMajorTimesScaleFactor;

        double lam, phi;

        y += this.ml0;
        if (Math.Abs(y) <= Epsilon)
        {
            lam = x;
            phi = 0.0;
        }
        else
        {
            double r = (y * y) + (x * x);
            phi = y;
            int iter = 0;
            for (; iter <= MaximumIterations; iter++)
            {
                double sp = Math.Sin(phi);
                double cp = Math.Cos(phi);
                if (Math.Abs(cp) < IterationTolerance)
                {
                    throw new InvalidOperationException("No Convergence");
                }

                double s2ph = sp * cp;
                double mlp = Math.Sqrt(1.0 - (this.es * sp * sp));
                double c = sp * mlp / cp;
                double ml = this.Mlfn(phi, sp, cp);
                double mlb = (ml * ml) + r;
                mlp = (1.0 - this.es) / (mlp * mlp * mlp);
                double dPhi = (ml + ml + (c * mlb) - (2.0 * y * ((c * ml) + 1.0))) / (
                               (this.es * s2ph * (mlb - (2.0 * y * ml)) / c) +
                               (2.0 * (y - ml) * ((c * mlp) - (1.0 / s2ph))) - mlp - mlp);
                if (Math.Abs(dPhi) <= IterationTolerance)
                {
                    break;
                }

                phi += dPhi;
            }

            if (iter > MaximumIterations)
            {
                throw new InvalidOperationException("No Convergence");
            }

            double c2 = Math.Sin(phi);
            lam = Math.Asin(x * Math.Tan(phi) * Math.Sqrt(1.0 - (this.es * c2 * c2))) / Math.Sin(phi);
        }

        x = Adjust_lon(lam + this.centralMeridian);
        y = phi;
    }

    /// <summary>
    /// Returns the inverse of this projection.
    /// </summary>
    /// <returns>IMathTransform that is the reverse of the current projection.</returns>
    public override MathTransform Inverse()
    {
        if (this.inverse == null)
        {
            this.inverse = new PolyconicProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <summary>
    /// Computes function. <code>f(s,c,e²) = c/sqrt(1 - s²*e²)</code> needed for the true scale
    /// latitude (Snyder 14-15), where <var>s</var> and <var>c</var> are the sine and cosine of
    /// the true scale latitude, and <var>e²</var> is the eccentricity squared.
    /// </summary>
    private double Msfn(double s, double c)
    {
        return c / Math.Sqrt(1.0 - ((s * s) * this.es));
    }
}
