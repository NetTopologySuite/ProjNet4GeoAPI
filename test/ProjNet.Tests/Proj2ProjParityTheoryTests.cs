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
using System.Text.Json;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates direct proj2proj parity fixtures against ProjNet transformations.
/// </summary>
public class Proj2ProjParityTheoryTests
{
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

        var source = (CoordinateSystem)coordinateSystemFactory.CreateFromWkt(testCase.SourceWkt);
        var target = (CoordinateSystem)coordinateSystemFactory.CreateFromWkt(testCase.TargetWkt);
        source.Authority = "EPSG";
        source.AuthorityCode = testCase.SourceSrid;
        target.Authority = "EPSG";
        target.AuthorityCode = testCase.TargetSrid;

        var transformation = transformationFactory.CreateFromCoordinateSystems(source, target);
        double[] output = transformation.MathTransform.Transform(new[] { testCase.InputX, testCase.InputY });
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
    public static IEnumerable<object[]> GetParityCases()
    {
        string fixturePath = Path.Combine(AppContext.BaseDirectory, "Generated", "proj2proj-direct-parity-fixture.json");
        Assert.True(File.Exists(fixturePath), "Fixture file not found: " + fixturePath);

        string json = File.ReadAllText(fixturePath);
        var fixture = JsonSerializer.Deserialize<Proj2ProjFixture>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        Assert.NotNull(fixture);
        Assert.NotNull(fixture.Cases);
        Assert.NotEmpty(fixture.Cases);

        foreach (var item in fixture.Cases)
        {
            yield return new object[] { item };
        }
    }

    /// <summary>
    /// Represents the documented type.
    /// </summary>
    public sealed class Proj2ProjCase
    {
        /// <summary>
        /// Gets or sets the EPSG operation code for the parity case.
        /// </summary>
        public int OperationCode { get; set; }

        /// <summary>
        /// Gets or sets the source SRID.
        /// </summary>
        public int SourceSrid { get; set; }

        /// <summary>
        /// Gets or sets the target SRID.
        /// </summary>
        public int TargetSrid { get; set; }

        /// <summary>
        /// Gets or sets the source CRS WKT definition.
        /// </summary>
        public string SourceWkt { get; set; }

        /// <summary>
        /// Gets or sets the target CRS WKT definition.
        /// </summary>
        public string TargetWkt { get; set; }

        /// <summary>
        /// Gets or sets the input x coordinate.
        /// </summary>
        public double InputX { get; set; }

        /// <summary>
        /// Gets or sets the input y coordinate.
        /// </summary>
        public double InputY { get; set; }

        /// <summary>
        /// Gets or sets the expected x coordinate.
        /// </summary>
        public double ExpectedX { get; set; }

        /// <summary>
        /// Gets or sets the expected y coordinate.
        /// </summary>
        public double ExpectedY { get; set; }

        /// <summary>
        /// Gets or sets the tolerance in meters for result comparison.
        /// </summary>
        public double ToleranceMeters { get; set; }
    }

    /// <summary>
    /// Represents the documented type.
    /// </summary>
    public sealed class Proj2ProjFixture
    {
        /// <summary>
        /// Gets or sets the fixture schema/version marker.
        /// </summary>
        public int FixtureVersion { get; set; }

        /// <summary>
        /// Gets or sets the generator identifier used to produce the fixture.
        /// </summary>
        public string Generator { get; set; }

        /// <summary>
        /// Gets or sets the parity cases included in the fixture payload.
        /// </summary>
        public List<Proj2ProjCase> Cases { get; set; }
    }
}
