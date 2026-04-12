// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System.Collections.Generic;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Data;
using Xunit;

/// <summary>
/// Tests for <see cref="CoordinateSystemServices.Clear"/> consistency.
/// </summary>
public class CoordinateSystemServicesClearTests
{
    /// <summary>
    /// Verifies that clearing the registry removes both SRID and authority-code lookups.
    /// </summary>
    [Fact]
    public void Clear_RemovesCoordinateSystemAndReverseLookup()
    {
        var services = new TestCoordinateSystemServices();

        services.Register(4326, GeographicCoordinateSystem.WGS84);

        Assert.Equal(4326, services.GetSRID("EPSG", 4326));
        Assert.Equal(1, services.RegisteredCount);

        services.ClearRegistry();

        Assert.Null(services.GetCoordinateSystem(4326));
        Assert.Null(services.GetSRID("EPSG", 4326));
        Assert.Equal(0, services.RegisteredCount);
    }

    private sealed class TestCoordinateSystemServices : CoordinateSystemServices
    {
        public TestCoordinateSystemServices()
            : base(
                new CoordinateSystemFactory(),
                new CoordinateTransformationFactory(),
                new List<CoordinateSystemDefinition>())
        {
        }

        public int RegisteredCount => this.Count;

        public void Register(int srid, CoordinateSystem coordinateSystem)
        {
            this.AddCoordinateSystem(srid, coordinateSystem);
        }

        public void ClearRegistry()
        {
            this.Clear();
        }
    }
}
