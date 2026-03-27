// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable

namespace ProjNet.CoordinateSystems;

/// <summary>
/// Orientation of axis. Some coordinate systems use non-standard orientations.
/// For example, the first axis in South African grids usually points West,
/// instead of East. This information is obviously relevant for algorithms
/// converting South African grid coordinates into Lat/Long.
/// </summary>
public enum AxisOrientationEnum : short
{
    /// <summary>
    /// Unknown or unspecified axis orientation. This can be used for local or fitted coordinate systems.
    /// </summary>
    Other = 0,

    /// <summary>
    /// Increasing ordinates values go North. This is usually used for Grid Y coordinates and Latitude.
    /// </summary>
    North = 1,

    /// <summary>
    /// Increasing ordinates values go South. This is rarely used.
    /// </summary>
    South = 2,

    /// <summary>
    /// Increasing ordinates values go East. This is rarely used.
    /// </summary>
    East = 3,

    /// <summary>
    /// Increasing ordinates values go West. This is usually used for Grid X coordinates and Longitude.
    /// </summary>
    West = 4,

    /// <summary>
    /// Increasing ordinates values go up. This is used for vertical coordinate systems.
    /// </summary>
    Up = 5,

    /// <summary>
    /// Increasing ordinates values go down. This is used for vertical coordinate systems.
    /// </summary>
    Down = 6,
}
