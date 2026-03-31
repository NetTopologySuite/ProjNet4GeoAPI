// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ProjNet.IO.Wkt;

/// <summary>
/// Horizontal datum defining the standard datum information.
/// </summary>
public class HorizontalDatum : Datum
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HorizontalDatum"/> class.
    /// </summary>
    /// <param name="ellipsoid">Ellipsoid.</param>
    /// <param name="toWgs84">Parameters for a Bursa Wolf transformation into WGS84.</param>
    /// <param name="type">Datum type.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="code">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    internal HorizontalDatum(
        Ellipsoid ellipsoid,
        Wgs84ConversionInfo? toWgs84,
        DatumType type,
        string name,
        string authority,
        long code,
        string alias,
        string remarks,
        string abbreviation)
        : base(type, name, authority, code, alias, remarks, abbreviation)
    {
        this.Ellipsoid = ellipsoid;
        this.Wgs84Parameters = toWgs84;
    }

    /// <summary>
    /// Gets the World Geodetic System 1984 (WGS 84) horizontal datum.
    /// </summary>
    /// <remarks>
    /// <para>No distinction is made between the original WGS 84 frame, WGS 84 (G730), WGS 84 (G873), and WGS 84 (G1150).
    /// Since 1997, WGS 84 has been maintained within 10 cm of the current ITRF.</para>
    /// <para>Area of use: World.</para>
    /// <para>Origin description: Defined through a consistent set of station coordinates. These have changed with time: by 0.7m
    /// on 29/6/1994 [WGS 84 (G730)], a further 0.2m on 29/1/1997 [WGS 84 (G873)] and a further 0.06m on
    /// 20/1/2002 [WGS 84 (G1150)].</para>
    /// </remarks>
    public static HorizontalDatum WGS84
    {
        get
        {
            return new HorizontalDatum(
                CoordinateSystems.Ellipsoid.WGS84,
                null,
                DatumType.HD_Geocentric,
                "World Geodetic System 1984",
                "EPSG",
                6326,
                string.Empty,
                "EPSG's WGS 84 datum has been the then current realisation. No distinction is made between the original WGS 84 frame, WGS 84 (G730), WGS 84 (G873) and WGS 84 (G1150). Since 1997, WGS 84 has been maintained within 10cm of the then current ITRF.",
                string.Empty);
        }
    }

    /// <summary>
    /// Gets the World Geodetic System 1972 (WGS 72) horizontal datum.
    /// </summary>
    /// <remarks>
    /// <para>Used by GPS before 1987. For Transit satellite positioning see also WGS 72BE. Datum code 6323 reserved for southern hemisphere ProjCS's.</para>
    /// <para>Area of use: World.</para>
    /// <para>Origin description: Developed from a worldwide distribution of terrestrial and
    /// geodetic satellite observations and defined through a set of station coordinates.</para>
    /// </remarks>
    public static HorizontalDatum WGS72
    {
        get
        {
            var datum =
                new HorizontalDatum(
                    CoordinateSystems.Ellipsoid.WGS72,
                    null,
                    DatumType.HD_Geocentric,
                    "World Geodetic System 1972",
                    "EPSG",
                    6322,
                    string.Empty,
                    "Used by GPS before 1987. For Transit satellite positioning see also WGS 72BE. Datum code 6323 reserved for southern hemisphere ProjCS's.",
                    string.Empty);
            datum.Wgs84Parameters = new Wgs84ConversionInfo(0, 0, 4.5, 0, 0, 0.554, 0.219);
            return datum;
        }
    }

    /// <summary>
    /// Gets the European Terrestrial Reference System 1989 (ETRS89) horizontal datum.
    /// </summary>
    /// <remarks>
    /// <para>Area of use:
    /// Europe: Albania; Andorra; Austria; Belgium; Bosnia and Herzegovina; Bulgaria; Croatia;
    /// Cyprus; Czech Republic; Denmark; Estonia; Finland; Faroe Islands; France; Germany; Greece;
    /// Hungary; Ireland; Italy; Latvia; Liechtenstein; Lithuania; Luxembourg; Malta; Netherlands;
    /// Norway; Poland; Portugal; Romania; San Marino; Serbia and Montenegro; Slovakia; Slovenia;
    /// Spain; Svalbard; Sweden; Switzerland; United Kingdom (UK) including Channel Islands and
    /// Isle of Man; Vatican City State.</para>
    /// <para>Origin description: Fixed to the stable part of the Eurasian continental
    /// plate and consistent with ITRS at the epoch 1989.0.</para>
    /// </remarks>
    public static HorizontalDatum ETRF89
    {
        get
        {
            var datum = new HorizontalDatum(
                CoordinateSystems.Ellipsoid.GRS80,
                null,
                DatumType.HD_Geocentric,
                "European Terrestrial Reference System 1989",
                "EPSG",
                6258,
                "ETRF89",
                "The distinction in usage between ETRF89 and ETRS89 is confused: although in principle conceptually different in practice both are used for the realisation.",
                string.Empty);
            datum.Wgs84Parameters = new Wgs84ConversionInfo();
            return datum;
        }
    }

    /// <summary>
    /// Gets the European Datum 1950 (ED50) horizontal datum.
    /// </summary>
    /// <remarks>
    /// <para>Area of use:
    /// Europe - west - Denmark; Faroe Islands; France offshore; Israel offshore; Italy including San
    /// Marino and Vatican City State; Ireland offshore; Netherlands offshore; Germany; Greece (offshore);
    /// North Sea; Norway; Spain; Svalbard; Turkey; United Kingdom UKCS offshore. Egypt - Western Desert.
    /// </para>
    /// <para>Origin description: Fundamental point: Potsdam (Helmert Tower).
    /// Latitude: 52 deg 22 min 51.4456 sec N; Longitude: 13 deg  3 min 58.9283 sec E (of Greenwich).</para>
    /// </remarks>
    public static HorizontalDatum ED50
    {
        get
        {
            return new HorizontalDatum(
                CoordinateSystems.Ellipsoid.International1924,
                new Wgs84ConversionInfo(-87, -98, -121, 0, 0, 0, 0),
                DatumType.HD_Geocentric,
                "European Datum 1950",
                "EPSG",
                6230,
                "ED50",
                string.Empty,
                string.Empty);
        }
    }

    /// <summary>
    /// Gets or sets the ellipsoid of the datum.
    /// </summary>
    public Ellipsoid Ellipsoid { get; set; }

    /// <summary>
    /// Gets or sets preferred parameters for a Bursa Wolf transformation into WGS84.
    /// </summary>
    public Wgs84ConversionInfo? Wgs84Parameters { get; set; }

    /// <summary>
    /// Gets the Well-known text for this object
    /// as defined in the simple features specification.
    /// </summary>
    public override string WKT
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "DATUM[\"{0}\", {1}", this.Name, this.Ellipsoid.WKT);
            if (this.Wgs84Parameters is not null)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, ", {0}", this.Wgs84Parameters.WKT);
            }

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
            return FormattableString.Invariant($"<CS_HorizontalDatum DatumType=\"{(int)this.DatumType}\">{this.InfoXml}{this.Ellipsoid.XML}{this.Wgs84Parameters?.XML ?? string.Empty}</CS_HorizontalDatum>");
        }
    }

    /// <summary>
    /// Converts this horizontal datum to a WKT syntax tree node.
    /// </summary>
    /// <returns>A <see cref="WktNode"/> representing this horizontal datum.</returns>
    public WktNode ToWktNode()
    {
        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            this.Ellipsoid.ToWktNode(),
        };

        if (this.Wgs84Parameters is not null)
        {
            children.Add(this.Wgs84Parameters.ToWktNode());
        }

        if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
        {
            children.Add(new WktKeywordNode(
                "AUTHORITY",
                new WktQuotedString(this.Authority),
                new WktQuotedString(this.AuthorityCode.ToString(CultureInfo.InvariantCulture))));
        }

        return new WktKeywordNode("DATUM", children);
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        if (obj is not HorizontalDatum datum)
        {
            return false;
        }

        if ((datum.Wgs84Parameters is null) != (this.Wgs84Parameters is null))
        {
            return false;
        }

        if (datum.Wgs84Parameters is not null
            && this.Wgs84Parameters is not null
            && !datum.Wgs84Parameters.Equals(this.Wgs84Parameters))
        {
            return false;
        }

        bool ellipsoidMatches =
            (this.Ellipsoid is null && datum.Ellipsoid is null)
            || (this.Ellipsoid is not null
                && datum.Ellipsoid is not null
                && datum.Ellipsoid.EqualParams(this.Ellipsoid));

        return ellipsoidMatches && this.DatumType == datum.DatumType;
    }
}
