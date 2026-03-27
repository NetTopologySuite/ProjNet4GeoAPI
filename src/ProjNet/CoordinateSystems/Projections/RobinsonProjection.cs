// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Robinson projection (<c>robin</c>).
/// </summary>
/// <remarks>
/// The Robinson projection is a pseudocylindrical projection defined by a look-up table of
/// x- and y-scale coefficients at 5° latitude intervals, with linear interpolation between
/// tabulated values. The inverse reads back from the table using the same interpolation.
/// </remarks>
[Serializable]
internal class RobinsonProjection : MapProjection
{
    private const double XScale = 0.8487;
    private const double YScale = 1.3523;

    private static readonly double[] CoeffX =
    {
        1.0000, 0.9986, 0.9954, 0.9900, 0.9822, 0.9730, 0.9600, 0.9427, 0.9216,
        0.8962, 0.8679, 0.8350, 0.7986, 0.7597, 0.7186, 0.6732, 0.6213, 0.5722, 0.5322,
    };

    private static readonly double[] CoeffY =
    {
        0.0000, 0.0620, 0.1240, 0.1860, 0.2480, 0.3100, 0.3720, 0.4340, 0.4958,
        0.5571, 0.6176, 0.6769, 0.7346, 0.7903, 0.8435, 0.8936, 0.9394, 0.9761, 1.0000,
    };

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double fiveDegrees;

    /// <summary>
    /// Initializes a new instance of the <see cref="RobinsonProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public RobinsonProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RobinsonProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public RobinsonProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Robinson";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1.0 / this.radius;
        this.fiveDegrees = PI / 36d;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new RobinsonProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phiAbs = Math.Abs(lat);

        int index = GetLatitudeBand(phiAbs);
        double fraction = (phiAbs - (index * this.fiveDegrees)) / this.fiveDegrees;
        double xCoeff = Interpolate(CoeffX, index, fraction);
        double yCoeff = Interpolate(CoeffY, index, fraction);

        lon = this.radius * XScale * lambda * xCoeff;
        lat = this.radius * YScale * yCoeff * Sign(lat);
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double yy = Math.Abs(y) * this.inverseRadius / YScale;
        int index = FindLatitudeBand(yy);
        double y0 = CoeffY[index];
        double y1 = CoeffY[index + 1];
        double fraction = (yy - y0) / (y1 - y0);

        double phi = ((index + fraction) * this.fiveDegrees) * Sign(y);
        double xCoeff = Interpolate(CoeffX, index, fraction);

        x = Adjust_lon(this.centralMeridian + ((x * this.inverseRadius) / (XScale * xCoeff)));
        y = phi;
    }

    private static int GetLatitudeBand(double phiAbs)
    {
        if (phiAbs >= HalfPi)
        {
            return CoeffX.Length - 2;
        }

        int index = (int)Math.Floor(phiAbs * 36d / PI);
        if (index < 0)
        {
            return 0;
        }

        return index >= CoeffX.Length - 1 ? CoeffX.Length - 2 : index;
    }

    private static int FindLatitudeBand(double yNormalized)
    {
        if (yNormalized <= CoeffY[0])
        {
            return 0;
        }

        if (yNormalized >= CoeffY[CoeffY.Length - 1])
        {
            return CoeffY.Length - 2;
        }

        for (int i = 0; i < CoeffY.Length - 1; i++)
        {
            if (yNormalized <= CoeffY[i + 1])
            {
                return i;
            }
        }

        return CoeffY.Length - 2;
    }

    private static double Interpolate(double[] values, int index, double fraction)
    {
        return values[index] + ((values[index + 1] - values[index]) * fraction);
    }
}
