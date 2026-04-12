// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Tests that exercise the internal <c>CoordinateSystemKey</c> equality and hashing
/// behavior indirectly through <see cref="CoordinateSystemServices"/> lookups.
/// </summary>
/// <remarks>
/// <c>CoordinateSystemKey</c> is a private nested class inside <see cref="CoordinateSystemServices"/>
/// and cannot be tested directly. These tests verify the lookup behavior that depends on key equality.
/// </remarks>
public class CoordinateSystemKeyTests
{
    /// <summary>
    /// Verifies that looking up a coordinate system by SRID returns a non-null result for a well-known code.
    /// </summary>
    [Fact]
    public void GetCoordinateSystem_BySrid_ReturnsNonNull()
    {
        CoordinateSystemServices css = CreateServices();

        CoordinateSystem? cs = css.GetCoordinateSystem(4326);

        Assert.NotNull(cs);
    }

    /// <summary>
    /// Verifies that looking up by authority and code returns the same object as by SRID.
    /// </summary>
    [Fact]
    public void GetCoordinateSystem_ByAuthorityAndCode_ReturnsSameAsById()
    {
        CoordinateSystemServices css = CreateServices();

        CoordinateSystem? bySrid = css.GetCoordinateSystem(4326);
        CoordinateSystem? byAuth = css.GetCoordinateSystem("EPSG", 4326);

        Assert.NotNull(bySrid);
        Assert.Same(bySrid, byAuth);
    }

    /// <summary>
    /// Verifies that GetSRID returns the expected value for a registered coordinate system.
    /// </summary>
    [Fact]
    public void GetSrid_ForRegisteredSystem_ReturnsExpectedValue()
    {
        CoordinateSystemServices css = CreateServices();

        int? srid = css.GetSRID("EPSG", 4326);

        Assert.Equal(4326, srid);
    }

    /// <summary>
    /// Verifies that GetCoordinateSystem returns null for an unknown SRID.
    /// </summary>
    [Fact]
    public void GetCoordinateSystem_UnknownSrid_ReturnsNull()
    {
        CoordinateSystemServices css = CreateServices();

        CoordinateSystem? cs = css.GetCoordinateSystem(999999);

        Assert.Null(cs);
    }

    /// <summary>
    /// Verifies that multiple well-known codes resolve to distinct coordinate system instances.
    /// </summary>
    [Fact]
    public void GetCoordinateSystem_DifferentSrids_ReturnDifferentInstances()
    {
        CoordinateSystemServices css = CreateServices();

        CoordinateSystem? cs4326 = css.GetCoordinateSystem(4326);
        CoordinateSystem? cs3857 = css.GetCoordinateSystem(3857);

        Assert.NotNull(cs4326);
        Assert.NotNull(cs3857);
        Assert.NotSame(cs4326, cs3857);
    }

    /// <summary>
    /// Verifies that repeated lookups for the same SRID return the same cached instance.
    /// </summary>
    [Fact]
    public void GetCoordinateSystem_RepeatedLookup_ReturnsCachedInstance()
    {
        CoordinateSystemServices css = CreateServices();

        CoordinateSystem? first = css.GetCoordinateSystem(4326);
        CoordinateSystem? second = css.GetCoordinateSystem(4326);

        Assert.Same(first, second);
    }

    /// <summary>
    /// Verifies that GetSRID returns null for an unregistered authority and code.
    /// </summary>
    [Fact]
    public void GetSrid_UnregisteredAuthorityCode_ReturnsNull()
    {
        CoordinateSystemServices css = CreateServices();

        int? srid = css.GetSRID("UNKNOWN", 99999);

        Assert.Null(srid);
    }

    /// <summary>
    /// Verifies that registering and looking up a coordinate system with an authority code larger than <see cref="int.MaxValue"/> does not overflow the lookup hash.
    /// </summary>
    [Fact]
    public void GetSrid_LargeAuthorityCode_DoesNotOverflowHashing()
    {
        const long LargeAuthorityCode = (long)int.MaxValue + 12345L;
        TestCoordinateSystemServices css = CreateMutableServices();
        GeographicCoordinateSystem coordinateSystem = GeographicCoordinateSystem.WGS84
            .WithAuthority("TEST", LargeAuthorityCode)
            .WithName("Large code WGS84");

        css.Register(4326, coordinateSystem);

        Assert.Equal(4326, css.GetSRID("TEST", LargeAuthorityCode));
        Assert.Same(coordinateSystem, css.GetCoordinateSystem("TEST", LargeAuthorityCode));
    }

    private static CoordinateSystemServices CreateServices()
    {
        return new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());
    }

    private static TestCoordinateSystemServices CreateMutableServices()
    {
        return new TestCoordinateSystemServices();
    }

    private sealed class TestCoordinateSystemServices : CoordinateSystemServices
    {
        public TestCoordinateSystemServices()
            : base(
                new CoordinateSystemFactory(),
                new CoordinateTransformationFactory(),
                [])
        {
        }

        public void Register(int srid, CoordinateSystem coordinateSystem)
        {
            this.AddCoordinateSystem(srid, coordinateSystem);
        }
    }
}
