// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

/// <summary>
/// Provides shared numeric tolerances for non-fixture tests.
/// </summary>
internal static class TestTolerances
{
    /// <summary>
    /// Gets the tolerance used for coordinate round-trip assertions.
    /// </summary>
    internal const double CoordinateRoundTrip = 1e-5d;

    /// <summary>
    /// Gets the tolerance used for angular-unit round-trip assertions.
    /// </summary>
    internal const double AngularUnitRoundTrip = 1e-13d;

    /// <summary>
    /// Gets the tolerance used when only floating-point noise should differ between results.
    /// </summary>
    internal const double StableResult = 1e-12d;
}
