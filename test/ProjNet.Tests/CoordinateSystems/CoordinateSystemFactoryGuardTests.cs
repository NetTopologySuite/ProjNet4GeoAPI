// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Verifies coordinate-system factory guards report the caller-facing parameter names.
/// </summary>
public class CoordinateSystemFactoryGuardTests
{
    private static readonly CoordinateSystemFactory Factory = new();

    /// <summary>
    /// Gets the factory method keys that reject blank names.
    /// </summary>
    public static TheoryData<string> BlankNameFactories =>
        new()
        {
            nameof(CoordinateSystemFactory.CreateCompoundCoordinateSystem),
            "CreateFittedCoordinateSystemFromWkt",
            "CreateFittedCoordinateSystemFromTransform",
            nameof(CoordinateSystemFactory.CreateFlattenedSphere),
            nameof(CoordinateSystemFactory.CreateProjection),
            nameof(CoordinateSystemFactory.CreateHorizontalDatum),
            nameof(CoordinateSystemFactory.CreatePrimeMeridian),
            nameof(CoordinateSystemFactory.CreateGeographicCoordinateSystem),
            nameof(CoordinateSystemFactory.CreateVerticalDatum),
            nameof(CoordinateSystemFactory.CreateVerticalCoordinateSystem),
            nameof(CoordinateSystemFactory.CreateGeocentricCoordinateSystem),
        };

    /// <summary>
    /// Verifies that all blank-name factory guards report <c>name</c>.
    /// </summary>
    /// <param name="factoryMethod">The factory method key.</param>
    [Theory]
    [MemberData(nameof(BlankNameFactories))]
    public void FactoryMethodsRejectBlankNameWithNameParamName(string factoryMethod)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => InvokeBlankName(factoryMethod));

        Assert.Equal("name", exception.ParamName);
    }

    /// <summary>
    /// Verifies that projection creation reports the parameter collection when it is empty.
    /// </summary>
    [Fact]
    public void CreateProjectionRejectsEmptyParametersWithParametersParamName()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => Factory.CreateProjection(
                "Mercator",
                "Mercator_1SP",
                []));

        Assert.Equal("parameters", exception.ParamName);
    }

    private static void InvokeBlankName(string factoryMethod)
    {
        switch (factoryMethod)
        {
            case nameof(CoordinateSystemFactory.CreateCompoundCoordinateSystem):
                Factory.CreateCompoundCoordinateSystem(" ", GeographicCoordinateSystem.WGS84, VerticalCoordinateSystem.ODN);
                return;
            case "CreateFittedCoordinateSystemFromWkt":
                Factory.CreateFittedCoordinateSystem(
                    " ",
                    GeographicCoordinateSystem.WGS84,
                    CreateIdentityAffineTransform().WKT,
                    CreateAxisInfoPair());
                return;
            case "CreateFittedCoordinateSystemFromTransform":
                Factory.CreateFittedCoordinateSystem(
                    " ",
                    GeographicCoordinateSystem.WGS84,
                    CreateIdentityAffineTransform(),
                    CreateAxisInfoPair());
                return;
            case nameof(CoordinateSystemFactory.CreateFlattenedSphere):
                Factory.CreateFlattenedSphere(" ", 6378137d, 298.257223563d, LinearUnit.Metre);
                return;
            case nameof(CoordinateSystemFactory.CreateProjection):
                Factory.CreateProjection(" ", "Mercator_1SP", CreateProjectionParameters());
                return;
            case nameof(CoordinateSystemFactory.CreateHorizontalDatum):
                Factory.CreateHorizontalDatum(" ", DatumType.HD_Geocentric, Ellipsoid.WGS84, null);
                return;
            case nameof(CoordinateSystemFactory.CreatePrimeMeridian):
                Factory.CreatePrimeMeridian(" ", AngularUnit.Degrees, 0d);
                return;
            case nameof(CoordinateSystemFactory.CreateGeographicCoordinateSystem):
                Factory.CreateGeographicCoordinateSystem(
                    " ",
                    AngularUnit.Degrees,
                    HorizontalDatum.WGS84,
                    PrimeMeridian.Greenwich,
                    new AxisInfo("Lon", AxisOrientationEnum.East),
                    new AxisInfo("Lat", AxisOrientationEnum.North));
                return;
            case nameof(CoordinateSystemFactory.CreateVerticalDatum):
                Factory.CreateVerticalDatum(" ", DatumType.VD_Orthometric);
                return;
            case nameof(CoordinateSystemFactory.CreateVerticalCoordinateSystem):
                Factory.CreateVerticalCoordinateSystem(" ", VerticalDatum.ODN, LinearUnit.Metre, new AxisInfo("Up", AxisOrientationEnum.Up));
                return;
            case nameof(CoordinateSystemFactory.CreateGeocentricCoordinateSystem):
                Factory.CreateGeocentricCoordinateSystem(" ", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);
                return;
            default:
                throw new InvalidOperationException($"Unknown factory method key '{factoryMethod}'.");
        }
    }

    private static AffineTransform CreateIdentityAffineTransform()
    {
        double[,] matrix = (double[,])Array.CreateInstance(typeof(double), 3, 3);
        matrix[0, 0] = 1d;
        matrix[1, 1] = 1d;
        matrix[2, 2] = 1d;
        return new AffineTransform(matrix);
    }

    private static List<AxisInfo> CreateAxisInfoPair()
    {
        return
        [
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North),
        ];
    }

    private static List<ProjectionParameter> CreateProjectionParameters()
    {
        return
        [
            new ProjectionParameter("latitude_of_origin", 0d),
            new ProjectionParameter("central_meridian", 0d),
        ];
    }
}
