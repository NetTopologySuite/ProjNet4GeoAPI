// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the S2 projection (<c>s2</c>).
/// </summary>
[Serializable]
internal sealed class S2Projection : MapProjection
{
    private const double HalfPiMinusFortPiHalf = HalfPi - (FortPi * 0.5d);

    /// <summary>
    /// Small bias to avoid tangent singularities close to machine precision.
    /// </summary>
    private const double TangentBias = 1.1102230246251565e-16d;

    private readonly Face face;
    private readonly UvToStProjectionType uvToStProjectionType;
    private readonly double aSquared;
    private readonly double oneMinusF;
    private readonly double oneMinusFSquared;

    /// <summary>
    /// Initializes a new instance of the <see cref="S2Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public S2Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="S2Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public S2Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "S2";
        this.face = DetermineFace(this.latOrigin, this.centralMeridian);
        this.uvToStProjectionType = ReadUvToStProjectionType(this.Parameters);

        if (this.es != 0d)
        {
            this.aSquared = this.semiMajor * this.semiMajor;
            this.oneMinusF = 1d - ((this.semiMajor - this.semiMinor) / this.semiMajor);
            this.oneMinusFSquared = this.oneMinusF * this.oneMinusF;
        }
        else
        {
            this.aSquared = 0d;
            this.oneMinusF = 1d;
            this.oneMinusFSquared = 1d;
        }
    }

    private enum Face
    {
        Front = 0,
        Right = 1,
        Top = 2,
        Back = 3,
        Left = 4,
        Bottom = 5,
    }

    private enum UvToStProjectionType
    {
        Linear = 0,
        Quadratic = 1,
        Tangent = 2,
        None = 3,
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new S2Projection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        // PROJ's s2 uses lon_0 only for face selection. Geographic input itself is
        // not recentered by lon_0 before face UV conversion.
        double lambda = lon;
        double phi = this.es == 0d
            ? lat
            : Math.Atan(this.oneMinusFSquared * Math.Tan(lat));

        Sincos(phi, out double sinPhi, out double cosPhi);
        Sincos(lambda, out double sinLambda, out double cosLambda);
        double x = cosPhi * cosLambda;
        double y = cosPhi * sinLambda;
        double z = sinPhi;

        ValidFaceXyzToUv(this.face, x, y, z, out double u, out double v);
        lon = UvToSt(u, this.uvToStProjectionType);
        lat = UvToSt(v, this.uvToStProjectionType);
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double u = StToUv(x, this.uvToStProjectionType);
        double v = StToUv(y, this.uvToStProjectionType);

        UvToSphereXyz(this.face, u, v, out double q, out double r, out double s);
        double lambda = Math.Atan2(r, q);
        double phi = Math.Acos(-s) - HalfPi;

        if (this.es != 0d)
        {
            bool invertSign = phi < 0d;
            double tanPhi = Math.Tan(phi);
            double xa = this.semiMinor / Math.Sqrt((tanPhi * tanPhi) + this.oneMinusFSquared);
            if (xa == 0d)
            {
                phi = HalfPi;
            }
            else
            {
                double inside = this.aSquared - (xa * xa);
                if (inside < 0d)
                {
                    inside = 0d;
                }

                phi = Math.Atan(Math.Sqrt(inside) / (this.oneMinusF * xa));
            }

            if (invertSign)
            {
                phi = -phi;
            }
        }

        if (Math.Abs(Math.Abs(phi) - HalfPi) <= Eps10)
        {
            lambda = 0d;
        }
        else if (Math.Abs(lambda + PI) <= Eps10)
        {
            lambda = PI;
        }

        x = Adjust_lon(lambda);
        y = phi;
    }

    private static double StToUv(double s, UvToStProjectionType projectionType)
    {
        switch (projectionType)
        {
            case UvToStProjectionType.Linear:
                return (2d * s) - 1d;
            case UvToStProjectionType.Quadratic:
                if (s >= 0.5d)
                {
                    return ((4d * (s * s)) - 1d) / 3d;
                }

                double oneMinusS = 1d - s;
                return (1d - (4d * oneMinusS * oneMinusS)) / 3d;
            case UvToStProjectionType.Tangent:
                double tangent = Math.Tan((HalfPi * s) - FortPi);
                return tangent + (TangentBias * tangent);
            default:
                return s;
        }
    }

    private static double UvToSt(double u, UvToStProjectionType projectionType)
    {
        switch (projectionType)
        {
            case UvToStProjectionType.Linear:
                return 0.5d * (u + 1d);
            case UvToStProjectionType.Quadratic:
                if (u >= 0d)
                {
                    return 0.5d * Math.Sqrt(Math.Max(0d, 1d + (3d * u)));
                }

                return 1d - (0.5d * Math.Sqrt(Math.Max(0d, 1d - (3d * u))));
            case UvToStProjectionType.Tangent:
                return (2d / PI) * (Math.Atan(u) + FortPi);
            default:
                return u;
        }
    }

    private static UvToStProjectionType ReadUvToStProjectionType(ProjectionParameterSet parameters)
    {
        double value = parameters.GetOptionalParameterValue("uv_to_st", 1d, "uvtost");
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            ArgumentGuard.ThrowArgument("Invalid value for uv_to_st parameter: expected linear, quadratic, tangent or none.");
        }

        int mode = (int)Math.Round(value);
        if (Math.Abs(value - mode) > Eps10)
        {
            ArgumentGuard.ThrowArgument("Invalid value for uv_to_st parameter: expected linear, quadratic, tangent or none.");
        }

        switch (mode)
        {
            case 0:
                return UvToStProjectionType.Linear;
            case 1:
                return UvToStProjectionType.Quadratic;
            case 2:
                return UvToStProjectionType.Tangent;
            case 3:
                return UvToStProjectionType.None;
            default:
                ArgumentGuard.ThrowArgument("Invalid value for uv_to_st parameter: expected linear, quadratic, tangent or none.");
                return UvToStProjectionType.None;
        }
    }

    private static void ValidFaceXyzToUv(Face face, double x, double y, double z, out double u, out double v)
    {
        switch (face)
        {
            case Face.Front:
                u = y / x;
                v = z / x;
                break;
            case Face.Right:
                u = -x / y;
                v = z / y;
                break;
            case Face.Top:
                u = -x / z;
                v = -y / z;
                break;
            case Face.Back:
                u = z / x;
                v = y / x;
                break;
            case Face.Left:
                u = z / y;
                v = -x / y;
                break;
            default:
                u = -y / z;
                v = -x / z;
                break;
        }
    }

    private static void UvToSphereXyz(Face face, double u, double v, out double x, out double y, out double z)
    {
        double majorCoord = 1d / Math.Sqrt(1d + (u * u) + (v * v));
        double minorCoord1 = u * majorCoord;
        double minorCoord2 = v * majorCoord;

        switch (face)
        {
            case Face.Front:
                x = majorCoord;
                y = minorCoord1;
                z = minorCoord2;
                break;
            case Face.Right:
                x = -minorCoord1;
                y = majorCoord;
                z = minorCoord2;
                break;
            case Face.Top:
                x = -minorCoord1;
                y = -minorCoord2;
                z = majorCoord;
                break;
            case Face.Back:
                x = -majorCoord;
                y = -minorCoord2;
                z = -minorCoord1;
                break;
            case Face.Left:
                x = minorCoord2;
                y = -majorCoord;
                z = -minorCoord1;
                break;
            default:
                x = minorCoord2;
                y = minorCoord1;
                z = -majorCoord;
                break;
        }
    }

    private static Face DetermineFace(double phi0, double lam0)
    {
        if (phi0 >= HalfPiMinusFortPiHalf)
        {
            return Face.Top;
        }

        if (phi0 <= -HalfPiMinusFortPiHalf)
        {
            return Face.Bottom;
        }

        if (Math.Abs(lam0) <= FortPi)
        {
            return Face.Front;
        }

        if (Math.Abs(lam0) <= HalfPi + FortPi)
        {
            return lam0 > 0d ? Face.Right : Face.Left;
        }

        return Face.Back;
    }
}
