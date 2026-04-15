// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using ProjNet.IO.CoordinateSystems;
using ProjNet.IO.Wkt;

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
public abstract class CoordinateSystem : Info
{
    private readonly List<AxisInfo> axisInfo;
    private readonly double[] defaultEnvelope;

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
        : this(name, authority, authorityCode, alias, abbreviation, remarks, [], null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateSystem"/> class with axis metadata.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    /// <param name="axisInfo">Axis definitions.</param>
    /// <param name="defaultEnvelope">Default envelope.</param>
    internal CoordinateSystem(
        string name,
        string authority,
        long authorityCode,
        string alias,
        string abbreviation,
        string remarks,
        List<AxisInfo> axisInfo,
        double[]? defaultEnvelope)
        : base(name, authority, authorityCode, alias, abbreviation, remarks)
    {
        this.axisInfo = ArgumentGuard.ThrowIfNull(axisInfo, nameof(axisInfo));
        this.defaultEnvelope = CloneDefaultEnvelope(defaultEnvelope);
    }

    /// <summary>
    /// Gets dimension of the coordinate system.
    /// </summary>
    public int Dimension
    {
        get { return this.AxisInfo.Count; }
    }

    /// <summary>
    /// Gets the axis definitions for this coordinate system.
    /// </summary>
    internal List<AxisInfo> AxisInfo => this.axisInfo;

    /// <summary>
    /// Gets default envelope of coordinate system.
    /// </summary>
    /// <remarks>
    /// Coordinate systems which are bounded should return the minimum bounding box of their domain.
    /// Unbounded coordinate systems should return a box which is as large as is likely to be used.
    /// For example, a (lon,lat) geographic coordinate system in degrees should return a box from
    /// (-180,-90) to (180,90), and a geocentric coordinate system could return a box from (-r,-r,-r)
    /// to (+r,+r,+r) where r is the approximate radius of the Earth.
    /// </remarks>
    public double[] DefaultEnvelope => this.defaultEnvelope.Length == 0 ? Array.Empty<double>() : (double[])this.defaultEnvelope.Clone();

    /// <summary>
    /// Gets the units for the dimension within coordinate system.
    /// Each dimension in the coordinate system has corresponding units.
    /// </summary>
    /// <param name="dimension">Zero-based index of the dimension.</param>
    /// <returns>The unit for the specified dimension.</returns>
    public abstract IUnit GetUnits(int dimension);

    /// <summary>
    /// Converts this coordinate system to a WKT syntax tree node.
    /// </summary>
    /// <returns>A <see cref="WktNode"/> representing this coordinate system.</returns>
    public virtual WktNode ToWktNode() => new WktIdentifier(this.WKT);

    /// <summary>
    /// Converts this coordinate system to a WKT syntax tree node for the requested WKT version.
    /// </summary>
    /// <param name="version">The WKT dialect to emit.</param>
    /// <returns>A <see cref="WktNode"/> representing this coordinate system in the requested WKT version.</returns>
    public virtual WktNode ToWktNode(WktVersion version)
    {
        WktVersionSupport.ThrowIfUnknown(version);
        return version == WktVersion.Wkt1
            ? this.ToWktNode()
            : throw WktVersionSupport.CreateNotSupportedException(this.GetType().Name, version);
    }

    /// <summary>
    /// Returns an XML representation of this coordinate system as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>An <see cref="XElement"/> containing the XML representation.</returns>
    public virtual XElement ToXml() => throw new NotSupportedException("XML serialization is not supported for this coordinate system type.");

    /// <summary>
    /// Serializes this coordinate system to PROJJSON.
    /// </summary>
    /// <returns>The serialized PROJJSON text.</returns>
    /// <exception cref="NotSupportedException">Thrown when PROJJSON serialization is not supported for this coordinate system type.</exception>
    public string ToProjJson() => ProjJsonWriter.ToJson(this);

    /// <summary>
    /// Gets axis details for dimension within coordinate system.
    /// </summary>
    /// <param name="dimension">Zero-based index of the axis.</param>
    /// <returns>The <see cref="AxisInfo"/> for the specified dimension.</returns>
    public AxisInfo GetAxis(int dimension)
    {
        if (dimension >= this.AxisInfo.Count || dimension < 0)
        {
            ArgumentGuard.ThrowArgumentOutOfRange(nameof(dimension), "AxisInfo not available for the requested dimension.");
        }

        return this.AxisInfo[dimension];
    }

    /// <summary>
    /// Clones a coordinate-system default envelope for constructor-time storage.
    /// </summary>
    /// <param name="envelope">Envelope values to clone.</param>
    /// <returns>A cloned envelope array, or <see cref="Array.Empty{T}"/> when no envelope is provided.</returns>
    internal static double[] CloneDefaultEnvelope(double[]? envelope)
    {
        if (envelope is null || envelope.Length == 0)
        {
            return Array.Empty<double>();
        }

        return (double[])envelope.Clone();
    }

    /// <inheritdoc />
    private protected override Info CloneWithAuthorityCore(string authority, long code)
    {
        return this switch
        {
            GeographicCoordinateSystem geographicCoordinateSystem => geographicCoordinateSystem.WithAuthority(authority, code),
            ProjectedCoordinateSystem projectedCoordinateSystem => projectedCoordinateSystem.WithAuthority(authority, code),
            GeocentricCoordinateSystem geocentricCoordinateSystem => geocentricCoordinateSystem.WithAuthority(authority, code),
            VerticalCoordinateSystem verticalCoordinateSystem => verticalCoordinateSystem.WithAuthority(authority, code),
            CompoundCoordinateSystem compoundCoordinateSystem => compoundCoordinateSystem.WithAuthority(authority, code),
            BoundCoordinateSystem boundCoordinateSystem => boundCoordinateSystem.WithAuthority(authority, code),
            FittedCoordinateSystem fittedCoordinateSystem => fittedCoordinateSystem.WithAuthority(authority, code),
            EngineeringCoordinateSystem engineeringCoordinateSystem => engineeringCoordinateSystem.WithAuthority(authority, code),
            ParametricCoordinateSystem parametricCoordinateSystem => parametricCoordinateSystem.WithAuthority(authority, code),
            TemporalCoordinateSystem temporalCoordinateSystem => temporalCoordinateSystem.WithAuthority(authority, code),
            _ => throw new NotSupportedException($"WithAuthority is not supported for coordinate system type '{this.GetType().FullName}'."),
        };
    }

    /// <inheritdoc />
    private protected override Info CloneWithNameCore(string name)
    {
        return this switch
        {
            GeographicCoordinateSystem geographicCoordinateSystem => geographicCoordinateSystem.WithName(name),
            ProjectedCoordinateSystem projectedCoordinateSystem => projectedCoordinateSystem.WithName(name),
            GeocentricCoordinateSystem geocentricCoordinateSystem => geocentricCoordinateSystem.WithName(name),
            VerticalCoordinateSystem verticalCoordinateSystem => verticalCoordinateSystem.WithName(name),
            CompoundCoordinateSystem compoundCoordinateSystem => compoundCoordinateSystem.WithName(name),
            BoundCoordinateSystem boundCoordinateSystem => boundCoordinateSystem.WithName(name),
            FittedCoordinateSystem fittedCoordinateSystem => fittedCoordinateSystem.WithName(name),
            EngineeringCoordinateSystem engineeringCoordinateSystem => engineeringCoordinateSystem.WithName(name),
            ParametricCoordinateSystem parametricCoordinateSystem => parametricCoordinateSystem.WithName(name),
            TemporalCoordinateSystem temporalCoordinateSystem => temporalCoordinateSystem.WithName(name),
            _ => throw new NotSupportedException($"WithName is not supported for coordinate system type '{this.GetType().FullName}'."),
        };
    }
}
