// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Tests for GeoTIFF-based horizontal and vertical grid shift operations at runtime.
/// </summary>
public class GeoTiffGridRuntimeTests
{
    private static readonly double[] GeoTiffGridInput = [4.5d, 52.5d, 0d];
    private static readonly double[] GeoTiffNodataInput = [4.05d, 52.1d, 0d];
    private static readonly double[] ProjectedGeoTiffGridInput = [-598000d, -1160019.9999d, 0d];

    /// <summary>
    /// Verifies that a <c>hgridshift</c> operation backed by a GeoTIFF horizontal grid file applies the expected coordinate shift.
    /// </summary>
    /// <param name="gridFileName">GeoTIFF horizontal grid fixture file name.</param>
    [Theory]
    [InlineData("test_hgrid.tif")]
    [InlineData("test_hgrid_positive_west.tif")]
    public void HgridshiftWithGeoTiffGridAppliesExpectedShift(string gridFileName)
    {
        string gridPath = FindGridPath(gridFileName);
        string operation = $"+proj=hgridshift +grids={gridPath}";

        bool ok = ProjPipelineMathTransformFactory.TryCreateMathTransform(operation, out MathTransform? transform, out string? skipReason);
        Assert.True(ok, skipReason);

        double[] output = Assert.IsType<MathTransform>(transform, exactMatch: false).Transform(GeoTiffGridInput);
        Assert.Equal(5.875d, output[0], 9);
        Assert.Equal(55.375d, output[1], 9);
    }

    /// <summary>
    /// Verifies that the direct <see cref="GeoTiffHGridShiftMathTransform"/> path applies the expected forward shift for the 2-D horizontal fixtures.
    /// </summary>
    /// <param name="gridFileName">GeoTIFF horizontal grid fixture file name.</param>
    [Theory]
    [InlineData("test_hgrid.tif")]
    [InlineData("test_hgrid_positive_west.tif")]
    public void GeoTiffHGridShiftMathTransformAppliesExpectedForwardShift(string gridFileName)
    {
        string gridPath = FindGridPath(gridFileName);
        var transform = new GeoTiffHGridShiftMathTransform([gridPath]);

        double[] output = transform.Transform(GeoTiffGridInput);

        Assert.Equal(5.875d, output[0], 9);
        Assert.Equal(55.375d, output[1], 9);
        Assert.Equal(0d, output[2], 12);
    }

    /// <summary>
    /// Verifies that the inverse <see cref="GeoTiffHGridShiftMathTransform"/> signals an outside-grid failure for the synthetic 2-D fixtures once the reverse iteration leaves the valid extent.
    /// </summary>
    /// <param name="gridFileName">GeoTIFF horizontal grid fixture file name.</param>
    [Theory]
    [InlineData("test_hgrid.tif")]
    [InlineData("test_hgrid_positive_west.tif")]
    public void GeoTiffHGridShiftMathTransformInverseSignalsOutsideGridForSyntheticFixture(string gridFileName)
    {
        string gridPath = FindGridPath(gridFileName);
        MathTransform inverse = new GeoTiffHGridShiftMathTransform([gridPath]).Inverse();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => inverse.Transform([5d, 53d, 0d]));

