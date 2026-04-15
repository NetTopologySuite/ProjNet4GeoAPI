// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Icosahedral Snyder Equal Area projection (<c>isea</c>).
/// </summary>
/// <remarks>
/// <para>The Icosahedral Snyder Equal Area projection distributes the globe over the
/// twenty faces of an icosahedron and applies a modified Lambert azimuthal equal-area
/// construction within each triangular face. The inverse projection follows PROJ's
/// current planar implementation and is therefore available for the supported
/// <c>+proj=isea</c> / <c>+orient=pole</c> planar subset with zero azimuth,
/// aperture 3, and resolution 4.</para>
/// <para>The formulation was independently verified against John P. Snyder,
/// "An equal-area map projection for polyhedral globes," <i>Cartographica</i>,
/// vol. 29, no. 1, pp. 10-21, 1992. The face subdivision into isosceles triangles and
/// the Snyder forward equations used within each face match the implementation here.</para>
/// </remarks>
/// <seealso href="https://en.wikipedia.org/wiki/Snyder_equal-area_projection">Wikipedia: Snyder equal-area projection.</seealso>
internal sealed partial class IseaProjection : MapProjection
{
    private const int NumIcosahedronFaces = 20;

    private const double DegToRad = PI / 180d;
    private const double Deg120 = 2.09439510239319549229d;
    private const double Deg180 = PI;

    private const double ERad = 0.91843818701052843323d;
    private const double FRad = 0.18871053078356206978d;

    private const double Sdc2VoS = 0.6523581397843681859886783d;
    private const double Tang = 0.76393202250021030358019673567d;
    private const double Tan30 = 0.57735026918962576450914878d;
    private const double CotTheta = 1d / Tan30;

    private const double CosG = 0.80901699437494742410229341718281905886d;
    private const double SinG = 0.587785252292473129168705954639072768597652d;
    private const double CosSdc2VoS = 0.7946544722917661229596057297879189448539d;
    private const double SinGcosSdc2VoS = SinG * CosSdc2VoS;

    private const double Sqrt3 = 1.73205080756887729352744634150587236694280525381038d;
    private const double Sin60 = Sqrt3 / 2d;

    private const double TableG = Tang * Sin60;
    private const double RPrimeOverR = 0.9103832815095032d;
    private const double TableH = 0.25d * Tang;

    private const double IseaStdLat = 1.01722196792335072101d;
    private const double IseaStdLon = 0.19634954084936207740d;

    private const double StandardInverseOrientationLat = (ERad + FRad) * 0.5d;
    private const double StandardInverseOrientationLon = -11.25d * DegToRad;

    private const double Precision = DegToRad * 1e-11d;
    private const double PrecisionPerDefinition = DegToRad * 1e-5d;
    private const double AzMax = 120d * DegToRad;
    private const double WestVertexLon = -144d * DegToRad;

    private const double SafeArcEpsilon = 1e-15d;

    private const int ModePlane = 0;
    private const int ModeDi = 1;
    private const int ModeDd = 2;
    private const int ModeHex = 3;

    private const int OrientIsea = 0;
    private const int OrientPole = 1;

    private static readonly GeoPoint[] FacesCenterDodecahedronVertices =
    [
        new GeoPoint(ERad, -144d * DegToRad),
        new GeoPoint(ERad, -72d * DegToRad),
        new GeoPoint(ERad, 0d * DegToRad),
        new GeoPoint(ERad, 72d * DegToRad),
        new GeoPoint(ERad, 144d * DegToRad),
        new GeoPoint(FRad, -144d * DegToRad),
        new GeoPoint(FRad, -72d * DegToRad),
        new GeoPoint(FRad, 0d * DegToRad),
        new GeoPoint(FRad, 72d * DegToRad),
        new GeoPoint(FRad, 144d * DegToRad),
        new GeoPoint(-FRad, -108d * DegToRad),
        new GeoPoint(-FRad, -36d * DegToRad),
        new GeoPoint(-FRad, 36d * DegToRad),
        new GeoPoint(-FRad, 108d * DegToRad),
        new GeoPoint(-FRad, 180d * DegToRad),
        new GeoPoint(-ERad, -108d * DegToRad),
        new GeoPoint(-ERad, -36d * DegToRad),
        new GeoPoint(-ERad, 36d * DegToRad),
        new GeoPoint(-ERad, 108d * DegToRad),
        new GeoPoint(-ERad, 180d * DegToRad),
    ];

    private readonly IseaOutputMode outputMode;
    private readonly int aperture;
    private readonly int resolution;
    private readonly double orientationLatitude;
    private readonly double orientationLongitude;
    private readonly double orientationAzimuth;
    private readonly IseaSinCos[] vertexLatSinCos = new IseaSinCos[NumIcosahedronFaces];
    [field: NonSerialized]
    private readonly IseaPlanarState planarState;
    [field: NonSerialized]
    private readonly IseaPlanarInverseProjection? planarInverseProjection;

