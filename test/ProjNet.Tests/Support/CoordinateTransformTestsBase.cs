// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Base class providing shared factories, stochastic input, and transformation assertions for transform-focused tests.
/// </summary>
public abstract class CoordinateTransformTestsBase
{
    private readonly CoordinateSystemFactory coordinateSystemFactory = CoordinateSystemTestHelpers.CreateCoordinateSystemFactory();
    private readonly CoordinateTransformationFactory coordinateTransformationFactory = CoordinateSystemTestHelpers.CreateCoordinateTransformationFactory();
    private readonly Random random = new();

    /// <summary>
    /// Gets the shared coordinate system factory used by transformation tests.
    /// </summary>
    protected CoordinateSystemFactory CoordinateSystemFactory => this.coordinateSystemFactory;

    /// <summary>
    /// Gets the shared transformation factory used by transformation tests.
    /// </summary>
    protected CoordinateTransformationFactory CoordinateTransformationFactory => this.coordinateTransformationFactory;

    /// <summary>
    /// Gets the random source used for stochastic test data when needed.
    /// </summary>
    protected Random Random => this.random;

    /// <summary>
    /// Gets or sets a value indicating whether verbose test diagnostics are enabled.
    /// </summary>
    protected bool Verbose { get; set; }

    /// <summary>
    /// Parses a coordinate system from WKT using the shared test factory.
    /// </summary>
    /// <param name="wkt">Well-known text representation of the coordinate system.</param>
    /// <returns>The parsed coordinate system.</returns>
    protected CoordinateSystem RequireCoordinateSystem(string wkt)
        => CoordinateSystemTestHelpers.RequireCoordinateSystem(this.CoordinateSystemFactory, wkt);

    /// <summary>
    /// Parses a coordinate system from WKT using the shared test factory and asserts the requested type.
    /// </summary>
    /// <typeparam name="TCoordinateSystem">The expected coordinate-system type.</typeparam>
    /// <param name="wkt">Well-known text representation of the coordinate system.</param>
    /// <returns>The parsed coordinate system.</returns>
    protected TCoordinateSystem RequireCoordinateSystem<TCoordinateSystem>(string wkt)
        where TCoordinateSystem : CoordinateSystem
        => CoordinateSystemTestHelpers.RequireCoordinateSystem<TCoordinateSystem>(this.CoordinateSystemFactory, wkt);

