// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;
using Xunit;

/// <summary>
/// Verifies projection-registry behaviors that are important for immutable projection identity handling.
/// </summary>
public class ProjectionsRegistryTests
{
    /// <summary>
    /// Verifies alias-based registry lookups keep the requested projection name while retaining the canonical implementation name as alias metadata.
    /// </summary>
    [Fact]
    public void CreateProjection_WithAliasLookup_PreservesRequestedName()
    {
        MapProjection projection = Assert.IsAssignableFrom<MapProjection>(
            ProjectionsRegistry.CreateProjection("mercator", CreateMercatorParameters()));

        Assert.Equal("mercator", projection.Name);
        Assert.Equal("mercator", projection.ClassName);
        Assert.Equal("Mercator_2SP", projection.Alias);
        Assert.Equal("EPSG", projection.Authority);
        Assert.Equal(9805, projection.AuthorityCode);
    }

    /// <summary>
    /// Verifies canonical registry lookups do not synthesize an alias when the constructor already assigns the requested name.
    /// </summary>
    [Fact]
    public void CreateProjection_WithCanonicalLookup_DoesNotAssignAlias()
    {
        MapProjection projection = Assert.IsAssignableFrom<MapProjection>(
            ProjectionsRegistry.CreateProjection("Mercator_1SP", CreateMercatorParameters(scaleFactor: 1d)));

        Assert.Equal("Mercator_1SP", projection.Name);
        Assert.Equal("Mercator_1SP", projection.ClassName);
        Assert.Equal(string.Empty, projection.Alias);
        Assert.Equal("EPSG", projection.Authority);
    }

    private static List<ProjectionParameter> CreateMercatorParameters(double? scaleFactor = null)
    {
        var parameters = new List<ProjectionParameter>
        {
            new("semi_major", 6378137d),
            new("semi_minor", 6356752.314245179d),
            new("central_meridian", 0d),
            new("latitude_of_origin", 0d),
            new("unit", 1d),
        };

        if (scaleFactor is not null)
        {
            parameters.Add(new ProjectionParameter("scale_factor", scaleFactor.Value));
        }

        return parameters;
    }
}
