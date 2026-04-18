// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests.CoordinateSystems.Transformations;

using System.Linq;
using ProjNet.Data;
using Xunit;

/// <summary>
/// Tests for the coordinate operation catalog provider.
/// </summary>
public class OperationCatalogProviderTests
{
    /// <summary>
    /// Verifies that the managed provider loads the generated operation catalog with a sufficient number of well-formed definitions.
    /// </summary>
    [Fact]
    public void ManagedOperationProviderLoadsGeneratedOperationCatalog()
    {
        var provider = new ManagedCoordinateOperationDefinitionProvider();
        var definitions = provider.GetDefinitions().ToList();

        Assert.True(definitions.Count > 2500);
        Assert.Contains(definitions, operation => operation.SourceSrid > 0 && operation.TargetSrid > 0);
        Assert.Contains(definitions, operation => !string.IsNullOrWhiteSpace(operation.MethodName));
        Assert.Contains(definitions, operation => !string.IsNullOrWhiteSpace(operation.ParameterFileName));
    }
}
