// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Data;

/// <summary>
/// Represents a coordinate system definition identified by SRID and serialized as WKT.
/// </summary>
/// <param name="Srid">The SRID of the coordinate system definition.</param>
/// <param name="Wkt">The Well-Known Text representation of the coordinate system.</param>
public readonly record struct CoordinateSystemDefinition(int Srid, string Wkt);
