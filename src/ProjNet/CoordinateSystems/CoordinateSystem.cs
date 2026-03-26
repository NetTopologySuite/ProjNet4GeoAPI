// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Base interface for all coordinate systems.
/// </summary>
/// <remarks>
/// <para>A coordinate system is a mathematical space, where the elements of the space
/// are called positions. Each position is described by a list of numbers. The length
/// of the list corresponds to the dimension of the coordinate system. So in a 2D
/// coordinate system each position is described by a list containing 2 numbers.</para>
/// <para>However, in a coordinate system, not all lists of numbers correspond to a
/// position - some lists may be outside the domain of the coordinate system. For
/// example, in a 2D Lat/Lon coordinate system, the list (91,91) does not correspond
/// to a position.</para>
/// <para>Some coordinate systems also have a mapping from the mathematical space into
/// locations in the real world. So in a Lat/Lon coordinate system, the mathematical
/// position (lat, long) corresponds to a location on the surface of the Earth. This
/// mapping from the mathematical space into real-world locations is called a Datum.</para>
/// </remarks>
[Serializable]
public abstract class CoordinateSystem : Info
{
    private List<AxisInfo> axisInfo = [];
    private double[] defaultEnvelope = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateSystem"/> class.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    internal CoordinateSystem(string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
        : base(name, authority, authorityCode, alias, abbreviation, remarks)
    {
    }

    /// <summary>
    /// Gets dimension of the coordinate system.
    /// </summary>
    public int Dimension
    {
        get { return this.AxisInfo.Count; }
    }

    /// <summary>
    /// Gets or sets the axis definitions for this coordinate system.
    /// </summary>
    internal List<AxisInfo> AxisInfo
    {
        get
        {
            return this.axisInfo;
        }

        set
        {
            ArgumentGuard.ThrowIfNull(value, nameof(value));
            this.axisInfo = value;
        }
    }

    /// <summary>
    /// Gets or sets default envelope of coordinate system.
    /// </summary>
    /// <remarks>
    /// Coordinate systems which are bounded should return the minimum bounding box of their domain.
    /// Unbounded coordinate systems should return a box which is as large as is likely to be used.
    /// For example, a (lon,lat) geographic coordinate system in degrees should return a box from
    /// (-180,-90) to (180,90), and a geocentric coordinate system could return a box from (-r,-r,-r)
    /// to (+r,+r,+r) where r is the approximate radius of the Earth.
    /// </remarks>
    public double[] DefaultEnvelope
    {
        get { return this.defaultEnvelope; }
        set { this.defaultEnvelope = value; }
    }

    /// <summary>
    /// Gets the units for the dimension within coordinate system.
    /// Each dimension in the coordinate system has corresponding units.
    /// </summary>
    /// <param name="dimension">Zero-based index of the dimension.</param>
    /// <returns>The unit for the specified dimension.</returns>
    public abstract IUnit GetUnits(int dimension);

    /// <summary>
    /// Gets axis details for dimension within coordinate system.
    /// </summary>
    /// <param name="dimension">Zero-based index of the axis.</param>
    /// <returns>The <see cref="AxisInfo"/> for the specified dimension.</returns>
    public AxisInfo GetAxis(int dimension)
    {
        if (dimension >= this.AxisInfo.Count || dimension < 0)
        {
            ArgumentGuard.ThrowArgument("AxisInfo not available for dimension " + dimension.ToString(CultureInfo.InvariantCulture));
        }

        return this.AxisInfo[dimension];
    }
}
