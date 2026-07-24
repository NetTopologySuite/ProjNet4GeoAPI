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
/// x- and y-scale coefficients at 5° latitude intervals. Coefficients are evaluated using
/// cubic polynomials per interval, matching the PROJ reference implementation.
/// Arthur H. Robinson's 1974 paper, "A New Map Projection," <i>International Yearbook of
/// Cartography</i>, introduced the tabulated coefficients for the projection. The modern
/// cubic interpolation scheme used here was independently verified against current GIS
/// practice and secondary references that document the four-coefficient per-band formulation.
/// </remarks>
/// <seealso href="https://en.wikipedia.org/wiki/Robinson_projection">Wikipedia: Robinson projection.</seealso>
internal sealed class RobinsonProjection : MapProjection
{
    private const int Nodes = 18;
    private const int MaxIterations = 100;
    private const double InverseTolerance = 1e-10d;
    private const double OneEps = 1.000001;
    private const double LatitudeBandScale = 11.45915590261646417544;
    private const double FiveDegreesInRadians = 0.08726646259971647884;

    private const double XScale = 0.8487;
    private const double YScale = 1.3523;

    private static readonly Coeff[] CoeffX =
    [
        new(1.0f, 2.2199e-17f, -7.15515e-05f, 3.1103e-06f),
        new(0.9986f, -0.000482243f, -2.4897e-05f, -1.3309e-06f),
        new(0.9954f, -0.00083103f, -4.48605e-05f, -9.86701e-07f),
        new(0.99f, -0.00135364f, -5.9661e-05f, 3.6777e-06f),
        new(0.9822f, -0.00167442f, -4.49547e-06f, -5.72411e-06f),
        new(0.973f, -0.00214868f, -9.03571e-05f, 1.8736e-08f),
        new(0.96f, -0.00305085f, -9.00761e-05f, 1.64917e-06f),
        new(0.9427f, -0.00382792f, -6.53386e-05f, -2.6154e-06f),
        new(0.9216f, -0.00467746f, -0.00010457f, 4.81243e-06f),
        new(0.8962f, -0.00536223f, -3.23831e-05f, -5.43432e-06f),
        new(0.8679f, -0.00609363f, -0.000113898f, 3.32484e-06f),
        new(0.835f, -0.00698325f, -6.40253e-05f, 9.34959e-07f),
        new(0.7986f, -0.00755338f, -5.00009e-05f, 9.35324e-07f),
        new(0.7597f, -0.00798324f, -3.5971e-05f, -2.27626e-06f),
        new(0.7186f, -0.00851367f, -7.01149e-05f, -8.6303e-06f),
        new(0.6732f, -0.00986209f, -0.000199569f, 1.91974e-05f),
        new(0.6213f, -0.010418f, 8.83923e-05f, 6.24051e-06f),
        new(0.5722f, -0.00906601f, 0.000182f, 6.24051e-06f),
        new(0.5322f, -0.00677797f, 0.000275608f, 6.24051e-06f),
    ];

    private static readonly Coeff[] CoeffY =
    [
        new(-5.20417e-18f, 0.0124f, 1.21431e-18f, -8.45284e-11f),
        new(0.062f, 0.0124f, -1.26793e-09f, 4.22642e-10f),
        new(0.124f, 0.0124f, 5.07171e-09f, -1.60604e-09f),
        new(0.186f, 0.0123999f, -1.90189e-08f, 6.00152e-09f),
        new(0.248f, 0.0124002f, 7.10039e-08f, -2.24e-08f),
        new(0.31f, 0.0123992f, -2.64997e-07f, 8.35986e-08f),
        new(0.372f, 0.0124029f, 9.88983e-07f, -3.11994e-07f),
        new(0.434f, 0.0123893f, -3.69093e-06f, -4.35621e-07f),
        new(0.4958f, 0.0123198f, -1.02252e-05f, -3.45523e-07f),
        new(0.5571f, 0.0121916f, -1.54081e-05f, -5.82288e-07f),
        new(0.6176f, 0.0119938f, -2.41424e-05f, -5.25327e-07f),
        new(0.6769f, 0.011713f, -3.20223e-05f, -5.16405e-07f),
        new(0.7346f, 0.0113541f, -3.97684e-05f, -6.09052e-07f),
        new(0.7903f, 0.0109107f, -4.89042e-05f, -1.04739e-06f),
        new(0.8435f, 0.0103431f, -6.4615e-05f, -1.40374e-09f),
        new(0.8936f, 0.00969686f, -6.4636e-05f, -8.547e-06f),
        new(0.9394f, 0.00840947f, -0.000192841f, -4.2106e-06f),
        new(0.9761f, 0.00616527f, -0.000256f, -4.2106e-06f),
        new(1.0f, 0.00328947f, -0.000319159f, -4.2106e-06f),
    ];

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
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new RobinsonProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phiAbs = Math.Abs(lat);

