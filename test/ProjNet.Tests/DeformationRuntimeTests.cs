// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates M8 runtime parity for <c>deformation</c>.
/// </summary>
public class DeformationRuntimeTests
{
    /// <summary>
    /// Gets invalid creation scenarios.
    /// </summary>
    /// <returns>Invalid case dataset.</returns>
    public static IEnumerable<TheoryDataRow<string, string>> InvalidCreationCases
    {
        get
        {
            yield return new TheoryDataRow<string, string>("+proj=deformation +dt=1 +ellps=GRS80", "xy_grids");
            yield return new TheoryDataRow<string, string>("+proj=deformation +xy_grids=alaska +dt=1 +ellps=GRS80", "z_grids");
            yield return new TheoryDataRow<string, string>("+proj=deformation +z_grids=egm96_15.gtx +dt=1 +ellps=GRS80", "xy_grids");
            yield return new TheoryDataRow<string, string>("+proj=deformation +xy_grids=alaska +z_grids=egm96_15.gtx +ellps=GRS80", "+dt or +t_epoch");
            yield return new TheoryDataRow<string, string>("+proj=deformation +xy_grids=alaska +z_grids=egm96_15.gtx +dt=1 +t_epoch=2016 +ellps=GRS80", "mutually exclusive");
            yield return new TheoryDataRow<string, string>("+proj=deformation +xy_grids=nonexisting +z_grids=egm96_15.gtx +dt=1 +ellps=GRS80", "Required grid");
            yield return new TheoryDataRow<string, string>("+proj=deformation +xy_grids=alaska +z_grids=nonexisting +dt=1 +ellps=GRS80", "Required grid");
        }
    }

    /// <summary>
    /// Gets forward vector scenarios from <c>deformation.gie</c>.
    /// </summary>
    /// <returns>Forward case dataset.</returns>
    public static IEnumerable<TheoryDataRow<string, double[], double[], double>> ForwardCases
    {
        get
        {
            string legacyOperation = "+proj=deformation +xy_grids=alaska +z_grids=egm96_15.gtx +ellps=GRS80 +dt=16.0";
            yield return Case(
                legacyOperation,
                CreateCartesianPoint(-3004295.5882503074d, -1093474.1690603832d, 5500477.1338251457d),
                CreateCartesianPoint(-3004295.7000d, -1093474.2097d, 5500477.3397d),
                1e-4d);

            string geotiffOperation = "+proj=deformation +grids=nkgrf03vel_realigned_extract.tif +ellps=GRS80 +dt=1";
            yield return Case(
                geotiffOperation,
                GeographicToCartesian(21.5d, 63d, 0d),
                GeographicToCartesian(21.5000000049d, 62.9999999937d, 0.0083d),
                2e-4d);
        }
    }

    /// <summary>
    /// Gets inverse 4D scenarios that rely on <c>+t_epoch</c>.
    /// </summary>
    /// <returns>Inverse case dataset.</returns>
    public static IEnumerable<TheoryDataRow<string, double[], double[], double>> InverseCases
    {
        get
        {
            yield return Case(
                "+proj=deformation +xy_grids=alaska +z_grids=egm96_15.gtx +ellps=GRS80 +t_epoch=2016.0 +inv",
                CreateCartesianPointWithTime(-3004295.5882503074d, -1093474.1690603832d, 5500477.1338251457d, 2000.0d),
                CreateCartesianPointWithTime(-3004295.7000d, -1093474.2097d, 5500477.3397d, 2000.0d),
                1e-4d);
        }
    }

