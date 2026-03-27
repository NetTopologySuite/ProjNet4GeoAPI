// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using System;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates M8 runtime parity for <c>molodensky</c>.
/// </summary>
public class MolodenskyRuntimeTests
{
    /// <summary>
    /// Verifies abridged Molodensky vector from PROJ <c>more_builtins.gie</c>.
    /// </summary>
    [Fact]
    public void MolodenskyAbridgedMatchesMoreBuiltinsVector()
    {
        const string operation = "+proj=molodensky +a=6378160 +rf=298.25 +da=-23 +df=-8.120449e-8 +dx=-134 +dy=-48 +dz=149 +abridged";
        MathTransform transform = CreateTransform(operation);
        double[] output = transform.Transform(CreatePoint(144.9667d, -37.8d, 50d));

        Assert.InRange(Math.Abs(output[0] - 144.968d), 0d, 2.5e-5);
        Assert.InRange(Math.Abs(output[1] - (-37.79848d)), 0d, 2.5e-5);
        Assert.InRange(Math.Abs(output[2] - 46.378d), 0d, 5e-3);
    }

    /// <summary>
    /// Verifies standard Molodensky vector from PROJ <c>more_builtins.gie</c>.
    /// </summary>
    [Fact]
    public void MolodenskyStandardMatchesMoreBuiltinsVector()
    {
        const string operation = "+proj=molodensky +a=6378160 +rf=298.25 +da=-23 +df=-8.120449e-8 +dx=-134 +dy=-48 +dz=149";
        MathTransform transform = CreateTransform(operation);
        double[] output = transform.Transform(CreatePoint(144.9667d, -37.8d, 50d));

        Assert.InRange(Math.Abs(output[0] - 144.968d), 0d, 2.5e-5);
        Assert.InRange(Math.Abs(output[1] - (-37.79848d)), 0d, 2.5e-5);
        Assert.InRange(Math.Abs(output[2] - 46.378d), 0d, 5e-3);
    }

    /// <summary>
    /// Verifies identity behavior for all-zero Molodensky parameter set.
    /// </summary>
    [Fact]
    public void MolodenskyZeroParametersBehavesAsIdentity()
    {
        const string operation = "+proj=molodensky +a=6378160 +rf=298.25 +da=0 +df=0 +dx=0 +dy=0 +dz=0";
        MathTransform transform = CreateTransform(operation);
        double[] output = transform.Transform(CreatePoint(144.9667d, -37.8d, 50d));

        Assert.InRange(Math.Abs(output[0] - 144.9667d), 0d, 1e-10);
        Assert.InRange(Math.Abs(output[1] - (-37.8d)), 0d, 1e-10);
        Assert.InRange(Math.Abs(output[2] - 50d), 0d, 1e-10);
    }

    /// <summary>
    /// Verifies inverse behavior for static Molodensky operation.
    /// </summary>
    [Fact]
    public void MolodenskyInverseRecoversInputApproximately()
    {
        const string forwardOperation = "+proj=molodensky +a=6378160 +rf=298.25 +da=-23 +df=-8.120449e-8 +dx=-134 +dy=-48 +dz=149";
        const string inverseOperation = "+proj=molodensky +a=6378160 +rf=298.25 +da=-23 +df=-8.120449e-8 +dx=-134 +dy=-48 +dz=149 +inv";
        MathTransform forward = CreateTransform(forwardOperation);
        MathTransform inverse = CreateTransform(inverseOperation);

        double[] source = CreatePoint(144.9667d, -37.8d, 50d);
        double[] transformed = forward.Transform(source);
        double[] recovered = inverse.Transform(transformed);

        Assert.InRange(Math.Abs(recovered[0] - source[0]), 0d, 2e-5);
        Assert.InRange(Math.Abs(recovered[1] - source[1]), 0d, 2e-5);
        Assert.InRange(Math.Abs(recovered[2] - source[2]), 0d, 2e-2);
    }

    /// <summary>
    /// Verifies required-parameter validation paths for Molodensky.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    /// <param name="expectedToken">Expected diagnostic token.</param>
    [Theory]
    [InlineData("+proj=molodensky +a=6378160 +rf=298.25", "missing dx")]
    [InlineData("+proj=molodensky +a=6378160 +rf=298.25 +dx=0", "missing dy")]
    [InlineData("+proj=molodensky +a=6378160 +rf=298.25 +dx=0 +dy=0", "missing dz")]
    [InlineData("+proj=molodensky +a=6378160 +rf=298.25 +dx=0 +dy=0 +dz=0", "missing da")]
    [InlineData("+proj=molodensky +a=6378160 +rf=298.25 +dx=0 +dy=0 +dz=0 +da=0", "missing df")]
    public void MolodenskyCreationFailsWhenMandatoryParametersAreMissing(string operation, string expectedToken)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string? skipReason);

        Assert.False(ok);
        Assert.Contains(expectedToken, Assert.IsAssignableFrom<string>(skipReason), StringComparison.Ordinal);
    }

    private static MathTransform CreateTransform(string operation)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform? transform, out string? skipReason);
        Assert.True(ok, skipReason);
        return Assert.IsAssignableFrom<MathTransform>(transform);
    }

    private static double[] CreatePoint(double x, double y, double z) => [x, y, z];
}
