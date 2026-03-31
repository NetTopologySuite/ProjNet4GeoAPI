// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ProjNet.IO.Wkt;

/// <summary>
/// The Projection class defines the standard information stored with a projection
/// objects. A projection object implements a coordinate transformation from a geographic
/// coordinate system to a projected coordinate system, given the ellipsoid for the
/// geographic coordinate system. It is expected that each coordinate transformation of
/// interest, e.g., Transverse Mercator, Lambert, will be implemented as a class of
/// type Projection, supporting the IProjection interface.
/// </summary>
public class Projection : Info, IProjection
{
    private readonly string className;
    private List<ProjectionParameter> parameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projection"/> class.
    /// </summary>
    /// <param name="className">Projection class name, for example <c>Transverse_Mercator</c>.</param>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="name">Projection display name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="code">Authority code.</param>
    /// <param name="alias">Alias name.</param>
    /// <param name="remarks">Additional remarks.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    internal Projection(
        string className,
        List<ProjectionParameter> parameters,
        string name,
        string authority,
        long code,
        string alias,
        string remarks,
        string abbreviation)
        : base(name, authority, code, alias, abbreviation, remarks)
    {
        this.parameters = parameters;
        this.className = className;
    }

    /// <summary>
    /// Gets the number of parameters of the projection.
    /// </summary>
    public int NumParameters => this.parameters.Count;

    /// <summary>
    /// Gets or sets the parameters of the projection.
    /// </summary>
    internal List<ProjectionParameter> Parameters
    {
        get => this.parameters;
        set => this.parameters = value;
    }

    /// <summary>
    /// Gets the projection classification name (e.g. "Transverse_Mercator").
    /// </summary>
    public string ClassName => this.className;

    /// <summary>
    /// Gets the Well-known text for this object
    /// as defined in the simple features specification.
    /// </summary>
    public override string WKT
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "PROJECTION[\"{0}\"", this.ClassName);
            if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, ", AUTHORITY[\"{0}\", \"{1}\"]", this.Authority, this.AuthorityCode);
            }

            sb.Append(']');
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
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat, "<CS_Projection Classname=\"{0}\">{1}", this.ClassName, this.InfoXml);
            foreach (ProjectionParameter param in this.Parameters)
            {
                sb.Append(param.XML);
            }

            sb.Append("</CS_Projection>");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Converts this projection to a WKT syntax tree node.
    /// </summary>
    /// <returns>A <see cref="WktNode"/> representing this projection.</returns>
    public WktNode ToWktNode()
    {
        var children = new List<WktNode>
        {
            new WktQuotedString(this.ClassName),
        };

        if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
        {
            children.Add(new WktKeywordNode(
                "AUTHORITY",
                new WktQuotedString(this.Authority),
                new WktQuotedString(this.AuthorityCode.ToString(CultureInfo.InvariantCulture))));
        }

        return new WktKeywordNode("PROJECTION", children);
    }

    /// <summary>
    /// Gets an indexed parameter of the projection.
    /// </summary>
    /// <param name="index">Index of parameter.</param>
    /// <returns>The projection parameter at the specified index.</returns>
    public ProjectionParameter GetParameter(int index)
    {
        return this.parameters[index];
    }

    /// <summary>
    /// Gets a named parameter of the projection.
    /// </summary>
    /// <remarks>The parameter name is case insensitive.</remarks>
    /// <param name="name">Name of the parameter to find.</param>
    /// <returns>The matching <see cref="ProjectionParameter"/>, or <see langword="null"/> if not found.</returns>
    public ProjectionParameter? GetParameter(string name)
    {
        foreach (ProjectionParameter par in this.parameters)
        {
            if (par.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return par;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        if (obj is not Projection projection)
        {
            return false;
        }

        if (projection.NumParameters != this.NumParameters)
        {
            return false;
        }

        for (int i = 0; i < this.parameters.Count; i++)
        {
            ProjectionParameter? param = this.GetParameter(projection.GetParameter(i).Name);
            if (param is null)
            {
                return false;
            }

            if (param.Value != projection.GetParameter(i).Value)
            {
                return false;
            }
        }

        return true;
    }
}