    /// <summary>
    /// Initializes a new instance of the <see cref="IseaProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public IseaProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IseaProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public IseaProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Icosahedral_Snyder_Equal_Area";

        int orientationCode = ReadDiscreteCode(
            this.Parameters.GetOptionalParameterValue("isea_orient", OrientIsea, "orient"),
            "orient");
        switch (orientationCode)
        {
            case OrientIsea:
                this.orientationLatitude = IseaStdLat;
                this.orientationLongitude = IseaStdLon;
                break;
            case OrientPole:
                this.orientationLatitude = HalfPi;
                this.orientationLongitude = 0d;
                break;
            default:
                ArgumentGuard.ThrowArgument("Invalid value for orient: only isea or pole are supported.");
                break;
        }

        this.orientationAzimuth = DegreesToRadians(
            this.Parameters.GetOptionalParameterValue("isea_azimuth", 0d, "isea_o_az", "isea_az", "azi"));

        this.aperture = ReadDiscreteCode(
            this.Parameters.GetOptionalParameterValue("isea_aperture", 3d, "aperture"),
            "aperture");
        this.resolution = ReadDiscreteCode(
            this.Parameters.GetOptionalParameterValue("isea_resolution", 4d, "resolution"),
            "resolution");

        int modeCode = ReadDiscreteCode(
            this.Parameters.GetOptionalParameterValue("isea_mode", ModePlane, "mode"),
            "mode");
        this.outputMode = modeCode switch
        {
            ModePlane => IseaOutputMode.Plane,
            ModeDi => IseaOutputMode.Di,
            ModeDd => IseaOutputMode.Dd,
            ModeHex => IseaOutputMode.Hex,
            _ => ArgumentGuard.ThrowArgument<IseaOutputMode>("Invalid value for mode: only plane, di, dd or hex are supported."),
        };

        if (this.outputMode != IseaOutputMode.Plane)
        {
            ArgumentGuard.ThrowArgument("ISEA mode is not supported in this wave. Only plane mode is currently implemented.");
        }

        for (int i = 0; i < NumIcosahedronFaces; i++)
        {
            GeoPoint center = FacesCenterDodecahedronVertices[i];
            this.vertexLatSinCos[i] = new IseaSinCos(Math.Sin(center.Lat), Math.Cos(center.Lat));
        }

