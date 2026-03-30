// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

/// <summary>
/// Applies vertical datum shifts loaded from GeoTIFF grid files.
/// </summary>
[Serializable]
internal sealed class GeoTiffVGridShiftMathTransform : MathTransform
{
    private const double RelativeTolerance = 1e-5d;
    private readonly ReadOnlyCollection<VerticalGrid> grids;
    private readonly double forwardMultiplier;
    private bool isInverted;
    private MathTransform? inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeoTiffVGridShiftMathTransform"/> class.
    /// </summary>
    /// <param name="gridPaths">Ordered GeoTIFF grid file paths to load.</param>
    /// <param name="forwardMultiplier">Multiplier applied to interpolated shift values in the forward direction.</param>
    internal GeoTiffVGridShiftMathTransform(IReadOnlyList<string> gridPaths, double forwardMultiplier)
    {
        gridPaths = ArgumentGuard.ThrowIfNull(gridPaths, nameof(gridPaths));

        if (double.IsNaN(forwardMultiplier) || double.IsInfinity(forwardMultiplier))
        {
            ArgumentGuard.ThrowArgument("Forward multiplier must be finite.", nameof(forwardMultiplier));
        }

        var loadedGrids = new List<VerticalGrid>(gridPaths.Count);
        for (int i = 0; i < gridPaths.Count; i++)
        {
            string path = gridPaths[i];
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            loadedGrids.AddRange(GeoTiffGridLoader.LoadVertical(path));
        }

        if (loadedGrids.Count == 0)
        {
            ArgumentGuard.ThrowArgument("No vertical grid could be loaded from GeoTIFF input.", nameof(gridPaths));
        }

        this.grids = new ReadOnlyCollection<VerticalGrid>(
            [.. loadedGrids.OrderBy(grid => grid.Area, Comparer<double>.Default)]);
        this.forwardMultiplier = forwardMultiplier;
    }

    private GeoTiffVGridShiftMathTransform(GeoTiffVGridShiftMathTransform source, bool isInverted)
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
        this.inverse ??= new GeoTiffVGridShiftMathTransform(this, !this.isInverted);

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
        if (!this.TryFindGridForPoint(x, y, out VerticalGrid? gridCandidate))
        {
            ArgumentGuard.ThrowArgument("Coordinate is outside the vertical GeoTIFF grid extent.");
        }

        VerticalGrid grid = ArgumentGuard.ThrowIfNull(gridCandidate, nameof(gridCandidate));
        double shift = InterpolateValue(grid, x, y, this.forwardMultiplier);
        if (!this.isInverted)
        {
            z += shift;
            return;
        }

