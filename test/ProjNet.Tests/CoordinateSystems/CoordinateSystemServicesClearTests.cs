// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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

    /// <summary>
    /// Verifies that clearing the registry allows the same SRID and authority code to be registered again without stale lookup state.
    /// </summary>
    [Fact]
    public void Clear_AllowsReRegisteringCoordinateSystemWithSameAuthorityCode()
    {
        var services = new TestCoordinateSystemServices();
        GeographicCoordinateSystem replacement = GeographicCoordinateSystem.WGS84
            .WithName("Replacement WGS84")
            .WithAuthority("EPSG", 4326);

        services.Register(4326, GeographicCoordinateSystem.WGS84);
        services.ClearRegistry();
        services.Register(4326, replacement);

        Assert.Same(replacement, services.GetCoordinateSystem(4326));
        Assert.Same(replacement, services.GetCoordinateSystem("EPSG", 4326));
        Assert.Equal(4326, services.GetSRID("EPSG", 4326));
        Assert.Equal(1, services.RegisteredCount);
    }

    /// <summary>
    /// Verifies that clearing the registry also removes cached SRID-pair transformations.
    /// </summary>
    [Fact]
    public void Clear_RemovesCachedTransformationInstances()
    {
        var services = new TestCoordinateSystemServices();

        services.Register(4326, GeographicCoordinateSystem.WGS84);
        services.Register(3857, ProjectedCoordinateSystem.WebMercator);

        ICoordinateTransformation first = Assert.IsAssignableFrom<ICoordinateTransformation>(services.CreateTransformation(4326, 3857));
        Assert.Same(first, Assert.IsAssignableFrom<ICoordinateTransformation>(services.CreateTransformation(4326, 3857)));

        services.ClearRegistry();
        services.Register(4326, GeographicCoordinateSystem.WGS84);
        services.Register(3857, ProjectedCoordinateSystem.WebMercator);

        ICoordinateTransformation second = Assert.IsAssignableFrom<ICoordinateTransformation>(services.CreateTransformation(4326, 3857));

        Assert.NotSame(first, second);
        Assert.Same(second, Assert.IsAssignableFrom<ICoordinateTransformation>(services.CreateTransformation(4326, 3857)));
    }

    /// <summary>
    /// Verifies that replacing a registered coordinate system invalidates affected cached transformations.
    /// </summary>
    [Fact]
    public void Register_ReplacementCoordinateSystem_InvalidatesAffectedTransformationCache()
    {
        var services = new TestCoordinateSystemServices();
        ProjectedCoordinateSystem replacement = ProjectedCoordinateSystem.WebMercator
            .WithName("Replacement Web Mercator")
            .WithAuthority("TEST", 93857);

        services.Register(4326, GeographicCoordinateSystem.WGS84);
        services.Register(3857, ProjectedCoordinateSystem.WebMercator);

        ICoordinateTransformation first = Assert.IsAssignableFrom<ICoordinateTransformation>(services.CreateTransformation(4326, 3857));

        services.Register(3857, replacement);
        Assert.Same(replacement, services.GetCoordinateSystem(3857));

        ICoordinateTransformation second = Assert.IsAssignableFrom<ICoordinateTransformation>(services.CreateTransformation(4326, 3857));

        Assert.NotSame(first, second);
        Assert.Same(second, Assert.IsAssignableFrom<ICoordinateTransformation>(services.CreateTransformation(4326, 3857)));
    }

    /// <summary>
    /// Verifies that clearing the registry uses the same lock as registration updates.
    /// </summary>
    /// <returns>A task that completes after the lock-observation assertion finishes.</returns>
    [Fact]
    public async Task Clear_WaitsForRegistryLock()
    {
        var services = new TestCoordinateSystemServices();
        using var clearStarted = new ManualResetEventSlim();
        using var clearCompleted = new ManualResetEventSlim();
        object syncRoot = services.GetSridDictionarySyncRoot();

        Monitor.Enter(syncRoot);
        try
        {
            var clearTask = Task.Run(
                () =>
                {
                    clearStarted.Set();
                    services.ClearRegistry();
                    clearCompleted.Set();
                },
                TestContext.Current.CancellationToken);

            Assert.True(clearStarted.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.False(clearCompleted.Wait(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken));

            Monitor.Exit(syncRoot);
            await clearTask.ConfigureAwait(true);
        }
        finally
        {
            if (Monitor.IsEntered(syncRoot))
            {
                Monitor.Exit(syncRoot);
            }
        }

        Assert.True(clearCompleted.IsSet);
    }

    private sealed class TestCoordinateSystemServices : CoordinateSystemServices
    {
        public TestCoordinateSystemServices()
            : base(
                CoordinateSystemTestHelpers.CreateCoordinateSystemFactory(),
                CoordinateSystemTestHelpers.CreateCoordinateTransformationFactory(),
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

        /// <summary>
        /// Gets the SRID dictionary sync root used by the service implementation.
        /// </summary>
        /// <returns>The sync root object for the SRID dictionary.</returns>
        public object GetSridDictionarySyncRoot()
        {
            FieldInfo field = typeof(CoordinateSystemServices).GetField("csBySrid", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var dictionary = (IDictionary)field.GetValue(this)!;
            return dictionary.SyncRoot;
        }
    }
}
