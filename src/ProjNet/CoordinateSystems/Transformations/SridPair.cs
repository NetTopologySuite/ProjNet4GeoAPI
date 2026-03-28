// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Represents a source/target SRID pair for dictionary lookups.
/// </summary>
/// <param name="SourceSrid">The source SRID.</param>
/// <param name="TargetSrid">The target SRID.</param>
internal readonly record struct SridPair(int SourceSrid, int TargetSrid)
{
}