        this.planarState = this.CreatePlanarState();
        this.planarInverseProjection = this.TryCreatePlanarInverseProjection();
    }

    /// <inheritdoc />
    protected override bool HasInverseSupport => this.planarInverseProjection is not null;

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return this.GetOrCreateInverse(() => new IseaProjection(this.Parameters.ToProjectionParameter(), this));
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        var input = new GeoPoint(lat, lon);
        int triangle = this.IseaTransform(input, out IseaPoint projected);

        IseaTriPlane(triangle, ref projected);

        lon = projected.X * this.SphericalRadius;
        lat = projected.Y * this.SphericalRadius;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        if (this.planarInverseProjection is null)
        {
            throw new InvalidOperationException("ISEA does not support inverse projection for this parameter set in this wave.");
        }

        double normalizedX = x * this.InverseSphericalRadius;
        double normalizedY = y * this.InverseSphericalRadius;

        double shiftedX = normalizedX + this.planarState.XOffset;
        double shiftedY = normalizedY + this.planarState.YOffset;

        if (!this.planarInverseProjection.TryCartesianToGeo(
            shiftedX,
            shiftedY,
            this.planarState,
            this.vertexLatSinCos,
            out GeoPoint geographicPoint))
        {
            throw new System.InvalidOperationException("Input data outside projection domain.");
        }

        x = Adjust_lon(geographicPoint.Lon);
        y = geographicPoint.Lat;
    }

    private static int ReadDiscreteCode(double value, string parameterName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            ArgumentGuard.ThrowArgument($"Invalid value for {parameterName}.");
        }

        int rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
        if (Math.Abs(value - rounded) > ProjectionConstants.Tolerance1E12)
        {
            ArgumentGuard.ThrowArgument($"Invalid value for {parameterName}.");
        }

        return rounded;
    }

    private static bool IsNearlyEqual(double x, double y)
    {
        return Math.Abs(x - y) <= ProjectionConstants.Tolerance1E12;
    }

    private static GeoPoint SnyderCtran(in GeoPoint np, in GeoPoint point)
    {
        double phi = point.Lat;
        double lambda = point.Lon;
        double alpha = np.Lat;
        double beta = np.Lon;
        double deltaLambda = lambda - beta;
        double cosP = Math.Cos(phi);
        double sinP = Math.Sin(phi);
        double cosA = Math.Cos(alpha);
        double sinA = Math.Sin(alpha);
        double cosDeltaLambda = Math.Cos(deltaLambda);
        double sinDeltaLambda = Math.Sin(deltaLambda);

        double sinPhiPrime = (sinA * sinP) - (cosA * cosP * cosDeltaLambda);

        double lambdaPrimeMinusBeta = Math.Atan2(
            cosP * sinDeltaLambda,
            (sinA * cosP * cosDeltaLambda) + (cosA * sinP));
        double lambdaPrime = NormalizeLongitude(lambdaPrimeMinusBeta + beta);

        return new GeoPoint(SafeArcSin(sinPhiPrime), lambdaPrime);
    }

    private static GeoPoint IseaCtran(in GeoPoint np, in GeoPoint point, double lon0)
    {
        var cnp = new GeoPoint(np.Lat, np.Lon + PI);
        GeoPoint transformed = SnyderCtran(cnp, point);
        double longitude = transformed.Lon - (-lon0 + np.Lon);
        return new GeoPoint(transformed.Lat, NormalizeLongitude(longitude));
    }

    private static void IseaTriPlane(int triangle, ref IseaPoint point)
    {
        if (IsDownTriangle(triangle))
        {
            point.X = -point.X;
            point.Y = -point.Y;
        }

        IseaPoint center = IseaTriangleXY(triangle);
        point.X += center.X;
        point.Y += center.Y;
    }

    private static bool IsDownTriangle(int triangle)
    {
        return ((triangle / 5) % 2) == 1;
    }

    private static IseaPoint IseaTriangleXY(int triangle)
    {
        int normalizedTriangle = triangle % NumIcosahedronFaces;
        if (normalizedTriangle < 0)
        {
            normalizedTriangle += NumIcosahedronFaces;
        }

        double x = TableG * ((normalizedTriangle % 5) - 2d) * 2d;
        if (normalizedTriangle > 9)
        {
            x += TableG;
        }

        double y = (normalizedTriangle / 5) switch
        {
            0 => 5d * TableH,
            1 => TableH,
            2 => -TableH,
            3 => -5d * TableH,
            _ => throw new System.InvalidOperationException("Input data outside projection domain."),
        };

        return new IseaPoint(x * RPrimeOverR, y * RPrimeOverR);
    }

    private static double AzAdjustment(int triangle)
    {
        if ((triangle >= 5 && triangle <= 9) || triangle == 15 || triangle == 16)
        {
            return PI;
        }

        return triangle >= 17 ? -PI : 0d;
    }

    private static double SafeArcSin(double value)
    {
        if (Math.Abs(value) < SafeArcEpsilon)
        {
            return 0d;
        }

        if (Math.Abs(value - 1d) < SafeArcEpsilon)
        {
            return HalfPi;
        }

        return Math.Abs(value + 1d) < SafeArcEpsilon ? -HalfPi : Math.Asin(value);
    }

    private static double SafeArcCos(double value)
    {
        if (Math.Abs(value) < SafeArcEpsilon)
        {
            return HalfPi;
        }

        if (Math.Abs(value + 1d) < SafeArcEpsilon)
        {
            return PI;
        }

        return Math.Abs(value - 1d) < SafeArcEpsilon ? 0d : Math.Acos(value);
    }

    private static int ClampInt(int value, int minimum, int maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }

    private static double NormalizeLongitude(double longitude)
    {
        double normalized = longitude % TwoPi;
        if (normalized > PI)
        {
            normalized -= TwoPi;
        }
        else if (normalized < -PI)
        {
            normalized += TwoPi;
        }

        return normalized;
    }

    private IseaPlanarInverseProjection? TryCreatePlanarInverseProjection()
    {
        if (this.outputMode != IseaOutputMode.Plane)
        {
            return null;
        }

        if (this.aperture != 3 || this.resolution != 4 || Math.Abs(this.orientationAzimuth) > ProjectionConstants.Tolerance1E12)
        {
            return null;
        }

        if (IsNearlyEqual(this.orientationLatitude, IseaStdLat) && IsNearlyEqual(this.orientationLongitude, IseaStdLon))
        {
            return new IseaPlanarInverseProjection(StandardInverseOrientationLat, StandardInverseOrientationLon);
        }

        return IsNearlyEqual(this.orientationLatitude, HalfPi) && IsNearlyEqual(this.orientationLongitude, 0d)
            ? new IseaPlanarInverseProjection(0d, 0d)
            : null;
    }

    private IseaPlanarState CreatePlanarState()
    {
        double normalizedR2 = 1d;
        if (this.e > Eps10)
        {
            double bOverA = this.semiMinor / this.semiMajor;
            double bOverASquared = bOverA * bOverA;
            double log1pe1me = Math.Log((1d + this.e) / (1d - this.e));
            normalizedR2 = 0.5d + ((bOverASquared * log1pe1me) / (4d * this.e));
        }

        double rPrime = RPrimeOverR * Math.Sqrt(normalizedR2);
        double rPrime2X = 2d * rPrime;
        double rPrimeTang = rPrime * Tang;
        double centerToBase = rPrimeTang * 0.5d;
        double triangleWidth = rPrimeTang * Sqrt3;
        double rPrime2Tan2g = rPrimeTang * rPrimeTang;

        double[] yOffsets =
        [
            -2d * centerToBase,
            -4d * centerToBase,
            -5d * centerToBase,
            -7d * centerToBase,
        ];

        double xOffset = 2.5d * triangleWidth;
        double yOffset = -1.5d * centerToBase;
        double scaleX = 1d / triangleWidth;
        double scaleY = 1d / (3d * centerToBase);

        return new IseaPlanarState(
            normalizedR2,
            rPrime2X,
            rPrimeTang,
            rPrime2Tan2g,
            centerToBase,
            triangleWidth,
            yOffsets,
            xOffset,
            yOffset,
            scaleX,
            scaleY);
    }

    private int IseaTransform(in GeoPoint input, out IseaPoint output)
    {
        GeoPoint pole = new(this.orientationLatitude, this.orientationLongitude);
        GeoPoint transformed = IseaCtran(pole, input, this.orientationAzimuth);
        int triangle = this.IseaSnyderForward(transformed, out output);
        return triangle;
    }

    private int IseaSnyderForward(in GeoPoint input, out IseaPoint output)
    {
        double sinLat = Math.Sin(input.Lat);
        double cosLat = Math.Cos(input.Lat);

        for (int i = 0; i < NumIcosahedronFaces; i++)
        {
            GeoPoint center = FacesCenterDodecahedronVertices[i];
            IseaSinCos centerLatSinCos = this.vertexLatSinCos[i];
            double deltaLon = input.Lon - center.Lon;
            double cosLatCosLon = cosLat * Math.Cos(deltaLon);
            double cosZ = (centerLatSinCos.Sin * sinLat) + (centerLatSinCos.Cos * cosLatCosLon);
            double z = SafeArcCos(cosZ);

            if (z > Sdc2VoS + 0.000005d)
            {
                continue;
            }

            double azimuth = Math.Atan2(
                cosLat * Math.Sin(deltaLon),
                (centerLatSinCos.Cos * sinLat) - (centerLatSinCos.Sin * cosLatCosLon));

            double azOffset = AzAdjustment(i);
            azimuth -= azOffset;
            if (azimuth < 0d)
            {
                azimuth += TwoPi;
            }

            int azimuthAdjustMultiples = 0;
            while (azimuth < 0d)
            {
                azimuth += Deg120;
                azimuthAdjustMultiples--;
            }

            while (azimuth > Deg120 + double.Epsilon)
            {
                azimuth -= Deg120;
                azimuthAdjustMultiples++;
            }

            double cosAz = Math.Cos(azimuth);
            double sinAz = Math.Sin(azimuth);
            double q = Math.Atan2(Tang, cosAz + (sinAz * CotTheta));

            if (z > q + 0.000005d)
            {
                continue;
            }

            double h = Math.Acos(ProjectionConstants.Clamp((sinAz * SinGcosSdc2VoS) - (cosAz * CosG), -1d, 1d));
            double area = azimuth + (36d * DegToRad) + h - Deg180;
            double azimuthPrime = Math.Atan2(
                2d * area,
                (RPrimeOverR * RPrimeOverR * Tang * Tang) - (2d * area * CotTheta));

            double denominator = Math.Cos(azimuthPrime) + (Math.Sin(azimuthPrime) * CotTheta);
            if (Math.Abs(denominator) <= Eps10)
            {
                continue;
            }

            double dPrime = (RPrimeOverR * Tang) / denominator;
            double sinQHalf = Math.Sin(q * 0.5d);
            if (Math.Abs(sinQHalf) <= Eps10)
            {
                continue;
            }

            double f = dPrime / (2d * RPrimeOverR * sinQHalf);
            double rho = 2d * RPrimeOverR * f * Math.Sin(z * 0.5d);

            azimuthPrime += Deg120 * azimuthAdjustMultiples;

            output = new IseaPoint(
                rho * Math.Sin(azimuthPrime),
                rho * Math.Cos(azimuthPrime));
            return i;
        }

        throw new System.InvalidOperationException("Input data outside projection domain.");
    }
}
