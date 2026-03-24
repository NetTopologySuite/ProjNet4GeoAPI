// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;

/// <summary>
/// A 2D coordinate system suitable for positions on the Earth's surface.
/// </summary>
[Serializable]
public abstract class HorizontalCoordinateSystem : CoordinateSystem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HorizontalCoordinateSystem"/> class.
    /// Creates an instance of HorizontalCoordinateSystem.
    /// </summary>
    /// <param name="datum">Horizontal datum.</param>
    /// <param name="axisInfo">Axis information.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="code">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    internal HorizontalCoordinateSystem(
        HorizontalDatum datum,
        List<AxisInfo> axisInfo,
        string name,
        string authority,
        long code,
        string alias,
        string remarks,
        string abbreviation)
        : base(name, authority, code, alias, abbreviation, remarks)
    {
        this.HorizontalDatum = datum;
        if (axisInfo.Count != 2)
        {
            throw new ArgumentException("Axis info should contain two axes for horizontal coordinate systems");
        }

        this.AxisInfo = axisInfo;
    }

    /// <summary>
    /// Gets or sets the HorizontalDatum.
    /// </summary>
    public HorizontalDatum HorizontalDatum { get; set; }
}
