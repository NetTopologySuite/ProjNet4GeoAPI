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
namespace ProjNET.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates M8 runtime parity for <c>tinshift</c>.
/// </summary>
public class Phase8TinShiftRuntimeTests
{
    /// <summary>
    /// Gets invalid creation scenarios.
    /// </summary>
    /// <returns>Invalid case dataset.</returns>
    public static IEnumerable<object[]> GetInvalidCreationCases()
    {
        yield return new object[] { "+proj=tinshift", "+file" };
        yield return new object[] { "+proj=tinshift +file=i_do_not_exist", "Cannot open" };
        yield return new object[] { "+proj=tinshift +file=" + FindFixturePath(Path.Combine("Fixtures", "gie", "tinshift.gie")), "invalid model" };
    }

    /// <summary>
    /// Gets forward vector scenarios from <c>tinshift.gie</c>.
    /// </summary>
    /// <returns>Forward case dataset.</returns>
    public static IEnumerable<object[]> GetForwardCases()
    {
        yield return Case(
            BuildTinShiftOperation("tinshift_crs_implicit.json"),
            CreatePoint(2d, 49d, 0d),
            CreatePoint(2.1d, 49.1d, 0d),
            1e-12d);

        yield return Case(
            BuildTinShiftOperation("tinshift_simplified_kkj_etrs.json"),
            CreatePoint(3210000d, 6700000d, 0d),
            CreatePoint(209948.3217d, 6697187.0009d, 0d),
            1e-4d);

        yield return Case(
            BuildTinShiftOperation("tinshift_simplified_n60_n2000.json"),
            CreatePoint(3210000d, 6700000d, 10d),
            CreatePoint(3210000d, 6700000d, 10.2886d),
            1e-4d);

        yield return Case(
            BuildTinShiftOperation("tinshift_fallback_nearest_side.json"),
            CreatePoint(2d, 3d, 0d),
            CreatePoint(4d, 6d, 0d),
            1e-9d);

        yield return Case(
            BuildTinShiftOperation("tinshift_fallback_nearest_centroid.json"),
            CreatePoint(3d, 0d, 0d),
            CreatePoint(3d, 0d, 0d),
            1e-9d);
    }

    /// <summary>
    /// Gets representative roundtrip scenarios.
    /// </summary>
    /// <returns>Roundtrip case dataset.</returns>
    public static IEnumerable<object[]> GetRoundtripCases()
    {
        yield return RoundtripCase(BuildTinShiftOperation("tinshift_crs_implicit.json"), CreatePoint(2d, 49d, 0d), 1e-9d);
        yield return RoundtripCase(BuildTinShiftOperation("tinshift_simplified_kkj_etrs.json"), CreatePoint(3210000d, 6700000d, 0d), 1e-6d);
        yield return RoundtripCase(BuildTinShiftOperation("tinshift_simplified_n60_n2000.json"), CreatePoint(3210000d, 6700000d, 10d), 1e-6d);
        yield return RoundtripCase(BuildTinShiftOperation("tinshift_fallback_nearest_side.json"), CreatePoint(2d, 3d, 0d), 1e-9d);
        yield return RoundtripCase(BuildTinShiftOperation("tinshift_fallback_nearest_centroid.json"), CreatePoint(3d, 0d, 0d), 1e-9d);
    }

