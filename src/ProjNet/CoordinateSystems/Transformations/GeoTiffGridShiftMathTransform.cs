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
namespace ProjNet.CoordinateSystems.Transformations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using BitMiracle.LibTiff.Classic;

    /// <summary>
    /// Represents a documented type.
    /// </summary>
    internal readonly struct SampleEncoding
    {
        private readonly ValueReader valueReader;

        private SampleEncoding(int bytesPerSample, ValueReader valueReader)
        {
            this.BytesPerSample = bytesPerSample;
            this.valueReader = valueReader;
        }

        /// <summary>
        /// Represents a documented type.
        /// </summary>
        /// <param name="buffer">The buffer value.</param>
        /// <param name="offset">The offset value.</param>
        /// <returns>The computed value.</returns>
        internal delegate double ValueReader(byte[] buffer, int offset);

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal int BytesPerSample { get; }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="bitsPerSample">The bitsPerSample value.</param>
        /// <param name="sampleFormat">The sampleFormat value.</param>
        /// <param name="encoding">The encoding value.</param>
        /// <returns>The computed value.</returns>
        internal static bool TryCreate(int bitsPerSample, SampleFormat sampleFormat, out SampleEncoding encoding)
        {
            switch (sampleFormat)
            {
                case SampleFormat.INT:
                    if (bitsPerSample == 16)
                    {
                        encoding = new SampleEncoding(2, (buffer, offset) => BitConverter.ToInt16(buffer, offset));
                        return true;
                    }

                    if (bitsPerSample == 32)
                    {
                        encoding = new SampleEncoding(4, (buffer, offset) => BitConverter.ToInt32(buffer, offset));
                        return true;
                    }

                    break;
                case SampleFormat.UINT:
                    if (bitsPerSample == 16)
                    {
                        encoding = new SampleEncoding(2, (buffer, offset) => BitConverter.ToUInt16(buffer, offset));
                        return true;
                    }

                    if (bitsPerSample == 32)
                    {
                        encoding = new SampleEncoding(4, (buffer, offset) => BitConverter.ToUInt32(buffer, offset));
                        return true;
                    }

                    break;
                case SampleFormat.IEEEFP:
                    if (bitsPerSample == 32)
                    {
                        encoding = new SampleEncoding(4, (buffer, offset) => BitConverter.ToSingle(buffer, offset));
                        return true;
                    }

                    if (bitsPerSample == 64)
                    {
                        encoding = new SampleEncoding(8, (buffer, offset) => BitConverter.ToDouble(buffer, offset));
                        return true;
                    }

                    break;
            }

            encoding = default;
            return false;
        }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="buffer">The buffer value.</param>
        /// <param name="offset">The offset value.</param>
        /// <returns>The computed value.</returns>
        internal double ReadValue(byte[] buffer, int offset)
        {
            return this.valueReader(buffer, offset);
        }
    }

    /// <summary>
    /// Represents a documented type.
    /// </summary>
    [Serializable]
    internal readonly struct SampleData
    {
        private readonly double[][] valuesBySample;
        private readonly double[] scaleBySample;
        private readonly double[] offsetBySample;
        private readonly int width;

        /// <summary>
        /// Initializes a new instance of the <see cref="SampleData"/> struct.
        /// </summary>
        /// <param name="valuesBySample">The valuesBySample value.</param>
        /// <param name="scaleBySample">The scaleBySample value.</param>
        /// <param name="offsetBySample">The offsetBySample value.</param>
        /// <param name="width">The width value.</param>
        internal SampleData(double[][] valuesBySample, double[] scaleBySample = null, double[] offsetBySample = null, int width = 0)
        {
            this.valuesBySample = valuesBySample;
            this.scaleBySample = scaleBySample ?? CreateConstant(valuesBySample?.Length ?? 0, 1d);
            this.offsetBySample = offsetBySample ?? CreateConstant(valuesBySample?.Length ?? 0, 0d);
            this.width = width;
        }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="angularScaleToDegree">The angularScaleToDegree value.</param>
        /// <returns>The computed value.</returns>
        internal SampleData ApplyAngularScale(double angularScaleToDegree)
        {
            if (Math.Abs(angularScaleToDegree - 1d) <= 1e-12d)
            {
                return this;
            }

            var scaled = new double[this.valuesBySample.Length][];
            for (int i = 0; i < scaled.Length; i++)
            {
                scaled[i] = this.valuesBySample[i];
            }

            double[] adjustedScale = new double[this.scaleBySample.Length];
            double[] adjustedOffset = new double[this.offsetBySample.Length];
            for (int i = 0; i < adjustedScale.Length; i++)
            {
                adjustedScale[i] = this.scaleBySample[i] * angularScaleToDegree;
                adjustedOffset[i] = this.offsetBySample[i] * angularScaleToDegree;
            }

            return new SampleData(scaled, adjustedScale, adjustedOffset, this.width);
        }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="scaleBySample">The scaleBySample value.</param>
        /// <param name="offsetBySample">The offsetBySample value.</param>
        /// <returns>The computed value.</returns>
        internal SampleData ApplyScaleOffset(IReadOnlyDictionary<int, double> scaleBySample, IReadOnlyDictionary<int, double> offsetBySample)
        {
            if ((scaleBySample is null || scaleBySample.Count == 0)
                && (offsetBySample is null || offsetBySample.Count == 0))
            {
                return this;
            }

            var copiedValues = new double[this.valuesBySample.Length][];
            for (int i = 0; i < copiedValues.Length; i++)
            {
                copiedValues[i] = this.valuesBySample[i];
            }

            double[] adjustedScale = new double[this.scaleBySample.Length];
            double[] adjustedOffset = new double[this.offsetBySample.Length];
            for (int i = 0; i < this.scaleBySample.Length; i++)
            {
                double scaleFactor = 1d;
                if (!(scaleBySample is null) && scaleBySample.TryGetValue(i, out double parsedScale))
                {
                    scaleFactor = parsedScale;
                }

                double offsetValue = 0d;
                if (!(offsetBySample is null) && offsetBySample.TryGetValue(i, out double parsedOffset))
                {
                    offsetValue = parsedOffset;
                }

                adjustedScale[i] = this.scaleBySample[i] * scaleFactor;
                adjustedOffset[i] = (this.offsetBySample[i] * scaleFactor) + offsetValue;
            }

            return new SampleData(copiedValues, adjustedScale, adjustedOffset, this.width);
        }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="sample">The sample value.</param>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns>The computed value.</returns>
        internal double GetValue(int sample, int x, int y)
        {
            int index = (y * this.width) + x;
            return (this.valuesBySample[sample][index] * this.scaleBySample[sample]) + this.offsetBySample[sample];
        }

        private static double[] CreateConstant(int count, double value)
        {
            var result = new double[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = value;
            }

            return result;
        }
    }

    [Serializable]
    internal sealed class GeoTiffHGridShiftMathTransform : MathTransform
    {
        private const double RelativeTolerance = 1e-5d;
        private const double InverseTolerance = 1e-12d;
        private const int MaxInverseIterations = 10;
        private readonly IReadOnlyList<HorizontalGrid> grids;
        private bool isInverted;

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
            return new GeoTiffHGridShiftMathTransform(this.grids.Select(grid => grid.SourcePath).ToArray())
            {
                isInverted = !this.isInverted,
            };
        }

        /// <inheritdoc />
        public override void Invert()
        {
            this.isInverted = !this.isInverted;
        }

        /// <inheritdoc />
        public override void Transform(ref double x, ref double y, ref double z)
        {
            if (!TryFindGridForPoint(x, y, out HorizontalGrid grid))
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

        private static (double lonShift, double latShift) InterpolateShift(HorizontalGrid grid, double longitude, double latitude)
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

    /// <summary>
    /// Represents a documented type.
    /// </summary>
    [Serializable]
    internal sealed class GeoTiffVGridShiftMathTransform : MathTransform
    {
        private const double RelativeTolerance = 1e-5d;
        private readonly IReadOnlyList<VerticalGrid> grids;
        private readonly double forwardMultiplier;
        private bool isInverted;

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
            return new GeoTiffVGridShiftMathTransform(this.grids.Select(grid => grid.SourcePath).ToArray(), this.forwardMultiplier)
            {
                isInverted = !this.isInverted,
            };
        }

        /// <inheritdoc />
        public override void Invert()
        {
            this.isInverted = !this.isInverted;
        }

        /// <inheritdoc />
        public override void Transform(ref double x, ref double y, ref double z)
        {
            if (!TryFindGridForPoint(x, y, out VerticalGrid grid))
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

    /// <summary>
    /// Represents a documented type.
    /// </summary>
    internal static class GeoTiffGridLoader
    {
        private const int ModelPixelScaleTag = 33550;
        private const int ModelTiePointTag = 33922;
        private const int ModelTransformationTag = 34264;
        private const int GeoKeyDirectoryTag = 34735;
        private const int GdalMetadataTag = 42112;
        private const int GdalNoDataTag = 42113;
        private const int GeogAngularUnitsGeoKey = 2054;
        private const int GtRasterTypeGeoKey = 1025;
        private const int RasterPixelIsPoint = 2;

        private enum GridMode
        {
            Horizontal,
            Vertical,
        }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="path">The path value.</param>
        /// <returns>The computed value.</returns>
        internal static IReadOnlyList<GeoTiffHGridShiftMathTransform.HorizontalGrid> LoadHorizontal(string path)
        {
            return LoadCore(path, GridMode.Horizontal)
                .Select(page => page.ToHorizontalGrid(path))
                .Where(grid => !(grid is null))
                .ToArray();
        }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="path">The path value.</param>
        /// <returns>The computed value.</returns>
        internal static IReadOnlyList<GeoTiffVGridShiftMathTransform.VerticalGrid> LoadVertical(string path)
        {
            return LoadCore(path, GridMode.Vertical)
                .Select(page => page.ToVerticalGrid(path))
                .Where(grid => !(grid is null))
                .ToArray();
        }

        private static IReadOnlyList<LoadedPage> LoadCore(string path, GridMode mode)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path is required.", nameof(path));
            }

            var pages = new List<LoadedPage>();
            using (Tiff tiff = Tiff.Open(path, "r"))
            {
                if (tiff is null)
                {
                    throw new InvalidDataException("Unable to open GeoTIFF grid.");
                }

                short pageIndex = 0;
                do
                {
                    if (!TryReadPage(path, tiff, mode, out LoadedPage page))
                    {
                        pageIndex++;
                        continue;
                    }

                    pages.Add(page);
                    pageIndex++;
                }
                while (tiff.ReadDirectory());
            }

            return pages;
        }

        private static bool TryReadPage(string path, Tiff tiff, GridMode mode, out LoadedPage page)
        {
            page = null;
            if (!TryGetIntField(tiff, TiffTag.IMAGEWIDTH, out int width)
                || !TryGetIntField(tiff, TiffTag.IMAGELENGTH, out int height)
                || width <= 1
                || height <= 1)
            {
                return false;
            }

            int samplesPerPixel = 1;
            if (TryGetIntField(tiff, TiffTag.SAMPLESPERPIXEL, out int foundSamples))
            {
                samplesPerPixel = Math.Max(1, foundSamples);
            }

            if (!TryGetSampleEncoding(tiff, out SampleEncoding encoding))
            {
                throw new InvalidDataException("Unsupported GeoTIFF sample encoding.");
            }

            if (!TryGetGeoTransform(tiff, width, height, out GeoTransform transform))
            {
                return false;
            }

            SampleData sampleData = ReadSampleData(tiff, width, height, samplesPerPixel, encoding);
            GeoMetadata metadata = ReadMetadata(tiff, samplesPerPixel);
            sampleData = sampleData.ApplyScaleOffset(metadata.ScaleBySample, metadata.OffsetBySample);
            switch (mode)
            {
                case GridMode.Horizontal:
                    if (!TryResolveHorizontalSampleIndices(samplesPerPixel, metadata, out int latitudeSample, out int longitudeSample, out bool positiveWest))
                    {
                        return false;
                    }

                    page = LoadedPage.CreateHorizontal(transform, sampleData, metadata, latitudeSample, longitudeSample, positiveWest);
                    return true;
                case GridMode.Vertical:
                    if (!TryResolveVerticalSampleIndex(samplesPerPixel, metadata, out int sampleIndex))
                    {
                        return false;
                    }

                    page = LoadedPage.CreateVertical(transform, sampleData, metadata, sampleIndex);
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported GeoTIFF grid mode.");
            }
        }

        private static bool TryResolveHorizontalSampleIndices(int samplesPerPixel, GeoMetadata metadata, out int latitudeSample, out int longitudeSample, out bool positiveWest)
        {
            latitudeSample = -1;
            longitudeSample = -1;
            positiveWest = false;
            for (int i = 0; i < samplesPerPixel; i++)
            {
                if (!metadata.DescriptionsBySample.TryGetValue(i, out string description))
                {
                    continue;
                }

                if (description.IndexOf("latitude_offset", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    latitudeSample = i;
                }
                else if (description.IndexOf("longitude_offset", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    longitudeSample = i;
                }
            }

            if (latitudeSample < 0 || longitudeSample < 0)
            {
                if (samplesPerPixel == 2)
                {
                    latitudeSample = 0;
                    longitudeSample = 1;
                }
                else
                {
                    return false;
                }
            }

            if (metadata.PositiveValueBySample.TryGetValue(longitudeSample, out string positiveValue)
                && positiveValue.Equals("west", StringComparison.OrdinalIgnoreCase))
            {
                positiveWest = true;
            }

            return true;
        }

        private static bool TryResolveVerticalSampleIndex(int samplesPerPixel, GeoMetadata metadata, out int sampleIndex)
        {
            for (int i = 0; i < samplesPerPixel; i++)
            {
                if (!metadata.DescriptionsBySample.TryGetValue(i, out string description))
                {
                    continue;
                }

                if (description.IndexOf("geoid_undulation", StringComparison.OrdinalIgnoreCase) >= 0
                    || description.IndexOf("vertical_offset", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    sampleIndex = i;
                    return true;
                }
            }

            sampleIndex = 0;
            return samplesPerPixel >= 1;
        }

        private static bool TryGetGeoTransform(Tiff tiff, int width, int height, out GeoTransform transform)
        {
            transform = default;
            bool pixelIsPoint = IsPixelIsPoint(tiff);
            double pixelOffset = pixelIsPoint ? 0d : 0.5d;

            if (TryGetDoubleArrayField(tiff, (TiffTag)ModelTransformationTag, out double[] matrix) && matrix.Length >= 16)
            {
                double a = matrix[0];
                double b = matrix[1];
                double c = matrix[3] + (pixelOffset * matrix[0]) + (pixelOffset * matrix[1]);
                double d = matrix[4];
                double e = matrix[5];
                double f = matrix[7] + (pixelOffset * matrix[4]) + (pixelOffset * matrix[5]);
                if (!TryCreateGeoTransform(width, height, a, b, c, d, e, f, out transform))
                {
                    return false;
                }

                return true;
            }

            if (!TryGetDoubleArrayField(tiff, (TiffTag)ModelPixelScaleTag, out double[] pixelScale) || pixelScale.Length < 2)
            {
                return false;
            }

            if (!TryGetDoubleArrayField(tiff, (TiffTag)ModelTiePointTag, out double[] tiePoints) || tiePoints.Length < 6)
            {
                return false;
            }

            double tieI = tiePoints[0];
            double tieJ = tiePoints[1];
            double tieX = tiePoints[3];
            double tieY = tiePoints[4];
            double scaleX = pixelScale[0];
            double scaleY = pixelScale[1];
            if (scaleX == 0d || scaleY == 0d)
            {
                return false;
            }

            double aSimple = scaleX;
            double bSimple = 0d;
            double cSimple = tieX + ((pixelOffset - tieI) * scaleX);
            double dSimple = 0d;
            double eSimple = -scaleY;
            double fSimple = tieY - ((pixelOffset - tieJ) * scaleY);
            return TryCreateGeoTransform(width, height, aSimple, bSimple, cSimple, dSimple, eSimple, fSimple, out transform);
        }

        private static bool TryCreateGeoTransform(int width, int height, double a, double b, double c, double d, double e, double f, out GeoTransform transform)
        {
            transform = default;
            double determinant = (a * e) - (b * d);
            if (Math.Abs(determinant) <= 1e-18d)
            {
                return false;
            }

            (double west, double east, double south, double north, double area, double epsilon) = ComputeBounds(width, height, a, b, c, d, e, f);
            transform = new GeoTransform(width, height, a, b, c, d, e, f, determinant, west, east, south, north, area, epsilon);
            return true;
        }

        private static (double west, double east, double south, double north, double area, double epsilon) ComputeBounds(
            int width,
            int height,
            double a,
            double b,
            double c,
            double d,
            double e,
            double f)
        {
            var xs = new[] { 0d, width - 1d, 0d, width - 1d };
            var ys = new[] { 0d, 0d, height - 1d, height - 1d };
            double west = double.PositiveInfinity;
            double east = double.NegativeInfinity;
            double south = double.PositiveInfinity;
            double north = double.NegativeInfinity;
            for (int i = 0; i < 4; i++)
            {
                double lon = (a * xs[i]) + (b * ys[i]) + c;
                double lat = (d * xs[i]) + (e * ys[i]) + f;
                west = Math.Min(west, lon);
                east = Math.Max(east, lon);
                south = Math.Min(south, lat);
                north = Math.Max(north, lat);
            }

            double area = Math.Max(1e-12d, (east - west) * (north - south));
            double resX = Math.Sqrt((a * a) + (d * d));
            double resY = Math.Sqrt((b * b) + (e * e));
            double epsilon = (resX + resY) * 1e-5d;
            return (west, east, south, north, area, epsilon);
        }

        private static SampleData ReadSampleData(Tiff tiff, int width, int height, int samplesPerPixel, SampleEncoding encoding)
        {
            int scanlineSize = tiff.ScanlineSize();
            var sampleValues = new double[samplesPerPixel][];
            for (int i = 0; i < samplesPerPixel; i++)
            {
                sampleValues[i] = new double[width * height];
            }

            PlanarConfig planarConfig = PlanarConfig.CONTIG;
            if (TryGetIntField(tiff, TiffTag.PLANARCONFIG, out int planarConfigValue))
            {
                planarConfig = (PlanarConfig)planarConfigValue;
            }

            if (planarConfig == PlanarConfig.SEPARATE && samplesPerPixel > 1)
            {
                for (int sample = 0; sample < samplesPerPixel; sample++)
                {
                    byte[] buffer = new byte[scanlineSize];
                    for (int row = 0; row < height; row++)
                    {
                        if (!tiff.ReadScanline(buffer, row, (short)sample))
                        {
                            throw new InvalidDataException("Failed to read GeoTIFF scanline.");
                        }

                        for (int column = 0; column < width; column++)
                        {
                            int offset = column * encoding.BytesPerSample;
                            sampleValues[sample][(row * width) + column] = encoding.ReadValue(buffer, offset);
                        }
                    }
                }
            }
            else
            {
                byte[] buffer = new byte[scanlineSize];
                for (int row = 0; row < height; row++)
                {
                    if (!tiff.ReadScanline(buffer, row))
                    {
                        throw new InvalidDataException("Failed to read GeoTIFF scanline.");
                    }

                    for (int column = 0; column < width; column++)
                    {
                        int pixelBase = column * encoding.BytesPerSample * samplesPerPixel;
                        for (int sample = 0; sample < samplesPerPixel; sample++)
                        {
                            int offset = pixelBase + (sample * encoding.BytesPerSample);
                            sampleValues[sample][(row * width) + column] = encoding.ReadValue(buffer, offset);
                        }
                    }
                }
            }

            return new SampleData(sampleValues, width: width);
        }

        private static GeoMetadata ReadMetadata(Tiff tiff, int samplesPerPixel)
        {
            var descriptionsBySample = new Dictionary<int, string>();
            var positiveValueBySample = new Dictionary<int, string>();
            var scaleBySample = new Dictionary<int, double>();
            var offsetBySample = new Dictionary<int, double>();
            var unitTypeBySample = new Dictionary<int, string>();

            if (TryGetStringField(tiff, (TiffTag)GdalMetadataTag, out string gdalMetadata) && !string.IsNullOrWhiteSpace(gdalMetadata))
            {
                string sanitizedMetadata = SanitizeXmlMetadata(gdalMetadata);
                ParseMetadataItems(sanitizedMetadata, samplesPerPixel, descriptionsBySample, positiveValueBySample, scaleBySample, offsetBySample, unitTypeBySample);
            }

            double? noDataValue = null;
            if (TryGetStringField(tiff, (TiffTag)GdalNoDataTag, out string noDataText)
                && double.TryParse(
                    CleanMetadataValue(noDataText),
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out double parsedNoData))
            {
                noDataValue = parsedNoData;
            }

            double angularScaleToDegree = ResolveAngularScaleToDegree(tiff);
            return new GeoMetadata(descriptionsBySample, positiveValueBySample, scaleBySample, offsetBySample, noDataValue, angularScaleToDegree, unitTypeBySample);
        }

        private static string SanitizeXmlMetadata(string metadata)
        {
            if (metadata is null)
            {
                return string.Empty;
            }

            string sanitized = metadata.Trim('\0', '\uFEFF', ' ', '\t', '\r', '\n');
            int firstTag = sanitized.IndexOf("<", StringComparison.Ordinal);
            if (firstTag > 0)
            {
                sanitized = sanitized.Substring(firstTag);
            }

            int lastTag = sanitized.LastIndexOf(">", StringComparison.Ordinal);
            if (lastTag >= 0 && lastTag + 1 < sanitized.Length)
            {
                sanitized = sanitized.Substring(0, lastTag + 1);
            }

            return sanitized;
        }

        private static string CleanMetadataValue(string value)
        {
            return value is null ? string.Empty : value.Trim('\0', ' ', '\t', '\r', '\n');
        }

        private static void ParseMetadataItems(
            string metadata,
            int samplesPerPixel,
            IDictionary<int, string> descriptionsBySample,
            IDictionary<int, string> positiveValueBySample,
            IDictionary<int, double> scaleBySample,
            IDictionary<int, double> offsetBySample,
            IDictionary<int, string> unitTypeBySample)
        {
            if (string.IsNullOrWhiteSpace(metadata))
            {
                return;
            }

            MatchCollection matches = Regex.Matches(
                metadata,
                "<Item(?<attrs>[^>]*)>(?<value>.*?)</Item>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                Group attrsGroup = match.Groups["attrs"];
                Group valueGroup = match.Groups["value"];
                if (!attrsGroup.Success || !valueGroup.Success)
                {
                    continue;
                }

                string attrs = attrsGroup.Value;
                string name = ExtractAttribute(attrs, "name");
                string sampleValue = ExtractAttribute(attrs, "sample");
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(sampleValue))
                {
                    continue;
                }

                if (!int.TryParse(sampleValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int sample)
                    || sample < 0
                    || sample >= samplesPerPixel)
                {
                    continue;
                }

                string value = CleanMetadataValue(valueGroup.Value);
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (name.Equals("DESCRIPTION", StringComparison.OrdinalIgnoreCase))
                {
                    descriptionsBySample[sample] = value;
                }
                else if (name.Equals("positive_value", StringComparison.OrdinalIgnoreCase))
                {
                    positiveValueBySample[sample] = value;
                }
                else if (name.Equals("SCALE", StringComparison.OrdinalIgnoreCase)
                    && double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double scale))
                {
                    scaleBySample[sample] = scale;
                }
                else if (name.Equals("OFFSET", StringComparison.OrdinalIgnoreCase)
                    && double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double offset))
                {
                    offsetBySample[sample] = offset;
                }
                else if (name.Equals("UNITTYPE", StringComparison.OrdinalIgnoreCase))
                {
                    unitTypeBySample[sample] = value;
                }
            }
        }

        private static string ExtractAttribute(string attrs, string attributeName)
        {
            if (string.IsNullOrWhiteSpace(attrs) || string.IsNullOrWhiteSpace(attributeName))
            {
                return string.Empty;
            }

            Match match = Regex.Match(
                attrs,
                "\\b" + Regex.Escape(attributeName) + "\\s*=\\s*\"(?<value>[^\"]*)\"",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success)
            {
                return string.Empty;
            }

            Group valueGroup = match.Groups["value"];
            return valueGroup.Success ? CleanMetadataValue(valueGroup.Value) : string.Empty;
        }

        private static double ResolveAngularScaleToDegree(Tiff tiff)
        {
            if (!TryGetShortArrayField(tiff, (TiffTag)GeoKeyDirectoryTag, out short[] keyDirectory) || keyDirectory.Length < 4)
            {
                return 1d;
            }

            int keyCount = keyDirectory[3];
            for (int i = 0; i < keyCount; i++)
            {
                int entryOffset = 4 + (i * 4);
                if (entryOffset + 3 >= keyDirectory.Length)
                {
                    break;
                }

                int keyId = keyDirectory[entryOffset];
                int tiffTagLocation = keyDirectory[entryOffset + 1];
                int valueOffset = keyDirectory[entryOffset + 3];
                if (keyId != GeogAngularUnitsGeoKey)
                {
                    continue;
                }

                int angularCode = tiffTagLocation == 0 ? valueOffset : 9102;
                switch (angularCode)
                {
                    case 9101:
                        return 180d / Math.PI;
                    case 9102:
                        return 1d;
                    case 9105:
                        return 0.9d;
                    default:
                        return 1d;
                }
            }

            return 1d;
        }

        private static bool IsPixelIsPoint(Tiff tiff)
        {
            if (!TryGetShortArrayField(tiff, (TiffTag)GeoKeyDirectoryTag, out short[] keyDirectory) || keyDirectory.Length < 4)
            {
                return false;
            }

            int keyCount = keyDirectory[3];
            for (int i = 0; i < keyCount; i++)
            {
                int entryOffset = 4 + (i * 4);
                if (entryOffset + 3 >= keyDirectory.Length)
                {
                    break;
                }

                int keyId = keyDirectory[entryOffset];
                int tiffTagLocation = keyDirectory[entryOffset + 1];
                int valueOffset = keyDirectory[entryOffset + 3];
                if (keyId == GtRasterTypeGeoKey && tiffTagLocation == 0)
                {
                    return valueOffset == RasterPixelIsPoint;
                }
            }

            return false;
        }

        private static bool TryGetSampleEncoding(Tiff tiff, out SampleEncoding encoding)
        {
            encoding = default;
            int bitsPerSample = 0;
            if (!TryGetIntField(tiff, TiffTag.BITSPERSAMPLE, out bitsPerSample))
            {
                return false;
            }

            SampleFormat sampleFormat = SampleFormat.IEEEFP;
            if (TryGetIntField(tiff, TiffTag.SAMPLEFORMAT, out int sampleFormatRaw))
            {
                sampleFormat = (SampleFormat)sampleFormatRaw;
            }

            if (!SampleEncoding.TryCreate(bitsPerSample, sampleFormat, out encoding))
            {
                return false;
            }

            return true;
        }

        private static bool TryGetIntField(Tiff tiff, TiffTag tag, out int value)
        {
            value = 0;
            FieldValue[] field = tiff.GetField(tag);
            if (field is null || field.Length == 0)
            {
                return false;
            }

            value = field[0].ToInt();
            return true;
        }

        private static bool TryGetStringField(Tiff tiff, TiffTag tag, out string value)
        {
            value = null;
            FieldValue[] field = tiff.GetField(tag);
            if (field is null || field.Length == 0)
            {
                return false;
            }

            value = field[field.Length - 1].ToString();
            if (string.IsNullOrEmpty(value) && field.Length > 1)
            {
                value = field[0].ToString();
            }

            return !(value is null);
        }

        private static bool TryGetDoubleArrayField(Tiff tiff, TiffTag tag, out double[] values)
        {
            values = Array.Empty<double>();
            FieldValue[] field = tiff.GetField(tag);
            if (field is null || field.Length == 0)
            {
                return false;
            }

            double[] candidate = field[field.Length - 1].ToDoubleArray();
            if (candidate is null || candidate.Length == 0)
            {
                return false;
            }

            values = candidate;
            return true;
        }

        private static bool TryGetShortArrayField(Tiff tiff, TiffTag tag, out short[] values)
        {
            values = Array.Empty<short>();
            FieldValue[] field = tiff.GetField(tag);
            if (field is null || field.Length == 0)
            {
                return false;
            }

            short[] candidate = field[field.Length - 1].ToShortArray();
            if (candidate is null || candidate.Length == 0)
            {
                return false;
            }

            values = candidate;
            return true;
        }

        private readonly struct GeoTransform
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="GeoTransform"/> struct.
            /// </summary>
            /// <param name="width">The width value.</param>
            /// <param name="height">The height value.</param>
            /// <param name="a">The a value.</param>
            /// <param name="b">The b value.</param>
            /// <param name="c">The c value.</param>
            /// <param name="d">The d value.</param>
            /// <param name="e">The e value.</param>
            /// <param name="f">The f value.</param>
            /// <param name="determinant">The determinant value.</param>
            /// <param name="west">The west value.</param>
            /// <param name="east">The east value.</param>
            /// <param name="south">The south value.</param>
            /// <param name="north">The north value.</param>
            /// <param name="area">The area value.</param>
            /// <param name="epsilon">The epsilon value.</param>
            internal GeoTransform(
                int width,
                int height,
                double a,
                double b,
                double c,
                double d,
                double e,
                double f,
                double determinant,
                double west,
                double east,
                double south,
                double north,
                double area,
                double epsilon)
            {
                this.Width = width;
                this.Height = height;
                this.A = a;
                this.B = b;
                this.C = c;
                this.D = d;
                this.E = e;
                this.F = f;
                this.Determinant = determinant;
                this.West = west;
                this.East = east;
                this.South = south;
                this.North = north;
                this.Area = area;
                this.Epsilon = epsilon;
            }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal int Width { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal int Height { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal double A { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal double B { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal double C { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal double D { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal double E { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal double F { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal double Determinant { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal double West { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal double East { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal double South { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal double North { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal double Area { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal double Epsilon { get; }
        }

        private readonly struct GeoMetadata
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="GeoMetadata"/> struct.
            /// </summary>
            /// <param name="descriptionsBySample">The descriptionsBySample value.</param>
            /// <param name="positiveValueBySample">The positiveValueBySample value.</param>
            /// <param name="scaleBySample">The scaleBySample value.</param>
            /// <param name="offsetBySample">The offsetBySample value.</param>
            /// <param name="noDataValue">The noDataValue value.</param>
            /// <param name="angularScaleToDegree">The angularScaleToDegree value.</param>
            /// <param name="unitTypeBySample">The unitTypeBySample value.</param>
            internal GeoMetadata(
                IReadOnlyDictionary<int, string> descriptionsBySample,
                IReadOnlyDictionary<int, string> positiveValueBySample,
                IReadOnlyDictionary<int, double> scaleBySample,
                IReadOnlyDictionary<int, double> offsetBySample,
                double? noDataValue,
                double angularScaleToDegree,
                IReadOnlyDictionary<int, string> unitTypeBySample)
            {
                this.DescriptionsBySample = descriptionsBySample;
                this.PositiveValueBySample = positiveValueBySample;
                this.ScaleBySample = scaleBySample;
                this.OffsetBySample = offsetBySample;
                this.NoDataValue = noDataValue;
                this.AngularScaleToDegree = angularScaleToDegree;
                this.UnitTypeBySample = unitTypeBySample;
            }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal IReadOnlyDictionary<int, string> DescriptionsBySample { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal IReadOnlyDictionary<int, string> PositiveValueBySample { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal IReadOnlyDictionary<int, double> ScaleBySample { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal IReadOnlyDictionary<int, double> OffsetBySample { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal double? NoDataValue { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal double AngularScaleToDegree { get; }

            /// <summary>
            /// Gets the documented value.
            /// </summary>
            internal IReadOnlyDictionary<int, string> UnitTypeBySample { get; }
        }

        private sealed class LoadedPage
        {
            private readonly GeoTransform transform;
            private readonly SampleData sampleData;
            private readonly GeoMetadata metadata;
            private readonly int latitudeSample;
            private readonly int longitudeSample;
            private readonly int verticalSample;
            private readonly bool longitudePositiveWest;
            private readonly GridMode mode;

            private LoadedPage(
                GeoTransform transform,
                SampleData sampleData,
                GeoMetadata metadata,
                int latitudeSample,
                int longitudeSample,
                int verticalSample,
                bool longitudePositiveWest,
                GridMode mode)
            {
                this.transform = transform;
                this.sampleData = sampleData;
                this.metadata = metadata;
                this.latitudeSample = latitudeSample;
                this.longitudeSample = longitudeSample;
                this.verticalSample = verticalSample;
                this.longitudePositiveWest = longitudePositiveWest;
                this.mode = mode;
            }

            /// <summary>
            /// Performs the documented operation.
            /// </summary>
            /// <param name="transform">The transform value.</param>
            /// <param name="sampleData">The sampleData value.</param>
            /// <param name="metadata">The metadata value.</param>
            /// <param name="latitudeSample">The latitudeSample value.</param>
            /// <param name="longitudeSample">The longitudeSample value.</param>
            /// <param name="longitudePositiveWest">The longitudePositiveWest value.</param>
            /// <returns>The computed value.</returns>
            internal static LoadedPage CreateHorizontal(
                GeoTransform transform,
                SampleData sampleData,
                GeoMetadata metadata,
                int latitudeSample,
                int longitudeSample,
                bool longitudePositiveWest)
            {
                return new LoadedPage(transform, sampleData, metadata, latitudeSample, longitudeSample, -1, longitudePositiveWest, GridMode.Horizontal);
            }

            /// <summary>
            /// Performs the documented operation.
            /// </summary>
            /// <param name="transform">The transform value.</param>
            /// <param name="sampleData">The sampleData value.</param>
            /// <param name="metadata">The metadata value.</param>
            /// <param name="verticalSample">The verticalSample value.</param>
            /// <returns>The computed value.</returns>
            internal static LoadedPage CreateVertical(
                GeoTransform transform,
                SampleData sampleData,
                GeoMetadata metadata,
                int verticalSample)
            {
                return new LoadedPage(transform, sampleData, metadata, -1, -1, verticalSample, false, GridMode.Vertical);
            }

            /// <summary>
            /// Performs the documented operation.
            /// </summary>
            /// <param name="sourcePath">The sourcePath value.</param>
            /// <returns>The computed value.</returns>
            internal GeoTiffHGridShiftMathTransform.HorizontalGrid ToHorizontalGrid(string sourcePath)
            {
                if (this.mode != GridMode.Horizontal)
                {
                    return null;
                }

                double latitudeScale = ResolveHorizontalShiftScaleToDegree(this.metadata, this.latitudeSample);
                double longitudeScale = ResolveHorizontalShiftScaleToDegree(this.metadata, this.longitudeSample);
                return new GeoTiffHGridShiftMathTransform.HorizontalGrid(
                    sourcePath,
                    this.transform.Width,
                    this.transform.Height,
                    this.transform.Area,
                    this.transform.Epsilon,
                    this.transform.West,
                    this.transform.East,
                    this.transform.South,
                    this.transform.North,
                    this.transform.A,
                    this.transform.B,
                    this.transform.C,
                    this.transform.D,
                    this.transform.E,
                    this.transform.F,
                    this.sampleData,
                    this.latitudeSample,
                    this.longitudeSample,
                    this.longitudePositiveWest,
                    latitudeScale,
                    longitudeScale);
            }

            /// <summary>
            /// Performs the documented operation.
            /// </summary>
            /// <param name="sourcePath">The sourcePath value.</param>
            /// <returns>The computed value.</returns>
            internal GeoTiffVGridShiftMathTransform.VerticalGrid ToVerticalGrid(string sourcePath)
            {
                if (this.mode != GridMode.Vertical)
                {
                    return null;
                }

                return new GeoTiffVGridShiftMathTransform.VerticalGrid(
                    sourcePath,
                    this.transform.Width,
                    this.transform.Height,
                    this.transform.Area,
                    this.transform.Epsilon,
                    this.transform.West,
                    this.transform.East,
                    this.transform.South,
                    this.transform.North,
                    this.transform.A,
                    this.transform.B,
                    this.transform.C,
                    this.transform.D,
                    this.transform.E,
                    this.transform.F,
                    this.sampleData,
                    this.verticalSample,
                    this.metadata.NoDataValue);
            }

            private static double ResolveHorizontalShiftScaleToDegree(GeoMetadata metadata, int sampleIndex)
            {
                if (metadata.UnitTypeBySample.TryGetValue(sampleIndex, out string unitType))
                {
                    string unit = unitType.Trim();
                    if (unit.Equals("degree", StringComparison.OrdinalIgnoreCase)
                        || unit.Equals("degrees", StringComparison.OrdinalIgnoreCase)
                        || unit.Equals("deg", StringComparison.OrdinalIgnoreCase))
                    {
                        return 1d;
                    }

                    if (unit.Equals("radian", StringComparison.OrdinalIgnoreCase)
                        || unit.Equals("radians", StringComparison.OrdinalIgnoreCase)
                        || unit.Equals("rad", StringComparison.OrdinalIgnoreCase))
                    {
                        return 180d / Math.PI;
                    }

                    if (unit.Equals("arc-second", StringComparison.OrdinalIgnoreCase)
                        || unit.Equals("arc-seconds", StringComparison.OrdinalIgnoreCase)
                        || unit.Equals("arc_second", StringComparison.OrdinalIgnoreCase)
                        || unit.Equals("arc_seconds", StringComparison.OrdinalIgnoreCase)
                        || unit.Equals("arcsecond", StringComparison.OrdinalIgnoreCase)
                        || unit.Equals("arcseconds", StringComparison.OrdinalIgnoreCase)
                        || unit.Equals("arcsec", StringComparison.OrdinalIgnoreCase))
                    {
                        return 1d / 3600d;
                    }
                }

                // PROJ hgridshift defaults to arc-second offsets when unit metadata is absent.
                return 1d / 3600d;
            }
        }
    }

    /// <summary>
    /// Represents a documented type.
    /// </summary>
    [Serializable]
    internal abstract class BaseGeoGrid
    {
        private readonly SampleData sampleData;
        private readonly double determinant;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseGeoGrid"/> class.
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
        protected BaseGeoGrid(
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
            SampleData sampleData)
        {
            this.SourcePath = sourcePath;
            this.Width = width;
            this.Height = height;
            this.Area = area;
            this.Epsilon = epsilon;
            this.West = west;
            this.East = east;
            this.South = south;
            this.North = north;
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
            this.E = e;
            this.F = f;
            this.sampleData = sampleData;
            this.determinant = (a * e) - (b * d);
        }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal string SourcePath { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal int Width { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal int Height { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double Area { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double Epsilon { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double West { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double East { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double South { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double North { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double A { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double B { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double C { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double D { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double E { get; }

        /// <summary>
        /// Gets the documented value.
        /// </summary>
        internal double F { get; }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="longitude">The longitude value.</param>
        /// <param name="latitude">The latitude value.</param>
        /// <returns>The computed value.</returns>
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

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="longitude">The longitude value.</param>
        /// <param name="latitude">The latitude value.</param>
        /// <param name="gridX">The gridX value.</param>
        /// <param name="gridY">The gridY value.</param>
        /// <returns>The computed value.</returns>
        internal bool TryMapToGridCoordinates(double longitude, double latitude, out double gridX, out double gridY)
        {
            if (TryMapRaw(longitude, latitude, out gridX, out gridY) && IsWithinGrid(gridX, gridY))
            {
                return true;
            }

            if (TryMapRaw(longitude + 360d, latitude, out gridX, out gridY) && IsWithinGrid(gridX, gridY))
            {
                return true;
            }

            if (TryMapRaw(longitude - 360d, latitude, out gridX, out gridY) && IsWithinGrid(gridX, gridY))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="sampleIndex">The sampleIndex value.</param>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns>The computed value.</returns>
        internal double GetSampleValue(int sampleIndex, int x, int y)
        {
            return this.sampleData.GetValue(sampleIndex, x, y);
        }

        private static bool IsWithinGrid(double x, double y)
        {
            return x >= -1e-8d && y >= -1e-8d;
        }

        private bool TryMapRaw(double longitude, double latitude, out double x, out double y)
        {
            double localX = longitude - this.C;
            double localY = latitude - this.F;
            x = ((localX * this.E) - (this.B * localY)) / this.determinant;
            y = ((this.A * localY) - (localX * this.D)) / this.determinant;
            return !double.IsNaN(x) && !double.IsNaN(y) && !double.IsInfinity(x) && !double.IsInfinity(y);
        }
    }
}
