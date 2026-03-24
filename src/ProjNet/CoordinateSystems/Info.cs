// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Globalization;
using System.Text;

/// <summary>
/// The Info object defines the standard information
/// stored with spatial reference objects.
/// </summary>
[Serializable]
public abstract class Info : IInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Info"/> class.
    /// A base interface for metadata applicable to coordinate system objects.
    /// </summary>
    /// <remarks>
    /// <para>The metadata items �Abbreviation�, �Alias�, �Authority�, �AuthorityCode�, �Name� and �Remarks�
    /// were specified in the Simple Features interfaces, so they have been kept here.</para>
    /// <para>This specification does not dictate what the contents of these items
    /// should be. However, the following guidelines are suggested:</para>
    /// <para>When <see href="ICoordinateSystemAuthorityFactory"/> is used to create an object, the �Authority�
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
        this.Name = name;
        this.Authority = authority;
        this.AuthorityCode = code;
        this.Alias = alias;
        this.Abbreviation = abbreviation;
        this.Remarks = remarks;
    }

    /// <summary>
    /// Gets or sets the name of the object.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the authority name for this object, e.g., "EPSG",
    /// is this is a standard object with an authority specific
    /// identity code. Returns "CUSTOM" if this is a custom object.
    /// </summary>
    public string Authority { get; set; }

    /// <summary>
    /// Gets or sets the authority specific identification code of the object.
    /// </summary>
    public long AuthorityCode { get; set; }

    /// <summary>
    /// Gets or sets the alias of the object.
    /// </summary>
    public string Alias { get; set; }

    /// <summary>
    /// Gets or sets the abbreviation of the object.
    /// </summary>
    public string Abbreviation { get; set; }

    /// <summary>
    /// Gets or sets the provider-supplied remarks for the object.
    /// </summary>
    public string Remarks { get; set; }

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
    /// Gets an XML string of the info object.
    /// </summary>
    internal string InfoXml
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append("<CS_Info");
            if (this.AuthorityCode > 0)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, " AuthorityCode=\"{0}\"", this.AuthorityCode);
            }

            if (!string.IsNullOrWhiteSpace(this.Abbreviation))
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, " Abbreviation=\"{0}\"", this.Abbreviation);
            }

            if (!string.IsNullOrWhiteSpace(this.Authority))
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, " Authority=\"{0}\"", this.Authority);
            }

            if (!string.IsNullOrWhiteSpace(this.Name))
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, " Name=\"{0}\"", this.Name);
            }

            sb.Append("/>");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Returns the Well-known text for this object
    /// as defined in the simple features specification.
    /// </summary>
    /// <returns>The computed value.</returns>
    public override string ToString() => this.WKT;

    /// <summary>
    /// Checks whether the values of this instance is equal to the values of another instance.
    /// Only parameters used for coordinate system are used for comparison.
    /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
    /// </summary>
    /// <param name="obj">The obj parameter.</param>
    /// <returns>True if equal.</returns>
    public abstract bool EqualParams(object obj);
}
