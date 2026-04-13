// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.Wkt;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ProjNet;
using ProjNet.IO.CoordinateSystems;

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

    /// <summary>
    /// Parses a complete WKT string into a keyword-node tree.
    /// </summary>
    /// <param name="source">The WKT source text.</param>
    /// <returns>The parsed root keyword node.</returns>
    internal static WktKeywordNode ParseTree(string source)
    {
        return ParseTree(new WktTokenizer(source));
    }

    /// <summary>
    /// Parses the tokenizer stream into a keyword-node tree.
    /// </summary>
    /// <param name="tokenizer">The tokenizer positioned at the start of a WKT expression.</param>
    /// <returns>The parsed root keyword node.</returns>
    internal static WktKeywordNode ParseTree(WktTokenizer tokenizer)
    {
        ArgumentGuard.ThrowIfNull(tokenizer, nameof(tokenizer));

        tokenizer.NextToken();
        WktKeywordNode root = ParseKeywordNode(tokenizer);
        if (!tokenizer.IsEndOfInput)
        {
            throw new ArgumentException(
                $"Unexpected token '{tokenizer.GetTokenString()}' at line {tokenizer.LineNumber} column {tokenizer.Column} after the root WKT node.",
                nameof(tokenizer));
        }

        return root;
    }

    private static WktKeywordNode ParseKeywordNode(WktTokenizer tokenizer)
    {
        if (tokenizer.GetTokenType() != TokenType.Word)
        {
            throw new ArgumentException(
                $"Expected a WKT keyword at line {tokenizer.LineNumber} column {tokenizer.Column}, but found '{tokenizer.GetTokenString()}'.",
                nameof(tokenizer));
        }

        string keyword = tokenizer.GetStringValue();
        tokenizer.NextToken();
        return ParseKeywordNodeAfterKeyword(tokenizer, keyword);
    }

    private static WktKeywordNode ParseKeywordNodeAfterKeyword(WktTokenizer tokenizer, string keyword)
    {
        WktBracket bracket = GetCurrentOpener(tokenizer);
        var children = new List<WktNode>();
        tokenizer.NextToken();

        while (!IsCloser(tokenizer, bracket))
        {
            if (tokenizer.IsEndOfInput)
            {
                throw new ArgumentException(
                    $"Unexpected end of input while parsing '{keyword}' at line {tokenizer.LineNumber} column {tokenizer.Column}.",
                    nameof(tokenizer));
            }

            children.Add(ParseNodeAndAdvance(tokenizer));
            if (IsComma(tokenizer))
            {
                tokenizer.NextToken();
                continue;
            }

            tokenizer.CheckCloser(bracket);
        }

        var node = new WktKeywordNode(keyword, children);
        tokenizer.NextToken();
        return node;
    }

    private static WktNode ParseNodeAndAdvance(WktTokenizer tokenizer)
    {
        switch (tokenizer.GetTokenType())
        {
            case TokenType.Symbol when tokenizer.GetTokenString() == "\"":
                string quotedValue = tokenizer.ReadDoubleQuotedWord();
                tokenizer.NextToken();
                return new WktQuotedString(quotedValue);

            case TokenType.Number:
                WktNode numericNode = CreateNumericNode(tokenizer);
                tokenizer.NextToken();
                return numericNode;

            case TokenType.Word:
                string word = tokenizer.GetStringValue();
                tokenizer.NextToken();
                return IsOpener(tokenizer)
                    ? ParseKeywordNodeAfterKeyword(tokenizer, word)
                    : new WktIdentifier(word);

            default:
                throw new ArgumentException(
                    $"Unexpected token '{tokenizer.GetTokenString()}' at line {tokenizer.LineNumber} column {tokenizer.Column} while parsing WKT.",
                    nameof(tokenizer));
        }
    }

    private static WktNode CreateNumericNode(WktTokenizer tokenizer)
    {
        string token = tokenizer.GetTokenString();
        if (token.IndexOfAny(['.', 'e', 'E']) < 0 &&
            int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int integerValue))
        {
            return new WktInteger(integerValue);
        }

        return new WktNumber(tokenizer.GetNumericValue());
    }

    private static bool IsComma(WktTokenizer tokenizer)
    {
        return tokenizer.GetTokenType() == TokenType.Symbol && tokenizer.GetTokenString() == ",";
    }

    private static bool IsOpener(WktTokenizer tokenizer)
    {
        return tokenizer.GetTokenType() == TokenType.Symbol &&
            (tokenizer.GetTokenString() == "[" || tokenizer.GetTokenString() == "(");
    }

    private static bool IsCloser(WktTokenizer tokenizer, WktBracket bracket)
    {
        return tokenizer.GetTokenType() == TokenType.Symbol &&
            ((bracket == WktBracket.Square && tokenizer.GetTokenString() == "]") ||
             (bracket == WktBracket.Round && tokenizer.GetTokenString() == ")"));
    }

    private static WktBracket GetCurrentOpener(WktTokenizer tokenizer)
    {
        if (tokenizer.GetTokenType() != TokenType.Symbol)
        {
            throw new ArgumentException(
                $"Expected an opening bracket after a WKT keyword at line {tokenizer.LineNumber} column {tokenizer.Column}, but found '{tokenizer.GetTokenString()}'.",
                nameof(tokenizer));
        }

        return tokenizer.GetTokenString() switch
        {
            "[" => WktBracket.Square,
            "(" => WktBracket.Round,
            _ => throw new ArgumentException(
                $"Expected an opening bracket after a WKT keyword at line {tokenizer.LineNumber} column {tokenizer.Column}, but found '{tokenizer.GetTokenString()}'.",
                nameof(tokenizer)),
        };
    }
}
