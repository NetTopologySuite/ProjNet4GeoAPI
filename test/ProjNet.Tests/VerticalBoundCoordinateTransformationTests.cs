// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.IO;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Verifies compound runtime routes built from WKT2 vertical <c>BOUNDCRS</c> metadata with <c>PARAMETERFILE</c>.
/// </summary>
public class VerticalBoundCoordinateTransformationTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Verifies that a compound CRS using a parsed vertical <c>BOUNDCRS</c> tail converts gravity-related heights to ellipsoidal heights via the referenced grid.
    /// </summary>
    [Fact]
    public void CreateFromCoordinateSystems_WithVerticalBoundCompoundSource_TransformsGravityRelatedHeightToEllipsoidalHeight()
    {
        string gridPath = FindGridPath("egm96_15_downsampled.gtx");
        BoundCoordinateSystem boundVertical = CoordinateSystemTestHelpers.RequireCoordinateSystem<BoundCoordinateSystem>(
            CoordinateSystemFactory,
            CreateVerticalBoundWkt(gridPath));

        CompoundCoordinateSystem source = CoordinateSystemFactory.CreateCompoundCoordinateSystem(
            "WGS 84 + EGM96 height",
            GeographicCoordinateSystem.WGS84,
            boundVertical);
        CompoundCoordinateSystem target = CoordinateSystemFactory.CreateCompoundCoordinateSystem(
            "WGS 84 + ellipsoidal height",
            GeographicCoordinateSystem.WGS84,
            CreateEllipsoidalHeightVerticalCoordinateSystem());

        ICoordinateTransformation transformation = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target);
        double[] output = transformation.MathTransform.Transform([12d, 56d, 0d]);

        Assert.Same(source, transformation.SourceCS);
        Assert.Same(target, transformation.TargetCS);
        Assert.Equal(12d, output[0], 12);
        Assert.Equal(56d, output[1], 12);
        Assert.Equal(36.9959410718d, output[2], 9);
    }

    /// <summary>
    /// Verifies that the inverse compound route converts ellipsoidal heights back to the bound gravity-related vertical CRS.
    /// </summary>
    [Fact]
    public void CreateFromCoordinateSystems_WithEllipsoidalHeightCompoundSource_TransformsEllipsoidalHeightToBoundVerticalHeight()
    {
        string gridPath = FindGridPath("egm96_15_downsampled.gtx");
        BoundCoordinateSystem boundVertical = CoordinateSystemTestHelpers.RequireCoordinateSystem<BoundCoordinateSystem>(
            CoordinateSystemFactory,
            CreateVerticalBoundWkt(gridPath));

        CompoundCoordinateSystem source = CoordinateSystemFactory.CreateCompoundCoordinateSystem(
            "WGS 84 + ellipsoidal height",
            GeographicCoordinateSystem.WGS84,
            CreateEllipsoidalHeightVerticalCoordinateSystem());
        CompoundCoordinateSystem target = CoordinateSystemFactory.CreateCompoundCoordinateSystem(
            "WGS 84 + EGM96 height",
            GeographicCoordinateSystem.WGS84,
            boundVertical);

        ICoordinateTransformation transformation = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target);
        double[] output = transformation.MathTransform.Transform([12d, 56d, 0d]);

        Assert.Same(source, transformation.SourceCS);
        Assert.Same(target, transformation.TargetCS);
        Assert.Equal(12d, output[0], 12);
        Assert.Equal(56d, output[1], 12);
        Assert.Equal(-36.9959410718d, output[2], 9);
    }

    private static VerticalCoordinateSystem CreateEllipsoidalHeightVerticalCoordinateSystem()
    {
        return CoordinateSystemFactory.CreateVerticalCoordinateSystem(
            "Ellipsoidal height",
            CoordinateSystemFactory.CreateVerticalDatum("Ellipsoidal height datum", DatumType.VD_Ellipsoidal),
            LinearUnit.Metre,
            new AxisInfo("Ellipsoidal height", AxisOrientationEnum.Up));
    }

    private static string CreateVerticalBoundWkt(string gridPath)
    {
        return $$"""
            BOUNDCRS[
                SOURCECRS[
                    VERTCRS["EGM96 height",
                        VDATUM["EGM96 geoid"],
                        CS[vertical,1],
                            AXIS["gravity-related height (H)",up,
                                LENGTHUNIT["metre",1]],
                        ID["EPSG",5773]]],
                TARGETCRS[
                    GEOGCRS["WGS 84",
                        DATUM["World Geodetic System 1984",
                            ELLIPSOID["WGS 84",6378137,298.257223563,
                                LENGTHUNIT["metre",1]]],
                        PRIMEM["Greenwich",0,
                            ANGLEUNIT["degree",0.0174532925199433]],
                        CS[ellipsoidal,3],
                            AXIS["latitude",north,
                                ORDER[1],
                                ANGLEUNIT["degree",0.0174532925199433]],
                            AXIS["longitude",east,
                                ORDER[2],
                                ANGLEUNIT["degree",0.0174532925199433]],
                            AXIS["ellipsoidal height",up,
                                ORDER[3],
                                LENGTHUNIT["metre",1]],
                        ID["EPSG",4979]]],
                ABRIDGEDTRANSFORMATION["WGS 84 to EGM96 height (1)",
                    METHOD["Geographic3D to GravityRelatedHeight (EGM)",
                        ID["EPSG",9661]],
                    PARAMETERFILE["Geoid (height correction) model file","{{gridPath}}"]]]
            """;
    }

    private static string FindGridPath(string fileName)
    {
        string direct = Path.Combine(AppContext.BaseDirectory, "Fixtures", "grids", fileName);
        if (File.Exists(direct))
        {
            return direct;
        }

        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "test", "ProjNet.Tests", "Fixtures", "grids", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException("Could not locate local test grid fixture under test\\ProjNet.Tests\\Fixtures\\grids.", fileName);
    }
}
