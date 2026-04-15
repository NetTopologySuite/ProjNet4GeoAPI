// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Applies horizontal grid-shift corrections loaded from GeoTIFF grids.
/// </summary>
/// <remarks>
/// <para>
/// Horizontal GeoTIFF grid shifts are applied by selecting the most specific
/// grid covering the input coordinate and interpolating longitude and latitude
/// offsets from the grid samples. The <c>hgridshift</c> path uses bilinear
/// interpolation, while the general <c>gridshift</c> path can honor GeoTIFF
/// metadata that requests biquadratic interpolation. Bilinear grids use a
/// fixed-point inverse iteration; biquadratic grids follow PROJ's
/// NOAA-compatible first-approximation reverse path.
/// </para>
/// <para>
/// The runtime was independently verified against PROJ's horizontal/grid-shift
/// documentation and the GeoTIFF grid specification used by PROJ.
/// </para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/transformations/hgridshift.html">PROJ: hgridshift.</seealso>
/// <seealso href="https://proj.org/en/stable/specifications/geodetictiffgrids.html">PROJ GeoTIFF grid specification.</seealso>
internal sealed class GeoTiffHGridShiftMathTransform : MathTransform
{
    private const double RelativeTolerance = 1e-5d;
    private const double InverseTolerance = 1e-12d;
    private readonly ReadOnlyCollection<HorizontalGrid> grids;
    private readonly bool? biquadraticInterpolationOverride;
    private readonly bool isInverted;
    private MathTransform? inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeoTiffHGridShiftMathTransform"/> class.
    /// </summary>
    /// <param name="gridPaths">Ordered GeoTIFF grid file paths to load.</param>
    /// <param name="biquadraticInterpolationOverride">
    /// Overrides the interpolation mode when <see langword="true"/> forces biquadratic interpolation,
    /// <see langword="false"/> forces bilinear interpolation, and <see langword="null"/> honors the grid metadata.
    /// </param>
    internal GeoTiffHGridShiftMathTransform(IReadOnlyList<string> gridPaths, bool? biquadraticInterpolationOverride = null)
    {
        gridPaths = ArgumentGuard.ThrowIfNull(gridPaths, nameof(gridPaths));
        this.biquadraticInterpolationOverride = biquadraticInterpolationOverride;

        this.grids = GridLoaderHelper.LoadMulti(
            gridPaths,
            nameof(gridPaths),
            "No horizontal grid could be loaded from GeoTIFF input.",
            static path => GeoTiffGridLoader.LoadHorizontal(path),
            static (left, right) => left.Area.CompareTo(right.Area));
    }

    private GeoTiffHGridShiftMathTransform(GeoTiffHGridShiftMathTransform source, bool isInverted)
    {
        source = ArgumentGuard.ThrowIfNull(source, nameof(source));

        this.grids = source.grids;
        this.biquadraticInterpolationOverride = source.biquadraticInterpolationOverride;
        this.isInverted = isInverted;
    }

    /// <inheritdoc />
    public override int DimSource => 3;

    /// <inheritdoc />
    public override int DimTarget => 3;

    /// <inheritdoc />
    public override bool Identity()
    {
        return false;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new GeoTiffHGridShiftMathTransform(this, !this.isInverted);

        return this.inverse;
    }

    /// <inheritdoc />
    public override void Invert()
    {
        throw new NotSupportedException("GeoTiffHGridShiftMathTransform is immutable. Use Inverse() to obtain inverted transform.");
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        if (!this.TryFindGridForPoint(x, y, out HorizontalGrid? gridCandidate))
        {
            TransformationThrowHelper.ThrowInvalidOperation("Coordinate is outside the horizontal GeoTIFF grid extent.");
        }

        HorizontalGrid grid = ArgumentGuard.ThrowIfNull(gridCandidate, nameof(gridCandidate));

        if (!this.isInverted)
        {
            (double lonShift, double latShift) = this.InterpolateShift(grid, x, y);
            x += lonShift;
            y += latShift;
            return;
        }

        this.InverseTransform(ref x, ref y, grid);
    }

