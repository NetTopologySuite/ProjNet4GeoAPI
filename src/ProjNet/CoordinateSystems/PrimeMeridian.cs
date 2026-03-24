// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
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
    /// Initializes a new instance of a prime meridian.
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
    /// Gets greenwich prime meridian.
    /// </summary>
    public static PrimeMeridian Greenwich => new(0.0, CoordinateSystems.AngularUnit.Degrees, "Greenwich", "EPSG", 8901, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets lisbon prime meridian.
    /// </summary>
    public static PrimeMeridian Lisbon => new(-9.0754862, CoordinateSystems.AngularUnit.Degrees, "Lisbon", "EPSG", 8902, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets paris prime meridian.
    /// Value adopted by IGN (Paris) in 1936. Equivalent to 2 deg 20min 14.025sec. Preferred by EPSG to earlier value of 2deg 20min 13.95sec (2.596898 grads) used by RGS London.
    /// </summary>
    public static PrimeMeridian Paris => new(2.5969213, CoordinateSystems.AngularUnit.Degrees, "Paris", "EPSG", 8903, string.Empty, string.Empty, "Value adopted by IGN (Paris) in 1936. Equivalent to 2 deg 20min 14.025sec. Preferred by EPSG to earlier value of 2deg 20min 13.95sec (2.596898 grads) used by RGS London.");

    /// <summary>
    /// Gets bogota prime meridian.
    /// </summary>
    public static PrimeMeridian Bogota => new(-74.04513, CoordinateSystems.AngularUnit.Degrees, "Bogota", "EPSG", 8904, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets madrid prime meridian.
    /// </summary>
    public static PrimeMeridian Madrid => new(-3.411658, CoordinateSystems.AngularUnit.Degrees, "Madrid", "EPSG", 8905, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets rome prime meridian.
    /// </summary>
    public static PrimeMeridian Rome => new(12.27084, CoordinateSystems.AngularUnit.Degrees, "Rome", "EPSG", 8906, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets bern prime meridian.
    /// 1895 value. Newer value of 7 deg 26 min 22.335 sec E determined in 1938.
    /// </summary>
    public static PrimeMeridian Bern => new(7.26225, CoordinateSystems.AngularUnit.Degrees, "Bern", "EPSG", 8907, string.Empty, string.Empty, "1895 value. Newer value of 7 deg 26 min 22.335 sec E determined in 1938.");

    /// <summary>
    /// Gets jakarta prime meridian.
    /// </summary>
    public static PrimeMeridian Jakarta => new(106.482779, CoordinateSystems.AngularUnit.Degrees, "Jakarta", "EPSG", 8908, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets ferro prime meridian.
    /// Used in Austria and former Czechoslovakia.
    /// </summary>
    public static PrimeMeridian Ferro => new(-17.66666666666667, CoordinateSystems.AngularUnit.Degrees, "Ferro", "EPSG", 8909, string.Empty, string.Empty, "Used in Austria and former Czechoslovakia.");

    /// <summary>
    /// Gets brussels prime meridian.
    /// </summary>
    public static PrimeMeridian Brussels => new(4.220471, CoordinateSystems.AngularUnit.Degrees, "Brussels", "EPSG", 8910, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets stockholm prime meridian.
    /// </summary>
    public static PrimeMeridian Stockholm => new(18.03298, CoordinateSystems.AngularUnit.Degrees, "Stockholm", "EPSG", 8911, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Gets athens prime meridian.
    /// Used in Greece for older mapping based on Hatt projection.
    /// </summary>
    public static PrimeMeridian Athens => new(23.4258815, CoordinateSystems.AngularUnit.Degrees, "Athens", "EPSG", 8912, string.Empty, string.Empty, "Used in Greece for older mapping based on Hatt projection.");

    /// <summary>
    /// Gets oslo prime meridian.
    /// Formerly known as Kristiania or Christiania.
    /// </summary>
    public static PrimeMeridian Oslo => new(10.43225, CoordinateSystems.AngularUnit.Degrees, "Oslo", "EPSG", 8913, string.Empty, string.Empty, "Formerly known as Kristiania or Christiania.");

    /// <summary>
    /// Gets or sets the longitude of the prime meridian (relative to the Greenwich prime meridian).
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Gets or sets the AngularUnits.
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

    /// <summary>
    /// Checks whether the values of this instance is equal to the values of another instance.
    /// Only parameters used for coordinate system are used for comparison.
    /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
    /// </summary>
    /// <param name="obj">The obj parameter.</param>
    /// <returns>True if equal.</returns>
    public override bool EqualParams(object obj)
    {
        if (!(obj is PrimeMeridian))
        {
            return false;
        }

        var prime = obj as PrimeMeridian;
        return prime.AngularUnit.EqualParams(this.AngularUnit) && prime.Longitude == this.Longitude;
    }
}
