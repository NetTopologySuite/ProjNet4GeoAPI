// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Icosahedral Snyder Equal Area projection (<c>isea</c>).
/// </summary>
[Serializable]
internal sealed class IseaProjection : MapProjection
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

    private readonly double radius;
    private readonly double inverseRadius;
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
    private readonly IseaPlanarInverseProjection planarInverseProjection;

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
    public IseaProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Icosahedral_Snyder_Equal_Area";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

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

    private enum IseaOutputMode
    {
        Plane,
        Di,
        Dd,
        Hex,
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new IseaProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        var input = new GeoPoint(lat, lon);
        int triangle = this.IseaTransform(input, out IseaPoint projected);

        IseaTriPlane(triangle, ref projected);

        lon = projected.X * this.radius;
        lat = projected.Y * this.radius;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        if (this.planarInverseProjection is null)
        {
            throw new InvalidOperationException("ISEA does not support inverse projection for this parameter set in this wave.");
        }

        double normalizedX = x * this.inverseRadius;
        double normalizedY = y * this.inverseRadius;

        double shiftedX = normalizedX + this.planarState.XOffset;
        double shiftedY = normalizedY + this.planarState.YOffset;

        if (!this.planarInverseProjection.TryCartesianToGeo(
            shiftedX,
            shiftedY,
            this.planarState,
            this.vertexLatSinCos,
            out GeoPoint geographicPoint))
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        x = Adjust_lon(geographicPoint.Lon);
        y = geographicPoint.Lat;
    }

    private static int ReadDiscreteCode(double value, string parameterName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            ArgumentGuard.ThrowArgument("Invalid value for " + parameterName + ".");
        }

        int rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
        if (Math.Abs(value - rounded) > 1e-12d)
        {
            ArgumentGuard.ThrowArgument("Invalid value for " + parameterName + ".");
        }

        return rounded;
    }

    private static bool IsNearlyEqual(double x, double y)
    {
        return Math.Abs(x - y) <= 1e-12d;
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
            _ => ArgumentGuard.ThrowArgument<double>("Input data outside projection domain."),
        };

        return new IseaPoint(x * RPrimeOverR, y * RPrimeOverR);
    }

    private static double AzAdjustment(int triangle)
    {
        if ((triangle >= 5 && triangle <= 9) || triangle == 15 || triangle == 16)
        {
            return PI;
        }

        if (triangle >= 17)
        {
            return -PI;
        }

        return 0d;
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

        if (Math.Abs(value + 1d) < SafeArcEpsilon)
        {
            return -HalfPi;
        }

        return Math.Asin(value);
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

        if (Math.Abs(value - 1d) < SafeArcEpsilon)
        {
            return 0d;
        }

        return Math.Acos(value);
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        if (value > maximum)
        {
            return maximum;
        }

        return value;
    }

    private static int ClampInt(int value, int minimum, int maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        if (value > maximum)
        {
            return maximum;
        }

        return value;
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

    private IseaPlanarInverseProjection TryCreatePlanarInverseProjection()
    {
        if (this.outputMode != IseaOutputMode.Plane)
        {
            return null;
        }

        if (this.aperture != 3 || this.resolution != 4 || Math.Abs(this.orientationAzimuth) > 1e-12d)
        {
            return null;
        }

        if (IsNearlyEqual(this.orientationLatitude, IseaStdLat) && IsNearlyEqual(this.orientationLongitude, IseaStdLon))
        {
            return new IseaPlanarInverseProjection(StandardInverseOrientationLat, StandardInverseOrientationLon);
        }

        if (IsNearlyEqual(this.orientationLatitude, HalfPi) && IsNearlyEqual(this.orientationLongitude, 0d))
        {
            return new IseaPlanarInverseProjection(0d, 0d);
        }

        return null;
    }

    private IseaPlanarState CreatePlanarState()
    {
        double normalizedR2;
        if (this.e > Eps10)
        {
            double bOverA = this.semiMinor / this.semiMajor;
            double bOverASquared = bOverA * bOverA;
            double log1pe1me = Math.Log((1d + this.e) / (1d - this.e));
            normalizedR2 = 0.5d + ((bOverASquared * log1pe1me) / (4d * this.e));
        }
        else
        {
            normalizedR2 = 1d;
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

            double h = Math.Acos(Clamp((sinAz * SinGcosSdc2VoS) - (cosAz * CosG), -1d, 1d));
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

        ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        output = default;
        return -1;
    }

    private readonly struct GeoPoint(double lat, double lon)
    {
        public double Lat { get; } = lat;

        public double Lon { get; } = lon;
    }

    private readonly struct IseaSinCos(double sin, double cos)
    {
        public double Sin { get; } = sin;

        public double Cos { get; } = cos;
    }

    private struct IseaPoint(double x, double y)
    {
        public double X = x;

        public double Y = y;
    }

    private sealed class IseaPlanarState
    {
        public IseaPlanarState(
            double r2,
            double rPrime2X,
            double rPrimeTang,
            double rPrime2Tan2g,
            double centerToBase,
            double triangleWidth,
            double[] yOffsets,
            double xOffset,
            double yOffset,
            double scaleX,
            double scaleY)
        {
            this.R2 = r2;
            this.RPrime2X = rPrime2X;
            this.RPrimeTang = rPrimeTang;
            this.RPrime2Tan2g = rPrime2Tan2g;
            this.CenterToBase = centerToBase;
            this.TriangleWidth = triangleWidth;
            this.YOffsets = yOffsets;
            this.XOffset = xOffset;
            this.YOffset = yOffset;
            this.ScaleX = scaleX;
            this.ScaleY = scaleY;
        }

        public double R2 { get; }

        public double RPrime2X { get; }

        public double RPrimeTang { get; }

        public double RPrime2Tan2g { get; }

        public double CenterToBase { get; }

        public double TriangleWidth { get; }

        public double[] YOffsets { get; }

        public double XOffset { get; }

        public double YOffset { get; }

        public double ScaleX { get; }

        public double ScaleY { get; }
    }

    private sealed class IseaPlanarInverseProjection
    {
        private readonly double orientationLatitude;
        private readonly double orientationLongitude;
        private readonly double cosOrientationLatitude;
        private readonly double sinOrientationLatitude;

        public IseaPlanarInverseProjection(double orientationLatitude, double orientationLongitude)
        {
            this.orientationLatitude = orientationLatitude;
            this.orientationLongitude = orientationLongitude;
            this.cosOrientationLatitude = Math.Cos(orientationLatitude);
            this.sinOrientationLatitude = Math.Sin(orientationLatitude);
        }

        public bool TryCartesianToGeo(
            double inputX,
            double inputY,
            IseaPlanarState state,
            IseaSinCos[] vertexLatSinCos,
            out GeoPoint result)
        {
            const double epsilon = 1e-11d;
            int face = 0;
            double positionX = inputX;
            double positionY = inputY;

            const double sr = -Sin60;
            const double cr = 0.5d;

            if (positionX < 0d
                || (positionX < (state.TriangleWidth * 0.5d)
                    && positionY < 0d
                    && ((positionY * cr) < (positionX * sr))))
            {
                positionX += 5d * state.TriangleWidth;
            }

            double yp = -((positionX * sr) + (positionY * cr));
            double x = ((positionX * cr) - (positionY * sr) + (yp * (1d / Sqrt3))) * state.ScaleX;
            double y = yp * state.ScaleY;

            if (x < 0d || (y > x && x < 5d - epsilon))
            {
                x += epsilon;
            }
            else if (x > 5d || (y < x && x > epsilon))
            {
                x -= epsilon;
            }

            if (y < 0d || (x > y && y < 6d - epsilon))
            {
                y += epsilon;
            }
            else if (y > 6d || (x < y && y > epsilon))
            {
                y -= epsilon;
            }

            if (x >= 0d && x <= 5d && y >= 0d && y <= 6d)
            {
                int ix = ClampInt((int)x, 0, 4);
                int iy = ClampInt((int)y, 0, 5);

                if (iy == ix || iy == ix + 1)
                {
                    int rhombus = ix + iy;
                    bool top = (x - ix) > (y - iy);
                    face = rhombus switch
                    {
                        0 => top ? 0 : 5,
                        2 => top ? 1 : 6,
                        4 => top ? 2 : 7,
                        6 => top ? 3 : 8,
                        8 => top ? 4 : 9,
                        1 => top ? 10 : 15,
                        3 => top ? 11 : 16,
                        5 => top ? 12 : 17,
                        7 => top ? 13 : 18,
                        9 => top ? 14 : 19,
                        _ => -1,
                    };
                    face++;
                }
            }

            if (face == 0)
            {
                result = default;
                return false;
            }

            int faceIndex = face - 1;
            int fy = faceIndex / 5;
            int fx = faceIndex - (5 * fy);

            // Match PROJ integer arithmetic: fy/2 is integer division in the source implementation.
            double rx = positionX - (((2d * fx) + (fy / 2) + 1d) * state.TriangleWidth * 0.5d);
            double ry = positionY - (state.YOffsets[fy] + (3d * state.CenterToBase));

            if (!this.TryIcosahedronToSphere(faceIndex, rx, ry, state, vertexLatSinCos, out GeoPoint dst))
            {
                result = default;
                return false;
            }

            double lon = dst.Lon;
            if (lon < -PI - epsilon)
            {
                lon += TwoPi;
            }
            else if (lon > PI + epsilon)
            {
                lon -= TwoPi;
            }

            result = new GeoPoint(dst.Lat, lon);
            return true;
        }

        private static double FaceOrientation(int face)
        {
            return (face <= 4 || (face >= 10 && face <= 14)) ? 0d : PI;
        }

        private bool TryIcosahedronToSphere(
            int face,
            double x,
            double y,
            IseaPlanarState state,
            IseaSinCos[] vertexLatSinCos,
            out GeoPoint result)
        {
            if (face < 0 || face >= NumIcosahedronFaces)
            {
                result = default;
                return false;
            }

            double az = Math.Atan2(x, y);
            double rho = Math.Sqrt((x * x) + (y * y));
            double azAdjustment = FaceOrientation(face);

            az += azAdjustment;
            while (az < 0d)
            {
                azAdjustment += AzMax;
                az += AzMax;
            }

            while (az > AzMax)
            {
                azAdjustment -= AzMax;
                az -= AzMax;
            }

            double sinAz = Math.Sin(az);
            double cosAz = Math.Cos(az);
            double cotAz = cosAz / sinAz;
            double area = state.RPrime2Tan2g / (2d * (cotAz + CotTheta));
            double deltaAz = 10d * Precision;
            double degAreaOverR2Plus180Minus36 = (area / state.R2) - WestVertexLon;
            double azEarth = az;

            while (Math.Abs(deltaAz) > Precision)
            {
                double sinAzEarth = Math.Sin(azEarth);
                double cosAzEarth = Math.Cos(azEarth);
                double h = Math.Acos(Clamp((sinAzEarth * SinGcosSdc2VoS) - (cosAzEarth * CosG), -1d, 1d));
                double function = degAreaOverR2Plus180Minus36 - h - azEarth;
                double derivativeDenominator = Math.Sin(h);
                if (Math.Abs(derivativeDenominator) <= Eps10)
                {
                    result = default;
                    return false;
                }

                double derivative = (((cosAzEarth * SinGcosSdc2VoS) + (sinAzEarth * CosG)) / derivativeDenominator) - 1d;
                if (Math.Abs(derivative) <= Eps10)
                {
                    result = default;
                    return false;
                }

                deltaAz = -function / derivative;
                azEarth += deltaAz;
            }

            double sinAzEarthFinal = Math.Sin(azEarth);
            double cosAzEarthFinal = Math.Cos(azEarth);
            double q = Math.Atan2(Tang, cosAzEarthFinal + (sinAzEarthFinal * CotTheta));
            double denominator = Math.Cos(az) + (Math.Sin(az) * CotTheta);
            if (Math.Abs(denominator) <= Eps10)
            {
                result = default;
                return false;
            }

            double d = state.RPrimeTang / denominator;
            double sinQHalf = Math.Sin(q * 0.5d);
            if (Math.Abs(sinQHalf) <= Eps10)
            {
                result = default;
                return false;
            }

            double f = d / (state.RPrime2X * sinQHalf);
            if (Math.Abs(f) <= Eps10)
            {
                result = default;
                return false;
            }

            double z = 2d * Math.Asin(Clamp(rho / (state.RPrime2X * f), -1d, 1d));

            azEarth -= azAdjustment;

            IseaSinCos latSinCos = vertexLatSinCos[face];
            double sinLat0 = latSinCos.Sin;
            double cosLat0 = latSinCos.Cos;
            double sinZ = Math.Sin(z);
            double cosZ = Math.Cos(z);
            double cosLat0SinZ = cosLat0 * sinZ;
            double latSin = (sinLat0 * cosZ) + (cosLat0SinZ * Math.Cos(azEarth));
            double lat = SafeArcSin(latSin);
            double lon = FacesCenterDodecahedronVertices[face].Lon +
                         Math.Atan2(
                             Math.Sin(azEarth) * cosLat0SinZ,
                             cosZ - (sinLat0 * Math.Sin(lat)));

            this.RevertOrientation(new GeoPoint(lat, lon), out result);
            return true;
        }

        private void RevertOrientation(in GeoPoint point, out GeoPoint result)
        {
            double lon = (point.Lat < (-HalfPi + PrecisionPerDefinition)
                          || point.Lat > (HalfPi - PrecisionPerDefinition))
                ? 0d
                : point.Lon;

            if (this.orientationLatitude != 0d || this.orientationLongitude != 0d)
            {
                double sinLat = Math.Sin(point.Lat);
                double cosLat = Math.Cos(point.Lat);
                double sinLon = Math.Sin(lon);
                double cosLon = Math.Cos(lon);
                double cosLonCosLat = cosLon * cosLat;
                double orientedLon = Math.Atan2(
                    sinLon * cosLat,
                    (cosLonCosLat * this.cosOrientationLatitude) + (sinLat * this.sinOrientationLatitude))
                    - this.orientationLongitude;

                result = new GeoPoint(
                    Math.Asin((sinLat * this.cosOrientationLatitude) - (cosLonCosLat * this.sinOrientationLatitude)),
                    orientedLon);
            }
            else
            {
                result = new GeoPoint(point.Lat, lon);
            }
        }
    }
}
