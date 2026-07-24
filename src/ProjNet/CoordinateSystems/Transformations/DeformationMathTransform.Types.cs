// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Nested grid data structures and interpolation helpers for the deformation transform.
/// </summary>
internal sealed partial class DeformationMathTransform
{
    private readonly struct InterpolationCell(int x0, int y0, int x1, int y1, double w00, double w01, double w10, double w11)
    {
        internal int X0 { get; } = x0;

        internal int Y0 { get; } = y0;

        internal int X1 { get; } = x1;

        internal int Y1 { get; } = y1;

        internal double W00 { get; } = w00;

        internal double W01 { get; } = w01;

        internal double W10 { get; } = w10;

        internal double W11 { get; } = w11;
    }

    private sealed class CTable2Grid
    {
        private readonly float[] eastValues;
        private readonly float[] northValues;

        private CTable2Grid(
            string sourcePath,
            double west,
            double east,
            double south,
            double north,
            double resolutionX,
            double resolutionY,
            int width,
            int height,
            float[] eastValues,
            float[] northValues)
        {
            this.SourcePath = sourcePath;
            this.West = west;
            this.East = east;
            this.South = south;
            this.North = north;
            this.ResolutionX = resolutionX;
            this.ResolutionY = resolutionY;
            this.Width = width;
            this.Height = height;
            this.eastValues = eastValues;
            this.northValues = northValues;
            this.Epsilon = (Math.Abs(resolutionX) + Math.Abs(resolutionY)) * RelativeTolerance;
            this.InvResolutionX = 1d / resolutionX;
            this.InvResolutionY = 1d / resolutionY;
            this.Area = Math.Abs((east - west) * (north - south));
        }

        internal string SourcePath { get; }

        internal double West { get; }

        internal double East { get; }

        internal double South { get; }

        internal double North { get; }

        internal double ResolutionX { get; }

        internal double ResolutionY { get; }

        internal int Width { get; }

        internal int Height { get; }

        internal double Epsilon { get; }

        internal double InvResolutionX { get; }

        internal double InvResolutionY { get; }

        internal double Area { get; }

        internal static CTable2Grid Load(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.Length < 160)
            {
                throw new InvalidDataException("CTABLE2 file is too small.");
            }

            string identifier = Encoding.ASCII.GetString(bytes, 0, 9);
            if (!identifier.Equals("CTABLE V2", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Horizontal grid file is not a CTable2 grid.");
            }

            double west = ReadDoubleLittleEndian(bytes, 96);
            double south = ReadDoubleLittleEndian(bytes, 104);
            double resolutionX = ReadDoubleLittleEndian(bytes, 112);
            double resolutionY = ReadDoubleLittleEndian(bytes, 120);
            int width = ReadInt32LittleEndian(bytes, 128);
            int height = ReadInt32LittleEndian(bytes, 132);
            if (!TransformationMath.IsFinite(west)
                || !TransformationMath.IsFinite(south)
                || !TransformationMath.IsFinite(resolutionX)
                || !TransformationMath.IsFinite(resolutionY)
                || width <= 0
                || height <= 0
                || Math.Abs(west) > (4d * Math.PI)
                || Math.Abs(south) > (Math.PI + 1e-5d)
                || resolutionX <= 1e-10d
                || resolutionY <= 1e-10d)
            {
                throw new InvalidDataException("CTABLE2 header contains invalid extents or resolution.");
            }

            long expectedDataBytes = (long)width * height * 8L;
            if (bytes.Length < 160 + expectedDataBytes)
            {
                throw new InvalidDataException("CTABLE2 data is truncated.");
            }

            float[] eastValues = new float[width * height];
            float[] northValues = new float[width * height];
            int offset = 160;
            for (int i = 0; i < eastValues.Length; i++)
            {
                eastValues[i] = ReadSingleLittleEndian(bytes, offset);
                northValues[i] = ReadSingleLittleEndian(bytes, offset + 4);
                offset += 8;
            }

            double east = west + ((width - 1) * resolutionX);
            double north = south + ((height - 1) * resolutionY);
            return new CTable2Grid(path, west, east, south, north, resolutionX, resolutionY, width, height, eastValues, northValues);
        }

