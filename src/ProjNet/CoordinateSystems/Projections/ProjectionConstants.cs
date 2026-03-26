// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

/// <summary>
/// Shared numeric constants reused across projection implementations.
/// </summary>
internal static class ProjectionConstants
{
    /// <summary>
    /// One third.
    /// </summary>
    internal const double OneThird = 0.33333333333333333333d;

    /// <summary>
    /// Two thirds.
    /// </summary>
    internal const double TwoThirds = 0.66666666666666666666d;

    /// <summary>
    /// One plus a 1e-7 tolerance margin.
    /// </summary>
    internal const double OnePlusEps7 = 1.0000001d;

    /// <summary>
    /// One plus a 1e-6 tolerance margin.
    /// </summary>
    internal const double OnePlusEps6 = 1.000001d;

    /// <summary>
    /// Shared 1e-12 tolerance.
    /// </summary>
    internal const double Tolerance1E12 = 1e-12d;
}
