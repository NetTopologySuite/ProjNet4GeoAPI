// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Nicolosi Globular projection (<c>nicol</c>).
/// </summary>
[Serializable]
internal class NicolosiProjection : MapProjection
{
    private const double Epsilon = 1e-10d;

    private readonly double radius;

    /// <summary>
    /// Initializes a new instance of the <see cref="NicolosiProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public NicolosiProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NicolosiProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public NicolosiProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Nicolosi";
        this.radius = this.semiMajor * this.scaleFactor;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new NicolosiProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double x;
        double y;

        if (Math.Abs(lambda) < Epsilon)
        {
            x = 0d;
            y = lat;
        }
        else if (Math.Abs(lat) < Epsilon)
        {
            x = lambda;
            y = 0d;
        }
        else if (Math.Abs(Math.Abs(lambda) - HalfPi) < Epsilon)
        {
            x = lambda * Math.Cos(lat);
            y = HalfPi * Math.Sin(lat);
        }
        else if (Math.Abs(Math.Abs(lat) - HalfPi) < Epsilon)
        {
            x = 0d;
            y = lat;
        }
        else
        {
            double tb = (HalfPi / lambda) - (lambda / HalfPi);
            double c = lat / HalfPi;
            double sp = Math.Sin(lat);
            double d = (1d - (c * c)) / (sp - c);
            double r2 = tb / d;
            r2 *= r2;
            double m = ((tb * sp / d) - (0.5d * tb)) / (1d + r2);
            double n = ((sp / r2) + (0.5d * d)) / (1d + (1d / r2));
            double cosLat = Math.Cos(lat);
            double xTerm = Math.Sqrt((m * m) + ((cosLat * cosLat) / (1d + r2)));
            x = HalfPi * (m + (lambda < 0d ? -xTerm : xTerm));
            double yTerm = Math.Sqrt((n * n) - (((sp * sp / r2) + (d * sp) - 1d) / (1d + (1d / r2))));
            y = HalfPi * (n + (lat < 0d ? yTerm : -yTerm));
        }

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Nicolosi does not support inverse projection in this wave.");
    }
}