    /// <summary>
    /// Creates a transformation between two coordinate systems using the shared factory.
    /// </summary>
    /// <param name="source">Source coordinate system.</param>
    /// <param name="target">Target coordinate system.</param>
    /// <returns>The created transformation.</returns>
    protected ICoordinateTransformation CreateTransformation(CoordinateSystem source, CoordinateSystem target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        return this.CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target);
    }

    /// <summary>
    /// Creates a transformation and asserts that the operation succeeds without throwing.
    /// </summary>
    /// <param name="source">Source coordinate system.</param>
    /// <param name="target">Target coordinate system.</param>
    /// <returns>The created transformation.</returns>
    protected ICoordinateTransformation AssertTransformationCreated(CoordinateSystem source, CoordinateSystem target)
    {
        ICoordinateTransformation? transformation = null;
        Exception? exception = Record.Exception(() => transformation = this.CreateTransformation(source, target));
        Assert.Null(exception);
        return Assert.IsType<ICoordinateTransformation>(transformation, exactMatch: false);
    }

    /// <summary>
    /// Checks whether the coordinate deltas between two points are below the provided tolerance.
    /// </summary>
    /// <param name="p1">First point.</param>
    /// <param name="p2">Second point.</param>
    /// <param name="tolerance">Maximum allowed absolute delta per ordinate.</param>
    /// <returns><see langword="true"/> when all compared ordinates are within tolerance.</returns>
    protected bool ToleranceLessThan(double[] p1, double[] p2, double tolerance)
    {
        ArgumentNullException.ThrowIfNull(p1);
        ArgumentNullException.ThrowIfNull(p2);

        double d0 = Math.Abs(p1[0] - p2[0]);
        double d1 = Math.Abs(p1[1] - p2[1]);
        if (p1.Length > 2 && p2.Length > 2)
        {
            double d2 = Math.Abs(p1[2] - p2[2]);
            if (this.Verbose)
            {
                Console.WriteLine("Allowed Tolerance {3}; got dx: {0}, dy: {1}, dz {2}", d0, d1, d2, tolerance);
            }

            return d0 < tolerance && d1 < tolerance && d2 < tolerance;
        }

        Console.WriteLine();
        if (this.Verbose)
        {
            Console.WriteLine("Allowed tolerance {2}; got dx: {0}, dy: {1}", d0, d1, tolerance);
        }

        return d0 < tolerance && d1 < tolerance;
    }

    /// <summary>
    /// Formats a readable error message for transformation mismatches.
    /// </summary>
    /// <param name="projection">Projection label used in the message.</param>
    /// <param name="pExpected">Expected coordinate.</param>
    /// <param name="pResult">Actual coordinate.</param>
    /// <param name="reverse">Whether the failing direction is reverse/inverse.</param>
    /// <returns>Formatted error string for diagnostics.</returns>
    protected string TransformationError(string projection, double[] pExpected, double[] pResult, bool reverse = false)
    {
        ArgumentNullException.ThrowIfNull(pExpected);
        ArgumentNullException.ThrowIfNull(pResult);

        return FormattableString.Invariant($"{projection} {(reverse ? "reverse" : "forward")} transformation outside tolerance!\n\tExpected [{pExpected[0]}, {pExpected[1]}],\n\tgot      [{pResult[0]}, {pResult[1]}],\n\tdelta    [{pExpected[0] - pResult[0]}, {pExpected[1] - pResult[1]}]");
    }

    /// <summary>
    /// Asserts that an actual transformed coordinate matches the expected coordinate within the provided tolerance.
    /// </summary>
    /// <param name="projection">Projection label used in assertion diagnostics.</param>
    /// <param name="expected">Expected coordinate.</param>
    /// <param name="actual">Actual coordinate.</param>
    /// <param name="tolerance">Maximum allowed absolute delta per ordinate.</param>
    /// <param name="reverse">Whether the asserted direction is reverse/inverse.</param>
    protected void AssertCoordinateWithinTolerance(string projection, double[] expected, double[] actual, double tolerance, bool reverse = false)
        => Assert.True(
            this.ToleranceLessThan(actual, expected, tolerance),
            this.TransformationError(projection, expected, actual, reverse));

    /// <summary>
    /// Executes a forward (and optionally reverse) transformation assertion with tolerance checks.
    /// </summary>
    /// <param name="title">Display title for error diagnostics.</param>
    /// <param name="source">Source coordinate system.</param>
    /// <param name="target">Target coordinate system.</param>
    /// <param name="testPoint">Input coordinate in source space.</param>
    /// <param name="expectedPoint">Expected coordinate in target space.</param>
    /// <param name="tolerance">Forward transformation tolerance.</param>
    /// <param name="reverseTolerance">Optional inverse tolerance; NaN skips inverse assertion.</param>
    protected void AssertTransformation(
        string title,
        CoordinateSystem source,
        CoordinateSystem target,
        double[] testPoint,
        double[] expectedPoint,
        double tolerance,
        double reverseTolerance = double.NaN)
    {
        ICoordinateTransformation transformation = this.CreateTransformation(source, target);
        double[] forwardResult = transformation.MathTransform.Transform(testPoint);
        this.AssertCoordinateWithinTolerance(title, expectedPoint, forwardResult, tolerance);

        if (double.IsNaN(reverseTolerance))
        {
            return;
        }

        double[] reverseResult = transformation.MathTransform.Inverse().Transform(forwardResult);
        this.AssertCoordinateWithinTolerance(title, testPoint, reverseResult, reverseTolerance, reverse: true);
    }
}
