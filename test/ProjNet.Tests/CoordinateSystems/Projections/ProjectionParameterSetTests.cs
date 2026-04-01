// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;
using Xunit;

/// <summary>
/// Tests for <see cref="ProjectionParameterSet"/>.
/// </summary>
public class ProjectionParameterSetTests
{
    /// <summary>
    /// Verifies that the constructor rejects a null parameter sequence.
    /// </summary>
    [Fact]
    public void Constructor_NullParameters_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ProjectionParameterSet(null!));
    }

    /// <summary>
    /// Verifies that the original parameter names and insertion order are preserved when enumerating.
    /// </summary>
    [Fact]
    public void ToProjectionParameter_PreservesOriginalNamesAndOrder()
    {
        ProjectionParameterSet parameterSet = CreateParameterSet(
            new ProjectionParameter("Central_Meridian", 15.0),
            new ProjectionParameter("Scale_Factor", 0.9996));

        ProjectionParameter[] parameters = parameterSet.ToProjectionParameter().ToArray();

        Assert.Collection(
            parameters,
            parameter =>
            {
                Assert.Equal("Central_Meridian", parameter.Name);
                Assert.Equal(15.0, parameter.Value);
            },
            parameter =>
            {
                Assert.Equal("Scale_Factor", parameter.Name);
                Assert.Equal(0.9996, parameter.Value);
            });
    }

    /// <summary>
    /// Verifies that mandatory lookup is case-insensitive for the primary name.
    /// </summary>
    [Fact]
    public void GetParameterValue_PrimaryName_IsCaseInsensitive()
    {
        ProjectionParameterSet parameterSet = CreateParameterSet(new ProjectionParameter("Central_Meridian", 15.0));

        double value = parameterSet.GetParameterValue("central_meridian");

        Assert.Equal(15.0, value);
    }

    /// <summary>
    /// Verifies that mandatory lookup falls back to alternate names.
    /// </summary>
    [Fact]
    public void GetParameterValue_AlternateName_ReturnsValue()
    {
        ProjectionParameterSet parameterSet = CreateParameterSet(new ProjectionParameter("Longitude_Of_Center", 10.0));

        double value = parameterSet.GetParameterValue("central_meridian", "longitude_of_center", "lon_0");

        Assert.Equal(10.0, value);
    }

    /// <summary>
    /// Verifies that missing mandatory parameters raise a helpful exception message.
    /// </summary>
    [Fact]
    public void GetParameterValue_MissingValue_ThrowsArgumentException()
    {
        ProjectionParameter[] parameters = [];
        var parameterSet = new ProjectionParameterSet(parameters);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => parameterSet.GetParameterValue("central_meridian", "longitude_of_center", "lon_0"));

        Assert.Equal("parameterName", exception.ParamName);
        Assert.Contains("Missing projection parameter 'central_meridian'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("'longitude_of_center'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("'lon_0'", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that optional lookup returns the primary value when present.
    /// </summary>
    [Fact]
    public void GetOptionalParameterValue_PrimaryName_ReturnsStoredValue()
    {
        ProjectionParameterSet parameterSet = CreateParameterSet(new ProjectionParameter("Scale_Factor", 0.9996));

        double value = parameterSet.GetOptionalParameterValue("scale_factor", 1.0);

        Assert.Equal(0.9996, value);
    }

    /// <summary>
    /// Verifies that optional lookup falls back to alternate names.
    /// </summary>
    [Fact]
    public void GetOptionalParameterValue_AlternateName_ReturnsStoredValue()
    {
        ProjectionParameterSet parameterSet = CreateParameterSet(new ProjectionParameter("Latitude_Of_Center", 45.0));

        double value = parameterSet.GetOptionalParameterValue("latitude_of_origin", 0.0, "latitude_of_center");

        Assert.Equal(45.0, value);
    }

    /// <summary>
    /// Verifies that optional lookup returns the supplied default value when absent.
    /// </summary>
    [Fact]
    public void GetOptionalParameterValue_MissingValue_ReturnsDefault()
    {
        ProjectionParameter[] parameters = [];
        var parameterSet = new ProjectionParameterSet(parameters);

        double value = parameterSet.GetOptionalParameterValue("scale_factor", 1.0, "k_0");

        Assert.Equal(1.0, value);
    }

    /// <summary>
    /// Verifies that <see cref="ProjectionParameterSet.Find"/> returns a parameter with its original casing.
    /// </summary>
    [Fact]
    public void Find_ExistingParameter_ReturnsParameterWithOriginalName()
    {
        ProjectionParameterSet parameterSet = CreateParameterSet(new ProjectionParameter("Scale_Factor", 0.9996));

        ProjectionParameter? parameter = parameterSet.Find("scale_factor");

        ProjectionParameter found = Assert.IsType<ProjectionParameter>(parameter);
        Assert.Equal("Scale_Factor", found.Name);
        Assert.Equal(0.9996, found.Value);
    }

    /// <summary>
    /// Verifies that <see cref="ProjectionParameterSet.Find"/> returns null when the parameter is absent.
    /// </summary>
    [Fact]
    public void Find_MissingParameter_ReturnsNull()
    {
        ProjectionParameter[] parameters = [];
        var parameterSet = new ProjectionParameterSet(parameters);

        Assert.Null(parameterSet.Find("scale_factor"));
    }

    /// <summary>
    /// Verifies that indexed access returns parameters in insertion order.
    /// </summary>
    [Fact]
    public void GetAtIndex_ReturnsParameterInInsertionOrder()
    {
        ProjectionParameterSet parameterSet = CreateParameterSet(
            new ProjectionParameter("central_meridian", 15.0),
            new ProjectionParameter("scale_factor", 0.9996));

        ProjectionParameter parameter = parameterSet.GetAtIndex(1);

        Assert.Equal("scale_factor", parameter.Name);
        Assert.Equal(0.9996, parameter.Value);
    }

    /// <summary>
    /// Verifies that indexed access rejects indices below the valid range.
    /// </summary>
    [Fact]
    public void GetAtIndex_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        ProjectionParameterSet parameterSet = CreateParameterSet(new ProjectionParameter("central_meridian", 15.0));

        Assert.Throws<ArgumentOutOfRangeException>(() => parameterSet.GetAtIndex(-1));
    }

    /// <summary>
    /// Verifies that indexed access rejects indices above the valid range.
    /// </summary>
    [Fact]
    public void GetAtIndex_IndexPastEnd_ThrowsArgumentOutOfRangeException()
    {
        ProjectionParameterSet parameterSet = CreateParameterSet(new ProjectionParameter("central_meridian", 15.0));

        Assert.Throws<ArgumentOutOfRangeException>(() => parameterSet.GetAtIndex(1));
    }

    /// <summary>
    /// Verifies that equality returns true for equivalent sets.
    /// </summary>
    [Fact]
    public void Equals_EquivalentSets_ReturnsTrue()
    {
        ProjectionParameterSet first = CreateParameterSet(
            new ProjectionParameter("central_meridian", 15.0),
            new ProjectionParameter("scale_factor", 0.9996));
        ProjectionParameterSet second = CreateParameterSet(
            new ProjectionParameter("Central_Meridian", 15.0),
            new ProjectionParameter("Scale_Factor", 0.9996));

        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
    }

    /// <summary>
    /// Verifies that equality returns false when the other set is null.
    /// </summary>
    [Fact]
    public void Equals_NullSet_ReturnsFalse()
    {
        ProjectionParameterSet first = CreateParameterSet(new ProjectionParameter("central_meridian", 15.0));

        Assert.False(first.Equals((ProjectionParameterSet?)null));
    }

    /// <summary>
    /// Verifies that equality returns false when the parameter counts differ.
    /// </summary>
    [Fact]
    public void Equals_DifferentCounts_ReturnsFalse()
    {
        ProjectionParameterSet first = CreateParameterSet(new ProjectionParameter("central_meridian", 15.0));
        ProjectionParameterSet second = CreateParameterSet(
            new ProjectionParameter("central_meridian", 15.0),
            new ProjectionParameter("scale_factor", 0.9996));

        Assert.False(first.Equals(second));
    }

    /// <summary>
    /// Verifies that equality returns false when the other set is missing a key.
    /// </summary>
    [Fact]
    public void Equals_MissingKey_ReturnsFalse()
    {
        ProjectionParameterSet first = CreateParameterSet(new ProjectionParameter("central_meridian", 15.0));
        ProjectionParameterSet second = CreateParameterSet(new ProjectionParameter("false_easting", 15.0));

        Assert.False(first.Equals(second));
    }

    /// <summary>
    /// Verifies that equality returns false when values differ.
    /// </summary>
    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        ProjectionParameterSet first = CreateParameterSet(new ProjectionParameter("central_meridian", 15.0));
        ProjectionParameterSet second = CreateParameterSet(new ProjectionParameter("central_meridian", 10.0));

        Assert.False(first.Equals(second));
    }

    /// <summary>
    /// Verifies that the object overload returns false for a different type.
    /// </summary>
    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        ProjectionParameterSet parameterSet = CreateParameterSet(new ProjectionParameter("central_meridian", 15.0));

        Assert.False(parameterSet.Equals("not a parameter set"));
    }

    /// <summary>
    /// Verifies that equivalent sets produce the same hash code.
    /// </summary>
    [Fact]
    public void GetHashCode_EquivalentSets_ReturnSameValue()
    {
        ProjectionParameterSet first = CreateParameterSet(
            new ProjectionParameter("central_meridian", 15.0),
            new ProjectionParameter("scale_factor", 0.9996));
        ProjectionParameterSet second = CreateParameterSet(
            new ProjectionParameter("Central_Meridian", 15.0),
            new ProjectionParameter("Scale_Factor", 0.9996));

        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    private static ProjectionParameterSet CreateParameterSet(params ProjectionParameter[] parameters)
    {
        return new ProjectionParameterSet(parameters);
    }
}