    /// <summary>
    /// Verifies argument-validation diagnostics for deformation setup.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    /// <param name="expectedToken">Expected diagnostic token.</param>
    [Theory]
    [MemberData(nameof(InvalidCreationCases))]
    public void DeformationCreationFailsForInvalidParameters(string operation, string expectedToken)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string? skipReason);
        Assert.False(ok);
        Assert.Contains(expectedToken, Assert.IsAssignableFrom<string>(skipReason), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies missing-time handling for both forward and inverse setup with <c>+t_epoch</c>.
    /// </summary>
    /// <param name="inverse">Whether inverse setup is used.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeformationRequiresObservationEpochForTEpochMode(bool inverse)
    {
        string operation = "+proj=deformation +xy_grids=alaska +z_grids=egm96_15.gtx +ellps=GRS80 +t_epoch=2016.0";
        if (inverse)
        {
            operation += " +inv";
        }

        MathTransform transform = CreateTransform(operation);
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => transform.Transform(CreateCartesianPointWithTime(-3004295.5882503074d, -1093474.1690603832d, 5500477.1338251457d, double.MaxValue)));
        Assert.Contains("time", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies coordinates outside configured grid extents are rejected.
    /// </summary>
    [Fact]
    public void DeformationOutsideGridFails()
    {
        MathTransform transform = CreateTransform("+proj=deformation +xy_grids=alaska +z_grids=egm96_15.gtx +ellps=GRS80 +dt=16");
        double[] input = GeographicToCartesian(-120d, 40d, 0d);
        ArgumentException exception = Assert.Throws<ArgumentException>(() => transform.Transform(input));
        Assert.Contains("outside", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies legacy ctable2+gtx and GeoTIFF vectors from <c>deformation.gie</c>.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    /// <param name="input">Input coordinate.</param>
    /// <param name="expected">Expected coordinate.</param>
    /// <param name="tolerance">Maximum per-axis absolute tolerance.</param>
    [Theory]
    [MemberData(nameof(ForwardCases))]
    [MemberData(nameof(InverseCases))]
    public void DeformationVectorsMatchGie(string operation, double[] input, double[] expected, double tolerance)
    {
        MathTransform transform = CreateTransform(operation);
        double[] output = transform.Transform(input);
        AssertCoordinateClose(output, expected, tolerance);
    }

    /// <summary>
    /// Verifies direct/iterative pairings produce reversible roundtrips.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    [Theory]
    [InlineData("+proj=deformation +xy_grids=alaska +z_grids=egm96_15.gtx +ellps=GRS80 +dt=16")]
    [InlineData("+proj=deformation +grids=nkgrf03vel_realigned_extract.tif +ellps=GRS80 +dt=1")]
    public void DeformationRoundtripRecoversInput(string operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        double[] input = operation.Contains("nkgrf", StringComparison.OrdinalIgnoreCase)
            ? GeographicToCartesian(21.5d, 63d, 0d)
            : CreateCartesianPoint(-3004295.5882503074d, -1093474.1690603832d, 5500477.1338251457d);

        MathTransform forward = CreateTransform(operation);
        MathTransform inverse = forward.Inverse();
        double[] projected = forward.Transform(input);
        double[] recovered = inverse.Transform(projected);
        AssertCoordinateClose(recovered, input, 2e-4d);
    }

    private static TheoryDataRow<string, double[], double[], double> Case(string operation, double[] input, double[] expected, double tolerance)
    {
        return new TheoryDataRow<string, double[], double[], double>(operation, input, expected, tolerance);
    }

    private static MathTransform CreateTransform(string operation)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform? transform, out string? skipReason);
        Assert.True(ok, skipReason);
        return Assert.IsAssignableFrom<MathTransform>(transform);
    }

    private static double[] CreateCartesianPoint(double x, double y, double z)
    {
        return new[] { x, y, z };
    }

    private static double[] CreateCartesianPointWithTime(double x, double y, double z, double t)
    {
        return new[] { x, y, z, t };
    }

    private static double[] GeographicToCartesian(double longitudeDegrees, double latitudeDegrees, double ellipsoidalHeight)
    {
        var parameters = new List<ProjectionParameter>
        {
            new ProjectionParameter("semi_major", Ellipsoid.GRS80.SemiMajorAxis),
            new ProjectionParameter("semi_minor", Ellipsoid.GRS80.SemiMinorAxis),
        };
        var transform = new GeocentricTransform(parameters, false);
        double x = longitudeDegrees;
        double y = latitudeDegrees;
        double z = ellipsoidalHeight;
        transform.Transform(ref x, ref y, ref z);
        return new[] { x, y, z };
    }

    private static void AssertCoordinateClose(double[] actual, double[] expected, double tolerance)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);
        Assert.Equal(expected.Length, actual.Length);
        int dimensionsToCompare = Math.Min(actual.Length, 3);
        for (int i = 0; i < dimensionsToCompare; i++)
        {
            double delta = Math.Abs(actual[i] - expected[i]);
            Assert.InRange(delta, 0d, tolerance);
        }

        if (actual.Length > 3)
        {
            Assert.Equal(expected[3], actual[3]);
        }
    }
}
