// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using ProjNet.IO.Wkt;

/// <summary>
/// Identifies a single member of a datum ensemble.
/// </summary>
public sealed class DatumEnsembleMember : IEquatable<DatumEnsembleMember>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatumEnsembleMember"/> class without an identifier.
    /// </summary>
    /// <param name="name">Member name.</param>
    public DatumEnsembleMember(string name)
        : this(name, string.Empty, -1)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatumEnsembleMember"/> class.
    /// </summary>
    /// <param name="name">Member name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    public DatumEnsembleMember(string name, string authority, long authorityCode)
    {
        this.Name = ArgumentGuard.ThrowIfNullOrWhiteSpace(name, nameof(name));
        this.Authority = authority ?? string.Empty;
        this.AuthorityCode = authorityCode;
    }

    /// <summary>
    /// Gets the member name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the authority name.
    /// </summary>
    public string Authority { get; }

    /// <summary>
    /// Gets the authority-specific identification code.
    /// </summary>
    public long AuthorityCode { get; }

    /// <inheritdoc />
    public bool Equals(DatumEnsembleMember? other)
    {
        return other is not null
            && string.Equals(this.Name, other.Name, StringComparison.Ordinal)
            && string.Equals(this.Authority, other.Authority, StringComparison.Ordinal)
            && this.AuthorityCode == other.AuthorityCode;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => this.Equals(obj as DatumEnsembleMember);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(this.Name, this.Authority, this.AuthorityCode);

    /// <inheritdoc />
    public override string ToString() => this.Name;

    /// <summary>
    /// Converts this ensemble member to a WKT2 <c>MEMBER</c> node.
    /// </summary>
    /// <param name="version">The WKT dialect to emit.</param>
    /// <returns>A <see cref="WktNode"/> representing this ensemble member.</returns>
    internal WktNode ToWktNode(WktVersion version)
    {
        WktVersionSupport.ThrowIfUnknown(version);
        if (version == WktVersion.Wkt1)
        {
            throw WktVersionSupport.CreateNotSupportedException(nameof(DatumEnsembleMember), version);
        }

        var memberNode = new WktKeywordNode("MEMBER", new WktQuotedString(this.Name));
        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        return idNode is null
            ? memberNode
            : new WktKeywordNode("MEMBER", new WktQuotedString(this.Name), idNode);
    }
}
