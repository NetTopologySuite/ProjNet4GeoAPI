// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates M8 runtime parity for <c>xyzgridshift</c>.
/// </summary>
public class XyzGridShiftRuntimeTests
{
    private const string GridPlaceholder = "{GRID}";

    /// <summary>
    /// Verifies that xyzgridshift can be created and applies a measurable shift.
    /// </summary>
    /// <param name="gridRef">Grid reference mode.</param>
    [Theory]
    [InlineData("input_crs")]
    [InlineData("output_crs")]
    public void XyzGridShiftCanBeCreatedAndShiftsCoordinates(string gridRef)
    {
        string gridPath = FindGridPath("subset_of_gr3df97a.tif");
        string operation = "+proj=xyzgridshift +grids=" + gridPath + " +grid_ref=" + gridRef + " +ellps=GRS80";
        string noShiftOperation = operation + " +multiplier=0";
        MathTransform transform = CreateTransform(operation);
        MathTransform noShiftTransform = CreateTransform(noShiftOperation);

        double[] input = CreateSampleInputPoint(gridPath);
        double[] output = transform.Transform(input);
        double[] noShift = noShiftTransform.Transform(input);

        Assert.False(double.IsNaN(output[0]) || double.IsInfinity(output[0]));
        Assert.False(double.IsNaN(output[1]) || double.IsInfinity(output[1]));
        Assert.False(double.IsNaN(output[2]) || double.IsInfinity(output[2]));

        double shiftedDistance = Math.Sqrt(
            ((output[0] - noShift[0]) * (output[0] - noShift[0]))
            + ((output[1] - noShift[1]) * (output[1] - noShift[1]))
            + ((output[2] - noShift[2]) * (output[2] - noShift[2])));
        Assert.InRange(shiftedDistance, 1e-6d, 5_000d);
    }

    /// <summary>
    /// Verifies multiplier scaling behaves linearly.
    /// </summary>
    [Fact]
    public void XyzGridShiftMultiplierScalesShiftLinearly()
    {
        string gridPath = FindGridPath("subset_of_gr3df97a.tif");
        MathTransform half = CreateTransform("+proj=xyzgridshift +grids=" + gridPath + " +grid_ref=input_crs +ellps=GRS80 +multiplier=0.5");
        MathTransform full = CreateTransform("+proj=xyzgridshift +grids=" + gridPath + " +grid_ref=input_crs +ellps=GRS80 +multiplier=1");

        double[] input = CreateSampleInputPoint(gridPath);
        double[] outputHalf = half.Transform(input);
        double[] outputFull = full.Transform(input);

        double halfDx = outputHalf[0] - input[0];
        double halfDy = outputHalf[1] - input[1];
        double halfDz = outputHalf[2] - input[2];
        double fullDx = outputFull[0] - input[0];
        double fullDy = outputFull[1] - input[1];
        double fullDz = outputFull[2] - input[2];

        Assert.InRange(Math.Abs((2d * halfDx) - fullDx), 0d, 1e-4);
        Assert.InRange(Math.Abs((2d * halfDy) - fullDy), 0d, 1e-4);
        Assert.InRange(Math.Abs((2d * halfDz) - fullDz), 0d, 1e-4);
    }

    /// <summary>
    /// Verifies direct/iterative pairings produce reversible roundtrips.
    /// </summary>
    /// <param name="gridRef">Grid reference mode.</param>
    [Theory]
    [InlineData("input_crs")]
    [InlineData("output_crs")]
    public void XyzGridShiftRoundtripRecoversInput(string gridRef)
    {
        string gridPath = FindGridPath("subset_of_gr3df97a.tif");
        string baseOperation = "+proj=xyzgridshift +grids=" + gridPath + " +grid_ref=" + gridRef + " +ellps=GRS80";
        MathTransform forward = CreateTransform(baseOperation);
        MathTransform inverse = CreateTransform(baseOperation + " +inv");

        double[] input = CreateSampleInputPoint(gridPath);
        double[] projected = forward.Transform(input);
        double[] recovered = inverse.Transform(projected);

        Assert.InRange(Math.Abs(recovered[0] - input[0]), 0d, 1e-3);
        Assert.InRange(Math.Abs(recovered[1] - input[1]), 0d, 1e-3);
        Assert.InRange(Math.Abs(recovered[2] - input[2]), 0d, 1e-3);
    }

