// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNET.Tests;

using System;
using System.Globalization;
using Xunit;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Represents the documented type.
/// </summary>
public class CoordinateTransformTestsBase
{
    private readonly CoordinateSystemFactory coordinateSystemFactory = new CoordinateSystemFactory();
    private readonly CoordinateTransformationFactory coordinateTransformationFactory = new CoordinateTransformationFactory();
    private readonly Random random = new Random();

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

        return string.Format(
            CultureInfo.InvariantCulture,
            "{6} {7} transformation outside tolerance!\n\tExpected [{0}, {1}],\n\tgot      [{2}, {3}],\n\tdelta    [{4}, {5}]",
            pExpected[0],
            pExpected[1],
            pResult[0],
            pResult[1],
            pExpected[0] - pResult[0],
            pExpected[1] - pResult[1],
            projection,
            reverse ? "reverse" : "forward");
    }

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
    public void Test(
        string title,
        CoordinateSystem source,
        CoordinateSystem target,
        double[] testPoint,
        double[] expectedPoint,
        double tolerance,
        double reverseTolerance = double.NaN)
    {
        var ct = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target);

        double[] forwardResult = ct.MathTransform.Transform(testPoint);
        double[] reverseResult = double.IsNaN(reverseTolerance)
                                ? testPoint
                                : ct.MathTransform.Inverse().Transform(forwardResult);

        bool forward = this.ToleranceLessThan(forwardResult, expectedPoint, tolerance);

        bool reverse = double.IsNaN(reverseTolerance) ||
                      this.ToleranceLessThan(reverseResult, testPoint, reverseTolerance);

        if (!forward)
        {
            this.TransformationError(title, expectedPoint, forwardResult);
        }

        if (!reverse)
        {
            this.TransformationError(title, testPoint, reverseResult, true);
        }

        Assert.True(forward && reverse);
    }
}
