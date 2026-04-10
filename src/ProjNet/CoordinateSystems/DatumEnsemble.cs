// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjNet.IO.Wkt;

/// <summary>
/// Describes a datum ensemble with member identifiers and ensemble accuracy metadata.
/// </summary>
public sealed class DatumEnsemble : IEquatable<DatumEnsemble>
{
    private const double EqualityTolerance = 1e-12;
    private readonly ReadOnlyCollection<DatumEnsembleMember> members;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatumEnsemble"/> class without an ellipsoid or identifier.
    /// </summary>
    /// <param name="name">Ensemble name.</param>
    /// <param name="members">Ensemble members.</param>
    /// <param name="accuracy">Ensemble accuracy.</param>
    public DatumEnsemble(string name, IReadOnlyList<DatumEnsembleMember> members, double accuracy)
        : this(name, members, accuracy, null, string.Empty, -1)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatumEnsemble"/> class.
    /// </summary>
    /// <param name="name">Ensemble name.</param>
    /// <param name="members">Ensemble members.</param>
    /// <param name="accuracy">Ensemble accuracy.</param>
    /// <param name="ellipsoid">Shared ellipsoid for geodetic ensembles, when available.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    public DatumEnsemble(
        string name,
        IReadOnlyList<DatumEnsembleMember> members,
        double accuracy,
        Ellipsoid? ellipsoid,
        string authority,
        long authorityCode)
    {
        this.Name = ArgumentGuard.ThrowIfNullOrWhiteSpace(name, nameof(name));
        members = ArgumentGuard.ThrowIfNull(members, nameof(members));
        if (members.Count == 0)
        {
            ArgumentGuard.ThrowArgument("Datum ensembles must contain at least one member.", nameof(members));
        }

        var memberArray = new DatumEnsembleMember[members.Count];
        for (int i = 0; i < members.Count; i++)
        {
            memberArray[i] = ArgumentGuard.ThrowIfNull(members[i], nameof(members));
        }

        ArgumentGuard.ThrowIfNotFinite(accuracy, nameof(accuracy), "Datum ensemble accuracy must be finite.");
        if (accuracy < 0d)
        {
            ArgumentGuard.ThrowArgumentOutOfRange(nameof(accuracy), accuracy, "Datum ensemble accuracy must be non-negative.");
        }

        this.members = Array.AsReadOnly(memberArray);
        this.Accuracy = accuracy;
        this.Ellipsoid = ellipsoid;
        this.Authority = authority ?? string.Empty;
        this.AuthorityCode = authorityCode;
    }

    /// <summary>
    /// Gets the ensemble name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the ensemble members in declaration order.
    /// </summary>
    public IReadOnlyList<DatumEnsembleMember> Members => this.members;

    /// <summary>
    /// Gets the stated ensemble accuracy.
    /// </summary>
    public double Accuracy { get; }

    /// <summary>
    /// Gets the shared ellipsoid for geodetic ensembles, when available.
    /// </summary>
    public Ellipsoid? Ellipsoid { get; }

    /// <summary>
    /// Gets the authority name.
    /// </summary>
    public string Authority { get; }

    /// <summary>
    /// Gets the authority-specific identification code.
    /// </summary>
    public long AuthorityCode { get; }

    /// <inheritdoc />
    public bool Equals(DatumEnsemble? other)
    {
        if (other is null
            || !string.Equals(this.Name, other.Name, StringComparison.Ordinal)
            || !string.Equals(this.Authority, other.Authority, StringComparison.Ordinal)
            || this.AuthorityCode != other.AuthorityCode
            || Math.Abs(this.Accuracy - other.Accuracy) > EqualityTolerance
            || (this.Ellipsoid is null) != (other.Ellipsoid is null))
        {
            return false;
        }

        if (this.Ellipsoid is not null
            && other.Ellipsoid is not null
            && !this.Ellipsoid.EqualParams(other.Ellipsoid))
        {
            return false;
        }

        if (this.members.Count != other.members.Count)
        {
            return false;
        }

        for (int i = 0; i < this.members.Count; i++)
        {
            if (!this.members[i].Equals(other.members[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => this.Equals(obj as DatumEnsemble);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(this.Name, StringComparer.Ordinal);
        hash.Add(this.Authority, StringComparer.Ordinal);
        hash.Add(this.AuthorityCode);
        hash.Add(this.Accuracy);
        if (this.Ellipsoid is not null)
        {
            hash.Add(this.Ellipsoid.SemiMajorAxis);
            hash.Add(this.Ellipsoid.SemiMinorAxis);
            hash.Add(this.Ellipsoid.InverseFlattening);
            hash.Add(this.Ellipsoid.IsIvfDefinitive);
            hash.Add(this.Ellipsoid.AxisUnit.MetersPerUnit);
        }

        for (int i = 0; i < this.members.Count; i++)
        {
            hash.Add(this.members[i]);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString() => this.Name;

    /// <summary>
    /// Converts this datum ensemble to a WKT2 <c>ENSEMBLE</c> node.
    /// </summary>
    /// <param name="version">The WKT dialect to emit.</param>
    /// <returns>A <see cref="WktNode"/> representing this datum ensemble.</returns>
    internal WktNode ToWktNode(WktVersion version)
    {
        WktVersionSupport.ThrowIfUnknown(version);
        if (version == WktVersion.Wkt1)
        {
            throw WktVersionSupport.CreateNotSupportedException(nameof(DatumEnsemble), version);
        }

        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
        };

        for (int i = 0; i < this.members.Count; i++)
        {
            children.Add(this.members[i].ToWktNode(version));
        }

        if (this.Ellipsoid is not null)
        {
            children.Add(this.Ellipsoid.ToWktNode(version));
        }

        children.Add(new WktKeywordNode("ENSEMBLEACCURACY", new WktNumber(this.Accuracy)));

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("ENSEMBLE", children);
    }
}
