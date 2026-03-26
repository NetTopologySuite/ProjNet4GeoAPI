// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable

namespace ProjNet.CoordinateSystems;

using System;
using System.Globalization;
using System.Text;

/// <summary>
/// A meridian used to take longitude measurements from.
/// </summary>
[Serializable]
public class PrimeMeridian : Info
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PrimeMeridian"/> class.
    /// </summary>
    /// <param name="longitude">Longitude of prime meridian.</param>
    /// <param name="angularUnit">Angular unit.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    internal PrimeMeridian(double longitude, AngularUnit angularUnit, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
        : base(name, authority, authorityCode, alias, abbreviation, remarks)
    {
        this.Longitude = longitude;
        this.AngularUnit = angularUnit;
    }

    /// <summary>
    /// Gets the Greenwich prime meridian (0° longitude).
    /// </summary>
    public static PrimeMeridian Greenwich => new(0.0, CoordinateSystems.AngularUnit.Degrees, "Greenwich", "EPSG", 8901, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets the Lisbon prime meridian.
    /// </summary>
    public static PrimeMeridian Lisbon => new(-9.0754862, CoordinateSystems.AngularUnit.Degrees, "Lisbon", "EPSG", 8902, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets the Paris prime meridian.
    /// </summary>
    /// <remarks>
    /// Value adopted by IGN (Paris) in 1936. Equivalent to 2 deg 20 min 14.025 sec.
    /// Preferred by EPSG over the earlier value of 2 deg 20 min 13.95 sec (2.596898 grads) used by RGS London.
    /// </remarks>
    public static PrimeMeridian Paris => new(2.5969213, CoordinateSystems.AngularUnit.Degrees, "Paris", "EPSG", 8903, string.Empty, string.Empty, "Value adopted by IGN (Paris) in 1936. Equivalent to 2 deg 20min 14.025sec. Preferred by EPSG to earlier value of 2deg 20min 13.95sec (2.596898 grads) used by RGS London.");

    /// <summary>
    /// Gets the Bogota prime meridian.
    /// </summary>
    public static PrimeMeridian Bogota => new(-74.04513, CoordinateSystems.AngularUnit.Degrees, "Bogota", "EPSG", 8904, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets the Madrid prime meridian.
    /// </summary>
    public static PrimeMeridian Madrid => new(-3.411658, CoordinateSystems.AngularUnit.Degrees, "Madrid", "EPSG", 8905, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets the Rome prime meridian.
    /// </summary>
    public static PrimeMeridian Rome => new(12.27084, CoordinateSystems.AngularUnit.Degrees, "Rome", "EPSG", 8906, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets the Bern prime meridian.
    /// </summary>
    /// <remarks>1895 value. A newer value of 7 deg 26 min 22.335 sec E was determined in 1938.</remarks>
    public static PrimeMeridian Bern => new(7.26225, CoordinateSystems.AngularUnit.Degrees, "Bern", "EPSG", 8907, string.Empty, string.Empty, "1895 value. Newer value of 7 deg 26 min 22.335 sec E determined in 1938.");

    /// <summary>
    /// Gets the Jakarta prime meridian.
    /// </summary>
    public static PrimeMeridian Jakarta => new(106.482779, CoordinateSystems.AngularUnit.Degrees, "Jakarta", "EPSG", 8908, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets the Ferro prime meridian.
    /// </summary>
    /// <remarks>Used in Austria and former Czechoslovakia.</remarks>
    public static PrimeMeridian Ferro => new(-17.66666666666667, CoordinateSystems.AngularUnit.Degrees, "Ferro", "EPSG", 8909, string.Empty, string.Empty, "Used in Austria and former Czechoslovakia.");

    /// <summary>
    /// Gets the Brussels prime meridian.
    /// </summary>
    public static PrimeMeridian Brussels => new(4.220471, CoordinateSystems.AngularUnit.Degrees, "Brussels", "EPSG", 8910, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets the Stockholm prime meridian.
    /// </summary>
    public static PrimeMeridian Stockholm => new(18.03298, CoordinateSystems.AngularUnit.Degrees, "Stockholm", "EPSG", 8911, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets the Athens prime meridian.
    /// </summary>
    /// <remarks>Used in Greece for older mapping based on the Hatt projection.</remarks>
    public static PrimeMeridian Athens => new(23.4258815, CoordinateSystems.AngularUnit.Degrees, "Athens", "EPSG", 8912, string.Empty, string.Empty, "Used in Greece for older mapping based on Hatt projection.");

    /// <summary>
    /// Gets the Oslo prime meridian.
    /// </summary>
    /// <remarks>Formerly known as Kristiania or Christiania.</remarks>
    public static PrimeMeridian Oslo => new(10.43225, CoordinateSystems.AngularUnit.Degrees, "Oslo", "EPSG", 8913, string.Empty, string.Empty, "Formerly known as Kristiania or Christiania.");

    /// <summary>
    /// Gets or sets the longitude of the prime meridian (relative to the Greenwich prime meridian).
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Gets or sets the angular unit used to express the longitude of this prime meridian.
    /// </summary>
    public AngularUnit AngularUnit { get; set; }

    /// <summary>
    /// Gets the Well-known text for this object
    /// as defined in the simple features specification.
    /// </summary>
    public override string WKT
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat, "PRIMEM[\"{0}\", {1}", this.Name, this.Longitude);
            if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, ", AUTHORITY[\"{0}\", \"{1}\"]", this.Authority, this.AuthorityCode);
            }

            sb.Append(']');
            return sb.ToString();
        }
    }

    /// <summary>
    /// Gets an XML representation of this object.
    /// </summary>
    public override string XML
    {
        get
        {
            return string.Format(
                CultureInfo.InvariantCulture.NumberFormat,
                "<CS_PrimeMeridian Longitude=\"{0}\" >{1}{2}</CS_PrimeMeridian>",
                this.Longitude,
                this.InfoXml,
                this.AngularUnit.XML);
        }
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        if (obj is not PrimeMeridian prime)
        {
            return false;
        }

        return prime.AngularUnit.EqualParams(this.AngularUnit) && prime.Longitude == this.Longitude;
    }
}
