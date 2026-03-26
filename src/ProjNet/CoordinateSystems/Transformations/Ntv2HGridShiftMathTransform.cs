// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Applies horizontal datum shifts using one or more NTv2 grid files.
/// </summary>
[Serializable]
internal sealed class Ntv2HGridShiftMathTransform : MathTransform
{
    private const double ArcSecondToDegree = 1d / 3600d;
    private const double RelativeTolerance = 1e-5d;
    private const double InverseTolerance = 1e-12d;
    private const int MaxInverseIterations = 10;

    private readonly ReadOnlyCollection<Ntv2GridSet> gridSets;
    private bool isInverted;
    private MathTransform? inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="Ntv2HGridShiftMathTransform"/> class.
    /// </summary>
    /// <param name="gridPaths">Ordered NTv2 grid file paths to load.</param>
    internal Ntv2HGridShiftMathTransform(IReadOnlyList<string> gridPaths)
    {
        gridPaths = ArgumentGuard.ThrowIfNull(gridPaths, nameof(gridPaths));

        var sets = new List<Ntv2GridSet>(gridPaths.Count);
        for (int i = 0; i < gridPaths.Count; i++)
        {
            string path = gridPaths[i];
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            sets.Add(Ntv2GridSet.Load(path));
        }

        if (sets.Count == 0)
        {
            ArgumentGuard.ThrowArgument("At least one NTv2 grid file must be provided.", nameof(gridPaths));
        }

        this.gridSets = new ReadOnlyCollection<Ntv2GridSet>(sets);
    }

    private Ntv2HGridShiftMathTransform(Ntv2HGridShiftMathTransform source, bool isInverted)
    {
        source = ArgumentGuard.ThrowIfNull(source, nameof(source));
        this.gridSets = source.gridSets;
        this.isInverted = isInverted;
    }

    /// <inheritdoc />
    public override int DimSource => 3;

    /// <inheritdoc />
    public override int DimTarget => 3;

    /// <inheritdoc />
    public override string WKT => throw new NotImplementedException();

    /// <inheritdoc />
    public override string XML => throw new NotImplementedException();