    /// <summary>
    /// Gets portable vectors harvested from PROJ <c>test_tinshift.cpp</c>.
    /// </summary>
    /// <returns>Forward case dataset.</returns>
    public static IEnumerable<object[]> GetCppUnitForwardCases()
    {
        yield return Case(
            BuildTinShiftOperation("tinshift_unit_basic_horizontal.json"),
            CreatePoint(0d, 0d, 1000d),
            CreatePoint(101d, 101d, 1000d),
            1e-12d);

        yield return Case(
            BuildTinShiftOperation("tinshift_unit_basic_horizontal.json"),
            CreatePoint(0d, 0.5d, 1000d),
            CreatePoint(100.5d, 101d, 1000d),
            1e-12d);

        yield return Case(
            BuildTinShiftOperation("tinshift_unit_basic_horizontal.json"),
            CreatePoint(0.5d, 0.5d, 1000d),
            CreatePoint(100.5d, 100.5d, 1000d),
            1e-12d);

        yield return Case(
            BuildTinShiftOperation("tinshift_unit_vertical_source_target.json"),
            CreatePoint(0d, 0d, 1000d),
            CreatePoint(0d, 0d, 1000.1d),
            1e-12d);

        yield return Case(
            BuildTinShiftOperation("tinshift_unit_vertical_source_target.json"),
            CreatePoint(0.5d, 0.75d, 1000d),
            CreatePoint(0.5d, 0.75d, 1000.325d),
            1e-12d);

        yield return Case(
            BuildTinShiftOperation("tinshift_unit_vertical_offset.json"),
            CreatePoint(0d, 0d, 1000d),
            CreatePoint(0d, 0d, 1000.1d),
            1e-12d);

        yield return Case(
            BuildTinShiftOperation("tinshift_unit_vertical_offset.json"),
            CreatePoint(0.5d, 0.75d, 1000d),
            CreatePoint(0.5d, 0.75d, 1000.325d),
            1e-12d);

        yield return Case(
            BuildTinShiftOperation("tinshift_unit_horizontal_vertical.json"),
            CreatePoint(0d, 0d, 1000d),
            CreatePoint(101d, 101d, 1000.1d),
            1e-12d);

        yield return Case(
            BuildTinShiftOperation("tinshift_unit_horizontal_vertical.json"),
            CreatePoint(0.5d, 0.75d, 1000d),
            CreatePoint(100.25d, 100.5d, 1000.325d),
            1e-12d);
    }

    /// <summary>
    /// Gets portable inverse vectors harvested from PROJ <c>test_tinshift.cpp</c>.
    /// </summary>
    /// <returns>Inverse case dataset.</returns>
    public static IEnumerable<object[]> GetCppUnitInverseCases()
    {
        yield return Case(
            BuildTinShiftOperation("tinshift_unit_basic_horizontal.json") + " +inv",
            CreatePoint(100.25d, 100.5d, 1000d),
            CreatePoint(0.5d, 0.75d, 1000d),
            1e-12d);

        yield return Case(
            BuildTinShiftOperation("tinshift_unit_vertical_source_target.json") + " +inv",
            CreatePoint(0.5d, 0.75d, 1000.325d),
            CreatePoint(0.5d, 0.75d, 1000d),
            1e-12d);

        yield return Case(
            BuildTinShiftOperation("tinshift_unit_vertical_offset.json") + " +inv",
            CreatePoint(0.5d, 0.75d, 1000.325d),
            CreatePoint(0.5d, 0.75d, 1000d),
            1e-12d);

        yield return Case(
            BuildTinShiftOperation("tinshift_unit_horizontal_vertical.json") + " +inv",
            CreatePoint(100.25d, 100.5d, 1000.325d),
            CreatePoint(0.5d, 0.75d, 1000d),
            1e-12d);
    }

