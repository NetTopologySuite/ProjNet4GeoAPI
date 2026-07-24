// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates direct proj2proj parity fixtures against ProjNet transformations.
/// </summary>
public class Proj2ProjParityTheoryTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Validates a direct projected pair against the reference fixture output.
    /// </summary>
    /// <param name="testCase">Fixture case containing source/target definitions and expected result.</param>
    [Theory]
    [MemberData(nameof(GetParityCases))]
    public void CreateFromCoordinateSystemsWithDirectProjectedPairStaysWithinProjReference(Proj2ProjCase testCase)
    {
        Assert.NotNull(testCase);

        var coordinateSystemFactory = new CoordinateSystemFactory();
        var transformationFactory = new CoordinateTransformationFactory();

        ProjectedCoordinateSystem source = Assert.IsType<ProjectedCoordinateSystem>(
            CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(coordinateSystemFactory, testCase.SourceWkt)
                .WithAuthority("EPSG", testCase.SourceSrid));
        ProjectedCoordinateSystem target = Assert.IsType<ProjectedCoordinateSystem>(
            CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(coordinateSystemFactory, testCase.TargetWkt)
                .WithAuthority("EPSG", testCase.TargetSrid));

        ICoordinateTransformation transformation = transformationFactory.CreateFromCoordinateSystems(source, target);
        double[] output = transformation.MathTransform.Transform([testCase.InputX, testCase.InputY]);
        double deltaX = Math.Abs(output[0] - testCase.ExpectedX);
        double deltaY = Math.Abs(output[1] - testCase.ExpectedY);

        Assert.Equal("EPSG", transformation.Authority);
        Assert.Equal(testCase.OperationCode, transformation.AuthorityCode);
        Assert.InRange(deltaX, 0d, testCase.ToleranceMeters);
        Assert.InRange(deltaY, 0d, testCase.ToleranceMeters);
    }

    /// <summary>
    /// Loads direct proj2proj parity test cases from the generated fixture.
    /// </summary>
    /// <returns>Fixture rows for theory execution.</returns>
    public static IEnumerable<TheoryDataRow<Proj2ProjCase>> GetParityCases()
    {
        string fixturePath = Path.Combine(AppContext.BaseDirectory, "Generated", "proj2proj-direct-parity-fixture.json");
        Assert.True(File.Exists(fixturePath), $"Fixture file not found: {fixturePath}");

        string json = File.ReadAllText(fixturePath);
        Proj2ProjFixture fixture = Assert.IsType<Proj2ProjFixture>(JsonSerializer.Deserialize<Proj2ProjFixture>(json, SerializerOptions));
        Assert.NotNull(fixture);
        List<Proj2ProjCase> cases = Assert.IsType<List<Proj2ProjCase>>(fixture.Cases);
        Assert.NotEmpty(fixture.Cases);

        foreach (Proj2ProjCase item in cases)
        {
            yield return new TheoryDataRow<Proj2ProjCase>(item);
        }
    }
}
