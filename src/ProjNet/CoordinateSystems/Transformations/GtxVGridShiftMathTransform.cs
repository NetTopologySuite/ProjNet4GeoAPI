// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

/// <summary>
/// Applies vertical datum shifts using one or more GTX grid files.
/// </summary>
/// <remarks>
/// <para>
/// GTX-based vertical shifts are applied by selecting the first grid covering
/// the input coordinate and bilinearly interpolating the surrounding raster
/// samples. The implementation preserves the historical PROJ convention that
/// the default forward multiplier is <c>-1</c> and mirrors PROJ's nodata
/// sentinel handling for GTX cells.
/// </para>
/// <para>
/// The runtime was independently verified against PROJ's published
/// <c>vgridshift</c> documentation and <c>vgridshift.cpp</c>.
/// </para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/transformations/vgridshift.html">PROJ: vgridshift.</seealso>
internal sealed class GtxVGridShiftMathTransform : MathTransform
{
    private const double RelativeTolerance = 1e-5d;

    /// <summary>
    /// Sentinel value used by GTX grids for nodata samples (matches PROJ <c>gtx.cpp</c> behavior).
    /// </summary>
    private const float GtxNoDataSentinel = -88.88880f;
    private readonly ReadOnlyCollection<GtxGrid> grids;
    private readonly double forwardMultiplier;
    private bool isInverted;
    private MathTransform? inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="GtxVGridShiftMathTransform"/> class.
    /// </summary>
    /// <param name="gridPaths">Ordered GTX grid file paths to load.</param>
    /// <param name="forwardMultiplier">Multiplier applied to interpolated values in forward direction.</param>
    internal GtxVGridShiftMathTransform(IReadOnlyList<string> gridPaths, double forwardMultiplier = -1d)
    {
        gridPaths = ArgumentGuard.ThrowIfNull(gridPaths, nameof(gridPaths));

        if (double.IsNaN(forwardMultiplier) || double.IsInfinity(forwardMultiplier))
        {
            ArgumentGuard.ThrowArgument("Forward multiplier must be finite.", nameof(forwardMultiplier));
        }

        var loadedGrids = new List<GtxGrid>(gridPaths.Count);
        for (int i = 0; i < gridPaths.Count; i++)
        {
            string path = gridPaths[i];
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            loadedGrids.Add(GtxGrid.Load(path));
        }

        if (loadedGrids.Count == 0)
        {
            ArgumentGuard.ThrowArgument("At least one GTX grid file must be provided.", nameof(gridPaths));
        }

        this.grids = new ReadOnlyCollection<GtxGrid>(loadedGrids);
        this.forwardMultiplier = forwardMultiplier;
    }

    private GtxVGridShiftMathTransform(GtxVGridShiftMathTransform source, bool isInverted)
    {
        source = ArgumentGuard.ThrowIfNull(source, nameof(source));

        this.grids = source.grids;
        this.forwardMultiplier = source.forwardMultiplier;
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
        this.inverse ??= new GtxVGridShiftMathTransform(this, !this.isInverted);

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
        if (!this.TryFindGridForPoint(x, y, out GtxGrid? selectedGridCandidate))
        {
            ArgumentGuard.ThrowArgument("Coordinate is outside the vertical grid extent.");
        }

        GtxGrid selectedGrid = ArgumentGuard.ThrowIfNull(selectedGridCandidate, nameof(selectedGridCandidate));
        double value = InterpolateValue(selectedGrid, x, y, this.forwardMultiplier);
        if (!this.isInverted)
        {
            z += value;
            return;
        }

        z -= value;
    }

    private bool TryFindGridForPoint(double longitude, double latitude, [NotNullWhen(true)] out GtxGrid? grid)
    {
        for (int i = 0; i < this.grids.Count; i++)
        {
            if (this.grids[i].Contains(longitude, latitude))
            {
                grid = this.grids[i];
                return true;
            }
        }

        grid = null;
        return false;
    }

