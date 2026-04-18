// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

/// <summary>
/// Represents one supported coordinate-system fixture sourced from the checked-in EPSG WKT archive.
/// </summary>
/// <param name="Srid">The final EPSG identifier for the coordinate system entry.</param>
/// <param name="ArchiveEntryPath">The archive entry path the WKT was loaded from.</param>
/// <param name="RootKeyword">The top-level WKT keyword for the coordinate system entry.</param>
/// <param name="Wkt">The WKT payload extracted from the archive entry.</param>
internal sealed record EpsgArchiveWktFixture(
    int Srid,
    string ArchiveEntryPath,
    string RootKeyword,
    string Wkt);
