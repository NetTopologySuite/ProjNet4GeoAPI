// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;

using System.Globalization;
using System.Xml.Linq;

/// <summary>
/// The Info object defines the standard information
/// stored with spatial reference objects.
/// </summary>
public abstract class Info : IInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Info"/> class.
    /// </summary>
    /// <remarks>
    /// <para>The metadata items �Abbreviation�, �Alias�, �Authority�, �AuthorityCode�, �Name� and �Remarks�
    /// were specified in the Simple Features interfaces, so they have been kept here.</para>
    /// <para>This specification does not dictate what the contents of these items
    /// should be. However, the following guidelines are suggested:</para>
    /// <para>When <c>ICoordinateSystemAuthorityFactory</c> is used to create an object, the �Authority�
    /// and 'AuthorityCode' values should be set to the authority name of the factory object, and the authority
    /// code supplied by the client, respectively. The other values may or may not be set. (If the authority is
    /// EPSG, the implementer may consider using the corresponding metadata values in the EPSG tables.)</para>
    /// <para>When <see cref="CoordinateSystemFactory"/> creates an object, the 'Name' should be set to the value
    /// supplied by the client. All of the other metadata items should be left empty.</para>
    /// </remarks>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="code">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    internal Info(
                    string name,
                    string authority,
                    long code,
                    string alias,
                    string abbreviation,
                    string remarks)
    {
        this.Name = name ?? string.Empty;
        this.Authority = authority ?? string.Empty;
        this.AuthorityCode = code;
        this.Alias = alias ?? string.Empty;
        this.Abbreviation = abbreviation ?? string.Empty;
        this.Remarks = remarks ?? string.Empty;
    }

    /// <summary>
    /// Gets the name of the object.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the authority name for this object, e.g., "EPSG",
    /// is this is a standard object with an authority specific
    /// identity code. Returns "CUSTOM" if this is a custom object.
    /// </summary>
    public string Authority { get; }

    /// <summary>
    /// Gets the authority specific identification code of the object.
    /// </summary>
    public long AuthorityCode { get; }

    /// <summary>
    /// Gets the alias of the object.
    /// </summary>
    public string Alias { get; }

    /// <summary>
    /// Gets the abbreviation of the object.
    /// </summary>
    public string Abbreviation { get; }

    /// <summary>
    /// Gets the provider-supplied remarks for the object.
    /// </summary>
    public string Remarks { get; }

    /// <summary>
    /// Gets the Well-known text for this object
    /// as defined in the simple features specification.
    /// </summary>
    public abstract string WKT { get; }

    /// <summary>
    /// Gets an XML representation of this object.
    /// </summary>
    public abstract string XML { get; }

    /// <summary>
    /// Gets an XML element of the info object.
    /// </summary>
    internal XElement InfoXmlElement
    {
        get
        {
            var element = new XElement("CS_Info");
            if (this.AuthorityCode > 0)
            {
                element.Add(new XAttribute("AuthorityCode", this.AuthorityCode.ToString(CultureInfo.InvariantCulture)));
            }

            if (!string.IsNullOrWhiteSpace(this.Abbreviation))
            {
                element.Add(new XAttribute("Abbreviation", this.Abbreviation));
            }

            if (!string.IsNullOrWhiteSpace(this.Authority))
            {
                element.Add(new XAttribute("Authority", this.Authority));
            }

            if (!string.IsNullOrWhiteSpace(this.Name))
            {
                element.Add(new XAttribute("Name", this.Name));
            }

            return element;
        }
    }

    /// <summary>
    /// Creates a copy of this object with updated authority metadata.
    /// </summary>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="code">Replacement authority-specific identification code.</param>
    /// <returns>A new instance of the same runtime type with updated authority metadata.</returns>
    public Info WithAuthority(string authority, long code)
    {
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return this.CloneWithAuthorityCore(authority, code);
    }

    /// <summary>
    /// Creates a copy of this object with an updated name.
    /// </summary>
    /// <param name="name">Replacement name.</param>
    /// <returns>A new instance of the same runtime type with the updated name.</returns>
    public Info WithName(string name)
    {
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return this.CloneWithNameCore(name);
    }

    /// <summary>
    /// Returns the Well-known text for this object
    /// as defined in the simple features specification.
    /// </summary>
    /// <returns>The Well-known text representation of this object.</returns>
    public override string ToString() => this.WKT;

    /// <summary>
    /// Checks whether the coordinate system parameter values of this instance are equal to those of another instance.
    /// Name, abbreviation, authority, alias, and remarks are excluded from the comparison.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns><see langword="true"/> if all coordinate system parameters are equal; otherwise, <see langword="false"/>.</returns>
    public abstract bool EqualParams(object obj);

    /// <summary>
    /// Creates a clone of this instance with updated authority metadata.
    /// </summary>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="code">Replacement authority-specific identification code.</param>
    /// <returns>A cloned instance of the same runtime type.</returns>
    private protected virtual Info CloneWithAuthorityCore(string authority, long code)
    {
        throw new NotSupportedException($"WithAuthority is not supported for info type '{this.GetType().FullName}'.");
    }

    /// <summary>
    /// Creates a clone of this instance with an updated name.
    /// </summary>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned instance of the same runtime type.</returns>
    private protected virtual Info CloneWithNameCore(string name)
    {
        throw new NotSupportedException($"WithName is not supported for info type '{this.GetType().FullName}'.");
    }
}
