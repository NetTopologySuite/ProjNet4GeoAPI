// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNET.Tests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using ProjNet.CoordinateSystems;

/// <summary>
/// Represents the documented type.
/// </summary>
internal sealed class SRIDReader
{
    private static readonly Lazy<CoordinateSystemFactory> CoordinateSystemFactory =
        new Lazy<CoordinateSystemFactory>(() => new CoordinateSystemFactory());

    /// <summary>
    /// Gets a coordinate system from the SRID.csv file.
    /// </summary>
    /// <param name="id">EPSG ID.</param>
    /// <param name="file">(optional) path to CSV File with WKT definitions.</param>
    /// <returns>Coordinate system, or <value>null</value> if no entry with <paramref name="id"/> was not found.</returns>
    public static CoordinateSystem GetCSbyID(int id, string file = null)
    {
        // ICoordinateSystemFactory factory = new CoordinateSystemFactory();
        foreach (var wkt in GetSrids(file))
        {
            if (wkt.WktId == id)
            {
                return CoordinateSystemFactory.Value.CreateFromWkt(wkt.Wkt);
            }
        }

        return null;
    }

    /// <summary>
    /// Enumerates all SRID's in the SRID.csv file.
    /// </summary>
    /// <param name="filename">The filename value.</param>
    /// <returns>Enumerator.</returns>
    public static IEnumerable<WktString> GetSrids(string filename = null)
    {
        var stream = string.IsNullOrWhiteSpace(filename)
            ? Assembly.GetExecutingAssembly().GetManifestResourceStream("ProjNET.Tests.SRID.csv")
            : File.OpenRead(filename);

        using (var sr = new StreamReader(stream, Encoding.UTF8))
        {
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                int split = line.IndexOf(';', StringComparison.Ordinal);
                if (split <= -1)
                {
                    continue;
                }

                var wkt = new WktString
                {
                    WktId = int.Parse(line.AsSpan(0, split), CultureInfo.InvariantCulture),
                    Wkt = line.Substring(split + 1),
                };
                yield return wkt;
            }
        }
    }

    /// <summary>
    /// Represents the documented type.
    /// </summary>
    public struct WktString
    {
        /// <summary>
        /// Well-known ID.
        /// </summary>
        public int WktId;

        /// <summary>
        /// Well-known Text.
        /// </summary>
        public string Wkt;
    }
}
