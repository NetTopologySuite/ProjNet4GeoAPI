// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System.Linq;
using ProjNet.Data;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class OperationCatalogProviderTests
{
    /// <summary>
    /// Performs the documented operation.
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