        internal bool Contains(double longitudeRadians, double latitudeRadians)
        {
            double lon = longitudeRadians;
            if (lon < this.West - this.Epsilon)
            {
                lon += TwoPi;
            }
            else if (lon > this.East + this.Epsilon)
            {
                lon -= TwoPi;
            }

            return lon >= this.West - this.Epsilon
                && lon <= this.East + this.Epsilon
                && latitudeRadians >= this.South - this.Epsilon
                && latitudeRadians <= this.North + this.Epsilon;
        }

        internal bool TryInterpolate(double longitudeRadians, double latitudeRadians, out double eastMmPerYear, out double northMmPerYear)
        {
            eastMmPerYear = 0d;
            northMmPerYear = 0d;
            double normalizedLongitude = longitudeRadians - this.West;
            if (normalizedLongitude + this.Epsilon < 0d)
            {
                normalizedLongitude += TwoPi;
            }
            else if (normalizedLongitude - this.Epsilon > this.East - this.West)
            {
                normalizedLongitude -= TwoPi;
            }

            double normalizedLatitude = latitudeRadians - this.South;
            double gridX = normalizedLongitude * this.InvResolutionX;
            double gridY = normalizedLatitude * this.InvResolutionY;
            int indexX = (int)Math.Floor(gridX);
            int indexY = (int)Math.Floor(gridY);
            double fractionX = gridX - indexX;
            double fractionY = gridY - indexY;
            if (!TryNormalizeInterpolationCell(this.Width, ref indexX, ref fractionX)
                || !TryNormalizeInterpolationCell(this.Height, ref indexY, ref fractionY))
            {
                return false;
            }

            int indexX2 = indexX + 1;
            int indexY2 = indexY + 1;
            double xy = fractionX * fractionY;
            double w00 = 1d - fractionX - fractionY + xy;
            double w10 = fractionX - xy;
            double w01 = fractionY - xy;
            double w11 = xy;
            eastMmPerYear = (this.GetEastValue(indexX, indexY) * w00)
                + (this.GetEastValue(indexX2, indexY) * w10)
                + (this.GetEastValue(indexX, indexY2) * w01)
                + (this.GetEastValue(indexX2, indexY2) * w11);
            northMmPerYear = (this.GetNorthValue(indexX, indexY) * w00)
                + (this.GetNorthValue(indexX2, indexY) * w10)
                + (this.GetNorthValue(indexX, indexY2) * w01)
                + (this.GetNorthValue(indexX2, indexY2) * w11);
            return TransformationMath.IsFinite(eastMmPerYear) && TransformationMath.IsFinite(northMmPerYear);
        }

