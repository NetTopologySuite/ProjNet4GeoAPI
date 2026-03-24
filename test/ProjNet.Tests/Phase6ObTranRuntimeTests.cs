// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using System;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates runtime pipeline support for <c>ob_tran</c>.
/// </summary>
public class Phase6ObTranRuntimeTests
{
    /// <summary>
    /// Verifies builtins vector parity for spherical ob_tran with latlong child projection.
    /// </summary>
    /// <param name="operation">PROJ operation string.</param>
    /// <param name="inputX">Input x.</param>
    /// <param name="inputY">Input y.</param>
    /// <param name="expectedX">Expected x.</param>
    /// <param name="expectedY">Expected y.</param>
    [Theory]
    [InlineData("+proj=ob_tran +R=6400000 +o_proj=latlon +o_lon_p=20 +o_lat_p=20 +lon_0=180", 2d, 1d, -2.685687214d, 1.237430235d)]
    [InlineData("+proj=ob_tran +R=6400000 +o_proj=latlon +o_lon_p=20 +o_lat_p=20 +lon_0=180", 2d, -1d, -2.695406975d, 1.202683395d)]
    [InlineData("+proj=ob_tran +R=6400000 +o_proj=latlon +o_lon_p=20 +o_lat_p=20 +lon_0=180", -2d, 1d, -2.899366393d, 1.237430235d)]
    [InlineData("+proj=ob_tran +R=6400000 +o_proj=latlon +o_lon_p=20 +o_lat_p=20 +lon_0=180", -2d, -1d, -2.889646631d, 1.202683395d)]
    public void ObTranLatLonMatchesBuiltinsForward(
        string operation,
        double inputX,
        double inputY,
        double expectedX,
        double expectedY)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] projected = transform.Transform(new[] { inputX, inputY });
        Assert.InRange(Math.Abs(projected[0] - expectedX), 0d, 1e-9);
        Assert.InRange(Math.Abs(projected[1] - expectedY), 0d, 1e-9);
    }

    /// <summary>
    /// Verifies builtins vector parity for ob_tran with mollweide child projection.
    /// </summary>
    [Fact]
    public void ObTranMollMatchesMoreBuiltinsForward()
    {
        const string operation = "+proj=ob_tran +o_proj=moll +R=6378137.0 +o_lon_p=0 +o_lat_p=0 +lon_0=180";
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] projected = transform.Transform(new[] { 10d, 20d });
        Assert.InRange(Math.Abs(projected[0] - (-1384841.18787d)), 0d, 1e-5);
        Assert.InRange(Math.Abs(projected[1] - 7581707.88240d), 0d, 1e-5);
    }

    /// <summary>
    /// Verifies inverse runtime behavior for latlong child projection.
    /// </summary>
    /// <param name="inputX">Input x.</param>
    /// <param name="inputY">Input y.</param>
    /// <param name="expectedX">Expected x.</param>
    /// <param name="expectedY">Expected y.</param>
    [Theory]
    [InlineData(200d, 100d, 121.551874841d, -2.536100157d)]
    [InlineData(200d, -100d, 63.261184340d, 17.585319579d)]
    [InlineData(-200d, 100d, -141.100733224d, 26.091712305d)]
    [InlineData(-200d, -100d, -65.862385599d, 51.830295078d)]
    public void ObTranLatLonMatchesBuiltinsInverse(
        double inputX,
        double inputY,
        double expectedX,
        double expectedY)
    {
        const string operation = "+proj=ob_tran +R=6400000 +o_proj=latlon +o_lon_p=20 +o_lat_p=20 +lon_0=180 +inv";
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] projected = transform.Transform(new[] { inputX, inputY });
        Assert.InRange(Math.Abs(projected[0] - expectedX), 0d, 1e-6);
        Assert.InRange(Math.Abs(projected[1] - expectedY), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies malformed nested ob_tran setup returns runtime validation error.
    /// </summary>
    [Fact]
    public void NestedObTranIsRejected()
    {
        const string operation = "+proj=ob_tran +R=6400000 +o_proj=ob_tran";
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string skipReason);

        Assert.False(ok);
        Assert.Contains("Nested ob_tran", skipReason, StringComparison.Ordinal);
    }
}