    /// <inheritdoc />
    public override bool Identity()
    {
        return false;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new Ntv2HGridShiftMathTransform(this, !this.isInverted);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    public override void Invert()
    {
        this.isInverted = !this.isInverted;
        this.inverse = null;
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        if (!this.TryFindGridForPoint(x, y, out Ntv2Grid selectedGrid))
        {
            ArgumentGuard.ThrowArgument("Coordinate is outside the horizontal grid extent.");
        }

        if (!this.isInverted)
        {
            (double lonShift, double latShift) = InterpolateShift(selectedGrid, x, y, true);
            x += lonShift;
            y += latShift;
            return;
        }

        InverseTransform(ref x, ref y, selectedGrid);
    }

    private static void InverseTransform(ref double longitude, ref double latitude, Ntv2Grid initialGrid)
    {
        double epsilon = initialGrid.Epsilon;
        double tbLon = NormalizeLongitudeToGrid(longitude, initialGrid.West, initialGrid.East, epsilon);
        double tbLat = latitude - initialGrid.South;
        (double firstLongShift, double firstLatShift) = InterpolateNormalized(initialGrid, tbLon, tbLat, true);

        double tLon = tbLon - firstLongShift;
        double tLat = tbLat - firstLatShift;
        double deltaLon = double.MaxValue;
        double deltaLat = double.MaxValue;
        int iterations = MaxInverseIterations;

        while (iterations-- > 0)
        {
            (double iterLongShift, double iterLatShift) = InterpolateNormalized(initialGrid, tLon, tLat, true);

            deltaLon = tLon + iterLongShift - tbLon;
            deltaLat = tLat + iterLatShift - tbLat;
            tLon -= deltaLon;
            tLat -= deltaLat;

            if ((deltaLon * deltaLon) + (deltaLat * deltaLat) <= (InverseTolerance * InverseTolerance))
            {
                longitude = NormalizeLongitude(tLon + initialGrid.West);
                latitude = tLat + initialGrid.South;
                return;
            }
        }

        ArgumentGuard.ThrowArgument("Inverse horizontal grid shift did not converge.");
    }

    private static (double LonShift, double LatShift) InterpolateShift(Ntv2Grid grid, double longitude, double latitude, bool compensateNtConvention)
    {
        double normalizedLongitude = NormalizeLongitudeToGrid(longitude, grid.West, grid.East, grid.Epsilon);
        double normalizedLatitude = latitude - grid.South;
        return InterpolateNormalized(grid, normalizedLongitude, normalizedLatitude, compensateNtConvention);
    }

    private static (double LonShift, double LatShift) InterpolateNormalized(Ntv2Grid grid, double normalizedLongitude, double normalizedLatitude, bool compensateNtConvention)
    {
        double x = normalizedLongitude / grid.ResolutionX;
        double y = normalizedLatitude / grid.ResolutionY;

        int indexX = (int)Math.Floor(x);
        int indexY = (int)Math.Floor(y);
        double fractionX = x - indexX;
        double fractionY = y - indexY;

        if (indexX < 0)
        {
            if (indexX == -1 && fractionX > 1d - (10d * RelativeTolerance))
            {
                indexX++;
                fractionX = 0d;
            }
            else
            {
                ArgumentGuard.ThrowArgument("Coordinate is outside the horizontal grid extent.");
            }
        }
        else if (indexX + 1 >= grid.Width)
        {
            if (indexX + 1 == grid.Width && fractionX < 10d * RelativeTolerance)
            {
                indexX--;
                fractionX = 1d;
            }
            else
            {
                ArgumentGuard.ThrowArgument("Coordinate is outside the horizontal grid extent.");
            }
        }

        if (indexY < 0)
        {
            if (indexY == -1 && fractionY > 1d - (10d * RelativeTolerance))
            {
                indexY++;
                fractionY = 0d;
            }
            else
            {
                ArgumentGuard.ThrowArgument("Coordinate is outside the horizontal grid extent.");
            }
        }
        else if (indexY + 1 >= grid.Height)
        {
            if (indexY + 1 == grid.Height && fractionY < 10d * RelativeTolerance)
            {
                indexY--;
                fractionY = 1d;
            }
            else
            {
                ArgumentGuard.ThrowArgument("Coordinate is outside the horizontal grid extent.");
            }
        }

        (double f00Lon, double f00Lat) = grid.GetShift(indexX, indexY, compensateNtConvention);
        (double f10Lon, double f10Lat) = grid.GetShift(indexX + 1, indexY, compensateNtConvention);
        (double f01Lon, double f01Lat) = grid.GetShift(indexX, indexY + 1, compensateNtConvention);
        (double f11Lon, double f11Lat) = grid.GetShift(indexX + 1, indexY + 1, compensateNtConvention);

        double m10 = fractionX;
        double m11 = m10;
        double m01 = 1d - fractionX;
        double m00 = m01;
        m11 *= fractionY;
        m01 *= fractionY;
        fractionY = 1d - fractionY;
        m00 *= fractionY;
        m10 *= fractionY;

        double lonShift = (m00 * f00Lon) + (m10 * f10Lon) + (m01 * f01Lon) + (m11 * f11Lon);
        double latShift = (m00 * f00Lat) + (m10 * f10Lat) + (m01 * f01Lat) + (m11 * f11Lat);
        return (lonShift, latShift);
    }

    private static double NormalizeLongitudeToGrid(double longitude, double west, double east, double epsilon)
    {
        double normalized = longitude - west;
        if (normalized + epsilon < 0d)
        {
            normalized += 360d;
        }
        else if (normalized - epsilon > east - west)
        {
            normalized -= 360d;
        }

        return normalized;
    }

    private static double NormalizeLongitude(double longitude)
    {
        double normalized = longitude;
        while (normalized < -180d)
        {
            normalized += 360d;
        }

        while (normalized > 180d)
        {
            normalized -= 360d;
        }

        return normalized;
    }

    private bool TryFindGridForPoint(double longitude, double latitude, [NotNullWhen(true)] out Ntv2Grid? grid)
    {
        grid = null;
        for (int setIndex = 0; setIndex < this.gridSets.Count; setIndex++)
        {
            if (this.gridSets[setIndex].TryFindGrid(longitude, latitude, out Ntv2Grid? candidate))
            {
                grid = candidate;
                return true;
            }
        }

        return false;
    }

    [Serializable]
    private sealed class Ntv2GridSet
    {
        private readonly IReadOnlyList<Ntv2Grid> rootGrids;

        private Ntv2GridSet(IReadOnlyList<string> sourcePaths, IReadOnlyList<Ntv2Grid> rootGrids)
        {
            this.SourcePaths = sourcePaths;
            this.rootGrids = rootGrids;
        }

        internal IReadOnlyList<string> SourcePaths { get; }

        internal static Ntv2GridSet Load(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.Length < 16 * 22)
            {
                throw new InvalidDataException("NTv2 file is too small.");
            }

            bool littleEndian = BitConverter.ToInt32(bytes, 8) == 11;
            bool bigEndian = ReadInt32(bytes, 8, false) == 11;
            if (!littleEndian && !bigEndian)
            {
                throw new InvalidDataException("Unable to detect NTv2 byte order.");
            }

            bool isLittleEndian = littleEndian;
            int overviewRecordCount = ReadInt32(bytes, 8, isLittleEndian);
            if (overviewRecordCount != 11)
            {
                throw new InvalidDataException("Unsupported NTv2 overview header.");
            }

            int gridRecordCount = ReadInt32(bytes, 24, isLittleEndian);
            if (gridRecordCount != 11)
            {
                throw new InvalidDataException("Unsupported NTv2 grid header.");
            }

            int gridFileCount = ReadInt32(bytes, 40, isLittleEndian);
            if (gridFileCount <= 0)
            {
                throw new InvalidDataException("NTv2 file does not contain grids.");
            }

            int position = overviewRecordCount * 16;
            var allGrids = new List<Ntv2Grid>(gridFileCount);
            var byName = new Dictionary<string, Ntv2Grid>(StringComparer.Ordinal);
            for (int gridIndex = 0; gridIndex < gridFileCount; gridIndex++)
            {
                if (position + (gridRecordCount * 16) > bytes.Length)
                {
                    throw new InvalidDataException("NTv2 grid header exceeds file size.");
                }

                string subName = ReadAscii(bytes, position + (0 * 16) + 8, 8);
                string parentName = ReadAscii(bytes, position + (1 * 16) + 8, 8);

                double south = ReadDouble(bytes, position + (4 * 16) + 8, isLittleEndian) * ArcSecondToDegree;
                double north = ReadDouble(bytes, position + (5 * 16) + 8, isLittleEndian) * ArcSecondToDegree;
                double east = -ReadDouble(bytes, position + (6 * 16) + 8, isLittleEndian) * ArcSecondToDegree;
                double west = -ReadDouble(bytes, position + (7 * 16) + 8, isLittleEndian) * ArcSecondToDegree;
                double resolutionY = ReadDouble(bytes, position + (8 * 16) + 8, isLittleEndian) * ArcSecondToDegree;
                double resolutionX = ReadDouble(bytes, position + (9 * 16) + 8, isLittleEndian) * ArcSecondToDegree;
                int gsCount = ReadInt32(bytes, position + (10 * 16) + 8, isLittleEndian);

                if (resolutionX <= 0d || resolutionY <= 0d)
                {
                    throw new InvalidDataException("NTv2 grid has invalid resolution.");
                }

                int width = (int)(Math.Abs(((east - west) / resolutionX) + 0.5d) + 1d);
                int height = (int)(Math.Abs(((north - south) / resolutionY) + 0.5d) + 1d);
                if (width <= 1 || height <= 1)
                {
                    throw new InvalidDataException("NTv2 grid dimensions are invalid.");
                }

                if (gsCount / width != height)
                {
                    throw new InvalidDataException("NTv2 GS_COUNT does not match grid dimensions.");
                }

                int dataOffset = position + (gridRecordCount * 16);
                int dataLength = gsCount * 16;
                if (dataOffset < 0 || dataOffset + dataLength > bytes.Length)
                {
                    throw new InvalidDataException("NTv2 grid data exceeds file size.");
                }

                float[] latShiftSeconds = new float[gsCount];
                float[] lonShiftSeconds = new float[gsCount];
                for (int i = 0; i < gsCount; i++)
                {
                    int cellOffset = dataOffset + (i * 16);
                    latShiftSeconds[i] = ReadSingle(bytes, cellOffset, isLittleEndian);
                    lonShiftSeconds[i] = ReadSingle(bytes, cellOffset + 4, isLittleEndian);
                }

                var grid = new Ntv2Grid(
                    string.IsNullOrWhiteSpace(subName) ? "GRID_" + gridIndex.ToString(CultureInfo.InvariantCulture) : subName,
                    parentName,
                    west,
                    east,
                    south,
                    north,
                    resolutionX,
                    resolutionY,
                    width,
                    height,
                    latShiftSeconds,
                    lonShiftSeconds);
                allGrids.Add(grid);
                byName[grid.Name] = grid;

                position = dataOffset + dataLength;
            }

            var rootGrids = new List<Ntv2Grid>(allGrids.Count);
            for (int i = 0; i < allGrids.Count; i++)
            {
                Ntv2Grid grid = allGrids[i];
                if (!string.IsNullOrWhiteSpace(grid.ParentName) && byName.TryGetValue(grid.ParentName, out Ntv2Grid parent))
                {
                    parent.AddChild(grid);
                }
                else
                {
                    rootGrids.Add(grid);
                }
            }

            return new Ntv2GridSet(new[] { path }, new ReadOnlyCollection<Ntv2Grid>(rootGrids));
        }

        internal bool TryFindGrid(double longitude, double latitude, [NotNullWhen(true)] out Ntv2Grid? grid)
        {
            grid = null;
            for (int i = 0; i < this.rootGrids.Count; i++)
            {
                Ntv2Grid root = this.rootGrids[i];
                if (!root.Contains(longitude, latitude))
                {
                    continue;
                }

                grid = root.FindDeepest(longitude, latitude);
                return true;
            }

            return false;
        }

        private static int ReadInt32(byte[] bytes, int offset, bool littleEndian)
        {
            if (littleEndian == BitConverter.IsLittleEndian)
            {
                return BitConverter.ToInt32(bytes, offset);
            }

            return (bytes[offset] << 24)
                | (bytes[offset + 1] << 16)
                | (bytes[offset + 2] << 8)
                | bytes[offset + 3];
        }

        private static double ReadDouble(byte[] bytes, int offset, bool littleEndian)
        {
            byte[] buffer = new byte[8];
            Buffer.BlockCopy(bytes, offset, buffer, 0, 8);
            if (littleEndian != BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }

            return BitConverter.ToDouble(buffer, 0);
        }

        private static float ReadSingle(byte[] bytes, int offset, bool littleEndian)
        {
            byte[] buffer = new byte[4];
            Buffer.BlockCopy(bytes, offset, buffer, 0, 4);
            if (littleEndian != BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }

            return BitConverter.ToSingle(buffer, 0);
        }

        private static string ReadAscii(byte[] bytes, int offset, int length)
        {
            return Encoding.ASCII.GetString(bytes, offset, length).TrimEnd('\0', ' ');
        }
    }