        private static int ReadInt32LittleEndian(byte[] bytes, int offset)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset, sizeof(int)));
        }

        private static double ReadDoubleLittleEndian(byte[] bytes, int offset)
        {
            long rawBits = BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(offset, sizeof(long)));
            return BitConverter.Int64BitsToDouble(rawBits);
        }

        private static float ReadSingleLittleEndian(byte[] bytes, int offset)
        {
            int rawBits = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset, sizeof(int)));
            Span<int> bitStorage = stackalloc int[1];
            bitStorage[0] = rawBits;
            return MemoryMarshal.Cast<int, float>(bitStorage)[0];
        }

        private float GetEastValue(int x, int y)
        {
            return this.eastValues[(y * this.Width) + x];
        }

        private float GetNorthValue(int x, int y)
        {
            return this.northValues[(y * this.Width) + x];
        }
    }

    private sealed class GtxGrid
    {
        private readonly float[] values;

        private GtxGrid(
            string sourcePath,
            double west,
            double east,
            double south,
            double north,
            double resolutionX,
            double resolutionY,
            int width,
            int height,
            bool fullWorldLongitude,
            float[] values)
        {
            this.SourcePath = sourcePath;
            this.West = west;
            this.East = east;
            this.South = south;
            this.North = north;
            this.ResolutionX = resolutionX;
            this.ResolutionY = resolutionY;
            this.Width = width;
            this.Height = height;
            this.FullWorldLongitude = fullWorldLongitude;
            this.values = values;
            this.Epsilon = (Math.Abs(resolutionX) + Math.Abs(resolutionY)) * RelativeTolerance;
            this.InvResolutionX = 1d / resolutionX;
            this.InvResolutionY = 1d / resolutionY;
            this.Area = Math.Abs((east - west) * (north - south));
        }

        internal string SourcePath { get; }

        internal double West { get; }

        internal double East { get; }

        internal double South { get; }

        internal double North { get; }

        internal double ResolutionX { get; }

        internal double ResolutionY { get; }

        internal int Width { get; }

        internal int Height { get; }

        internal bool FullWorldLongitude { get; }

        internal double Epsilon { get; }

        internal double InvResolutionX { get; }

        internal double InvResolutionY { get; }

        internal double Area { get; }

        internal static GtxGrid Load(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.Length < 40)
            {
                throw new InvalidDataException("GTX file is too small.");
            }

            double yOrigin = ReadDoubleBigEndian(bytes, 0);
            double xOrigin = ReadDoubleBigEndian(bytes, 8);
            double yStep = ReadDoubleBigEndian(bytes, 16);
            double xStep = ReadDoubleBigEndian(bytes, 24);
            int rows = ReadInt32BigEndian(bytes, 32);
            int columns = ReadInt32BigEndian(bytes, 36);
            if (columns <= 0
                || rows <= 0
                || xOrigin < -360d
                || xOrigin > 360d
                || yOrigin < -90d
                || yOrigin > 90d)
            {
                throw new InvalidDataException("GTX header contains invalid extents.");
            }

            if (xOrigin >= 180d)
            {
                xOrigin -= 360d;
            }

            if (xStep == 0d || yStep == 0d)
            {
                throw new InvalidDataException("GTX header contains invalid resolution.");
            }

            long expectedDataBytes = (long)rows * columns * sizeof(float);
            if (bytes.Length < 40 + expectedDataBytes)
            {
                throw new InvalidDataException("GTX data is truncated.");
            }

            float[] values = new float[rows * columns];
            int offset = 40;
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = ReadSingleBigEndian(bytes, offset);
                offset += sizeof(float);
            }

            double west = DegreesToRadians(xOrigin);
            double south = DegreesToRadians(yOrigin);
            double resolutionX = DegreesToRadians(xStep);
            double resolutionY = DegreesToRadians(yStep);
            double east = west + (resolutionX * (columns - 1));
            double north = south + (resolutionY * (rows - 1));
            double worldWidth = Math.Abs(resolutionX) * columns;
            bool fullWorldLongitude = Math.Abs(worldWidth - TwoPi) <= (Math.Abs(resolutionX) * 1e-4d);
            return new GtxGrid(path, west, east, south, north, resolutionX, resolutionY, columns, rows, fullWorldLongitude, values);
        }

        internal bool Contains(double longitudeRadians, double latitudeRadians)
        {
            double lon = longitudeRadians;
            if (lon < this.West - this.Epsilon)
            {
                lon += TwoPi;
            }
            else if (lon > this.East + this.Epsilon)
            {
                lon -= TwoPi;
            }

            return lon >= this.West - this.Epsilon
                && lon <= this.East + this.Epsilon
                && latitudeRadians >= this.South - this.Epsilon
                && latitudeRadians <= this.North + this.Epsilon;
        }

        internal bool TryInterpolate(double longitudeRadians, double latitudeRadians, out double valueMmPerYear)
        {
            valueMmPerYear = 0d;
            double gridX = (longitudeRadians - this.West) * this.InvResolutionX;
            if (longitudeRadians < this.West)
            {
                if (this.FullWorldLongitude)
                {
                    gridX = PositiveModulo(gridX, this.Width);
                }
                else
                {
                    gridX = (longitudeRadians + TwoPi - this.West) * this.InvResolutionX;
                }
            }
            else if (longitudeRadians > this.East)
            {
                if (this.FullWorldLongitude)
                {
                    gridX = PositiveModulo(gridX, this.Width);
                }
                else
                {
                    gridX = (longitudeRadians - TwoPi - this.West) * this.InvResolutionX;
                }
            }

            double gridY = (latitudeRadians - this.South) * this.InvResolutionY;
            int gridIx = (int)Math.Floor(gridX);
            int gridIy = (int)Math.Floor(gridY);
            if (gridIx < 0 || gridIx >= this.Width || gridIy < 0 || gridIy >= this.Height)
            {
                return false;
            }

            double fractionX = gridX - gridIx;
            double fractionY = gridY - gridIy;
            int gridIx2 = gridIx + 1;
            if (gridIx2 >= this.Width)
            {
                gridIx2 = this.FullWorldLongitude ? 0 : this.Width - 1;
            }

            int gridIy2 = gridIy + 1;
            if (gridIy2 >= this.Height)
            {
                gridIy2 = this.Height - 1;
            }

            float valueA = this.GetValue(gridIx, gridIy);
            float valueB = this.GetValue(gridIx2, gridIy);
            float valueC = this.GetValue(gridIx, gridIy2);
            float valueD = this.GetValue(gridIx2, gridIy2);

            double gridXy = fractionX * fractionY;
            double weightA = 1d - fractionX - fractionY + gridXy;
            double weightB = fractionX - gridXy;
            double weightC = fractionY - gridXy;
            double weightD = gridXy;

            bool aValid = !IsNoData(valueA, 1d);
            bool bValid = !IsNoData(valueB, 1d);
            bool cValid = !IsNoData(valueC, 1d);
            bool dValid = !IsNoData(valueD, 1d);
            int validCount = (aValid ? 1 : 0) + (bValid ? 1 : 0) + (cValid ? 1 : 0) + (dValid ? 1 : 0);
            if (validCount == 0)
            {
                return false;
            }

            if (validCount == 4)
            {
                valueMmPerYear = (valueA * weightA) + (valueB * weightB) + (valueC * weightC) + (valueD * weightD);
                return TransformationMath.IsFinite(valueMmPerYear);
            }

            double weightedValue = 0d;
            double totalWeight = 0d;
            if (aValid)
            {
                weightedValue += valueA * weightA;
                totalWeight += weightA;
            }

            if (bValid)
            {
                weightedValue += valueB * weightB;
                totalWeight += weightB;
            }

            if (cValid)
            {
                weightedValue += valueC * weightC;
                totalWeight += weightC;
            }

            if (dValid)
            {
                weightedValue += valueD * weightD;
                totalWeight += weightD;
            }

            if (totalWeight == 0d)
            {
                return false;
            }

            valueMmPerYear = weightedValue / totalWeight;
            return TransformationMath.IsFinite(valueMmPerYear);
        }

        private static bool IsNoData(float value, double multiplier)
        {
            double scaled = value * multiplier;
            return scaled > 1000d || scaled < -1000d || value == TransformationMath.GtxNoDataSentinel;
        }

        private static double PositiveModulo(double value, int modulus)
        {
            if (modulus <= 0)
            {
                return value;
            }

            double result = value % modulus;
            if (result < 0d)
            {
                result += modulus;
            }

            return result;
        }

        private static int ReadInt32BigEndian(byte[] bytes, int offset)
        {
            return BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, sizeof(int)));
        }

        private static double ReadDoubleBigEndian(byte[] bytes, int offset)
        {
            long rawBits = BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(offset, sizeof(long)));
            return BitConverter.Int64BitsToDouble(rawBits);
        }

        private static float ReadSingleBigEndian(byte[] bytes, int offset)
        {
            int rawBits = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, sizeof(int)));
            Span<int> bitStorage = stackalloc int[1];
            bitStorage[0] = rawBits;
            return MemoryMarshal.Cast<int, float>(bitStorage)[0];
        }

        private float GetValue(int x, int y)
        {
            return this.values[(y * this.Width) + x];
        }
    }
}