    private void InverseTransform(ref double longitude, ref double latitude, HorizontalGrid initialGrid)
    {
        (double firstLonShift, double firstLatShift) = this.InterpolateShift(initialGrid, longitude, latitude);
        double targetLongitude = longitude;
        double targetLatitude = latitude;
        double candidateLongitude = targetLongitude - firstLonShift;
        double candidateLatitude = targetLatitude - firstLatShift;
        if (this.ShouldUseBiquadraticInterpolation(initialGrid))
        {
            // PROJ follows NOAA NCAT here and uses the first approximation for biquadratic reverse shifts.
            longitude = initialGrid.NormalizeFirstAxis(candidateLongitude);
            latitude = candidateLatitude;
            return;
        }

        int iterations = TransformationMath.MaxInverseIterations;
        while (iterations-- > 0)
        {
            (double iterLonShift, double iterLatShift) = this.InterpolateShift(initialGrid, candidateLongitude, candidateLatitude);
            double deltaLongitude = candidateLongitude + iterLonShift - targetLongitude;
            double deltaLatitude = candidateLatitude + iterLatShift - targetLatitude;
            candidateLongitude -= deltaLongitude;
            candidateLatitude -= deltaLatitude;
            if ((deltaLongitude * deltaLongitude) + (deltaLatitude * deltaLatitude) <= (InverseTolerance * InverseTolerance))
            {
                longitude = initialGrid.NormalizeFirstAxis(candidateLongitude);
                latitude = candidateLatitude;
                return;
            }
        }

        TransformationThrowHelper.ThrowInvalidOperation("Inverse horizontal GeoTIFF grid shift did not converge.");
    }

    private (double LonShift, double LatShift) InterpolateShift(HorizontalGrid grid, double longitude, double latitude)
    {
        if (!grid.TryMapToGridCoordinates(longitude, latitude, out double gridX, out double gridY))
        {
            TransformationThrowHelper.ThrowInvalidOperation("Coordinate is outside the horizontal GeoTIFF grid extent.");
        }

        return this.ShouldUseBiquadraticInterpolation(grid)
            ? InterpolationMath.InterpolateBiquadraticShift(grid, gridX, gridY)
            : InterpolationMath.InterpolateBilinearShift(grid, gridX, gridY);
    }

