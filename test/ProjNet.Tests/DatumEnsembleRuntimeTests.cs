// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Verifies runtime transformation resolution for ensemble-backed datum metadata.
/// </summary>
public class DatumEnsembleRuntimeTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();

    /// <summary>
    /// Verifies ensemble-backed geographic CRS reuse the existing identity path against the equivalent single datum.
    /// </summary>
    [Fact]
    public void CreateTransformation_WithGeographicDatumEnsembleAndSingleDatum_UsesExistingIdentityPath()
    {
        GeographicCoordinateSystem source = CreateEnsembleBackedGeographicCoordinateSystem();
        GeographicCoordinateSystem target = GeographicCoordinateSystem.WGS84;

        ICoordinateTransformation transformation = CoordinateTransformTests.GetTransformation(source, target);
        double[] transformed = transformation.MathTransform.Transform([12.5d, 55.7d]);

        Assert.Equal(12.5d, transformed[0], 12);
        Assert.Equal(55.7d, transformed[1], 12);
    }

    /// <summary>
    /// Verifies ensemble-backed vertical CRS preserve the current resolver behavior of the equivalent single datum case.
    /// </summary>
    [Fact]
    public void CreateTransformation_WithVerticalDatumEnsembleAndSingleDatum_PreservesCurrentResolverBehavior()
    {
        VerticalCoordinateSystem source = CreateEnsembleBackedVerticalCoordinateSystem();
        VerticalCoordinateSystem singleDatumSource = CoordinateSystemFactory.CreateVerticalCoordinateSystem(
            "Example ensemble height",
            CoordinateSystemFactory.CreateVerticalDatum("Single datum", DatumType.VD_GeoidModelDerived),
            LinearUnit.Metre,
            new AxisInfo("Gravity-related height", AxisOrientationEnum.Up));
        VerticalCoordinateSystem target = CoordinateSystemFactory.CreateVerticalCoordinateSystem(
            "Example ensemble height",
            CoordinateSystemFactory.CreateVerticalDatum("Single datum", DatumType.VD_GeoidModelDerived),
            LinearUnit.Metre,
            new AxisInfo("Gravity-related height", AxisOrientationEnum.Up));

        NotSupportedException singleDatumException = Assert.Throws<NotSupportedException>(
            () => CoordinateTransformTests.GetTransformation(singleDatumSource, target));
        NotSupportedException ensembleException = Assert.Throws<NotSupportedException>(
            () => CoordinateTransformTests.GetTransformation(source, target));

        Assert.Equal(singleDatumException.Message, ensembleException.Message);
    }

    private static GeographicCoordinateSystem CreateEnsembleBackedGeographicCoordinateSystem()
    {
        HorizontalDatum datum = HorizontalDatum.WGS84;
        datum.Name = "World Geodetic System 1984 ensemble";
        datum.Authority = "EPSG";
        datum.AuthorityCode = 6326;
        datum.Ensemble = new DatumEnsemble(
            "World Geodetic System 1984 ensemble",
            [
                new DatumEnsembleMember("World Geodetic System 1984 (Transit)", "EPSG", 1166),
                new DatumEnsembleMember("World Geodetic System 1984 (G730)", "EPSG", 1152),
            ],
            2d,
            datum.Ellipsoid,
            "EPSG",
            6326);

        return CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "WGS 84",
            AngularUnit.Degrees,
            datum,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
    }

    private static VerticalCoordinateSystem CreateEnsembleBackedVerticalCoordinateSystem()
    {
        VerticalDatum datum = CoordinateSystemFactory.CreateVerticalDatum("Example vertical ensemble", DatumType.VD_GeoidModelDerived);
        datum.Ensemble = new DatumEnsemble(
            "Example vertical ensemble",
            [
                new DatumEnsembleMember("Datum A"),
                new DatumEnsembleMember("Datum B"),
            ],
            0.05d);

        return CoordinateSystemFactory.CreateVerticalCoordinateSystem(
            "Example ensemble height",
            datum,
            LinearUnit.Metre,
            new AxisInfo("Gravity-related height", AxisOrientationEnum.Up));
    }
}
