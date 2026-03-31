// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.Wkt;

using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Represents a WKT keyword node with children, e.g. <c>GEOGCS["WGS 84", ...]</c>.
/// </summary>
public sealed class WktKeywordNode : WktNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WktKeywordNode"/> class.
    /// </summary>
    /// <param name="keyword">The WKT keyword.</param>
    /// <param name="children">The child nodes.</param>
    public WktKeywordNode(string keyword, params WktNode[] children)
    {
        this.Keyword = keyword;
        this.Children = children;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WktKeywordNode"/> class.
    /// </summary>
    /// <param name="keyword">The WKT keyword.</param>
    /// <param name="children">The child nodes.</param>
    public WktKeywordNode(string keyword, IReadOnlyList<WktNode> children)
    {
        this.Keyword = keyword;
        this.Children = children;
    }

    /// <summary>
    /// Gets the WKT keyword, e.g. <c>GEOGCS</c>, <c>DATUM</c>.
    /// </summary>
    public string Keyword { get; }

    /// <summary>
    /// Gets the child nodes of this keyword node.
    /// </summary>
    public IReadOnlyList<WktNode> Children { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(this.Keyword);
        sb.Append('[');
        for (int i = 0; i < this.Children.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }

            sb.Append(this.Children[i].ToString());
        }

        sb.Append(']');
        return sb.ToString();
    }

    /// <inheritdoc />
    public override string ToFormattedString(int indentLevel = 0, int indentSize = 4)
    {
        string indent = new string(' ', indentLevel * indentSize);
        string childIndent = new string(' ', (indentLevel + 1) * indentSize);
        var sb = new StringBuilder();
        sb.Append(indent);
        sb.Append(this.Keyword);
        sb.Append('[');

        bool hasComplexChildren = this.Children.Any(c => c is WktKeywordNode);
        if (hasComplexChildren && this.Children.Count > 0)
        {
            sb.AppendLine();
            for (int i = 0; i < this.Children.Count; i++)
            {
                if (this.Children[i] is WktKeywordNode keywordChild)
                {
                    sb.Append(keywordChild.ToFormattedString(indentLevel + 1, indentSize));
                }
                else
                {
                    sb.Append(childIndent);
                    sb.Append(this.Children[i].ToString());
                }

                if (i < this.Children.Count - 1)
                {
                    sb.Append(',');
                }

                sb.AppendLine();
            }

            sb.Append(indent);
        }
        else
        {
            for (int i = 0; i < this.Children.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(this.Children[i].ToString());
            }
        }

        sb.Append(']');
        return sb.ToString();
    }
}
