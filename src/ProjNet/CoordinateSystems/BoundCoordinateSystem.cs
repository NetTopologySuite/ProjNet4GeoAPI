// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using ProjNet.IO.Wkt;

/// <summary>
/// A coordinate system that retains explicit BoundCRS metadata alongside its source coordinate system.
/// </summary>
/// <remarks>
/// <para>
/// The bound coordinate system keeps the source coordinate system, the target or hub coordinate
/// system, and the bound transformation definition together as a first-class model instead of
/// scattering that metadata across datum and vertical-coordinate-system implementation details.
/// </para>
/// <para>
/// Until dedicated BoundCRS serializers are implemented, legacy WKT1 and XML output intentionally
/// fall back to the source coordinate system representation.
/// </para>
/// </remarks>
public class BoundCoordinateSystem : CoordinateSystem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoundCoordinateSystem"/> class.
    /// </summary>
    /// <param name="sourceCoordinateSystem">Source coordinate system described by the bound CRS.</param>
    /// <param name="targetCoordinateSystem">Target or hub coordinate system used by the bound transformation.</param>
    /// <param name="transformation">Bound transformation metadata.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    protected internal BoundCoordinateSystem(
        CoordinateSystem sourceCoordinateSystem,
        CoordinateSystem targetCoordinateSystem,
        BoundTransformation transformation,
        string name,
        string authority,
        long authorityCode,
        string alias,
        string abbreviation,
        string remarks)
        : base(name, authority, authorityCode, alias, abbreviation, remarks)
    {
        this.SourceCoordinateSystem = ArgumentGuard.ThrowIfNull(sourceCoordinateSystem, nameof(sourceCoordinateSystem));
        this.TargetCoordinateSystem = ArgumentGuard.ThrowIfNull(targetCoordinateSystem, nameof(targetCoordinateSystem));
        this.Transformation = ArgumentGuard.ThrowIfNull(transformation, nameof(transformation));

        this.AxisInfo = new List<AxisInfo>(this.SourceCoordinateSystem.Dimension);
        for (int dimension = 0; dimension < this.SourceCoordinateSystem.Dimension; dimension++)
        {
            this.AxisInfo.Add(new AxisInfo(this.SourceCoordinateSystem.GetAxis(dimension)));
        }

        if (this.SourceCoordinateSystem.DefaultEnvelope.Length > 0)
        {
            this.DefaultEnvelope = (double[])this.SourceCoordinateSystem.DefaultEnvelope.Clone();
        }
    }

    /// <summary>
    /// Gets the source coordinate system described by the bound CRS.
    /// </summary>
    public CoordinateSystem SourceCoordinateSystem { get; }

    /// <summary>
    /// Gets the target or hub coordinate system used by the bound transformation.
    /// </summary>
    public CoordinateSystem TargetCoordinateSystem { get; }

    /// <summary>
    /// Gets the bound transformation metadata.
    /// </summary>
    public BoundTransformation Transformation { get; }

    /// <inheritdoc />
    public override string WKT => this.ToWktNode().ToString();

    /// <inheritdoc />
    public override string XML => this.SourceCoordinateSystem.XML;

    /// <inheritdoc />
    public override XElement ToXml() => this.SourceCoordinateSystem.ToXml();

    /// <inheritdoc />
    public override WktNode ToWktNode() => this.SourceCoordinateSystem.ToWktNode();

    /// <inheritdoc />
    public override WktNode ToWktNode(WktVersion version)
    {
        WktVersionSupport.ThrowIfUnknown(version);
        return version == WktVersion.Wkt1
            ? this.SourceCoordinateSystem.ToWktNode(version)
            : BoundCoordinateSystemSupport.CreateWkt2BoundCoordinateSystemNode(this);
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        return obj is BoundCoordinateSystem boundCoordinateSystem
            && this.SourceCoordinateSystem.EqualParams(boundCoordinateSystem.SourceCoordinateSystem)
            && this.TargetCoordinateSystem.EqualParams(boundCoordinateSystem.TargetCoordinateSystem)
            && this.Transformation.Equals(boundCoordinateSystem.Transformation);
    }

    /// <inheritdoc />
    public override IUnit GetUnits(int dimension) => this.SourceCoordinateSystem.GetUnits(dimension);
}
