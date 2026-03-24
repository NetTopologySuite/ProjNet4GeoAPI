// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    private MathTransform inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeoTiffHGridShiftMathTransform"/> class.
    /// </summary>
    /// <param name="gridPaths">The gridPaths value.</param>
    internal GeoTiffHGridShiftMathTransform(IReadOnlyList<string> gridPaths)
    {
        if (gridPaths is null)
        {
            throw new ArgumentNullException(nameof(gridPaths));
        }

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
            throw new ArgumentException("No horizontal grid could be loaded from GeoTIFF input.", nameof(gridPaths));
        }

        this.grids = new ReadOnlyCollection<HorizontalGrid>(
            loadedGrids.OrderBy(grid => grid.Area, Comparer<double>.Default).ToArray());
    }

    private GeoTiffHGridShiftMathTransform(GeoTiffHGridShiftMathTransform source, bool isInverted)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

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
        if (!this.TryFindGridForPoint(x, y, out HorizontalGrid grid))
        {
            throw new ArgumentException("Coordinate is outside the horizontal GeoTIFF grid extent.");
        }

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

        throw new ArgumentException("Inverse horizontal GeoTIFF grid shift did not converge.");
    }

    private static (double LonShift, double LatShift) InterpolateShift(HorizontalGrid grid, double longitude, double latitude)
    {
        if (!grid.TryMapToGridCoordinates(longitude, latitude, out double gridX, out double gridY))
        {
            throw new ArgumentException("Coordinate is outside the horizontal GeoTIFF grid extent.");
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

            throw new ArgumentException("Coordinate is outside the horizontal GeoTIFF grid extent.");
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

        throw new ArgumentException("Coordinate is outside the horizontal GeoTIFF grid extent.");
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

    private bool TryFindGridForPoint(double longitude, double latitude, out HorizontalGrid grid)
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
        /// <param name="latitudeSampleIndex">The latitudeSampleIndex value.</param>
        /// <param name="longitudeSampleIndex">The longitudeSampleIndex value.</param>
        /// <param name="longitudeIsPositiveWest">The longitudeIsPositiveWest value.</param>
        /// <param name="latitudeUnitScale">The latitudeUnitScale value.</param>
        /// <param name="longitudeUnitScale">The longitudeUnitScale value.</param>
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
        /// Performs the documented operation.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns>The computed value.</returns>
        internal double GetLatitudeShift(int x, int y)
        {
            return this.GetSampleValue(this.latitudeSampleIndex, x, y) * this.latitudeUnitScale;
        }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns>The computed value.</returns>
        internal double GetLongitudeShift(int x, int y)
        {
            double value = this.GetSampleValue(this.longitudeSampleIndex, x, y) * this.longitudeUnitScale;
            return this.longitudeIsPositiveWest ? -value : value;
        }
    }
}
