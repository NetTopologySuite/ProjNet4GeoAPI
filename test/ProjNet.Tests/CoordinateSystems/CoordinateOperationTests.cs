// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using ProjNet.IO.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for <see cref="CoordinateOperation"/> and <see cref="ConcatenatedOperation"/>.
/// </summary>
public class CoordinateOperationTests
{
    /// <summary>
    /// Verifies that the coordinate operation constructor stores its properties.
    /// </summary>
    [Fact]
    public void CoordinateOperation_Constructor_SetsProperties()
    {
        CoordinateOperation operation = CreateCoordinateOperation("Axis swap");

        Assert.Equal("Axis swap", operation.Name);
        Assert.Equal("Affine parametric transformation", operation.MethodName);
        Assert.Single(operation.Parameters);
        Assert.Equal("Parameter A", operation.Parameters[0].Name);
    }

    /// <summary>
    /// Verifies that <see cref="Info.WithAuthority"/> returns a coordinate-operation clone with updated authority metadata.
    /// </summary>
    [Fact]
    public void CoordinateOperation_WithAuthority_ReturnsUpdatedClone()
    {
        CoordinateOperation original = CreateCoordinateOperation("Axis swap");
        CoordinateOperation clone = original.WithAuthority("EPSG", 9603);

        Assert.Equal("EPSG", clone.Authority);
        Assert.Equal(9603, clone.AuthorityCode);
        Assert.Equal("TEST", original.Authority);
        Assert.Equal(1, original.AuthorityCode);
        Assert.NotSame(original, clone);
        Assert.NotSame(original.SourceCoordinateSystem, clone.SourceCoordinateSystem);
        Assert.True(original.SourceCoordinateSystem.EqualParams(clone.SourceCoordinateSystem));
    }

    /// <summary>
    /// Verifies that coordinate operation WKT output contains the expected keywords.
    /// </summary>
    [Fact]
    public void CoordinateOperation_Wkt_ContainsSourceTargetAndMethod()
    {
        string wkt = CreateCoordinateOperation("Axis swap").ToWktNode(WktVersion.Wkt22019).ToString();

        Assert.StartsWith("COORDINATEOPERATION[", wkt, System.StringComparison.Ordinal);
        Assert.Contains("SOURCECRS[", wkt, System.StringComparison.Ordinal);
        Assert.Contains("TARGETCRS[", wkt, System.StringComparison.Ordinal);
        Assert.Contains("METHOD[\"Affine parametric transformation\"]", wkt, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that the concatenated operation constructor stores its steps.
    /// </summary>
    [Fact]
    public void ConcatenatedOperation_Constructor_SetsSteps()
    {
        ConcatenatedOperation operation = CreateConcatenatedOperation();

        Assert.Equal("Chained operation", operation.Name);
        Assert.Equal(2, operation.Steps.Count);
        Assert.Equal("Step 1", operation.Steps[0].Name);
        Assert.Equal("Step 2", operation.Steps[1].Name);
    }

    /// <summary>
    /// Verifies that <see cref="Info.WithAuthority"/> returns a concatenated-operation clone with updated authority metadata.
    /// </summary>
    [Fact]
    public void ConcatenatedOperation_WithAuthority_ReturnsUpdatedClone()
    {
        ConcatenatedOperation original = CreateConcatenatedOperation();
        ConcatenatedOperation clone = original.WithAuthority("EPSG", 9604);

        Assert.Equal("EPSG", clone.Authority);
        Assert.Equal(9604, clone.AuthorityCode);
        Assert.Equal("TEST", original.Authority);
        Assert.Equal(2, original.AuthorityCode);
        Assert.NotSame(original, clone);
        Assert.Equal(original.Steps.Count, clone.Steps.Count);
        Assert.NotSame(original.Steps[0], clone.Steps[0]);
        Assert.True(original.Steps[0].EqualParams(clone.Steps[0]));
    }

    /// <summary>
    /// Verifies that concatenated operation WKT output contains STEP blocks.
    /// </summary>
    [Fact]
    public void ConcatenatedOperation_Wkt_ContainsStepBlocks()
    {
        string wkt = CreateConcatenatedOperation().ToWktNode(WktVersion.Wkt22019).ToString();

        Assert.StartsWith("CONCATENATEDOPERATION[", wkt, System.StringComparison.Ordinal);
        Assert.Contains("STEP[COORDINATEOPERATION[", wkt, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that concatenated operation WKT2 serialization round-trips through the reader without losing step metadata.
    /// </summary>
    [Fact]
    public void ConcatenatedOperation_Wkt2_RoundTripsThroughReader()
    {
        ConcatenatedOperation original = CreateConcatenatedOperation();
        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();
        ConcatenatedOperation parsed = Assert.IsType<ConcatenatedOperation>(CoordinateSystemWktReader.Parse(wkt));

        Assert.True(original.EqualParams(parsed));
        Assert.Equal(original.Name, parsed.Name);
        Assert.Equal(original.Steps.Count, parsed.Steps.Count);
        Assert.Equal(original.Steps[0].MethodName, parsed.Steps[0].MethodName);
        Assert.Equal(original.Steps[1].Parameters[0].Value, parsed.Steps[1].Parameters[0].Value);
    }

    private static CoordinateOperation CreateCoordinateOperation(string name)
    {
        return new CoordinateOperation(
            "Affine parametric transformation",
            new List<Parameter> { new("Parameter A", 1d) },
            GeographicCoordinateSystem.WGS84,
            GeographicCoordinateSystem.WGS84,
            name,
            "TEST",
            1,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static ConcatenatedOperation CreateConcatenatedOperation()
    {
        return new ConcatenatedOperation(
            new List<CoordinateOperation>
            {
                CreateCoordinateOperation("Step 1"),
                CreateCoordinateOperation("Step 2"),
            },
            GeographicCoordinateSystem.WGS84,
            GeographicCoordinateSystem.WGS84,
            "Chained operation",
            "TEST",
            2,
            string.Empty,
            string.Empty,
            string.Empty);
    }
}