    [Serializable]
    private sealed class Ntv2Grid
    {
        private readonly List<Ntv2Grid> children = new();
        private readonly float[] latShiftSeconds;
        private readonly float[] lonShiftSeconds;

        internal Ntv2Grid(
            string name,
            string parentName,
            double west,
            double east,
            double south,
            double north,
            double resolutionX,
            double resolutionY,
            int width,
            int height,
            float[] latShiftSeconds,
            float[] lonShiftSeconds)
        {
            this.Name = name;
            this.ParentName = parentName;
            this.West = west;
            this.East = east;
            this.South = south;
            this.North = north;
            this.ResolutionX = resolutionX;
            this.ResolutionY = resolutionY;
            this.Width = width;
            this.Height = height;
            this.latShiftSeconds = latShiftSeconds;
            this.lonShiftSeconds = lonShiftSeconds;
            this.Epsilon = (resolutionX + resolutionY) * RelativeTolerance;
        }

        internal string Name { get; }

        internal string ParentName { get; }

        internal double West { get; }

        internal double East { get; }

        internal double South { get; }

        internal double North { get; }

        internal double ResolutionX { get; }

        internal double ResolutionY { get; }

        internal int Width { get; }

        internal int Height { get; }

        internal double Epsilon { get; }

        internal void AddChild(Ntv2Grid child)
        {
            this.children.Add(child);
        }

