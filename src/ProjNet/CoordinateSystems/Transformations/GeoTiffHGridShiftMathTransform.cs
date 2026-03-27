// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

/// <summary>
/// Applies horizontal grid-shift corrections loaded from GeoTIFF grids.
/// </summary>
[Serializable]
internal sealed class GeoTiffHGridShiftMathTransform : MathTransform
{
    private const double RelativeTolerance = 1e-5d;
    private const double InverseTolerance = 1e-12d;
    private const int MaxInverseIterations = 10;
    private readonly ReadOnlyCollection<HorizontalGrid> grids;
    private bool isInverted;
    private MathTransform? inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeoTiffHGridShiftMathTransform"/> class.
    /// </summary>
    /// <param name="gridPaths">Ordered GeoTIFF grid file paths to load.</param>
    internal GeoTiffHGridShiftMathTransform(IReadOnlyList<string> gridPaths)
    {
        gridPaths = ArgumentGuard.ThrowIfNull(gridPaths, nameof(gridPaths));

        var loadedGrids = new List<HorizontalGrid>(gridPaths.Count);
        for (int i = 0; i < gridPaths.Count; i++)
        {
            string path = gridPaths[i];
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            loadedGrids.AddRange(GeoTiffGridLoader.LoadHorizontal(path));
        }

        if (loadedGrids.Count == 0)
        {
            ArgumentGuard.ThrowArgument("No horizontal grid could be loaded from GeoTIFF input.", nameof(gridPaths));
        }

        this.grids = new ReadOnlyCollection<HorizontalGrid>(
            loadedGrids.OrderBy(grid => grid.Area, Comparer<double>.Default).ToArray());
    }

    private GeoTiffHGridShiftMathTransform(GeoTiffHGridShiftMathTransform source, bool isInverted)
    {
        source = ArgumentGuard.ThrowIfNull(source, nameof(source));

        this.grids = source.grids;
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
            this.inverse = new GeoTiffHGridShiftMathTransform(this, !this.isInverted);
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
        if (!this.TryFindGridForPoint(x, y, out HorizontalGrid? gridCandidate))
        {
            ArgumentGuard.ThrowArgument("Coordinate is outside the horizontal GeoTIFF grid extent.");
        }

        HorizontalGrid grid = ArgumentGuard.ThrowIfNull(gridCandidate, nameof(gridCandidate));

        if (!this.isInverted)
        {
            (double lonShift, double latShift) = InterpolateShift(grid, x, y);
            x += lonShift;
            y += latShift;
            return;
        }

        InverseTransform(ref x, ref y, grid);
    }

    private static void InverseTransform(ref double longitude, ref double latitude, HorizontalGrid initialGrid)
    {
        (double firstLonShift, double firstLatShift) = InterpolateShift(initialGrid, longitude, latitude);
        double targetLongitude = longitude;
        double targetLatitude = latitude;
        double candidateLongitude = targetLongitude - firstLonShift;
        double candidateLatitude = targetLatitude - firstLatShift;
        int iterations = MaxInverseIterations;
        while (iterations-- > 0)
        {
            (double iterLonShift, double iterLatShift) = InterpolateShift(initialGrid, candidateLongitude, candidateLatitude);
            double deltaLongitude = candidateLongitude + iterLonShift - targetLongitude;
            double deltaLatitude = candidateLatitude + iterLatShift - targetLatitude;
            candidateLongitude -= deltaLongitude;
            candidateLatitude -= deltaLatitude;
            if ((deltaLongitude * deltaLongitude) + (deltaLatitude * deltaLatitude) <= (InverseTolerance * InverseTolerance))
            {
                longitude = NormalizeLongitude(candidateLongitude);
                latitude = candidateLatitude;
                return;
            }
        }

        ArgumentGuard.ThrowArgument("Inverse horizontal GeoTIFF grid shift did not converge.");
    }

