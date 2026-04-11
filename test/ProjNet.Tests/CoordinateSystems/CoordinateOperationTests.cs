// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System.Collections.Generic;
using ProjNet.CoordinateSystems;
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
    /// Verifies that concatenated operation WKT output contains STEP blocks.
    /// </summary>
    [Fact]
    public void ConcatenatedOperation_Wkt_ContainsStepBlocks()
    {
        string wkt = CreateConcatenatedOperation().ToWktNode(WktVersion.Wkt22019).ToString();

        Assert.StartsWith("CONCATENATEDOPERATION[", wkt, System.StringComparison.Ordinal);
        Assert.Contains("STEP[COORDINATEOPERATION[", wkt, System.StringComparison.Ordinal);
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
