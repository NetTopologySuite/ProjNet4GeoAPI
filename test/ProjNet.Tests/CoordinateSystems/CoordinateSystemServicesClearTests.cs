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
