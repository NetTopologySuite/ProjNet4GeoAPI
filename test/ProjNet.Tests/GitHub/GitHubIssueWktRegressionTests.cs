// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.GitHub;

using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using Xunit;

/// <summary>
/// Regression tests for GitHub issues that focus on WKT parsing behavior.
/// </summary>
public class GitHubIssueWktRegressionTests
{
    private const string ExtensionWkt1 =
        """
        PROJCS["NAD27/BLM59N(ftUS)",GEOGCS["NAD27",DATUM["North_American_Datum_1927",SPHEROID["Clarke1866",6378206.4,294.978698213898],EXTENSION["PROJ4_GRIDS","NTv2_0.gsb"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.0174532925199433,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4267"]],PROJECTION["Transverse_Mercator"],PARAMETER["latitude_of_origin",0],PARAMETER["central_meridian",171],PARAMETER["scale_factor",0.9996],PARAMETER["false_easting",1640416.67],PARAMETER["false_northing",0],UNIT["USsurveyfoot",0.304800609601219],AXIS["Easting",EAST],AXIS["Northing",NORTH],AUTHORITY["EPSG","4399"]]
        """;

    private const string ExtensionWkt2 =
        """
        PROJCS["NAD27/BLM60N(ftUS)",GEOGCS["NAD27",DATUM["North_American_Datum_1927",SPHEROID["Clarke1866",6378206.4,294.978698213898],EXTENSION["PROJ4_GRIDS","NTv2_0.gsb"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.0174532925199433,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4267"]],PROJECTION["Transverse_Mercator"],PARAMETER["latitude_of_origin",0],PARAMETER["central_meridian",177],PARAMETER["scale_factor",0.9996],PARAMETER["false_easting",1640416.67],PARAMETER["false_northing",0],UNIT["USsurveyfoot",0.304800609601219],AXIS["Easting",EAST],AXIS["Northing",NORTH],AUTHORITY["EPSG","4400"]]
        """;

    private const string ExtensionWkt3 =
        """
        PROJCS["WGS84/Pseudo-Mercator",GEOGCS["WGS84",DATUM["WGS_1984",SPHEROID["WGS84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.0174532925199433,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]],PROJECTION["Mercator_1SP"],PARAMETER["central_meridian",0],PARAMETER["scale_factor",1],PARAMETER["false_easting",0],PARAMETER["false_northing",0],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AXIS["Easting",EAST],AXIS["Northing",NORTH],EXTENSION["PROJ4","+proj=merc+a=6378137+b=6378137+lat_ts=0+lon_0=0+x_0=0+y_0=0+k=1+units=m+nadgrids=@null+wktext+no_defs"],AUTHORITY["EPSG","3857"]]
        """;

    private const string ExtensionWkt4 =
        """
        COMPD_CS["WGS84/Pseudo-Mercator+EGM2008geoidheight",PROJCS["WGS84/Pseudo-Mercator",GEOGCS["WGS84",DATUM["WGS_1984",SPHEROID["WGS84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.0174532925199433,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]],PROJECTION["Mercator_1SP"],PARAMETER["central_meridian",0],PARAMETER["scale_factor",1],PARAMETER["false_easting",0],PARAMETER["false_northing",0],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AXIS["Easting",EAST],AXIS["Northing",NORTH],EXTENSION["PROJ4","+proj=merc+a=6378137+b=6378137+lat_ts=0+lon_0=0+x_0=0+y_0=0+k=1+units=m+nadgrids=@null+wktext+no_defs"],AUTHORITY["EPSG","3857"]],VERT_CS["EGM2008height",VERT_DATUM["EGM2008geoid",2005,AUTHORITY["EPSG","1027"]],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AXIS["Gravity-relatedheight",UP],AUTHORITY["EPSG","3855"]],AUTHORITY["EPSG","6871"]]
        """;

