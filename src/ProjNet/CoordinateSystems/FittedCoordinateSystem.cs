// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.IO.Wkt;

/// <summary>
/// A coordinate system which sits inside another coordinate system. The fitted
/// coordinate system can be rotated and shifted, or use any other math transform
/// to inject itself into the base coordinate system.
/// </summary>
/// <remarks>
/// <para>
/// Thread safety: Instances are immutable after construction and may be shared across threads.
/// </para>
/// </remarks>
public class FittedCoordinateSystem : CoordinateSystem // , IFittedCoordinateSystem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FittedCoordinateSystem"/> class.
    /// </summary>
    /// <param name="baseSystem">Underlying coordinate system.</param>
    /// <param name="transform">Transformation from fitted coordinate system to the base one.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="code">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    /// <param name="axisInfo">Optional axis definitions for the fitted system.</param>
    protected internal FittedCoordinateSystem(
        CoordinateSystem baseSystem,
        MathTransform transform,
        string name,
        string authority,
        long code,
        string alias,
        string remarks,
        string abbreviation,
        IReadOnlyList<AxisInfo>? axisInfo = null)
        : base(name, authority, code, alias, abbreviation, remarks, CreateAxisInfo(baseSystem, axisInfo, name), null)
    {
        this.BaseCoordinateSystem = ArgumentGuard.ThrowIfNull(baseSystem, nameof(baseSystem));
        this.ToBaseTransform = ArgumentGuard.ThrowIfNull(transform, nameof(transform));
    }

    /// <summary>
    /// Gets the math transform that maps this fitted coordinate system into the base coordinate system.
    /// </summary>
    public MathTransform ToBaseTransform { get; }

    /// <summary>
    /// Gets underlying coordinate system.
    /// </summary>
    public CoordinateSystem BaseCoordinateSystem { get; }

    /// <summary>
    /// Gets the Well-known text for this object as defined in the simple features specification.
    /// </summary>
    public override string WKT => this.ToWktNode().ToString();

    /// <summary>
    /// Gets an XML representation of this object.
    /// </summary>
    public override string XML => this.ToXml().ToString(SaveOptions.DisableFormatting);

    /// <summary>
    /// Creates a copy of this coordinate system with updated authority metadata.
    /// </summary>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="code">Replacement authority-specific identification code.</param>
    /// <returns>A new <see cref="FittedCoordinateSystem"/> with updated authority metadata.</returns>
    public new FittedCoordinateSystem WithAuthority(string authority, long code) => InfoAuthorityCloneHelper.CloneWithAuthority(this, authority, code);

    /// <summary>
    /// Creates a copy of this coordinate system with an updated name.
    /// </summary>
    /// <param name="name">Replacement name.</param>
    /// <returns>A new <see cref="FittedCoordinateSystem"/> with the updated name.</returns>
    public new FittedCoordinateSystem WithName(string name) => InfoAuthorityCloneHelper.CloneWithName(this, name);

    /// <summary>
    /// Returns an XML representation of this fitted coordinate system as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>No value is returned because XML serialization is not supported for fitted coordinate systems.</returns>
    /// <exception cref="NotSupportedException">Always thrown because XML serialization is not supported for fitted coordinate systems.</exception>
    public override XElement ToXml() => throw new NotSupportedException("XML serialization is not supported for fitted coordinate systems.");

    /// <inheritdoc />
    public override WktNode ToWktNode()
    {
        return new WktKeywordNode(
            "FITTED_CS",
            new WktQuotedString(this.Name),
            new WktIdentifier(this.ToBaseTransform.WKT),
            this.BaseCoordinateSystem.ToWktNode());
    }

    /// <inheritdoc />
    public override WktNode ToWktNode(WktVersion version)
    {
        WktVersionSupport.ThrowIfUnknown(version);
        return version == WktVersion.Wkt1
            ? this.ToWktNode()
            : this.CreateWkt2Node();
    }

    /// <summary>
    /// Gets the Well-Known Text of the math transform to the base coordinate system.
    /// </summary>
    /// <remarks>
    /// The dimension of this fitted coordinate system is determined by the source
    /// dimension of the math transform. The transform must be one-to-one within
    /// this coordinate system's domain, and the base coordinate system dimension
    /// must be at least as large as the dimension of this coordinate system.
    /// </remarks>
    /// <returns>The WKT string of the transform to the base coordinate system.</returns>
    public string ToBase() => this.ToBaseTransform.WKT;

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        var fcs = obj as FittedCoordinateSystem;
        if (fcs is not null)
        {
            if (fcs.Dimension != this.Dimension)
            {
                return false;
            }

            for (int i = 0; i < fcs.Dimension; i++)
            {
                if (fcs.GetAxis(i).Orientation != this.GetAxis(i).Orientation)
                {
                    return false;
                }

                if (!fcs.GetUnits(i).EqualParams(this.GetUnits(i)))
                {
                    return false;
                }
            }

            if (fcs.BaseCoordinateSystem.EqualParams(this.BaseCoordinateSystem))
            {
                string fcsToBase = fcs.ToBase();
                string thisToBase = this.ToBase();
                if (string.Equals(fcsToBase, thisToBase, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <inheritdoc />
    public override IUnit GetUnits(int dimension) => this.BaseCoordinateSystem.GetUnits(dimension);

    private static List<AxisInfo> CreateAxisInfo(CoordinateSystem baseSystem, IReadOnlyList<AxisInfo>? axisInfo, string name)
    {
        baseSystem = ArgumentGuard.ThrowIfNull(baseSystem, nameof(baseSystem));
        if (axisInfo is null || axisInfo.Count == 0)
        {
            var clonedAxisInfo = new List<AxisInfo>(baseSystem.Dimension);
            for (int dim = 0; dim < baseSystem.Dimension; dim++)
            {
                clonedAxisInfo.Add(baseSystem.GetAxis(dim));
            }

            return clonedAxisInfo;
        }

        if (axisInfo.Count != baseSystem.Dimension)
        {
            ArgumentGuard.ThrowArgument($"Fitted coordinate system '{name}' expects {baseSystem.Dimension} axes but received {axisInfo.Count}.");
        }

        var explicitAxisInfo = new List<AxisInfo>(axisInfo.Count);
        foreach (AxisInfo axis in axisInfo)
        {
            explicitAxisInfo.Add(new AxisInfo(axis));
        }

        return explicitAxisInfo;
    }

    private static WktKeywordNode CreateWkt2DerivingConversionNode(IProjection derivingConversion, WktNode translationUnitNode)
    {
        static WktKeywordNode CreateWkt2DerivingParameterNode(ProjectionParameter parameter, WktNode translationUnitNode)
        {
            var children = new List<WktNode>
            {
                new WktQuotedString(parameter.Name),
                new WktNumber(parameter.Value),
            };

            if (parameter.Name is "A0" or "B0")
            {
                children.Add(translationUnitNode);
            }
            else
            {
                children.Add(new WktKeywordNode(
                    "SCALEUNIT",
                    new WktQuotedString("unity"),
                    new WktNumber(1)));
            }

            return new WktKeywordNode("PARAMETER", children);
        }

        string conversionName = string.IsNullOrWhiteSpace(derivingConversion.Name)
            ? DerivedCoordinateSystemSupport.DefaultDerivingConversionName
            : derivingConversion.Name;
        var children = new List<WktNode>
        {
            new WktQuotedString(conversionName),
            new WktKeywordNode(
                "METHOD",
                new WktQuotedString(derivingConversion.ClassName)),
        };

        for (int i = 0; i < derivingConversion.NumParameters; i++)
        {
            children.Add(CreateWkt2DerivingParameterNode(derivingConversion.GetParameter(i), translationUnitNode));
        }

        return new WktKeywordNode("DERIVINGCONVERSION", children);
    }

    private WktKeywordNode CreateWkt2Node()
    {
        Projection derivingConversion = DerivedCoordinateSystemSupport.CreateAffineConversion(this.ToBaseTransform, DerivedCoordinateSystemSupport.DefaultDerivingConversionName);
        return this.BaseCoordinateSystem switch
        {
            GeographicCoordinateSystem geographicCoordinateSystem => this.CreateWkt2DerivedGeographicNode(geographicCoordinateSystem, derivingConversion),
            ProjectedCoordinateSystem projectedCoordinateSystem => this.CreateWkt2DerivedProjectedNode(projectedCoordinateSystem, derivingConversion),
            _ => throw new NotSupportedException("WKT2 output for fitted coordinate systems currently supports only affine transforms based on two-dimensional geographic or projected coordinate systems."),
        };
    }

    private WktKeywordNode CreateWkt2DerivedGeographicNode(GeographicCoordinateSystem baseCoordinateSystem, IProjection derivingConversion)
    {
        if (this.Dimension != 2 || this.AxisInfo.Count != this.Dimension)
        {
            throw new NotSupportedException("WKT2 output for fitted geographic coordinate systems currently supports only two axes.");
        }

        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            baseCoordinateSystem.CreateWkt2BaseNode("BASEGEOGCRS"),
            CreateWkt2DerivingConversionNode(derivingConversion, baseCoordinateSystem.AngularUnit.ToWktNode(WktVersion.Wkt22019)),
            new WktKeywordNode(
                "CS",
                new WktIdentifier("ellipsoidal"),
                new WktInteger(this.Dimension)),
        };

        for (int i = 0; i < this.AxisInfo.Count; i++)
        {
            children.Add(this.GetAxis(i).ToWktNode(WktVersion.Wkt22019));
        }

        children.Add(baseCoordinateSystem.AngularUnit.ToWktNode(WktVersion.Wkt22019));

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("GEOGCRS", children);
    }

    private WktKeywordNode CreateWkt2DerivedProjectedNode(ProjectedCoordinateSystem baseCoordinateSystem, IProjection derivingConversion)
    {
        if (this.Dimension != 2 || this.AxisInfo.Count != this.Dimension)
        {
            throw new NotSupportedException("WKT2 output for fitted projected coordinate systems currently supports only two axes.");
        }

        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            baseCoordinateSystem.CreateWkt2BaseNode("BASEPROJCRS"),
            CreateWkt2DerivingConversionNode(derivingConversion, baseCoordinateSystem.LinearUnit.ToWktNode(WktVersion.Wkt22019)),
            new WktKeywordNode(
                "CS",
                new WktIdentifier("Cartesian"),
                new WktInteger(this.Dimension)),
        };

        for (int i = 0; i < this.AxisInfo.Count; i++)
        {
            children.Add(this.GetAxis(i).ToWktNode(WktVersion.Wkt22019));
        }

        children.Add(baseCoordinateSystem.LinearUnit.ToWktNode(WktVersion.Wkt22019));

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("DERIVEDPROJCRS", children);
    }
}