        z -= shift;
    }

    private static double InterpolateValue(VerticalGrid grid, double longitude, double latitude, double multiplier)
    {
        if (!grid.TryMapToGridCoordinates(longitude, latitude, out double gridX, out double gridY))
        {
            ArgumentGuard.ThrowArgument("Coordinate is outside the vertical GeoTIFF grid extent.");
        }

        int indexX = (int)Math.Floor(gridX);
        int indexY = (int)Math.Floor(gridY);
        double fractionX = gridX - indexX;
        double fractionY = gridY - indexY;
        NormalizeInterpolationCell(grid.Width, ref indexX, ref fractionX);
        NormalizeInterpolationCell(grid.Height, ref indexY, ref fractionY);

        int indexX2 = indexX + 1;
        int indexY2 = indexY + 1;

        double valueA = grid.GetValue(indexX, indexY);
        double valueB = grid.GetValue(indexX2, indexY);
        double valueC = grid.GetValue(indexX, indexY2);
        double valueD = grid.GetValue(indexX2, indexY2);

        bool aValid = !grid.IsNoData(valueA);
        bool bValid = !grid.IsNoData(valueB);
        bool cValid = !grid.IsNoData(valueC);
        bool dValid = !grid.IsNoData(valueD);
        int validCount = (aValid ? 1 : 0) + (bValid ? 1 : 0) + (cValid ? 1 : 0) + (dValid ? 1 : 0);
        if (validCount == 0)
        {
            ArgumentGuard.ThrowArgument("Coordinate falls on vertical GeoTIFF grid nodata region.");
        }

        double xy = fractionX * fractionY;
        double wA = 1d - fractionX - fractionY + xy;
        double wB = fractionX - xy;
        double wC = fractionY - xy;
        double wD = xy;

        if (validCount == 4)
        {
            return ((valueA * wA) + (valueB * wB) + (valueC * wC) + (valueD * wD)) * multiplier;
        }

        double weightedValue = 0d;
        double totalWeight = 0d;
        if (aValid)
        {
            weightedValue += valueA * wA;
            totalWeight += wA;
        }

        if (bValid)
        {
            weightedValue += valueB * wB;
            totalWeight += wB;
        }

        if (cValid)
        {
            weightedValue += valueC * wC;
            totalWeight += wC;
        }

        if (dValid)
        {
            weightedValue += valueD * wD;
            totalWeight += wD;
        }

        if (totalWeight == 0d)
        {
            ArgumentGuard.ThrowArgument("Coordinate falls on vertical GeoTIFF grid nodata region.");
        }

        return (weightedValue / totalWeight) * multiplier;
    }

    private static void NormalizeInterpolationCell(int size, ref int index, ref double fraction)
    {
        if (index < 0)
        {
            if (index == -1 && fraction > 1d - (10d * RelativeTolerance))
            {
                index = 0;
                fraction = 0d;
                return;
            }

            ArgumentGuard.ThrowArgument("Coordinate is outside the vertical GeoTIFF grid extent.");
        }

        if (index + 1 < size)
        {
            return;
        }

        if (index + 1 == size && fraction < 10d * RelativeTolerance)
        {
            index = size - 2;
            fraction = 1d;
            return;
        }

        ArgumentGuard.ThrowArgument("Coordinate is outside the vertical GeoTIFF grid extent.");
    }

    private bool TryFindGridForPoint(double longitude, double latitude, [NotNullWhen(true)] out VerticalGrid? grid)
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

    /// <summary>
    /// Represents a single vertical-shift band loaded from a GeoTIFF grid file.
    /// </summary>
    [Serializable]
    internal sealed class VerticalGrid : BaseGeoGrid
    {
        private readonly int sampleIndex;
        private readonly double? noDataValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="VerticalGrid"/> class.
        /// </summary>
        /// <param name="sourcePath">Path of the source GeoTIFF file.</param>
        /// <param name="width">Number of grid columns.</param>
        /// <param name="height">Number of grid rows.</param>
        /// <param name="area">Geographic coverage area used to order grids by specificity.</param>
        /// <param name="epsilon">Tolerance in degrees used for boundary checks.</param>
        /// <param name="west">Western boundary in degrees.</param>
        /// <param name="east">Eastern boundary in degrees.</param>
        /// <param name="south">Southern boundary in degrees.</param>
        /// <param name="north">Northern boundary in degrees.</param>
        /// <param name="a">Affine coefficient: longitude change per grid column.</param>
        /// <param name="b">Affine coefficient: longitude change per grid row.</param>
        /// <param name="c">Affine coefficient: longitude of the grid origin.</param>
        /// <param name="d">Affine coefficient: latitude change per grid column.</param>
        /// <param name="e">Affine coefficient: latitude change per grid row.</param>
        /// <param name="f">Affine coefficient: latitude of the grid origin.</param>
        /// <param name="sampleData">The raster sample data store.</param>
        /// <param name="sampleIndex">Zero-based band index of the vertical-shift sample.</param>
        /// <param name="noDataValue">No-data sentinel value, or <see langword="null"/> if none is defined.</param>
        internal VerticalGrid(
            string sourcePath,
            int width,
            int height,
            double area,
            double epsilon,
            double west,
            double east,
            double south,
            double north,
            double a,
            double b,
            double c,
            double d,
            double e,
            double f,
            SampleData sampleData,
            int sampleIndex,
            double? noDataValue)
            : base(sourcePath, width, height, area, epsilon, west, east, south, north, a, b, c, d, e, f, sampleData)
        {
            this.sampleIndex = sampleIndex;
            this.noDataValue = noDataValue;
        }

        /// <summary>
        /// Gets the scaled vertical shift value at the specified grid cell.
        /// </summary>
        /// <param name="x">Column index.</param>
        /// <param name="y">Row index.</param>
        /// <returns>The raw sample value at (<paramref name="x"/>, <paramref name="y"/>).</returns>
        internal double GetValue(int x, int y)
        {
            return this.GetSampleValue(this.sampleIndex, x, y);
        }

        /// <summary>
        /// Determines whether the specified sample value equals the no-data sentinel.
        /// </summary>
        /// <param name="value">The sample value to test.</param>
        /// <returns><see langword="true"/> when <paramref name="value"/> matches the no-data value; otherwise <see langword="false"/>.</returns>
        internal bool IsNoData(double value)
        {
            return this.noDataValue.HasValue && Math.Abs(value - this.noDataValue.Value) <= 1e-4d;
        }
    }
}
