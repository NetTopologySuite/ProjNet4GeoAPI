// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Data;
using Xunit;

/// <summary>
/// Tests for <see cref="CoordinateSystemServices"/>.
/// </summary>
public class CoordinateSystemServicesTests
{
    /// <summary>
    /// Verifies that the default constructor initializes the service with EPSG 4326 and EPSG 3857 coordinate systems.
    /// </summary>
    [Fact]
    public void TestConstructor()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());

        Assert.NotNull(css.GetCoordinateSystem(4326));
        Assert.NotNull(css.GetCoordinateSystem(3857));
    }

    /// <summary>
    /// Verifies that the SRID-based transformation overload reuses the cached transformation instance for repeated requests.
    /// </summary>
    [Fact]
    public void CreateTransformationBySrid_ReusesCachedTransformationInstance()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());

        ICoordinateTransformation first = Assert.IsAssignableFrom<ICoordinateTransformation>(css.CreateTransformation(4326, 3857));
        ICoordinateTransformation second = Assert.IsAssignableFrom<ICoordinateTransformation>(css.CreateTransformation(4326, 3857));

        Assert.Same(first, second);
    }

    /// <summary>
    /// Verifies that concurrent SRID-based requests converge on the same cached transformation instance.
    /// </summary>
    /// <returns>A task that completes after the concurrent cache assertions finish.</returns>
    [Fact]
    public async Task CreateTransformationBySrid_ConcurrentCallsReturnSameCachedTransformation()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());

        Task<ICoordinateTransformation?>[] tasks = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() => css.CreateTransformation(4326, 3857), TestContext.Current.CancellationToken))
            .ToArray();
        ICoordinateTransformation?[] transformations = await Task.WhenAll(tasks).ConfigureAwait(true);
        ICoordinateTransformation first = Assert.IsAssignableFrom<ICoordinateTransformation>(transformations[0]);

        for (int i = 1; i < transformations.Length; i++)
        {
            Assert.Same(first, Assert.IsAssignableFrom<ICoordinateTransformation>(transformations[i]));
        }
    }

    /// <summary>
    /// Verifies that <c>TryGetCoordinateSystem</c> by SRID returns <see langword="true"/> and a non-null system for a known SRID, and <see langword="false"/> with <see langword="null"/> for an unknown SRID.
    /// </summary>
    [Fact]
    public void TestTryGetCoordinateSystemBySrid()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());

        bool found = css.TryGetCoordinateSystem(4326, out CoordinateSystem? coordinateSystem);
        bool missing = css.TryGetCoordinateSystem(999999, out CoordinateSystem? missingCoordinateSystem);

        Assert.True(found);
        Assert.NotNull(coordinateSystem);
        Assert.False(missing);
        Assert.Null(missingCoordinateSystem);
    }

    /// <summary>
    /// Verifies that <c>TryGetCoordinateSystem</c> by authority and code returns <see langword="true"/> for a known entry and <see langword="false"/> for an unknown code.
    /// </summary>
    [Fact]
    public void TestTryGetCoordinateSystemByAuthorityCode()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());

        bool found = css.TryGetCoordinateSystem("EPSG", 3857, out CoordinateSystem? coordinateSystem);
        bool missing = css.TryGetCoordinateSystem("EPSG", -1, out CoordinateSystem? missingCoordinateSystem);

        Assert.True(found);
        Assert.NotNull(coordinateSystem);
        Assert.False(missing);
        Assert.Null(missingCoordinateSystem);
    }

    /// <summary>
    /// Ensures authority/code lookup returns null when the coordinate system is not registered.
    /// </summary>
    [Fact]
    public void GetCoordinateSystemByAuthorityCodeReturnsNullWhenMissing()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());

        CoordinateSystem? missing = css.GetCoordinateSystem("EPSG", -1);

        Assert.Null(missing);
    }

    /// <summary>
    /// Verifies that catalog resolution returns the canonical registered instance for a parsed coordinate system with matching authority metadata.
    /// </summary>
    [Fact]
    public void ResolveFromCatalogReturnsCanonicalCatalogInstance()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());
        CoordinateSystem catalog = Assert.IsAssignableFrom<CoordinateSystem>(css.GetCoordinateSystem(4326));
        CoordinateSystem parsed = Assert.IsAssignableFrom<CoordinateSystem>(new CoordinateSystemFactory().CreateFromWkt(catalog.WKT));

        CoordinateSystem resolved = css.ResolveFromCatalog(parsed);

        Assert.NotSame(catalog, parsed);
        Assert.Same(catalog, resolved);
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystemServices.TryResolveFromCatalog"/> returns the canonical registered instance for a parsed coordinate system with matching authority metadata.
    /// </summary>
    [Fact]
    public void TryResolveFromCatalogReturnsCanonicalCatalogInstance()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());
        CoordinateSystem catalog = Assert.IsAssignableFrom<CoordinateSystem>(css.GetCoordinateSystem(3857));
        CoordinateSystem parsed = Assert.IsAssignableFrom<CoordinateSystem>(new CoordinateSystemFactory().CreateFromWkt(catalog.WKT));

        bool resolved = css.TryResolveFromCatalog(parsed, out CoordinateSystem? resolvedCoordinateSystem);

        Assert.True(resolved);
        Assert.NotSame(catalog, parsed);
        Assert.Same(catalog, resolvedCoordinateSystem);
    }

    /// <summary>
    /// Verifies that catalog resolution preserves the parsed instance when the parsed coordinate system has no top-level authority metadata.
    /// </summary>
    [Fact]
    public void ResolveFromCatalogReturnsInputWhenAuthorityMetadataIsMissing()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());
        CoordinateSystem parsed = GeographicCoordinateSystem.WGS84.WithAuthority(string.Empty, -1);

        CoordinateSystem resolved = css.ResolveFromCatalog(parsed);

        Assert.Same(parsed, resolved);
    }

    /// <summary>
    /// Verifies that catalog resolution preserves the parsed instance when the parsed coordinate system points to an unregistered authority code.
    /// </summary>
    [Fact]
    public void ResolveFromCatalogReturnsInputWhenAuthorityCodeIsUnknown()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());
        CoordinateSystem parsed = GeographicCoordinateSystem.WGS84.WithAuthority("EPSG", 999999);

        CoordinateSystem resolved = css.ResolveFromCatalog(parsed);

        Assert.Same(parsed, resolved);
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystemServices.TryResolveFromCatalog"/> returns <see langword="false"/> when the parsed coordinate system has no top-level authority metadata.
    /// </summary>
    [Fact]
    public void TryResolveFromCatalogReturnsFalseWhenAuthorityMetadataIsMissing()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());
        CoordinateSystem parsed = GeographicCoordinateSystem.WGS84.WithAuthority(string.Empty, -1);

        bool resolved = css.TryResolveFromCatalog(parsed, out CoordinateSystem? resolvedCoordinateSystem);

        Assert.False(resolved);
        Assert.Null(resolvedCoordinateSystem);
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystemServices.TryResolveFromCatalog"/> returns <see langword="false"/> when the parsed coordinate system points to an unregistered authority code.
    /// </summary>
    [Fact]
    public void TryResolveFromCatalogReturnsFalseWhenAuthorityCodeIsUnknown()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());
        CoordinateSystem parsed = GeographicCoordinateSystem.WGS84.WithAuthority("EPSG", 999999);

        bool resolved = css.TryResolveFromCatalog(parsed, out CoordinateSystem? resolvedCoordinateSystem);

        Assert.False(resolved);
        Assert.Null(resolvedCoordinateSystem);
    }

    /// <summary>
    /// Verifies that <c>GetAvailableSridValues</c> returns a non-empty array that includes well-known SRIDs such as 4326 and 3857.
    /// </summary>
    [Fact]
    public void TestGetAvailableSridValues()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());

        int[] srids = css.GetAvailableSridValues();
        Assert.NotNull(srids);
        Assert.True(Array.IndexOf(srids, 4326) >= 0);
        Assert.True(Array.IndexOf(srids, 3857) >= 0);
    }

    /// <summary>
    /// Verifies that the managed provider exposes more than 7000 definitions and includes EPSG 4326 and EPSG 3857.
    /// </summary>
    [Fact]
    public void TestManagedProviderIncludesFullGeneratedCatalog()
    {
        var provider = new ManagedCoordinateSystemDefinitionProvider();
        var definitions = provider.GetDefinitions().ToList();

        Assert.True(definitions.Count > 7000);
        Assert.Contains(definitions, item => item.Srid == 4326);
        Assert.Contains(definitions, item => item.Srid == 3857);
    }

    /// <summary>
    /// Verifies that an <see cref="ProjNet.Data.IManagedCoordinateSystemProvider"/> is consumed directly without invoking WKT parsing.
    /// </summary>
    [Fact]
    public void TestManagedObjectProviderBypassesWktParsing()
    {
        var provider = new TestManagedProvider();
        var css = new CoordinateSystemServices(provider);

        Assert.NotNull(css.GetCoordinateSystem(4326));
        Assert.NotNull(css.GetCoordinateSystem(3857));
    }

    /// <summary>
    /// Verifies that an exception thrown by a provider during initialization is wrapped in an <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void TestInitializationFailurePropagatesAsInvalidOperationException()
    {
        var css = new CoordinateSystemServices(new ThrowingDefinitionProvider());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => css.GetCoordinateSystem(4326));
        Assert.NotNull(exception.InnerException);
        Assert.Equal("Coordinate system initialization failed.", exception.Message);
    }

    /// <summary>
    /// Validates CSV-backed constructor loading for coordinate system definitions.
    /// </summary>
    /// <param name="csvPath">Path to the CSV definition file, or empty for embedded defaults.</param>
    [Theory]
    [InlineData(@"")]
    public void TestConstructorLoadCsv(string csvPath)
    {
        if (!string.IsNullOrWhiteSpace(csvPath))
        {
            if (!File.Exists(csvPath))
            {
                Xunit.Assert.Skip("Specified file not found");
            }
        }

        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory(),
            LoadCsv(csvPath));

        Assert.NotNull(css.GetCoordinateSystem(4326));
        Assert.NotNull(css.GetCoordinateSystem("EPSG", 4326));
        Assert.True(ReferenceEquals(css.GetCoordinateSystem("EPSG", 4326), css.GetCoordinateSystem(4326)));
    }

    /// <summary>
    /// Loads SRID/WKT definitions from CSV input or embedded defaults.
    /// </summary>
    /// <param name="csvPath">Optional path to an external CSV file.</param>
    /// <returns>Sequence of SRID/WKT pairs.</returns>
    internal static IEnumerable<CoordinateSystemDefinition> LoadCsv(string? csvPath = null)
    {
        Debug.WriteLine(FormattableString.Invariant($"Reading '{csvPath ?? "SRID.csv from resources stream"}'."));
        var sw = new Stopwatch();
        sw.Start();

        foreach (SRIDReader.WktString sridWkt in SRIDReader.GetSrids(csvPath))
        {
            yield return new CoordinateSystemDefinition(sridWkt.WktId, sridWkt.Wkt);
        }

        sw.Stop();
        Debug.WriteLine(FormattableString.Invariant($"Read '{csvPath ?? "SRID.csv from resources stream"}' in {sw.ElapsedMilliseconds:N0}ms"));
    }

    private sealed class TestManagedProvider : ICoordinateSystemDefinitionProvider, IManagedCoordinateSystemProvider
    {
        public IEnumerable<CoordinateSystemEntry> GetCoordinateSystems()
        {
            yield return new CoordinateSystemEntry(4326, GeographicCoordinateSystem.WGS84);
            yield return new CoordinateSystemEntry(3857, ProjectedCoordinateSystem.WebMercator);
        }

        public IEnumerable<CoordinateSystemDefinition> GetDefinitions()
        {
            yield return new CoordinateSystemDefinition(4326, "INVALID_WKT_SHOULD_NOT_BE_USED");
        }
    }

    private sealed class ThrowingDefinitionProvider : ICoordinateSystemDefinitionProvider
    {
        public IEnumerable<CoordinateSystemDefinition> GetDefinitions()
        {
            throw new InvalidOperationException("Synthetic provider failure.");
        }
    }
}
