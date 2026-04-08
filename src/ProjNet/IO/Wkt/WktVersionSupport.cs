// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.Wkt;

using System;
using ProjNet;

/// <summary>
/// Provides shared validation and exception helpers for versioned WKT node serialization.
/// </summary>
internal static class WktVersionSupport
{
    /// <summary>
    /// Validates that the supplied WKT version is one of the supported enum values.
    /// </summary>
    /// <param name="version">The WKT version to validate.</param>
    internal static void ThrowIfUnknown(WktVersion version)
    {
        if (version is not WktVersion.Wkt1 and not WktVersion.Wkt22019)
        {
            ArgumentGuard.ThrowArgumentOutOfRange(nameof(version), version, "Unsupported WKT version.");
        }
    }

    /// <summary>
    /// Creates a standard not-supported exception for not-yet-implemented WKT versions.
    /// </summary>
    /// <param name="subject">The object or type that does not yet support the requested version.</param>
    /// <param name="version">The requested WKT version.</param>
    /// <returns>A <see cref="NotSupportedException"/> describing the unsupported serialization request.</returns>
    internal static NotSupportedException CreateNotSupportedException(string subject, WktVersion version)
        => new($"WKT version '{version}' is not implemented for {subject}.");

    /// <summary>
    /// Creates a WKT2 <c>ID</c> node when authority metadata is available.
    /// </summary>
    /// <param name="authority">The authority name.</param>
    /// <param name="authorityCode">The authority code.</param>
    /// <returns>A WKT2 <c>ID</c> node, or <see langword="null"/> when no authority metadata is available.</returns>
    internal static WktKeywordNode? CreateIdNode(string authority, long authorityCode)
    {
        if (string.IsNullOrWhiteSpace(authority) || authorityCode <= 0)
        {
            return null;
        }

        WktNode authorityCodeNode = authorityCode is >= int.MinValue and <= int.MaxValue
            ? new WktInteger((int)authorityCode)
            : new WktNumber(authorityCode);

        return new WktKeywordNode(
            "ID",
            new WktQuotedString(authority),
            authorityCodeNode);
    }
}
