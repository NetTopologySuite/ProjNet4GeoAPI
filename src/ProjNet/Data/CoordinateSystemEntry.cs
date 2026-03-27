// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Data;

using ProjNet.CoordinateSystems;

/// <summary>
/// Represents a resolved coordinate system object identified by SRID.
/// </summary>
/// <param name="Srid">The SRID of the coordinate system.</param>
/// <param name="CoordinateSystem">The resolved coordinate system object.</param>
public readonly record struct CoordinateSystemEntry(int Srid, CoordinateSystem CoordinateSystem);
