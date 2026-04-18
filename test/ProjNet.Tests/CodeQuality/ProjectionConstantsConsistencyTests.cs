// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Reflection;
using ProjNet.CoordinateSystems.Projections;
using Xunit;

/// <summary>
/// Verifies that shared projection constants keep their expected mathematical values.
/// </summary>
public class ProjectionConstantsConsistencyTests
{
    /// <summary>
    /// Verifies that the shared map-projection angle constants match the corresponding fractions of pi.
    /// </summary>
    [Fact]
    public void MapProjectionAngleConstants_MatchExpectedPiFractions()
    {
        double fortPi = GetMapProjectionConstant("FortPi");
        double halfPi = GetMapProjectionConstant("HalfPi");

        Assert.Equal(Math.PI / 4d, fortPi, 15);
        Assert.Equal(Math.PI / 2d, halfPi, 15);
    }

    /// <summary>
    /// Verifies that the shared projection helper constants keep their documented numeric values.
    /// </summary>
    [Fact]
    public void ProjectionConstants_MatchExpectedNumericValues()
    {
        Type projectionConstantsType = GetProjectionConstantsType();

        Assert.Equal(1d / 3d, GetProjectionConstant(projectionConstantsType, "OneThird"), 15);
        Assert.Equal(2d / 3d, GetProjectionConstant(projectionConstantsType, "TwoThirds"), 15);
        Assert.Equal(1.0000001d, GetProjectionConstant(projectionConstantsType, "OnePlusEps7"), 15);
        Assert.Equal(1.000001d, GetProjectionConstant(projectionConstantsType, "OnePlusEps6"), 15);
        Assert.Equal(1e-12d, GetProjectionConstant(projectionConstantsType, "Tolerance1E12"), 15);
    }

    private static double GetMapProjectionConstant(string fieldName)
    {
        FieldInfo field = Assert.IsType<FieldInfo>(
            typeof(MapProjection).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic),
            exactMatch: false);
        return Assert.IsType<double>(field.GetRawConstantValue());
    }

    private static Type GetProjectionConstantsType()
    {
        Assembly assembly = Assert.IsType<Assembly>(Assembly.GetAssembly(typeof(MapProjection)), exactMatch: false);
        return Assert.IsType<Type>(
            assembly.GetType("ProjNet.CoordinateSystems.Projections.ProjectionConstants"),
            exactMatch: false);
    }

    private static double GetProjectionConstant(Type projectionConstantsType, string fieldName)
    {
        FieldInfo field = Assert.IsType<FieldInfo>(
            projectionConstantsType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public),
            exactMatch: false);
        return Assert.IsType<double>(field.GetRawConstantValue());
    }
}