    private const string Xian1980Wkt =
        """
        PROJCS["Xian_1980_GK_CM_105E",GEOGCS["GCS_Xian_1980",DATUM["Xian_1980",SPHEROID["Xian_1980",6332140,398.257,AUTHORITY["EPSG","7049"]],AUTHORITY["EPSG","6610"]],PRIMEM["Greenwich",0],UNIT["degree",0.0171234925199433]],UNIT["metre",1,AUTHORITY["EPSG","9001"]],PROJECTION["Transverse_Mercator"],PARAMETER["latitude_of_origin",0],PARAMETER["central_meridian",105],PARAMETER["scale_factor",1],PARAMETER["false_easting",500000],PARAMETER["false_northing",0]]
        """;

    private readonly CoordinateSystemFactory coordinateSystemFactory = new();

    /// <summary>
    /// Gets DATUM-level EXTENSION samples from upstream pull request #111.
    /// </summary>
    public static IEnumerable<TheoryDataRow<string, long>> DatumExtensionCases
    {
        get
        {
            yield return new TheoryDataRow<string, long>(ExtensionWkt1, 4399L);
            yield return new TheoryDataRow<string, long>(ExtensionWkt2, 4400L);
        }
    }

    /// <summary>
    /// Verifies that GitHub issue #106 is fixed for DATUM-level EXTENSION nodes from PR #111.
    /// </summary>
    /// <param name="wkt">Projected WKT with a DATUM-level EXTENSION node.</param>
    /// <param name="expectedAuthorityCode">Expected projected authority code.</param>
    [GitHubIssue(106)]
    [Theory(DisplayName = "Issue #106, DATUM-level EXTENSION WKTs parse successfully")]
    [MemberData(nameof(DatumExtensionCases))]
    public void DatumLevelExtensionsParseAsProjectedCoordinateSystems(string wkt, long expectedAuthorityCode)
    {
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(this.coordinateSystemFactory, wkt);

        Assert.Equal(expectedAuthorityCode, projected.AuthorityCode);
        Assert.Equal("Transverse_Mercator", projected.Projection.ClassName);
        Assert.Equal(4267, projected.GeographicCoordinateSystem.AuthorityCode);
    }

    /// <summary>
    /// Verifies that GitHub issue #106 is fixed for the PROJCS-level EXTENSION sample from PR #111.
    /// </summary>
    [GitHubIssue(106)]
    [Fact(DisplayName = "Issue #106, PROJCS-level EXTENSION WKT parses successfully")]
    public void ProjcsLevelExtensionParsesAsProjectedCoordinateSystem()
    {
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(this.coordinateSystemFactory, ExtensionWkt3);

        Assert.Equal(3857, projected.AuthorityCode);
        Assert.Equal("Mercator_1SP", projected.Projection.ClassName);
        Assert.Equal(4326, projected.GeographicCoordinateSystem.AuthorityCode);
    }

    /// <summary>
    /// Verifies that GitHub issue #106 is fixed for the compound CRS sample from PR #111.
    /// </summary>
    [GitHubIssue(106)]
    [Fact(DisplayName = "Issue #106, COMPD_CS with nested EXTENSION parses successfully")]
    public void CompoundCoordinateSystemWithExtensionParsesSuccessfully()
    {
        CompoundCoordinateSystem compound = CoordinateSystemTestHelpers.RequireCoordinateSystem<CompoundCoordinateSystem>(this.coordinateSystemFactory, ExtensionWkt4);

        Assert.Equal(6871, compound.AuthorityCode);
        Assert.IsType<ProjectedCoordinateSystem>(compound.HeadCoordinateSystem);
        Assert.IsType<VerticalCoordinateSystem>(compound.TailCoordinateSystem);
    }

    /// <summary>
    /// Verifies that the Xian 1980 WKT from GitHub issue #65 parses as a projected coordinate system.
    /// </summary>
    [GitHubIssue(65)]
    [Fact(DisplayName = "Issue #65, Xian_1980 projected WKT parses successfully")]
    public void Xian1980ProjectedCoordinateSystemParsesSuccessfully()
    {
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(this.coordinateSystemFactory, Xian1980Wkt);

        Assert.Equal("Transverse_Mercator", projected.Projection.ClassName);
        Assert.Equal(6332140d, projected.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid.SemiMajorAxis);
    }
}