    private static (double LonShift, double LatShift) InterpolateShift(HorizontalGrid grid, double longitude, double latitude)
    {
        if (!grid.TryMapToGridCoordinates(longitude, latitude, out double gridX, out double gridY))
        {
            ArgumentGuard.ThrowArgument("Coordinate is outside the horizontal GeoTIFF grid extent.");
        }

        int indexX = (int)Math.Floor(gridX);
        int indexY = (int)Math.Floor(gridY);
        double fractionX = gridX - indexX;
        double fractionY = gridY - indexY;
        NormalizeInterpolationCell(grid.Width, ref indexX, ref fractionX);
        NormalizeInterpolationCell(grid.Height, ref indexY, ref fractionY);

        int indexX2 = indexX + 1;
        int indexY2 = indexY + 1;
        double latA = grid.GetLatitudeShift(indexX, indexY);
        double latB = grid.GetLatitudeShift(indexX2, indexY);
        double latC = grid.GetLatitudeShift(indexX, indexY2);
        double latD = grid.GetLatitudeShift(indexX2, indexY2);

        double lonA = grid.GetLongitudeShift(indexX, indexY);
        double lonB = grid.GetLongitudeShift(indexX2, indexY);
        double lonC = grid.GetLongitudeShift(indexX, indexY2);
        double lonD = grid.GetLongitudeShift(indexX2, indexY2);

        double xy = fractionX * fractionY;
        double wA = 1d - fractionX - fractionY + xy;
        double wB = fractionX - xy;
        double wC = fractionY - xy;
        double wD = xy;

        double latitudeShift = (latA * wA) + (latB * wB) + (latC * wC) + (latD * wD);
        double longitudeShift = (lonA * wA) + (lonB * wB) + (lonC * wC) + (lonD * wD);
        return (longitudeShift, latitudeShift);
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

            ArgumentGuard.ThrowArgument("Coordinate is outside the horizontal GeoTIFF grid extent.");
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

        ArgumentGuard.ThrowArgument("Coordinate is outside the horizontal GeoTIFF grid extent.");
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

    private bool TryFindGridForPoint(double longitude, double latitude, [NotNullWhen(true)] out HorizontalGrid? grid)
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
    /// Represents a single horizontal-shift grid loaded from a GeoTIFF file.
    /// </summary>
    [Serializable]
    internal sealed class HorizontalGrid : BaseGeoGrid
    {
        private readonly int latitudeSampleIndex;
        private readonly int longitudeSampleIndex;
        private readonly bool longitudeIsPositiveWest;
        private readonly double latitudeUnitScale;
        private readonly double longitudeUnitScale;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeoTiffHGridShiftMathTransform.HorizontalGrid"/> class.
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
        /// <param name="latitudeSampleIndex">Zero-based band index of the latitude-shift sample.</param>
        /// <param name="longitudeSampleIndex">Zero-based band index of the longitude-shift sample.</param>
        /// <param name="longitudeIsPositiveWest">
        /// <see langword="true"/> when the longitude shift values are stored with positive-west convention
        /// and must be negated before use.
        /// </param>
        /// <param name="latitudeUnitScale">Scale factor to convert the raw latitude-shift sample to degrees.</param>
        /// <param name="longitudeUnitScale">Scale factor to convert the raw longitude-shift sample to degrees.</param>
        internal HorizontalGrid(
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
            int latitudeSampleIndex,
            int longitudeSampleIndex,
            bool longitudeIsPositiveWest,
            double latitudeUnitScale,
            double longitudeUnitScale)
            : base(sourcePath, width, height, area, epsilon, west, east, south, north, a, b, c, d, e, f, sampleData)
        {
            this.latitudeSampleIndex = latitudeSampleIndex;
            this.longitudeSampleIndex = longitudeSampleIndex;
            this.longitudeIsPositiveWest = longitudeIsPositiveWest;
            this.latitudeUnitScale = latitudeUnitScale;
            this.longitudeUnitScale = longitudeUnitScale;
        }

        /// <summary>
        /// Gets the latitude shift in degrees at the specified grid cell.
        /// </summary>
        /// <param name="x">Column index.</param>
        /// <param name="y">Row index.</param>
        /// <returns>The latitude shift in degrees at (<paramref name="x"/>, <paramref name="y"/>).</returns>
        internal double GetLatitudeShift(int x, int y)
        {
            return this.GetSampleValue(this.latitudeSampleIndex, x, y) * this.latitudeUnitScale;
        }

        /// <summary>
        /// Gets the longitude shift in degrees at the specified grid cell, with positive-east sign convention applied.
        /// </summary>
        /// <param name="x">Column index.</param>
        /// <param name="y">Row index.</param>
        /// <returns>The longitude shift in degrees at (<paramref name="x"/>, <paramref name="y"/>), positive east.</returns>
        internal double GetLongitudeShift(int x, int y)
        {
            double value = this.GetSampleValue(this.longitudeSampleIndex, x, y) * this.longitudeUnitScale;
            return this.longitudeIsPositiveWest ? -value : value;
        }
    }
}
