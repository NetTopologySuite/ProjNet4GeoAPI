// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNET.Tests;

using System;
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
        var actual = new double[expected.Length];
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
        var actual = new double[expected.Length];
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
        var destination = new double[3];

        var exception = Assert.Throws<ArgumentException>(() => transform.Transform(new ReadOnlySpan<double>(input), destination.AsSpan()));
        Assert.Equal("result", exception.ParamName);
    }

    /// <summary>
    /// Verifies span overload parity for convex hull and domain flag APIs.
    /// </summary>
    [Fact]
    public void ConvexHullAndDomainFlagsSpanOverloadsMatchListOverloads()
    {
        var transform = new StubMathTransform();
        double[] ordinates = [10.0, 20.0, 30.0, 40.0];

        var hullFromList = transform.GetCodomainConvexHull(new System.Collections.Generic.List<double>(ordinates));
        var hullFromSpan = transform.GetCodomainConvexHull(ordinates.AsSpan());
        Assert.Equal(hullFromList, hullFromSpan);

        DomainFlags flagsFromList = transform.GetDomainFlags(new System.Collections.Generic.List<double>(ordinates));
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

        public override System.Collections.Generic.List<double> GetCodomainConvexHull(System.Collections.Generic.List<double> points)
        {
            return new System.Collections.Generic.List<double>(points);
        }

        public override DomainFlags GetDomainFlags(System.Collections.Generic.List<double> points)
        {
            return points.Count == 0 ? DomainFlags.Outside : DomainFlags.Inside;
        }
    }
}
