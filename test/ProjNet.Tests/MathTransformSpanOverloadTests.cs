// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Verifies span-based overload behavior on <see cref="MathTransform"/>.
/// </summary>
public class MathTransformSpanOverloadTests
{
    /// <summary>
    /// Verifies span point transform parity for 2D identity transforms.
    /// </summary>
    [Fact]
    public void TransformReadOnlySpan2DMatchesArrayTransform()
    {
        MathTransform transform = new StubMathTransform();
        double[] input = [12.5, -7.25];

        double[] expected = transform.Transform(input);
        double[] actual = new double[expected.Length];
        transform.Transform(new ReadOnlySpan<double>(input), actual.AsSpan());

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Verifies span point transform parity for 4D identity transforms.
    /// </summary>
    [Fact]
    public void TransformReadOnlySpan4DMatchesArrayTransform()
    {
        MathTransform transform = new StubMathTransform();
        double[] input = [1.0, 2.0, 3.0, 4.0];

        double[] expected = transform.Transform(input);
        double[] actual = new double[expected.Length];
        transform.Transform(new ReadOnlySpan<double>(input), actual.AsSpan());

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Verifies span point transform parity for dimensions above 4 ordinates.
    /// </summary>
    [Fact]
    public void TransformReadOnlySpan5DMatchesArrayTransform()
    {
        MathTransform transform = new IdentityMathTransform(5);
        double[] input = [11.0, -3.5, 4.25, 2026.0, 99.75];

        double[] expected = transform.Transform(input);
        double[] actual = new double[expected.Length];
        transform.Transform(new ReadOnlySpan<double>(input), actual.AsSpan());

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Verifies that too-small destination spans are rejected.
    /// </summary>
    [Fact]
    public void TransformReadOnlySpanWithSmallDestinationThrows()
    {
        MathTransform transform = new StubMathTransform();
        double[] input = [1.0, 2.0, 3.0, 4.0];
        double[] destination = new double[3];

        ArgumentException exception = Assert.Throws<ArgumentException>(() => transform.Transform(new ReadOnlySpan<double>(input), destination.AsSpan()));
        Assert.Equal("result", exception.ParamName);
    }

    /// <summary>
    /// Verifies that span point transforms only overwrite the required destination prefix.
    /// </summary>
    [Fact]
    public void TransformReadOnlySpanWithLargerDestinationPreservesRemainingValues()
    {
        MathTransform transform = new IdentityMathTransform(3);
        double[] input = [4.0, 5.0, 6.0];
        double[] expected = transform.Transform(input);

        double[] actual = new double[6];
        for (int i = 0; i < actual.Length; i++)
        {
            actual[i] = -1.0;
        }

        transform.Transform(new ReadOnlySpan<double>(input), actual.AsSpan());

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], 12);
        }

        for (int i = expected.Length; i < actual.Length; i++)
        {
            Assert.Equal(-1.0, actual[i], 12);
        }
    }

    /// <summary>
    /// Verifies span overload parity for convex hull and domain flag APIs.
    /// </summary>
    [Fact]
    public void ConvexHullAndDomainFlagsSpanOverloadsMatchListOverloads()
    {
        var transform = new StubMathTransform();
        double[] ordinates = [10.0, 20.0, 30.0, 40.0];

        List<double> hullFromList = transform.GetCodomainConvexHull(new List<double>(ordinates));
        List<double> hullFromSpan = transform.GetCodomainConvexHull(ordinates.AsSpan());
        Assert.Equal(hullFromList, hullFromSpan);

        DomainFlags flagsFromList = transform.GetDomainFlags(new List<double>(ordinates));
        DomainFlags flagsFromSpan = transform.GetDomainFlags(ordinates.AsSpan());
        Assert.Equal(flagsFromList, flagsFromSpan);
    }

    /// <summary>
    /// Verifies span overload parity for empty convex hull and domain flag inputs.
    /// </summary>
    [Fact]
    public void ConvexHullAndDomainFlagsSpanOverloadsMatchListOverloadsForEmptyInput()
    {
        var transform = new StubMathTransform();
        double[] ordinates = [];

        List<double> hullFromList = transform.GetCodomainConvexHull(new List<double>(ordinates));
        List<double> hullFromSpan = transform.GetCodomainConvexHull(ordinates.AsSpan());
        Assert.Equal(hullFromList, hullFromSpan);

        DomainFlags flagsFromList = transform.GetDomainFlags(new List<double>(ordinates));
        DomainFlags flagsFromSpan = transform.GetDomainFlags(ordinates.AsSpan());
        Assert.Equal(flagsFromList, flagsFromSpan);
    }

    private sealed class StubMathTransform : MathTransform
    {
        public override int DimSource => 2;

        public override int DimTarget => 2;

        public override string WKT => "PARAM_MT[\"Stub\"]";

        public override string XML => "<Stub />";

        public override MathTransform Inverse() => this;

        public override void Invert()
        {
        }

        public override bool Identity() => true;

        public override void Transform(ref double x, ref double y, ref double z)
        {
        }

        public override List<double> GetCodomainConvexHull(List<double> points)
        {
            return new List<double>(points);
        }

        public override DomainFlags GetDomainFlags(List<double> points)
        {
            return points.Count == 0 ? DomainFlags.Outside : DomainFlags.Inside;
        }
    }
}
