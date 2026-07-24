// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;

/// <summary>
/// A set of quantities from which other quantities are calculated.
/// </summary>
/// <remarks>
/// In the OGC abstract model, a datum can be described as a set of real points on the
/// earth that have coordinates. More practically, it is the set of parameters that
/// defines the origin and orientation of a coordinate system with respect to the earth.
/// The definition may include text and/or numeric parameters tied to physical locations
/// (such as the center of mass) and physical directions (such as the axis of spin).
/// It may also include temporal behavior, such as the rate of change of the coordinate
/// axes orientation.
/// </remarks>
public abstract class Datum : Info
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Datum"/> class.
    /// </summary>
    /// <param name="type">Datum type.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="code">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    /// <param name="ensemble">Retained datum-ensemble metadata.</param>
    internal Datum(
        DatumType type,
        string name,
        string authority,
        long code,
        string alias,
        string remarks,
        string abbreviation,
        DatumEnsemble? ensemble = null)
        : base(name, authority, code, alias, abbreviation, remarks)
    {
        this.DatumType = type;
        this.Ensemble = ensemble;
    }

    /// <summary>
    /// Gets the type of the datum as an enumerated code.
    /// </summary>
    public DatumType DatumType { get; }

    /// <summary>
    /// Gets retained datum-ensemble metadata when this datum represents an ensemble-backed CRS definition.
    /// </summary>
    public DatumEnsemble? Ensemble { get; }

    /// <summary>
    /// Creates a copy of this datum with updated retained datum-ensemble metadata.
    /// </summary>
    /// <param name="ensemble">Replacement ensemble metadata, or <see langword="null"/> to clear it.</param>
    /// <returns>A new datum instance with updated ensemble metadata.</returns>
    public Datum WithEnsemble(DatumEnsemble? ensemble)
    {
        return InfoAuthorityCloneHelper.CloneWithEnsemble(this, ensemble);
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        return obj is Datum datum && datum.DatumType == this.DatumType;
    }

    /// <inheritdoc />
    private protected override Info CloneWithAuthorityCore(string authority, long code)
    {
        return this switch
        {
            HorizontalDatum horizontalDatum => horizontalDatum.WithAuthority(authority, code),
            VerticalDatum verticalDatum => verticalDatum.WithAuthority(authority, code),
            EngineeringDatum engineeringDatum => engineeringDatum.WithAuthority(authority, code),
            ParametricDatum parametricDatum => parametricDatum.WithAuthority(authority, code),
            TemporalDatum temporalDatum => temporalDatum.WithAuthority(authority, code),
            _ => throw new NotSupportedException($"WithAuthority is not supported for datum type '{this.GetType().FullName}'."),
        };
    }

    /// <inheritdoc />
    private protected override Info CloneWithNameCore(string name)
    {
        return this switch
        {
            HorizontalDatum horizontalDatum => horizontalDatum.WithName(name),
            VerticalDatum verticalDatum => verticalDatum.WithName(name),
            EngineeringDatum engineeringDatum => engineeringDatum.WithName(name),
            ParametricDatum parametricDatum => parametricDatum.WithName(name),
            TemporalDatum temporalDatum => temporalDatum.WithName(name),
            _ => throw new NotSupportedException($"WithName is not supported for datum type '{this.GetType().FullName}'."),
        };
    }
}