    private static double InterpolateValue(GtxGrid grid, double longitude, double latitude, double multiplier)
    {
        double gridX = (longitude - grid.West) * grid.InvResolutionX;
        if (longitude < grid.West)
        {
            gridX = (longitude + 360d - grid.West) * grid.InvResolutionX;
        }
        else if (longitude > grid.East)
        {
            gridX = (longitude - 360d - grid.West) * grid.InvResolutionX;
        }

        double gridY = (latitude - grid.South) * grid.InvResolutionY;

        int indexX = (int)Math.Floor(gridX);
        int indexY = (int)Math.Floor(gridY);
        if (indexX < 0 || indexX >= grid.Width || indexY < 0 || indexY >= grid.Height)
        {
            ArgumentGuard.ThrowArgument("Coordinate is outside the vertical grid extent.");
        }

        double fractionX = gridX - indexX;
        double fractionY = gridY - indexY;

        int indexX2 = indexX + 1;
        if (indexX2 >= grid.Width)
        {
            if (indexX2 == grid.Width && fractionX < 10d * RelativeTolerance)
            {
                indexX = grid.Width - 2;
                indexX2 = grid.Width - 1;
                fractionX = 1d;
            }
            else
            {
                ArgumentGuard.ThrowArgument("Coordinate is outside the vertical grid extent.");
            }
        }

        int indexY2 = indexY + 1;
        if (indexY2 >= grid.Height)
        {
            if (indexY2 == grid.Height && fractionY < 10d * RelativeTolerance)
            {
                indexY = grid.Height - 2;
                indexY2 = grid.Height - 1;
                fractionY = 1d;
            }
            else
            {
                ArgumentGuard.ThrowArgument("Coordinate is outside the vertical grid extent.");
            }
        }

        float valueA = grid.GetValue(indexX, indexY);
        float valueB = grid.GetValue(indexX2, indexY);
        float valueC = grid.GetValue(indexX, indexY2);
        float valueD = grid.GetValue(indexX2, indexY2);

        bool aValid = !IsNoData(valueA, multiplier);
        bool bValid = !IsNoData(valueB, multiplier);
        bool cValid = !IsNoData(valueC, multiplier);
        bool dValid = !IsNoData(valueD, multiplier);
        int validCount = (aValid ? 1 : 0) + (bValid ? 1 : 0) + (cValid ? 1 : 0) + (dValid ? 1 : 0);
        if (validCount == 0)
        {
            ArgumentGuard.ThrowArgument("Coordinate falls on vertical grid nodata region.");
        }

        double gridXy = fractionX * fractionY;
        double weightA = 1d - fractionX - fractionY + gridXy;
        double weightB = fractionX - gridXy;
        double weightC = fractionY - gridXy;
        double weightD = gridXy;

        if (validCount == 4)
        {
            double value = (valueA * weightA) + (valueB * weightB) + (valueC * weightC) + (valueD * weightD);
            return value * multiplier;
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
            ArgumentGuard.ThrowArgument("Coordinate falls on vertical grid nodata region.");
        }

        return (weightedValue / totalWeight) * multiplier;
    }

    private static bool IsNoData(float value, double multiplier)
    {
        double scaled = value * multiplier;
        return scaled > 1000d || scaled < -1000d || value == GtxNoDataSentinel;
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
            this.values = values;
            this.Epsilon = (Math.Abs(resolutionX) + Math.Abs(resolutionY)) * RelativeTolerance;
            this.InvResolutionX = 1d / resolutionX;
            this.InvResolutionY = 1d / resolutionY;
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
            int cols = ReadInt32BigEndian(bytes, 36);

            if (cols <= 0 || rows <= 0 || xOrigin < -360d || xOrigin > 360d || yOrigin < -90d || yOrigin > 90d)
            {
                throw new InvalidDataException("GTX header contains invalid extents.");
            }

            if (xStep == 0d || yStep == 0d)
            {
                throw new InvalidDataException("GTX header contains invalid resolution.");
            }

            if (xOrigin >= 180d)
            {
                xOrigin -= 360d;
            }

            long expectedDataBytes = (long)rows * cols * sizeof(float);
            if (bytes.Length < 40 + expectedDataBytes)
            {
                throw new InvalidDataException("GTX file data is truncated.");
            }

            float[] values = new float[rows * cols];
            int offset = 40;
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = ReadSingleBigEndian(bytes, offset);
                offset += sizeof(float);
            }

            double west = xOrigin;
            double south = yOrigin;
            double east = xOrigin + (xStep * (cols - 1));
            double north = yOrigin + (yStep * (rows - 1));
            return new GtxGrid(
                path,
                west,
                east,
                south,
                north,
                xStep,
                yStep,
                cols,
                rows,
                values);
        }

        internal bool Contains(double longitude, double latitude)
        {
            double lon = longitude;
            if (lon < this.West - this.Epsilon)
            {
                lon += 360d;
            }
            else if (lon > this.East + this.Epsilon)
            {
                lon -= 360d;
            }

            return lon >= this.West - this.Epsilon
                && lon <= this.East + this.Epsilon
                && latitude >= this.South - this.Epsilon
                && latitude <= this.North + this.Epsilon;
        }

        internal float GetValue(int x, int y)
        {
            return this.values[(y * this.Width) + x];
        }

        private static double ReadDoubleBigEndian(byte[] bytes, int offset)
        {
            long rawBits = BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(offset, sizeof(long)));
            Span<long> bitStorage = stackalloc long[1];
            bitStorage[0] = rawBits;
            return MemoryMarshal.Cast<long, double>(bitStorage)[0];
        }

        private static int ReadInt32BigEndian(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24)
                | (bytes[offset + 1] << 16)
                | (bytes[offset + 2] << 8)
                | bytes[offset + 3];
        }

        private static float ReadSingleBigEndian(byte[] bytes, int offset)
        {
            int rawBits = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, sizeof(int)));
            Span<int> bitStorage = stackalloc int[1];
            bitStorage[0] = rawBits;
            return MemoryMarshal.Cast<int, float>(bitStorage)[0];
        }
    }
}
