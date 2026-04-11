// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ProjNet.IO.Wkt;

/// <summary>
/// A WKT2 concatenated operation consisting of one or more coordinate-operation steps.
/// </summary>
public sealed class ConcatenatedOperation : Info
{
    private readonly List<CoordinateOperation> steps;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcatenatedOperation"/> class.
    /// </summary>
    /// <param name="steps">Operation steps.</param>
    /// <param name="sourceCoordinateSystem">Source coordinate system.</param>
    /// <param name="targetCoordinateSystem">Target coordinate system.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    public ConcatenatedOperation(
        IReadOnlyList<CoordinateOperation> steps,
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
        steps = ArgumentGuard.ThrowIfNull(steps, nameof(steps));
        if (steps.Count == 0)
        {
            ArgumentGuard.ThrowArgument("Concatenated operations require at least one step.", nameof(steps));
        }

        this.SourceCoordinateSystem = ArgumentGuard.ThrowIfNull(sourceCoordinateSystem, nameof(sourceCoordinateSystem));
        this.TargetCoordinateSystem = ArgumentGuard.ThrowIfNull(targetCoordinateSystem, nameof(targetCoordinateSystem));
        this.steps = steps.Select(step => ArgumentGuard.ThrowIfNull(step, nameof(steps))).ToList();
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
    /// Gets the concatenated operation steps.
    /// </summary>
    public IReadOnlyList<CoordinateOperation> Steps => this.steps;

    /// <inheritdoc />
    public override string WKT => this.ToWktNode(WktVersion.Wkt22019).ToString();

    /// <inheritdoc />
    public override string XML => this.ToXml().ToString(SaveOptions.DisableFormatting);

    /// <summary>
    /// Converts this concatenated operation to a WKT syntax tree node.
    /// </summary>
    /// <returns>A <see cref="WktNode"/> representing this concatenated operation.</returns>
    public WktNode ToWktNode() => this.ToWktNode(WktVersion.Wkt22019);

    /// <summary>
    /// Converts this concatenated operation to a WKT syntax tree node for the requested version.
    /// </summary>
    /// <param name="version">The WKT dialect to emit.</param>
    /// <returns>A <see cref="WktNode"/> representing this concatenated operation.</returns>
    public WktNode ToWktNode(WktVersion version)
    {
        WktVersionSupport.ThrowIfUnknown(version);
        if (version == WktVersion.Wkt1)
        {
            throw WktVersionSupport.CreateNotSupportedException(nameof(ConcatenatedOperation), version);
        }

        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            new WktKeywordNode("SOURCECRS", this.SourceCoordinateSystem.ToWktNode(version)),
            new WktKeywordNode("TARGETCRS", this.TargetCoordinateSystem.ToWktNode(version)),
        };

        foreach (CoordinateOperation step in this.steps)
        {
            children.Add(new WktKeywordNode("STEP", step.ToWktNode(version)));
        }

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("CONCATENATEDOPERATION", children);
    }

    /// <summary>
    /// Returns an XML representation of this concatenated operation as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>An <see cref="XElement"/> containing the XML representation.</returns>
    public XElement ToXml()
    {
        var element = new XElement("CS_ConcatenatedOperation");
        element.Add(this.InfoXmlElement);
        element.Add(new XElement("SourceCoordinateSystem", this.SourceCoordinateSystem.ToXml()));
        element.Add(new XElement("TargetCoordinateSystem", this.TargetCoordinateSystem.ToXml()));
        foreach (CoordinateOperation step in this.steps)
        {
            element.Add(new XElement("Step", step.ToXml()));
        }

        return element;
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        if (obj is not ConcatenatedOperation concatenatedOperation
            || !this.SourceCoordinateSystem.EqualParams(concatenatedOperation.SourceCoordinateSystem)
            || !this.TargetCoordinateSystem.EqualParams(concatenatedOperation.TargetCoordinateSystem)
            || this.steps.Count != concatenatedOperation.steps.Count)
        {
            return false;
        }

        for (int i = 0; i < this.steps.Count; i++)
        {
            if (!this.steps[i].EqualParams(concatenatedOperation.steps[i]))
            {
                return false;
            }
        }

        return true;
    }
}
