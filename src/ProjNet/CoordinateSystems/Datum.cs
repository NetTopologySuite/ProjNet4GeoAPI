// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;

/// <summary>
/// A set of quantities from which other quantities are calculated.
/// </summary>
/// <remarks>
/// For the OGC abstract model, it can be defined as a set of real points on the earth
/// that have coordinates. EG. A datum can be thought of as a set of parameters
/// defining completely the origin and orientation of a coordinate system with respect
/// to the earth. A textual description and/or a set of parameters describing the
/// relationship of a coordinate system to some predefined physical locations (such
/// as center of mass) and physical directions (such as axis of spin). The definition
/// of the datum may also include the temporal behavior (such as the rate of change of
/// the orientation of the coordinate axes).
/// </remarks>
[Serializable]
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
    internal Datum(
        DatumType type,
        string name,
        string authority,
        long code,
        string alias,
        string remarks,
        string abbreviation)
        : base(name, authority, code, alias, abbreviation, remarks)
    {
        this.DatumType = type;
    }

    /// <summary>
    /// Gets or sets the type of the datum as an enumerated code.
    /// </summary>
    public DatumType DatumType { get; set; }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        if (obj is not Datum datum)
        {
            return false;
        }

        return datum.DatumType == this.DatumType;
    }
}
