// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ProjNet.IO.Wkt;

/// <summary>
/// A WKT2 coordinate operation model containing method, parameter, and endpoint metadata.
/// </summary>
public sealed class CoordinateOperation : Info
{
    private readonly List<Parameter> parameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateOperation"/> class.
    /// </summary>
    /// <param name="methodName">Method name.</param>
    /// <param name="parameters">Operation parameters.</param>
    /// <param name="sourceCoordinateSystem">Source coordinate system.</param>
    /// <param name="targetCoordinateSystem">Target coordinate system.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    public CoordinateOperation(
        string methodName,
        IReadOnlyList<Parameter> parameters,
        CoordinateSystem sourceCoordinateSystem,
        CoordinateSystem targetCoordinateSystem,
        string name,
        string authority,
        long authorityCode,
        string alias,
        string abbreviation,
        string remarks)
        : base(name, authority, authorityCode, alias, abbreviation, remarks)
    {
        this.MethodName = string.IsNullOrWhiteSpace(methodName)
            ? ArgumentGuard.ThrowArgument<string>("Coordinate operations require a method name.", nameof(methodName))
            : methodName;
        this.SourceCoordinateSystem = ArgumentGuard.ThrowIfNull(sourceCoordinateSystem, nameof(sourceCoordinateSystem));
        this.TargetCoordinateSystem = ArgumentGuard.ThrowIfNull(targetCoordinateSystem, nameof(targetCoordinateSystem));
        parameters = ArgumentGuard.ThrowIfNull(parameters, nameof(parameters));
        this.parameters = parameters.Select(parameter => ArgumentGuard.ThrowIfNull(parameter, nameof(parameters))).ToList();
    }

    /// <summary>
    /// Gets or sets the source coordinate system.
    /// </summary>
    public CoordinateSystem SourceCoordinateSystem { get; set; }

    /// <summary>
    /// Gets or sets the target coordinate system.
    /// </summary>
    public CoordinateSystem TargetCoordinateSystem { get; set; }

    /// <summary>
    /// Gets or sets the operation method name.
    /// </summary>
    public string MethodName { get; set; }

    /// <summary>
    /// Gets the operation parameters.
    /// </summary>
    public IReadOnlyList<Parameter> Parameters => this.parameters;

    /// <inheritdoc />
    public override string WKT => this.ToWktNode(WktVersion.Wkt22019).ToString();

    /// <inheritdoc />
    public override string XML => this.ToXml().ToString(SaveOptions.DisableFormatting);

    /// <summary>
    /// Converts this coordinate operation to a WKT syntax tree node.
    /// </summary>
    /// <returns>A <see cref="WktNode"/> representing this coordinate operation.</returns>
    public WktNode ToWktNode() => this.ToWktNode(WktVersion.Wkt22019);

    /// <summary>
    /// Converts this coordinate operation to a WKT syntax tree node for the requested version.
    /// </summary>
    /// <param name="version">The WKT dialect to emit.</param>
    /// <returns>A <see cref="WktNode"/> representing this coordinate operation.</returns>
    public WktNode ToWktNode(WktVersion version)
    {
        WktVersionSupport.ThrowIfUnknown(version);
        if (version == WktVersion.Wkt1)
        {
            throw WktVersionSupport.CreateNotSupportedException(nameof(CoordinateOperation), version);
        }

        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            new WktKeywordNode("SOURCECRS", this.SourceCoordinateSystem.ToWktNode(version)),
            new WktKeywordNode("TARGETCRS", this.TargetCoordinateSystem.ToWktNode(version)),
            new WktKeywordNode("METHOD", new WktQuotedString(this.MethodName)),
        };

        foreach (Parameter parameter in this.parameters)
        {
            children.Add(new WktKeywordNode(
                "PARAMETER",
                new WktQuotedString(parameter.Name),
                new WktNumber(parameter.Value)));
        }

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("COORDINATEOPERATION", children);
    }

    /// <summary>
    /// Returns an XML representation of this coordinate operation as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>An <see cref="XElement"/> containing the XML representation.</returns>
    public XElement ToXml()
    {
        var element = new XElement("CS_CoordinateOperation", new XAttribute("MethodName", this.MethodName));
        element.Add(this.InfoXmlElement);
        element.Add(new XElement("SourceCoordinateSystem", this.SourceCoordinateSystem.ToXml()));
        element.Add(new XElement("TargetCoordinateSystem", this.TargetCoordinateSystem.ToXml()));
        foreach (Parameter parameter in this.parameters)
        {
            element.Add(new XElement(
                "Parameter",
                new XAttribute("Name", parameter.Name),
                new XAttribute("Value", parameter.Value.ToString(CultureInfo.InvariantCulture))));
        }

        return element;
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        if (obj is not CoordinateOperation coordinateOperation
            || !string.Equals(this.MethodName, coordinateOperation.MethodName, StringComparison.OrdinalIgnoreCase)
            || !this.SourceCoordinateSystem.EqualParams(coordinateOperation.SourceCoordinateSystem)
            || !this.TargetCoordinateSystem.EqualParams(coordinateOperation.TargetCoordinateSystem)
            || this.parameters.Count != coordinateOperation.parameters.Count)
        {
            return false;
        }

        for (int i = 0; i < this.parameters.Count; i++)
        {
            if (!string.Equals(this.parameters[i].Name, coordinateOperation.parameters[i].Name, StringComparison.OrdinalIgnoreCase)
                || this.parameters[i].Value != coordinateOperation.parameters[i].Value)
            {
                return false;
            }
        }

        return true;
    }
}