    private bool ShouldUseBiquadraticInterpolation(HorizontalGrid grid)
    {
        if (grid.Width < 3 || grid.Height < 3)
        {
            return false;
        }

        return this.biquadraticInterpolationOverride ?? grid.UsesBiquadraticInterpolation;
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
    /// Provides bilinear and biquadratic interpolation helpers for horizontal GeoTIFF grid samples.
    /// </summary>
    internal static class InterpolationMath
    {
        /// <summary>
        /// Interpolates a shift from the surrounding four grid samples.
        /// </summary>
        /// <param name="grid">The grid supplying the shift samples.</param>
        /// <param name="gridX">The fractional grid x-coordinate.</param>
        /// <param name="gridY">The fractional grid y-coordinate.</param>
        /// <returns>The interpolated longitude and latitude shift.</returns>
        internal static (double LonShift, double LatShift) InterpolateBilinearShift(HorizontalGrid grid, double gridX, double gridY)
        {
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

        /// <summary>
        /// Interpolates a shift from the surrounding nine grid samples using PROJ's biquadratic window.
        /// </summary>
        /// <param name="grid">The grid supplying the shift samples.</param>
        /// <param name="gridX">The fractional grid x-coordinate.</param>
        /// <param name="gridY">The fractional grid y-coordinate.</param>
        /// <returns>The interpolated longitude and latitude shift.</returns>
        internal static (double LonShift, double LatShift) InterpolateBiquadraticShift(HorizontalGrid grid, double gridX, double gridY)
        {
            int indexX = (int)Math.Floor(gridX);
            int indexY = (int)Math.Floor(gridY);
            double fractionX = gridX - indexX;
            double fractionY = gridY - indexY;
            NormalizeInterpolationCell(grid.Width, ref indexX, ref fractionX);
            NormalizeInterpolationCell(grid.Height, ref indexY, ref fractionY);
            NormalizeBiquadraticWindow(grid.Width, ref indexX, ref fractionX);
            NormalizeBiquadraticWindow(grid.Height, ref indexY, ref fractionY);

            Span<double> latitudeShiftByRow = stackalloc double[3];
            Span<double> longitudeShiftByRow = stackalloc double[3];
            for (int rowOffset = 0; rowOffset < 3; rowOffset++)
            {
                int sampleY = indexY + rowOffset;
                latitudeShiftByRow[rowOffset] = QuadraticInterpolate(
                    fractionX,
                    grid.GetLatitudeShift(indexX, sampleY),
                    grid.GetLatitudeShift(indexX + 1, sampleY),
                    grid.GetLatitudeShift(indexX + 2, sampleY));
                longitudeShiftByRow[rowOffset] = QuadraticInterpolate(
                    fractionX,
                    grid.GetLongitudeShift(indexX, sampleY),
                    grid.GetLongitudeShift(indexX + 1, sampleY),
                    grid.GetLongitudeShift(indexX + 2, sampleY));
            }

            return (
                QuadraticInterpolate(fractionY, longitudeShiftByRow[0], longitudeShiftByRow[1], longitudeShiftByRow[2]),
                QuadraticInterpolate(fractionY, latitudeShiftByRow[0], latitudeShiftByRow[1], latitudeShiftByRow[2]));
        }

        private static void NormalizeInterpolationCell(int size, ref int index, ref double fraction)
        {
            if (index < 0)
            {
                if (index == -1 && fraction > 1d - (10d * GeoTiffHGridShiftMathTransform.RelativeTolerance))
                {
                    index = 0;
                    fraction = 0d;
                    return;
                }

                TransformationThrowHelper.ThrowInvalidOperation("Coordinate is outside the horizontal GeoTIFF grid extent.");
            }

            if (index + 1 < size)
            {
                return;
            }

            if (index + 1 == size && fraction < 10d * GeoTiffHGridShiftMathTransform.RelativeTolerance)
            {
                index = size - 2;
                fraction = 1d;
                return;
            }

            TransformationThrowHelper.ThrowInvalidOperation("Coordinate is outside the horizontal GeoTIFF grid extent.");
        }

        private static void NormalizeBiquadraticWindow(int size, ref int index, ref double fraction)
        {
            if ((fraction <= 0.5d && index > 0) || (index + 2 == size))
            {
                index -= 1;
                fraction += 1d;
            }
        }

        private static double QuadraticInterpolate(double xToInterpolate, double f0, double f1, double f2)
        {
            double delta0 = f1 - f0;
            double delta1 = f2 - f1;
            double secondDelta0 = delta1 - delta0;
            return f0 + (xToInterpolate * delta0) + (0.5d * xToInterpolate * (xToInterpolate - 1d) * secondDelta0);
        }
    }

    /// <summary>
    /// Represents a single horizontal-shift grid loaded from a GeoTIFF file.
    /// </summary>
    internal sealed class HorizontalGrid : BaseGeoGrid
    {
        private readonly int latitudeSampleIndex;
        private readonly int longitudeSampleIndex;
        private readonly bool longitudeIsPositiveWest;
        private readonly double latitudeUnitScale;
        private readonly double longitudeUnitScale;
        private readonly bool normalizeFirstAxis;
        private readonly bool usesBiquadraticInterpolation;

        /// <summary>
        /// Initializes a new instance of the <see cref="HorizontalGrid"/> class.
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
        /// <param name="latitudeUnitScale">Scale factor to convert the raw second-axis shift sample to runtime coordinate units.</param>
        /// <param name="longitudeUnitScale">Scale factor to convert the raw first-axis shift sample to runtime coordinate units.</param>
        /// <param name="normalizeFirstAxis">
        /// <see langword="true"/> when the first axis represents wrapped longitudes and inverse results should be normalized.
        /// </param>
        /// <param name="usesBiquadraticInterpolation">
        /// <see langword="true"/> when the grid metadata requests biquadratic interpolation instead of bilinear interpolation.
        /// </param>
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
            double longitudeUnitScale,
            bool normalizeFirstAxis,
            bool usesBiquadraticInterpolation)
            : base(sourcePath, width, height, area, epsilon, west, east, south, north, a, b, c, d, e, f, sampleData)
        {
            this.latitudeSampleIndex = latitudeSampleIndex;
            this.longitudeSampleIndex = longitudeSampleIndex;
            this.longitudeIsPositiveWest = longitudeIsPositiveWest;
            this.latitudeUnitScale = latitudeUnitScale;
            this.longitudeUnitScale = longitudeUnitScale;
            this.normalizeFirstAxis = normalizeFirstAxis;
            this.usesBiquadraticInterpolation = usesBiquadraticInterpolation;
        }

        /// <summary>
        /// Gets a value indicating whether this grid uses biquadratic interpolation.
        /// </summary>
        internal bool UsesBiquadraticInterpolation => this.usesBiquadraticInterpolation;

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

        /// <summary>
        /// Normalizes the first-axis result when the grid operates in wrapped-longitude space.
        /// </summary>
        /// <param name="value">The first-axis value produced by inverse interpolation.</param>
        /// <returns>The normalized first-axis value.</returns>
        internal double NormalizeFirstAxis(double value)
        {
            return this.normalizeFirstAxis ? TransformationMath.NormalizeLongitudeDegrees(value) : value;
        }
    }
}
