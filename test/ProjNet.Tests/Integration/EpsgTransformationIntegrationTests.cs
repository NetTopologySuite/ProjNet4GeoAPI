// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using ProjNet;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Verifies representative EPSG SRID transformations across multiple operation categories.
/// </summary>
[Collection(GlobalEnvironmentTestIsolation.Name)]
public class EpsgTransformationIntegrationTests
{
    private static readonly CoordinateSystemServices Services = new();

    /// <summary>
    /// Provides representative EPSG SRID pairs with reference coordinates and inverse round-trip tolerances.
    /// </summary>
    /// <returns>Integration rows covering projection-only, projected-chain, datum-shifted, and grid-backed EPSG pairs.</returns>
    public static TheoryDataRow<int, int, double, double, double, double, double, double, double, double, string>[] GetRepresentativeCases()
    {
        return
        [
            new TheoryDataRow<int, int, double, double, double, double, double, double, double, double, string>(4326, 3857, 10d, 10d, 1113194.90793274d, 1118889.97485796d, 1e-6d, 1e-6d, 1e-9d, 1e-9d, "projection-only 4326->3857"),
            new TheoryDataRow<int, int, double, double, double, double, double, double, double, double, string>(3857, 4326, 1113194.90793274d, 1118889.97485796d, 10d, 10d, 1e-9d, 1e-9d, 1e-6d, 1e-6d, "projection-only 3857->4326"),
            new TheoryDataRow<int, int, double, double, double, double, double, double, double, double, string>(25832, 3857, 702575d, 6153153d, 1358761.89d, 7456070.47d, 0.02d, 0.02d, 0.02d, 0.02d, "projected chain 25832->3857"),
            new TheoryDataRow<int, int, double, double, double, double, double, double, double, double, string>(25832, 4326, 702575d, 6153153d, 12.20596573266128d, 55.48246005652269d, 5e-7d, 5e-7d, 0.02d, 0.02d, "utm to geographic 25832->4326"),
            new TheoryDataRow<int, int, double, double, double, double, double, double, double, double, string>(27700, 4326, 362895d, 155602d, -2.5335813d, 51.2983258d, 0.0005d, 0.0005d, 2d, 2d, "helmert-backed 27700->4326"),
            new TheoryDataRow<int, int, double, double, double, double, double, double, double, double, string>(26910, 4326, 3523562.711189d, 6246615.391161d, -82.0479097d, 48.4185597d, 0.01d, 0.01d, -1d, -1d, "datum-shifted 26910->4326"),
            new TheoryDataRow<int, int, double, double, double, double, double, double, double, double, string>(4326, 3035, 16.4d, 48.2d, 4796297.431434812d, 2807999.1539475969d, 1e-2d, 1e-2d, 1e-2d, 1e-2d, "lambert azimuthal equal-area 4326->3035"),
            new TheoryDataRow<int, int, double, double, double, double, double, double, double, double, string>(31466, 25832, 2598417.333192d, 5930677.980308d, 399340.601863d, 5928794.177992d, 3.5d, 3.5d, 1e-2d, 1e-2d, "dhdn fallback/grid 31466->25832"),
            new TheoryDataRow<int, int, double, double, double, double, double, double, double, double, string>(31467, 25832, 3399371.190396d, 5930724.531323d, 399340.601862d, 5928794.177992d, 3.5d, 3.5d, 1e-2d, 1e-2d, "dhdn fallback/grid 31467->25832"),
            new TheoryDataRow<int, int, double, double, double, double, double, double, double, double, string>(31467, 25833, 3615881.001454d, 5940351.727710d, 218617.111391d, 5945399.220269d, 3.5d, 3.5d, 1e-2d, 1e-2d, "dhdn fallback/grid 31467->25833"),
        ];
    }

    /// <summary>
    /// Verifies that representative EPSG pairs resolve, match their reference coordinate, and round-trip through the resolved inverse transform.
    /// </summary>
    /// <param name="sourceSrid">Source EPSG SRID.</param>
    /// <param name="targetSrid">Target EPSG SRID.</param>
    /// <param name="inputX">Source x or longitude.</param>
    /// <param name="inputY">Source y or latitude.</param>
    /// <param name="expectedX">Expected target x or longitude.</param>
    /// <param name="expectedY">Expected target y or latitude.</param>
    /// <param name="expectedToleranceX">Accepted target x tolerance.</param>
    /// <param name="expectedToleranceY">Accepted target y tolerance.</param>
    /// <param name="roundTripToleranceX">Accepted source x tolerance after inverse transformation.</param>
    /// <param name="roundTripToleranceY">Accepted source y tolerance after inverse transformation.</param>
    /// <param name="caseLabel">Human-readable label for assertion output.</param>
    [Theory]
    [MemberData(nameof(GetRepresentativeCases))]
    public void CreateTransformation_WithRepresentativeEpsgPairs_MatchesReferenceAndRoundTrips(
        int sourceSrid,
        int targetSrid,
        double inputX,
        double inputY,
        double expectedX,
        double expectedY,
        double expectedToleranceX,
        double expectedToleranceY,
        double roundTripToleranceX,
        double roundTripToleranceY,
        string caseLabel)
    {
        ICoordinateTransformation forward = Assert.IsType<ICoordinateTransformation>(Services.CreateTransformation(sourceSrid, targetSrid), exactMatch: false);

        double[] output = forward.MathTransform.Transform([inputX, inputY]);

        Assert.True(!double.IsNaN(output[0]), FormattableString.Invariant($"{caseLabel}: expected finite X output."));
        Assert.True(!double.IsNaN(output[1]), FormattableString.Invariant($"{caseLabel}: expected finite Y output."));
        AssertWithinTolerance(output[0], expectedX, expectedToleranceX, caseLabel, "target X");
        AssertWithinTolerance(output[1], expectedY, expectedToleranceY, caseLabel, "target Y");

        if (roundTripToleranceX < 0d || roundTripToleranceY < 0d)
        {
            return;
        }

        Assert.True(forward.MathTransform.IsInvertible, FormattableString.Invariant($"{caseLabel}: expected resolved transform to expose inverse support."));

        MathTransform inverse = Assert.IsType<MathTransform>(forward.MathTransform.Inverse(), exactMatch: false);
        double[] roundTripped = inverse.Transform(output);
        AssertWithinTolerance(roundTripped[0], inputX, roundTripToleranceX, caseLabel, "round-trip X");
        AssertWithinTolerance(roundTripped[1], inputY, roundTripToleranceY, caseLabel, "round-trip Y");
    }

    private static void AssertWithinTolerance(double actual, double expected, double tolerance, string caseLabel, string axisLabel)
    {
        double delta = Math.Abs(actual - expected);
        Assert.True(
            delta <= tolerance,
            FormattableString.Invariant($"{caseLabel}: expected {axisLabel} delta <= {tolerance:R} but was {delta:R} (expected {expected:R}, actual {actual:R})."));
    }
}
