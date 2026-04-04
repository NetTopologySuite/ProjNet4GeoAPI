// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Airy projection (<c>airy</c>).
/// </summary>
/// <remarks>
/// <para>The Airy projection is a spherical azimuthal minimum-error construction for
/// the region bounded by an angular distance from the tangency point. This
/// implementation follows PROJ's <c>airy</c> formulation, including the
/// aspect-dependent forward equations, the optional <c>lat_b</c> minimum-error
/// radius parameter, and the <c>no_cut</c> option for hemisphere clipping.</para>
/// <para>George Biddell Airy introduced this minimum-error azimuthal projection in
/// 1861. Snyder's summary notes that the construction approaches azimuthal
/// equidistant behaviour for β values up to 90 degrees. Inverse projection is
/// not supported in this implementation.</para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/projections/airy.html">PROJ documentation: Airy.</seealso>
/// <seealso href="https://mathworld.wolfram.com/AiryProjection.html">MathWorld: Airy Projection.</seealso>
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
    public AiryProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
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
    protected override bool HasInverseSupport => false;

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return this.GetOrCreateInverse(() => new AiryProjection(this.Parameters.ToProjectionParameter(), this));
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
                    ArgumentGuard.ThrowArgument("Input data outside projection domain.");
                }

                double s = 1d - cosz;
                double kRho;
                if (Math.Abs(s) > Epsilon)
                {
                    double t = 0.5d * (1d + cosz);
                    if (Math.Abs(t) <= Eps10)
                    {
                        ArgumentGuard.ThrowArgument("Input data outside projection domain.");
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
                    ArgumentGuard.ThrowArgument("Input data outside projection domain.");
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
