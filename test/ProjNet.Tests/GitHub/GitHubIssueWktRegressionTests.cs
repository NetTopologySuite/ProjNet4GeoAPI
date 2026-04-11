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
}
