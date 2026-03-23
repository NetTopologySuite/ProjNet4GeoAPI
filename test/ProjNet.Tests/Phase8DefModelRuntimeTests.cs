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
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates M8 runtime parity for <c>defmodel</c>.
/// </summary>
public class Phase8DefModelRuntimeTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();
    private static readonly Lazy<MathTransform> MercatorInverseTransform = new Lazy<MathTransform>(CreateMercatorInverseTransform);

    /// <summary>
    /// Gets invalid creation scenarios.
    /// </summary>
    /// <returns>Invalid case dataset.</returns>
    public static IEnumerable<object[]> GetInvalidCreationCases()
    {
        yield return new object[] { "+proj=defmodel", "+model" };
        yield return new object[] { "+proj=defmodel +model=i_do_not_exist", "Cannot open" };
        yield return new object[] { "+proj=defmodel +model=" + FindFixturePath(Path.Combine("Fixtures", "gie", "defmodel.gie")), "invalid model" };
    }

    /// <summary>
    /// Gets forward vector scenarios from <c>defmodel.gie</c>.
    /// </summary>
    /// <returns>Forward case dataset.</returns>
    public static IEnumerable<object[]> GetForwardCases()
    {
        yield return Case(
            BuildDefModelOperation("simple_model_degree_horizontal.json"),
            CreatePoint(2d, 49d, 30d, 2020d),
            CreatePoint(3d, 51d, 30d, 2020d),
            1e-9d);

        yield return Case(
            BuildDefModelOperation("simple_model_degree_3d.json"),
            CreatePoint(2d, 49d, 30d, 2020d),
            CreatePoint(3d, 51d, 33d, 2020d),
            1e-9d);

        yield return Case(
            BuildDefModelOperation("simple_model_metre_horizontal.json"),
            ConvertMercatorProjectedPointToGeographic(CreatePoint(10d, 20d, 30d, 2020d)),
            ConvertMercatorProjectedPointToGeographic(CreatePoint(11d, 22d, 30d, 2020d)),
            1e-8d);

        yield return Case(
            BuildDefModelOperation("simple_model_metre_3d.json"),
            ConvertMercatorProjectedPointToGeographic(CreatePoint(10d, 20d, 30d, 2020d)),
            ConvertMercatorProjectedPointToGeographic(CreatePoint(11d, 22d, 33d, 2020d)),
            1e-8d);

        string projectedOperation = BuildDefModelOperation("simple_model_projected.json");
        yield return Case(projectedOperation, CreatePoint(1500200.0, 5400400.0, 30d, 2020d), CreatePoint(1500200.588, 5400399.722, 30.6084, 2020d), 1e-6d);
        yield return Case(projectedOperation, CreatePoint(1500000.0, 5400000.0, 30d, 2020d), CreatePoint(1500000.4, 5399999.8, 30.84, 2020d), 1e-6d);
        yield return Case(projectedOperation, CreatePoint(1501000.0, 5400000.0, 30d, 2020d), CreatePoint(1501000.5, 5399999.75, 30.75, 2020d), 1e-6d);
        yield return Case(projectedOperation, CreatePoint(1500000.0, 5401000.0, 30d, 2020d), CreatePoint(1500000.8, 5400999.6, 30.36, 2020d), 1e-6d);
        yield return Case(projectedOperation, CreatePoint(1501000.0, 5401000.0, 30d, 2020d), CreatePoint(1501001.0, 5400999.7, 30d, 2020d), 1e-6d);

        yield return Case(
            BuildDefModelOperation("simple_model_metre_3d_geocentric.json"),
            ConvertMercatorProjectedPointToGeographic(CreatePoint(10d, 20d, 30d, 2020d)),
            ConvertMercatorProjectedPointToGeographic(CreatePoint(11d, 22d, 33d, 2020d)),
            1e-8d);

        yield return Case(
            BuildDefModelOperation("simple_model_metre_vertical.json"),
            CreatePoint(2d, 49d, 30d, 2020d),
            CreatePoint(2d, 49d, 33d, 2020d),
            1e-7d);

        yield return Case(
            BuildDefModelOperation("simple_model_metre_vertical.json"),
            CreatePoint(362d, 49d, 30d, 2020d),
            CreatePoint(2d, 49d, 33d, 2020d),
            1e-7d);

        yield return Case(
            BuildDefModelOperation("simple_model_wrap_east.json"),
            CreatePoint(165.9d, -37.3d, 10d, 2020d),
            CreatePoint(165.9d, -37.3d, 10.4525d, 2020d),
            1e-6d);

        yield return Case(
            BuildDefModelOperation("simple_model_wrap_west.json"),
            CreatePoint(165.9d, -37.3d, 10d, 2020d),
            CreatePoint(165.9d, -37.3d, 10.4525d, 2020d),
            1e-6d);

        string polarOperation = BuildDefModelOperation("simple_model_polar.json");
        yield return Case(polarOperation, CreatePoint(20d, -90d, 15d, 2020d), CreatePoint(27.4743245365d, -89.9999747721d, 18d, 2020d), 3e-5d);
        yield return Case(polarOperation, CreatePoint(120d, -90d, 15d, 2020d), CreatePoint(27.4737934098d, -89.9999747718d, 18d, 2020d), 3e-5d);
        yield return Case(polarOperation, CreatePoint(235d, -89.5d, 15d, 2020d), CreatePoint(-124.9986638571d, -89.5000223708d, 17.375d, 2020d), 3e-5d);
        yield return Case(polarOperation, CreatePoint(45d, -89.5d, 15d, 2020d), CreatePoint(44.9991295392d, -89.4999759438d, 18.5469d, 2020d), 3e-5d);
    }

    /// <summary>
    /// Gets representative roundtrip scenarios.
    /// </summary>
    /// <returns>Roundtrip case dataset.</returns>
    public static IEnumerable<object[]> GetRoundtripCases()
    {
        yield return RoundtripCase(BuildDefModelOperation("simple_model_degree_horizontal.json"), CreatePoint(2d, 49d, 30d, 2020d), 1e-8d);
        yield return RoundtripCase(BuildDefModelOperation("simple_model_degree_3d.json"), CreatePoint(2d, 49d, 30d, 2020d), 1e-8d);
        yield return RoundtripCase(BuildDefModelOperation("simple_model_metre_horizontal.json"), ConvertMercatorProjectedPointToGeographic(CreatePoint(10d, 20d, 30d, 2020d)), 1e-8d);
        yield return RoundtripCase(BuildDefModelOperation("simple_model_metre_3d.json"), ConvertMercatorProjectedPointToGeographic(CreatePoint(10d, 20d, 30d, 2020d)), 1e-8d);
        yield return RoundtripCase(BuildDefModelOperation("simple_model_metre_3d_geocentric.json"), ConvertMercatorProjectedPointToGeographic(CreatePoint(10d, 20d, 30d, 2020d)), 1e-8d);
        yield return RoundtripCase(BuildDefModelOperation("simple_model_metre_vertical.json"), CreatePoint(2d, 49d, 30d, 2020d), 1e-8d);
        yield return RoundtripCase(BuildDefModelOperation("simple_model_polar.json"), CreatePoint(45d, -89.5d, 15d, 2020d), 2e-5d);
        yield return RoundtripCase(BuildDefModelOperation("simple_model_projected.json"), CreatePoint(1500200d, 5400400d, 30d, 2020d), 1e-4d);
    }

    /// <summary>
    /// Verifies model argument validation paths.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    /// <param name="expectedToken">Expected diagnostic token.</param>
    [Theory]
    [MemberData(nameof(GetInvalidCreationCases))]
    public void DefModelCreationFailsForInvalidModelConfiguration(string operation, string expectedToken)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string skipReason);
        Assert.False(ok);
        Assert.Contains(expectedToken, skipReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies missing-time handling for both forward and inverse setup.
    /// </summary>
    /// <param name="inverse">Whether inverse setup is used.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefModelRequiresObservationEpoch(bool inverse)
    {
        string operation = BuildDefModelOperation("simple_model_degree_horizontal.json");
        if (inverse)
        {
            operation += " +inv";
        }

        MathTransform transform = CreateTransform(operation);
        ArgumentException exception = Assert.Throws<ArgumentException>(() => transform.Transform(CreatePoint(2d, 49d, 30d, double.MaxValue)));
        Assert.Contains("observation epoch", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies forward vectors from <c>defmodel.gie</c>.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    /// <param name="input">Input coordinate.</param>
    /// <param name="expected">Expected coordinate.</param>
    /// <param name="tolerance">Maximum per-axis absolute tolerance.</param>
    [Theory]
    [MemberData(nameof(GetForwardCases))]
    public void DefModelForwardVectorsMatchGie(string operation, double[] input, double[] expected, double tolerance)
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
    public void DefModelRoundtripRecoversInput(string operation, double[] input, double tolerance)
    {
        MathTransform forward = CreateTransform(operation);
        MathTransform inverse = forward.Inverse();

        double[] projected = forward.Transform(input);
        double[] recovered = inverse.Transform(projected);
        AssertCoordinateClose(recovered, input, tolerance);
    }

    private static object[] Case(string operation, double[] input, double[] expected, double tolerance)
    {
        return new object[] { operation, input, expected, tolerance };
    }

    private static object[] RoundtripCase(string operation, double[] input, double tolerance)
    {
        return new object[] { operation, input, tolerance };
    }

    private static string BuildDefModelOperation(string modelFileName)
    {
        return "+proj=defmodel +model=" + FindDefModelPath(modelFileName);
    }

    private static string FindDefModelPath(string modelFileName)
    {
        return FindFixturePath(Path.Combine("Fixtures", "defmodel", modelFileName));
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

    private static double[] CreatePoint(double x, double y, double z, double t)
    {
        return new[] { x, y, z, t };
    }

    private static double[] ConvertMercatorProjectedPointToGeographic(double[] point)
    {
        double[] geographic = MercatorInverseTransform.Value.Transform(new[] { point[0], point[1] });
        return CreatePoint(geographic[0], geographic[1], point[2], point[3]);
    }

    private static MathTransform CreateMercatorInverseTransform()
    {
        var projectionParameters = new List<ProjectionParameter>
        {
            new ProjectionParameter("latitude_of_origin", 0d),
            new ProjectionParameter("central_meridian", 0d),
            new ProjectionParameter("scale_factor", 1d),
            new ProjectionParameter("false_easting", 0d),
            new ProjectionParameter("false_northing", 0d),
        };

        IProjection projection = CoordinateSystemFactory.CreateProjection("Mercator", "mercator", projectionParameters);
        var geographic = GeographicCoordinateSystem.WGS84;
        var projected = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
            "Mercator",
            geographic,
            projection,
            LinearUnit.Metre,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        return CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic).MathTransform;
    }

    private static void AssertCoordinateClose(double[] actual, double[] expected, double tolerance)
    {
        Assert.NotNull(actual);
        Assert.NotNull(expected);
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            double delta = Math.Abs(actual[i] - expected[i]);
            Assert.InRange(
                delta,
                0d,
                tolerance);
        }
    }
}
