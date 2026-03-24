// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

/// <summary>
/// Represents a documented type.
/// </summary>
[Serializable]
internal sealed class GeoTiffVGridShiftMathTransform : MathTransform
{
    private const double RelativeTolerance = 1e-5d;
    private readonly ReadOnlyCollection<VerticalGrid> grids;
    private readonly double forwardMultiplier;
    private bool isInverted;
    private MathTransform inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeoTiffVGridShiftMathTransform"/> class.
    /// </summary>
    /// <param name="gridPaths">The gridPaths value.</param>
    /// <param name="forwardMultiplier">The forwardMultiplier value.</param>
    internal GeoTiffVGridShiftMathTransform(IReadOnlyList<string> gridPaths, double forwardMultiplier)
    {
        if (gridPaths is null)
        {
            throw new ArgumentNullException(nameof(gridPaths));
        }

        if (double.IsNaN(forwardMultiplier) || double.IsInfinity(forwardMultiplier))
        {
            throw new ArgumentException("Forward multiplier must be finite.", nameof(forwardMultiplier));
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
            throw new ArgumentException("No vertical grid could be loaded from GeoTIFF input.", nameof(gridPaths));
        }

        this.grids = new ReadOnlyCollection<VerticalGrid>(
            loadedGrids.OrderBy(grid => grid.Area, Comparer<double>.Default).ToArray());
        this.forwardMultiplier = forwardMultiplier;
    }

    private GeoTiffVGridShiftMathTransform(GeoTiffVGridShiftMathTransform source, bool isInverted)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

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
        if (this.inverse is null)
        {
            this.inverse = new GeoTiffVGridShiftMathTransform(this, !this.isInverted);
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
        if (!this.TryFindGridForPoint(x, y, out VerticalGrid grid))
        {
            throw new ArgumentException("Coordinate is outside the vertical GeoTIFF grid extent.");
        }

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
            throw new ArgumentException("Coordinate is outside the vertical GeoTIFF grid extent.");
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
            throw new ArgumentException("Coordinate falls on vertical GeoTIFF grid nodata region.");
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
            throw new ArgumentException("Coordinate falls on vertical GeoTIFF grid nodata region.");
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

            throw new ArgumentException("Coordinate is outside the vertical GeoTIFF grid extent.");
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

        throw new ArgumentException("Coordinate is outside the vertical GeoTIFF grid extent.");
    }

    private bool TryFindGridForPoint(double longitude, double latitude, out VerticalGrid grid)
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
    /// Represents a documented type.
    /// </summary>
    [Serializable]
    internal sealed class VerticalGrid : BaseGeoGrid
    {
        private readonly int sampleIndex;
        private readonly double? noDataValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeoTiffVGridShiftMathTransform.VerticalGrid"/> class.
        /// </summary>
        /// <param name="sourcePath">The sourcePath value.</param>
        /// <param name="width">The width value.</param>
        /// <param name="height">The height value.</param>
        /// <param name="area">The area value.</param>
        /// <param name="epsilon">The epsilon value.</param>
        /// <param name="west">The west value.</param>
        /// <param name="east">The east value.</param>
        /// <param name="south">The south value.</param>
        /// <param name="north">The north value.</param>
        /// <param name="a">The a value.</param>
        /// <param name="b">The b value.</param>
        /// <param name="c">The c value.</param>
        /// <param name="d">The d value.</param>
        /// <param name="e">The e value.</param>
        /// <param name="f">The f value.</param>
        /// <param name="sampleData">The sampleData value.</param>
        /// <param name="sampleIndex">The sampleIndex value.</param>
        /// <param name="noDataValue">The noDataValue value.</param>
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
        /// Performs the documented operation.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns>The computed value.</returns>
        internal double GetValue(int x, int y)
        {
            return this.GetSampleValue(this.sampleIndex, x, y);
        }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="value">The value value.</param>
        /// <returns>The computed value.</returns>
        internal bool IsNoData(double value)
        {
            if (!this.noDataValue.HasValue)
            {
                return false;
            }

            return Math.Abs(value - this.noDataValue.Value) <= 1e-4d;
        }
    }
}
