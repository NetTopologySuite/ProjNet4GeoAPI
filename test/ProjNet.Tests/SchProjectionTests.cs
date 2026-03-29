// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates SCH projection runtime parity.
/// </summary>
public class SchProjectionTests
{
    private const string SchForwardOperation = "+proj=sch +datum=WGS84 +plat_0=30.0 +plon_0=45.0 +phdg_0=-12.0 +nodefs";
    private const string SchInverseOperation = "+proj=sch +datum=WGS84 +plat_0=30.0 +plon_0=45.0 +phdg_0=-12.0 +nodefs +inv";

    /// <summary>
    /// Verifies aliases resolve through runtime pipeline factory for SCH.
    /// </summary>
    /// <param name="operation">PROJ operation string.</param>
    [Theory]
    [InlineData("+proj=sch +datum=WGS84 +plat_0=30 +plon_0=45 +phdg_0=-12")]
    [InlineData("+proj=spherical_cross_track_height +datum=WGS84 +plat_0=30 +plon_0=45 +phdg_0=-12")]
    public void SupportsSchAliasesInRuntime(string operation)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform? transform, out string? skipReason);

        Assert.True(ok, skipReason);
        Assert.IsType<MathTransform>(transform, exactMatch: false);
    }

    /// <summary>
    /// Verifies SCH forward vectors from PROJ <c>test_cs2cs_various.yaml</c>.
    /// </summary>
    /// <param name="inputLongitude">Input longitude in degrees.</param>
    /// <param name="inputLatitude">Input latitude in degrees.</param>
    /// <param name="inputHeight">Input height in meters.</param>
    /// <param name="expectedX">Expected SCH X.</param>
    /// <param name="expectedY">Expected SCH Y.</param>
    /// <param name="expectedZ">Expected SCH Z.</param>
    [Theory]
    [InlineData(0d, 0d, 0d, -1977112.0305592d, 5551475.1418378d, 6595.7256583d)]
    [InlineData(0d, 90d, 0d, 6618337.9734775d, -1152927.4060894d, 10055.1157181d)]
    [InlineData(45d, 45d, 0d, 1630035.5650122d, -342353.6396475d, 128.3445654d)]
    [InlineData(45.1d, 44.9d, 0d, 1617547.4295637d, -347855.9734973d, 125.4645102d)]
    [InlineData(44.9d, 45.1d, 0d, 1642526.7453121d, -336878.8571851d, 131.3265616d)]
    [InlineData(30d, 45d, 0d, 1974596.2356203d, 787409.8217445d, 773.0028577d)]
    public void MatchesSchForwardVectors(
        double inputLongitude,
        double inputLatitude,
        double inputHeight,
        double expectedX,
        double expectedY,
        double expectedZ)
    {
        MathTransform transform = CreateTransform(SchForwardOperation);
        double[] projected = transform.Transform(CreatePoint(inputLongitude, inputLatitude, inputHeight));

        Assert.InRange(Math.Abs(projected[0] - expectedX), 0d, 1e-6);
        Assert.InRange(Math.Abs(projected[1] - expectedY), 0d, 1e-6);
        Assert.InRange(Math.Abs(projected[2] - expectedZ), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies SCH inverse vectors from PROJ <c>test_cs2cs_various.yaml</c>.
    /// </summary>
    /// <param name="inputX">Input SCH X.</param>
    /// <param name="inputY">Input SCH Y.</param>
    /// <param name="inputZ">Input SCH Z.</param>
    /// <param name="expectedLongitude">Expected longitude in degrees.</param>
    /// <param name="expectedLatitude">Expected latitude in degrees.</param>
    /// <param name="expectedHeight">Expected ellipsoidal height in meters.</param>
    [Theory]
    [InlineData(0d, 0d, 2d, 45d, 30d, 2d)]
    [InlineData(0d, 1000d, 0d, 44.989863d, 29.998124d, -0.000362d)]
    [InlineData(1000d, 0d, 0d, 44.997845d, 30.008824d, 0d)]
    [InlineData(1000d, 1000d, 0d, 44.987707d, 30.006948d, -0.000523d)]
    public void MatchesSchInverseVectors(
        double inputX,
        double inputY,
        double inputZ,
        double expectedLongitude,
        double expectedLatitude,
        double expectedHeight)
    {
        MathTransform transform = CreateTransform(SchInverseOperation);
        double[] geographic = transform.Transform(CreatePoint(inputX, inputY, inputZ));

        Assert.InRange(Math.Abs(geographic[0] - expectedLongitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(geographic[1] - expectedLatitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(geographic[2] - expectedHeight), 0d, 2e-3);
    }

    /// <summary>
    /// Verifies runtime validation errors for missing mandatory SCH parameters.
    /// </summary>
    /// <param name="operation">PROJ operation string.</param>
    /// <param name="expectedToken">Token expected in validation message.</param>
    [Theory]
    [InlineData("+proj=sch +plon_0=45 +phdg_0=-12", "plat_0")]
    [InlineData("+proj=sch +plat_0=30 +phdg_0=-12", "plon_0")]
    [InlineData("+proj=sch +plat_0=30 +plon_0=45", "phdg_0")]
    public void SchCreationFailsWhenMandatoryParametersAreMissing(string operation, string expectedToken)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string? skipReason);

        Assert.False(ok);
        Assert.Contains(expectedToken, skipReason, StringComparison.Ordinal);
    }

    private static MathTransform CreateTransform(string operation)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform? transform, out string? skipReason);
        Assert.True(ok, skipReason);
        return Assert.IsType<MathTransform>(transform, exactMatch: false);
    }

    private static double[] CreatePoint(double x, double y, double z) => [x, y, z];
}
