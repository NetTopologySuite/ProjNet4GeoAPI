// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class ProjectionKernelAlignmentTests
{
    private static readonly double[] LambertAliasInput = [100000d, 100000d];
    private static readonly double[] MercatorAliasInput = [1000d, 2000d];
    private static readonly double[] TransverseMercatorAliasInput = [500000d, 4649776.22482d];

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Validates Mercator family aliases against the projection registry.
    /// </summary>
    /// <param name="projectionName">Projection alias to resolve.</param>
    [Theory]
    [InlineData("Mercator (variant A)")]
    [InlineData("Mercator (variant B)")]
    [InlineData("Web_Mercator")]
    public void SupportsMercatorVariantAliases(string projectionName)
    {
        ProjectedCoordinateSystem source = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            $"PROJCS[\"Alias Mercator\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"{projectionName}\"],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1],AUTHORITY[\"EPSG\",\"3857\"]]");

        GeographicCoordinateSystem target = GeographicCoordinateSystem.WGS84;
        ICoordinateTransformation transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target);
        double[] result = transform.MathTransform.Transform(MercatorAliasInput);

        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Validates Transverse Mercator family aliases against the projection registry.
    /// </summary>
    /// <param name="projectionName">Projection alias to resolve.</param>
    [Theory]
    [InlineData("Transverse_Mercator_South_Oriented")]
    [InlineData("Gauss_Kruger")]
    [InlineData("UTM")]
    [InlineData("ETMERC")]
    [InlineData("Extended_Transverse_Mercator")]
    public void SupportsTransverseMercatorAliases(string projectionName)
    {
        ProjectedCoordinateSystem source = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            $"PROJCS[\"Alias TM\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",9],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1],AUTHORITY[\"EPSG\",\"32632\"]]");

        GeographicCoordinateSystem target = GeographicCoordinateSystem.WGS84;
        ICoordinateTransformation transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target);
        double[] result = transform.MathTransform.Transform(TransverseMercatorAliasInput);

        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Validates Lambert Conformal Conic aliases against the projection registry.
    /// </summary>
    /// <param name="projectionName">Projection alias to resolve.</param>
    [Theory]
    [InlineData("Lambert_Conformal_Conic_1SP")]
    [InlineData("Lambert_Conformal_Conic_2SP_Belgium")]
    public void SupportsLambertConformalAliases(string projectionName)
    {
        ProjectedCoordinateSystem source = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            $"PROJCS[\"Alias LCC\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",40],PARAMETER[\"central_meridian\",-100],PARAMETER[\"standard_parallel_1\",33],PARAMETER[\"standard_parallel_2\",45],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]");

        GeographicCoordinateSystem target = GeographicCoordinateSystem.WGS84;
        ICoordinateTransformation transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target);
        double[] result = transform.MathTransform.Transform(LambertAliasInput);

        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }
}