    /// <summary>
    /// Verifies file argument validation paths.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    /// <param name="expectedToken">Expected diagnostic token.</param>
    [Theory]
    [MemberData(nameof(GetInvalidCreationCases))]
    public void TinShiftCreationFailsForInvalidModelConfiguration(string operation, string expectedToken)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string skipReason);
        Assert.False(ok);
        Assert.Contains(expectedToken, skipReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies points outside triangulation fail without fallback strategy.
    /// </summary>
    [Fact]
    public void TinShiftOutsideTriangulationWithoutFallbackFails()
    {
        MathTransform transform = CreateTransform(BuildTinShiftOperation("tinshift_crs_implicit.json"));
        ArgumentException exception = Assert.Throws<ArgumentException>(() => transform.Transform(CreatePoint(0d, 0d, 0d)));
        Assert.Contains("failed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies the PROJ unit-case outside-triangle failure vector.
    /// </summary>
    [Fact]
    public void TinShiftCppUnitReferenceOutsideTriangleFails()
    {
        MathTransform transform = CreateTransform(BuildTinShiftOperation("tinshift_unit_basic_horizontal.json"));
        Assert.Throws<ArgumentException>(() => transform.Transform(CreatePoint(-0.1d, 0d, 1000d)));
    }

    /// <summary>
    /// Verifies both forward and inverse fail outside triangulation when fallback is none.
    /// </summary>
    /// <param name="inverse">Indicates whether inverse direction is tested.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TinShiftOutsideTriangulationFailsForDirection(bool inverse)
    {
        string operation = BuildTinShiftOperation("tinshift_crs_implicit.json");
        if (inverse)
        {
            operation += " +inv";
        }

        MathTransform transform = CreateTransform(operation);
        ArgumentException exception = Assert.Throws<ArgumentException>(() => transform.Transform(CreatePoint(0d, 0d, 0d)));
        Assert.Contains("failed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies forward vectors from <c>tinshift.gie</c>.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    /// <param name="input">Input coordinate.</param>
    /// <param name="expected">Expected coordinate.</param>
    /// <param name="tolerance">Maximum per-axis absolute tolerance.</param>
    [Theory]
    [MemberData(nameof(GetForwardCases))]
    public void TinShiftForwardVectorsMatchGie(string operation, double[] input, double[] expected, double tolerance)
    {
        MathTransform transform = CreateTransform(operation);
        double[] output = transform.Transform(input);
        AssertCoordinateClose(output, expected, tolerance);
    }

    /// <summary>
    /// Verifies roundtrip behavior using transform/inverse transform pairs.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    /// <param name="input">Input coordinate.</param>
    /// <param name="tolerance">Maximum per-axis absolute tolerance.</param>
    [Theory]
    [MemberData(nameof(GetRoundtripCases))]
    public void TinShiftRoundtripRecoversInput(string operation, double[] input, double tolerance)
    {
        MathTransform forward = CreateTransform(operation);
        MathTransform inverse = forward.Inverse();

        double[] projected = forward.Transform(input);
        double[] recovered = inverse.Transform(projected);
        AssertCoordinateClose(recovered, input, tolerance);
    }

    /// <summary>
    /// Verifies harvested forward vectors from PROJ unit tests.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    /// <param name="input">Input coordinate.</param>
    /// <param name="expected">Expected coordinate.</param>
    /// <param name="tolerance">Maximum per-axis absolute tolerance.</param>
    [Theory]
    [MemberData(nameof(GetCppUnitForwardCases))]
    public void TinShiftForwardVectorsMatchCppUnitReference(string operation, double[] input, double[] expected, double tolerance)
    {
        MathTransform transform = CreateTransform(operation);
        double[] output = transform.Transform(input);
        AssertCoordinateClose(output, expected, tolerance);
    }

    /// <summary>
    /// Verifies harvested inverse vectors from PROJ unit tests.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    /// <param name="input">Input coordinate.</param>
    /// <param name="expected">Expected coordinate.</param>
    /// <param name="tolerance">Maximum per-axis absolute tolerance.</param>
    [Theory]
    [MemberData(nameof(GetCppUnitInverseCases))]
    public void TinShiftInverseVectorsMatchCppUnitReference(string operation, double[] input, double[] expected, double tolerance)
    {
        MathTransform transform = CreateTransform(operation);
        double[] output = transform.Transform(input);
        AssertCoordinateClose(output, expected, tolerance);
    }

    private static object[] Case(string operation, double[] input, double[] expected, double tolerance)
    {
        return new object[] { operation, input, expected, tolerance };
    }

    private static object[] RoundtripCase(string operation, double[] input, double tolerance)
    {
        return new object[] { operation, input, tolerance };
    }

    private static string BuildTinShiftOperation(string fileName)
    {
        return "+proj=tinshift +file=" + FindTinShiftPath(fileName);
    }

    private static string FindTinShiftPath(string fileName)
    {
        return FindFixturePath(Path.Combine("Fixtures", "tinshift", fileName));
    }

    private static string FindFixturePath(string relativePath)
    {
        string direct = Path.Combine(AppContext.BaseDirectory, relativePath);
        if (File.Exists(direct))
        {
            return direct;
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "test", "ProjNet.Tests", relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException("Could not locate local test fixture under test\\ProjNet.Tests\\.", relativePath);
    }

    private static MathTransform CreateTransform(string operation)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);
        Assert.True(ok, skipReason);
        return transform;
    }

    private static double[] CreatePoint(double x, double y, double z)
    {
        return new[] { x, y, z };
    }

    private static void AssertCoordinateClose(double[] actual, double[] expected, double tolerance)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            double delta = Math.Abs(actual[i] - expected[i]);
            Assert.InRange(delta, 0d, tolerance);
        }
    }
}