        internal Ntv2Grid FindDeepest(double longitude, double latitude)
        {
            for (int i = 0; i < this.children.Count; i++)
            {
                Ntv2Grid child = this.children[i];
                if (!child.Contains(longitude, latitude))
                {
                    continue;
                }

                return child.FindDeepest(longitude, latitude);
            }

            return this;
        }

        internal bool Contains(double longitude, double latitude)
        {
            return IsPointInExtent(longitude, latitude, this.West, this.East, this.South, this.North, this.Epsilon);
        }

        internal (double LonShift, double LatShift) GetShift(int x, int y, bool compensateNtConvention)
        {
            int fileX = (this.Width - 1) - x;
            int index = (y * this.Width) + fileX;
            double latShift = this.latShiftSeconds[index] * ArcSecondToDegree;
            double lonShift = this.lonShiftSeconds[index] * ArcSecondToDegree;
            if (compensateNtConvention)
            {
                lonShift = -lonShift;
            }

            return (lonShift, latShift);
        }

        private static bool IsPointInExtent(double longitude, double latitude, double west, double east, double south, double north, double epsilon)
        {
            double lon = longitude;
            if (lon < west - epsilon)
            {
                lon += 360d;
            }
            else if (lon > east + epsilon)
            {
                lon -= 360d;
            }

            return lon >= west - epsilon
                && lon <= east + epsilon
                && latitude >= south - epsilon
                && latitude <= north + epsilon;
        }
    }
}