    /// <summary>
    /// Verifies argument-validation diagnostics for xyzgridshift setup.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    /// <param name="expectedToken">Expected diagnostic token.</param>
    [Theory]
    [InlineData("+proj=xyzgridshift +ellps=GRS80", "+grids")]
    [InlineData("+proj=xyzgridshift +grids={GRID} +ellps=GRS80 +grid_ref=invalid", "grid_ref")]
    [InlineData("+proj=xyzgridshift +grids={GRID} +ellps=GRS80 +multiplier=abc", "multiplier")]
    [InlineData("+proj=xyzgridshift +grids={GRID} +grid_ref=input_crs", "ellipsoid")]
    public void XyzGridShiftCreationFailsForInvalidParameters(string operation, string expectedToken)
    {
        if (operation.Contains(GridPlaceholder, StringComparison.Ordinal))
        {
            operation = operation.Replace(GridPlaceholder, FindGridPath("subset_of_gr3df97a.tif"), StringComparison.Ordinal);
        }

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string skipReason);
        Assert.False(ok);
        Assert.Contains(expectedToken, skipReason, StringComparison.OrdinalIgnoreCase);
    }

    private static MathTransform CreateTransform(string operation)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);
        Assert.True(ok, skipReason);
        return transform;
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

        throw new FileNotFoundException("Could not locate local test grid fixture under test\\ProjNet.Tests\\Fixtures\\grids.", fileName);
    }

    private static double[] CreateSampleInputPoint(string gridPath)
    {
        IReadOnlyList<GeoTiffXyzGridShiftMathTransform.XyzGrid> grids = GeoTiffGridLoader.LoadXyz(gridPath);
        if (grids.Count == 0)
        {
            throw new InvalidOperationException("No xyz grid pages were loaded.");
        }

        for (int i = 0; i < grids.Count; i++)
        {
            GeoTiffXyzGridShiftMathTransform.XyzGrid grid = grids[i];
            if (TrySelectSamplePoint(grid, out double lon, out double lat))
            {
                return GeographicToGeocentric(lon, lat, 0d);
            }
        }

        throw new InvalidOperationException("Could not locate a stable in-grid xyzgridshift sample point.");
    }

    private static bool TrySelectSamplePoint(GeoTiffXyzGridShiftMathTransform.XyzGrid grid, out double lon, out double lat)
    {
        lon = 0d;
        lat = 0d;
        for (int y = 0; y < grid.Height - 1; y++)
        {
            for (int x = 0; x < grid.Width - 1; x++)
            {
                if (GetCellShiftMagnitude(grid, x, y) <= 1e-12d)
                {
                    continue;
                }

                double candidateGridX = x + 0.5d;
                double candidateGridY = y + 0.5d;
                double candidateLon = (grid.A * candidateGridX) + (grid.B * candidateGridY) + grid.C;
                double candidateLat = (grid.D * candidateGridX) + (grid.E * candidateGridY) + grid.F;
                if (!grid.TryMapToGridCoordinates(candidateLon, candidateLat, out double mappedX, out double mappedY))
                {
                    continue;
                }

                if (mappedX <= 0.25d
                    || mappedY <= 0.25d
                    || mappedX >= (grid.Width - 1.25d)
                    || mappedY >= (grid.Height - 1.25d))
                {
                    continue;
                }

                lon = candidateLon;
                lat = candidateLat;
                return true;
            }
        }

        return false;
    }

    private static double GetCellShiftMagnitude(GeoTiffXyzGridShiftMathTransform.XyzGrid grid, int x, int y)
    {
        double dx = Math.Abs(grid.GetXShift(x, y))
            + Math.Abs(grid.GetXShift(x + 1, y))
            + Math.Abs(grid.GetXShift(x, y + 1))
            + Math.Abs(grid.GetXShift(x + 1, y + 1));
        double dy = Math.Abs(grid.GetYShift(x, y))
            + Math.Abs(grid.GetYShift(x + 1, y))
            + Math.Abs(grid.GetYShift(x, y + 1))
            + Math.Abs(grid.GetYShift(x + 1, y + 1));
        double dz = Math.Abs(grid.GetZShift(x, y))
            + Math.Abs(grid.GetZShift(x + 1, y))
            + Math.Abs(grid.GetZShift(x, y + 1))
            + Math.Abs(grid.GetZShift(x + 1, y + 1));
        return dx + dy + dz;
    }

    private static double[] GeographicToGeocentric(double longitudeDegrees, double latitudeDegrees, double height)
    {
        var parameters = new List<ProjectionParameter>
        {
            new ProjectionParameter("semi_major", Ellipsoid.GRS80.SemiMajorAxis),
            new ProjectionParameter("semi_minor", Ellipsoid.GRS80.SemiMinorAxis),
        };
        var transform = new GeocentricTransform(parameters, false);
        double x = longitudeDegrees;
        double y = latitudeDegrees;
        double z = height;
        transform.Transform(ref x, ref y, ref z);
        return [x, y, z];
    }

}
