// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using System;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates M8 runtime parity for <c>vertoffset</c>.
/// </summary>
public class Phase8VertOffsetRuntimeTests
{
    private const string BuiltinsOperation = "+proj=vertoffset +lat_0=46.9166666666666666 +lon_0=8.183333333333334 +dh=-0.245 +slope_lat=-0.210 +slope_lon=-0.032 +ellps=GRS80";

    /// <summary>
    /// Verifies the PROJ <c>more_builtins.gie</c> vertical offset and slope vector.
    /// </summary>
    [Fact]
    public void VertOffsetMatchesMoreBuiltinsVector()
    {
        MathTransform transform = CreateTransform(BuiltinsOperation);
        double[] output = transform.Transform(CreatePoint(9.666666666666666d, 47.333333333333336d, 473.0d));

        Assert.InRange(Math.Abs(output[0] - 9.666666666666666d), 0d, 1e-12);
        Assert.InRange(Math.Abs(output[1] - 47.333333333333336d), 0d, 1e-12);
        Assert.InRange(Math.Abs(output[2] - 472.690d), 0d, 1e-3);
    }

    /// <summary>
    /// Verifies inverse operation recovers the original elevation.
    /// </summary>
    [Fact]
    public void VertOffsetInverseRecoversInputHeight()
    {
        MathTransform forward = CreateTransform(BuiltinsOperation);
        MathTransform inverse = CreateTransform(BuiltinsOperation + " +inv");

        double[] source = CreatePoint(9.666666666666666d, 47.333333333333336d, 473.0d);
        double[] transformed = forward.Transform(source);
        double[] recovered = inverse.Transform(transformed);

        Assert.InRange(Math.Abs(recovered[0] - source[0]), 0d, 1e-12);
        Assert.InRange(Math.Abs(recovered[1] - source[1]), 0d, 1e-12);
        Assert.InRange(Math.Abs(recovered[2] - source[2]), 0d, 1e-3);
    }

    /// <summary>
    /// Verifies optional parameters default to identity-like behavior when omitted.
    /// </summary>
    [Fact]
    public void VertOffsetDefaultsToNoOffsetWhenParametersAreMissing()
    {
        MathTransform transform = CreateTransform("+proj=vertoffset +ellps=GRS80");
        double[] output = transform.Transform(CreatePoint(10d, 47d, 100d));

        Assert.Equal(10d, output[0], 12);
        Assert.Equal(47d, output[1], 12);
        Assert.Equal(100d, output[2], 12);
    }

    /// <summary>
    /// Verifies validation failures for malformed numeric arguments.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    /// <param name="expectedToken">Expected diagnostic token.</param>
    [Theory]
    [InlineData("+proj=vertoffset +lat_0=abc", "lat_0")]
    [InlineData("+proj=vertoffset +lon_0=abc", "lon_0")]
    [InlineData("+proj=vertoffset +dh=abc", "dh")]
    [InlineData("+proj=vertoffset +slope_lat=abc", "slope_lat")]
    [InlineData("+proj=vertoffset +slope_lon=abc", "slope_lon")]
    public void VertOffsetCreationFailsForInvalidParameters(string operation, string expectedToken)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string skipReason);

        Assert.False(ok);
        Assert.Contains(expectedToken, skipReason, StringComparison.Ordinal);
    }

    private static MathTransform CreateTransform(string operation)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);
        Assert.True(ok, skipReason);
        return transform;
    }

    private static double[] CreatePoint(double x, double y, double z) => [x, y, z];
}