        Assert.Contains("outside the horizontal GeoTIFF grid extent", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that a projected GeoTIFF <c>gridshift</c> grid applies both the raster delta and the metadata-defined constant offsets in projected coordinates.
    /// </summary>
    [Fact]
    public void GridshiftWithProjectedGeoTiffGridAppliesProjectedShift()
    {
        string gridPath = FindGridPath("test_gridshift_projected.tif");
        string operation = $"+proj=gridshift +grids={gridPath}";

        bool ok = ProjPipelineMathTransformFactory.TryCreateMathTransform(operation, out MathTransform? transform, out string? skipReason);
        Assert.True(ok, skipReason);

        double[] output = Assert.IsType<MathTransform>(transform, exactMatch: false).Transform(ProjectedGeoTiffGridInput);
        Assert.InRange(output[0], -5597999.884994847700000d - 1e-9d, -5597999.884994847700000d + 1e-9d);
        Assert.InRange(output[1], -6160019.977750200778246d - 1e-9d, -6160019.977750200778246d + 1e-9d);
        Assert.Equal(0d, output[2], 12);
    }

    /// <summary>
    /// Verifies that a projected GeoTIFF <c>gridshift</c> grid can force PROJ-style bilinear interpolation explicitly.
    /// </summary>
    [Fact]
    public void GridshiftWithProjectedGeoTiffGridCanForceBilinearInterpolation()
    {
        string gridPath = FindGridPath("test_gridshift_projected.tif");
        string operation = $"+proj=gridshift +grids={gridPath} +interpolation=bilinear";

        bool ok = ProjPipelineMathTransformFactory.TryCreateMathTransform(operation, out MathTransform? transform, out string? skipReason);
        Assert.True(ok, skipReason);

        double[] output = Assert.IsType<MathTransform>(transform, exactMatch: false).Transform(ProjectedGeoTiffGridInput);
        Assert.InRange(output[0], -5597999.884979997761548d - 1e-9d, -5597999.884979997761548d + 1e-9d);
        Assert.InRange(output[1], -6160019.977770000696182d - 1e-9d, -6160019.977770000696182d + 1e-9d);
        Assert.Equal(0d, output[2], 12);
    }

    /// <summary>
    /// Verifies that a <c>vgridshift</c> operation backed by a GeoTIFF vertical grid file applies the expected vertical shift and leaves horizontal coordinates unchanged.
    /// </summary>
    /// <param name="gridFileName">GeoTIFF vertical grid fixture file name.</param>
    [Theory]
    [InlineData("test_vgrid_pixelispoint.tif")]
    [InlineData("test_vgrid_uint16_with_scale_offset.tif")]
    public void VgridshiftWithGeoTiffGridAppliesExpectedDefaultShift(string gridFileName)
    {
        string gridPath = FindGridPath(gridFileName);
        string operation = $"+proj=vgridshift +grids={gridPath} +multiplier=1";

        bool ok = ProjPipelineMathTransformFactory.TryCreateMathTransform(operation, out MathTransform? transform, out string? skipReason);
        Assert.True(ok, skipReason);

        double[] output = Assert.IsType<MathTransform>(transform, exactMatch: false).Transform(GeoTiffGridInput);
        Assert.Equal(4.5d, output[0], 9);
        Assert.Equal(52.5d, output[1], 9);
        Assert.Equal(11.5d, output[2], 9);
    }

    /// <summary>
    /// Verifies that a <c>vgridshift</c> operation backed by a GeoTIFF grid with nodata cells performs weighted interpolation to produce the expected Z value.
    /// </summary>
    [Fact]
    public void VgridshiftWithGeoTiffNodataPerformsWeightedInterpolation()
    {
        string gridPath = FindGridPath("test_vgrid_nodata.tif");
        string operation = $"+proj=vgridshift +grids={gridPath} +multiplier=1";

        bool ok = ProjPipelineMathTransformFactory.TryCreateMathTransform(operation, out MathTransform? transform, out string? skipReason);
        Assert.True(ok, skipReason);

        double[] output = Assert.IsType<MathTransform>(transform, exactMatch: false).Transform(GeoTiffNodataInput);
        Assert.Equal(10d, output[2], 7);
    }

    /// <summary>
    /// Verifies temporary GeoTIFF sample buffers are returned to the configured array pool on successful load.
    /// </summary>
    [Fact]
    public void LoadHorizontalReturnsRentedSampleBuffersToArrayPoolOnSuccess()
    {
        string gridPath = FindGridPath("test_hgrid.tif");
        var pool = new TrackingDoubleArrayPool();

        IReadOnlyList<GeoTiffHGridShiftMathTransform.HorizontalGrid> grids = GeoTiffGridLoader.LoadHorizontal(gridPath, pool);

        Assert.NotEmpty(grids);
        Assert.Equal(pool.RentedArrays.Count, pool.ReturnedArrays.Count);
        foreach (double[] rented in pool.RentedArrays)
        {
            int returnCount = 0;
            foreach (double[] returned in pool.ReturnedArrays)
            {
                if (ReferenceEquals(rented, returned))
                {
                    returnCount++;
                }
            }

            Assert.Equal(1, returnCount);
        }
    }

    /// <summary>
    /// Verifies partially-rented temporary GeoTIFF sample buffers are returned when loading fails.
    /// </summary>
    [Fact]
    public void LoadHorizontalReturnsAlreadyRentedSampleBuffersWhenRentThrows()
    {
        string gridPath = FindGridPath("test_hgrid.tif");
        var pool = new ThrowingAfterFirstRentArrayPool();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => GeoTiffGridLoader.LoadHorizontal(gridPath, pool));

        Assert.Contains("rent", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(pool.RentedArrays);
        Assert.Single(pool.ReturnedArrays);
        Assert.Same(pool.RentedArrays[0], pool.ReturnedArrays[0]);
    }

    /// <summary>
    /// Verifies raw grid coordinates beyond the last interpolable cell are rejected before interpolation can address cells outside the raster.
    /// </summary>
    [Fact]
    public void TryMapToGridCoordinatesRejectsUpperBoundBeyondLastInterpolableCell()
    {
        var sampleData = new SampleData([[0d, 0d, 0d, 0d]], width: 2);
        var grid = new TestGeoGrid(
            width: 2,
            height: 2,
            west: 0d,
            east: 2d,
            south: 0d,
            north: 1d,
            a: 1d,
            b: 0d,
            c: 0d,
            d: 0d,
            e: 1d,
            f: 0d,
            sampleData: sampleData);

        bool inside = grid.TryMapToGridCoordinates(1.5d, 0.5d, out double gridX, out double gridY);

        Assert.True(grid.Contains(1.5d, 0.5d));
        Assert.False(inside);
        Assert.True(double.IsFinite(gridX));
        Assert.True(double.IsFinite(gridY));
    }

    private static string FindGridPath(string fileName)
    {
        string direct = Path.Combine(AppContext.BaseDirectory, "Fixtures", "grids", fileName);
        if (File.Exists(direct))
        {
            return direct;
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "test", "ProjNet.Tests", "Fixtures", "grids", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException("Could not locate a GeoTIFF grid fixture under test\\ProjNet.Tests\\Fixtures\\grids.", fileName);
    }

    private class TrackingDoubleArrayPool : ArrayPool<double>
    {
        private readonly ArrayPool<double> inner = ArrayPool<double>.Shared;

        public List<double[]> RentedArrays { get; } = [];

        public List<double[]> ReturnedArrays { get; } = [];

        public override double[] Rent(int minimumLength)
        {
            double[] buffer = this.inner.Rent(minimumLength);
            this.RentedArrays.Add(buffer);
            return buffer;
        }

        public override void Return(double[] array, bool clearArray = false)
        {
            this.ReturnedArrays.Add(array);
            this.inner.Return(array, clearArray);
        }
    }

    private sealed class ThrowingAfterFirstRentArrayPool : TrackingDoubleArrayPool
    {
        private int rentCount;

        public override double[] Rent(int minimumLength)
        {
            this.rentCount++;
            return this.rentCount > 1 ? throw new InvalidOperationException("Simulated rent failure") : base.Rent(minimumLength);
        }
    }

    private sealed class TestGeoGrid : BaseGeoGrid
    {
        internal TestGeoGrid(
            int width,
            int height,
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
            : base("test", width, height, area: width * height, epsilon: 0d, west, east, south, north, a, b, c, d, e, f, sampleData)
        {
        }
    }
}
