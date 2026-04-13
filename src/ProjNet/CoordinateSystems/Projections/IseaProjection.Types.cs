// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;

/// <summary>
/// Nested geometry and inverse-projection helper types for the ISEA projection.
/// </summary>
internal sealed partial class IseaProjection
{
    private enum IseaOutputMode
    {
        Plane,
        Di,
        Dd,
        Hex,
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
                double h = Math.Acos(ProjectionConstants.Clamp((sinAzEarth * SinGcosSdc2VoS) - (cosAzEarth * CosG), -1d, 1d));
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

            double z = 2d * Math.Asin(ProjectionConstants.Clamp(rho / (state.RPrime2X * f), -1d, 1d));

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
