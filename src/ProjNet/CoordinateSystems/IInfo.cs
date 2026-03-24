// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

/// <summary>
/// The ISpatialReferenceInfo interface defines the standard
/// information stored with spatial reference objects. This
/// interface is reused for many of the spatial reference
/// objects in the system.
/// </summary>
public interface IInfo
{
    /// <summary>
    /// Gets the name of the object.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the authority name for this object, e.g., "<c>EPSG</c>",
    /// is this is a standard object with an authority specific
    /// identity code. Returns <c>CUSTOM</c> if this is a custom object.
    /// </summary>
    string Authority { get; }

    /// <summary>
    /// Gets the authority specific identification code of the object.
    /// </summary>
    long AuthorityCode { get; }

    /// <summary>
    /// Gets the alias of the object.
    /// </summary>
    string Alias { get; }

    /// <summary>
    /// Gets the abbreviation of the object.
    /// </summary>
    string Abbreviation { get; }

    /// <summary>
    /// Gets the provider-supplied remarks for the object.
    /// </summary>
    string Remarks { get; }

    /// <summary>
    /// Gets the Well-known text for this spatial reference object
    /// as defined in the simple features specification.
    /// </summary>
    string WKT { get; }

    /// <summary>
    /// Gets an XML representation of this object.
    /// </summary>
    string XML { get; }

    /// <summary>
    /// Checks whether the values of this instance is equal to the values of another instance.
    /// Only parameters used for coordinate system are used for comparison.
    /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
    /// </summary>
    /// <param name="obj">The obj parameter.</param>
    /// <returns>True if equal.</returns>
    bool EqualParams(object obj);
}
