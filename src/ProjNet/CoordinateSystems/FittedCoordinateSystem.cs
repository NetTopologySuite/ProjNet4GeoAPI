// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.IO.Wkt;

/// <summary>
/// A coordinate system which sits inside another coordinate system. The fitted
/// coordinate system can be rotated and shifted, or use any other math transform
/// to inject itself into the base coordinate system.
/// </summary>
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
    protected internal FittedCoordinateSystem(
        CoordinateSystem baseSystem,
        MathTransform transform,
        string name,
        string authority,
        long code,
        string alias,
        string remarks,
        string abbreviation)
        : base(name, authority, code, alias, abbreviation, remarks)
    {
        this.BaseCoordinateSystem = ArgumentGuard.ThrowIfNull(baseSystem, nameof(baseSystem));
        this.ToBaseTransform = ArgumentGuard.ThrowIfNull(transform, nameof(transform));

        // get axis infos from the source
        this.AxisInfo = new List<AxisInfo>(baseSystem.Dimension);
        for (int dim = 0; dim < baseSystem.Dimension; dim++)
        {
            this.AxisInfo.Add(baseSystem.GetAxis(dim));
        }
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
    public override string WKT
    {
        get
        {
            // <fitted cs>          = FITTED_CS["<name>", <to base>, <base cs>]
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "FITTED_CS[\"{0}\", {1}, {2}]", this.Name, this.ToBaseTransform.WKT, this.BaseCoordinateSystem.WKT);
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
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Returns an XML representation of this fitted coordinate system as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>Not implemented; always throws <see cref="NotImplementedException"/>.</returns>
    /// <exception cref="NotImplementedException">Always thrown because XML serialization is not supported for fitted coordinate systems.</exception>
    public override XElement ToXml()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override WktNode ToWktNode()
    {
        return new WktKeywordNode(
            "FITTED_CS",
            new WktQuotedString(this.Name),
            new WktIdentifier(this.ToBaseTransform.WKT),
            this.BaseCoordinateSystem.ToWktNode());
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
}