        int index = GetLatitudeBand(phiAbs);
        if (index < 0 || index > Nodes)
        {
            ProjectionThrowHelper.ThrowOutsideProjectionDomain();
        }

        double dphi = RadiansToDegrees(phiAbs - (FiveDegreesInRadians * index));
        double xCoeff = Evaluate(CoeffX[index], dphi);
        double yCoeff = Evaluate(CoeffY[index], dphi);

        lon = this.SphericalRadius * XScale * lambda * xCoeff;
        lat = this.SphericalRadius * YScale * yCoeff * Sign(lat);
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double normalizedY = Math.Abs(y) * this.InverseSphericalRadius / YScale;
        double lambda = x * this.InverseSphericalRadius / XScale;

        if (normalizedY >= 1d)
        {
            if (normalizedY > OneEps)
            {
                ProjectionThrowHelper.ThrowOutsideProjectionDomain();
            }

            y = y < 0d ? -HalfPi : HalfPi;
            x = Adjust_lon(this.centralMeridian + (lambda / CoeffX[Nodes].C0));
            return;
        }

        int index = FindLatitudeBand(normalizedY);
        Coeff yc = CoeffY[index];

        double t = 5d * (normalizedY - yc.C0) / (CoeffY[index + 1].C0 - yc.C0);
        bool converged = false;
        for (int iteration = 0; iteration < MaxIterations; iteration++)
        {
            double delta = (Evaluate(yc, t) - normalizedY) / EvaluateDerivative(yc, t);
            t -= delta;
            if (Math.Abs(delta) < InverseTolerance)
            {
                converged = true;
                break;
            }
        }

        if (!converged)
        {
            ProjectionThrowHelper.ThrowOutsideProjectionDomain();
        }

        double phi = DegreesToRadians((5d * index) + t);
        if (y < 0d)
        {
            phi = -phi;
        }

        double lambdaResult = lambda / Evaluate(CoeffX[index], t);
        if (Math.Abs(lambdaResult) > PI)
        {
            ProjectionThrowHelper.ThrowOutsideProjectionDomain();
        }

        x = Adjust_lon(this.centralMeridian + lambdaResult);
        y = phi;
    }

    private static int GetLatitudeBand(double phiAbs)
    {
        if (double.IsNaN(phiAbs))
        {
            return -1;
        }

        int index = (int)Math.Floor((phiAbs * LatitudeBandScale) + 1e-15d);
        if (index >= Nodes)
        {
            index = Nodes;
        }

        return index;
    }

    private static int FindLatitudeBand(double yNormalized)
    {
        int index = (int)Math.Floor(yNormalized * Nodes);
        if (index < 0 || index >= Nodes)
        {
            ProjectionThrowHelper.ThrowOutsideProjectionDomain();
        }

        while (true)
        {
            if (CoeffY[index].C0 > yNormalized)
            {
                index--;
            }
            else if (CoeffY[index + 1].C0 <= yNormalized)
            {
                index++;
            }
            else
            {
                return index;
            }
        }
    }

    private static double Evaluate(Coeff coeff, double z)
    {
        return coeff.C0 + (z * (coeff.C1 + (z * (coeff.C2 + (z * coeff.C3)))));
    }

    private static double EvaluateDerivative(Coeff coeff, double z)
    {
        return coeff.C1 + (2d * z * coeff.C2) + (3d * z * z * coeff.C3);
    }

    private readonly struct Coeff
    {
        public Coeff(float c0, float c1, float c2, float c3)
        {
            this.C0 = c0;
            this.C1 = c1;
            this.C2 = c2;
            this.C3 = c3;
        }

        public float C0 { get; }

        public float C1 { get; }

        public float C2 { get; }

        public float C3 { get; }
    }
}
