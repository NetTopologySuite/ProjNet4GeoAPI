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

public class Proj2ProjParityTheoryTests
{
    [Theory]
    [MemberData(nameof(GetParityCases))]
    public void CreateFromCoordinateSystems_WithDirectProjectedPair_StaysWithinProjReference(Proj2ProjCase testCase)
    {
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

    public sealed class Proj2ProjCase
    {
        public int OperationCode { get; set; }

        public int SourceSrid { get; set; }

        public int TargetSrid { get; set; }

        public string SourceWkt { get; set; }

        public string TargetWkt { get; set; }

        public double InputX { get; set; }

        public double InputY { get; set; }

        public double ExpectedX { get; set; }

        public double ExpectedY { get; set; }

        public double ToleranceMeters { get; set; }
    }

    public sealed class Proj2ProjFixture
    {
        public int FixtureVersion { get; set; }

        public string Generator { get; set; }

        public List<Proj2ProjCase> Cases { get; set; }
    }
}
