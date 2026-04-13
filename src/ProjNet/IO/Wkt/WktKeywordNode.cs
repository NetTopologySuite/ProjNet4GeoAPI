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

    /// <summary>
    /// Gets the string value of the indexed quoted-string child.
    /// </summary>
    /// <param name="index">Zero-based occurrence index among direct quoted-string children.</param>
    /// <returns>The quoted-string child value.</returns>
    internal string GetString(int index)
    {
        return this.GetLeafChild(index, static child => child is WktQuotedString, static child => ((WktQuotedString)child).Value, nameof(index));
    }

    /// <summary>
    /// Gets the numeric value of the indexed numeric child.
    /// </summary>
    /// <param name="index">Zero-based occurrence index among direct numeric children.</param>
    /// <returns>The numeric child value as a <see cref="double"/>.</returns>
    internal double GetNumber(int index)
    {
        return this.GetLeafChild(index, IsNumericNode, GetNumericValue, nameof(index));
    }

    /// <summary>
    /// Gets the identifier text of the indexed identifier child.
    /// </summary>
    /// <param name="index">Zero-based occurrence index among direct identifier children.</param>
    /// <returns>The identifier child text.</returns>
    internal string GetIdentifier(int index)
    {
        return this.GetLeafChild(index, static child => child is WktIdentifier, static child => ((WktIdentifier)child).Name, nameof(index));
    }

    /// <summary>
    /// Finds the first direct keyword child matching the requested keyword.
    /// </summary>
    /// <param name="keyword">Keyword to match.</param>
    /// <returns>The first matching child, or <see langword="null"/>.</returns>
    internal WktKeywordNode? FindChild(string keyword)
    {
        return this.FindChild([keyword]);
    }

    /// <summary>
    /// Finds the first direct keyword child matching any of the requested keywords.
    /// </summary>
    /// <param name="keywords">Keywords to match.</param>
    /// <returns>The first matching child, or <see langword="null"/>.</returns>
    internal WktKeywordNode? FindChild(params string[] keywords)
    {
        ArgumentGuard.ThrowIfNull(keywords, nameof(keywords));

        foreach (WktNode child in this.Children)
        {
            if (child is not WktKeywordNode keywordChild)
            {
                continue;
            }

            for (int i = 0; i < keywords.Length; i++)
            {
                if (string.Equals(keywordChild.Keyword, keywords[i], StringComparison.OrdinalIgnoreCase))
                {
                    return keywordChild;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Finds all direct keyword children with the requested keyword.
    /// </summary>
    /// <param name="keyword">Keyword to match.</param>
    /// <returns>The matching direct keyword children.</returns>
    internal IReadOnlyList<WktKeywordNode> FindChildren(string keyword)
    {
        ArgumentGuard.ThrowIfNull(keyword, nameof(keyword));

        var matches = new List<WktKeywordNode>();
        foreach (WktNode child in this.Children)
        {
            if (child is WktKeywordNode keywordChild &&
                string.Equals(keywordChild.Keyword, keyword, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(keywordChild);
            }
        }

        return matches;
    }

    /// <summary>
    /// Gets all direct quoted-string child values.
    /// </summary>
    /// <returns>The quoted-string child values.</returns>
    internal IReadOnlyList<string> GetAllStrings()
    {
        return this.Children
            .OfType<WktQuotedString>()
            .Select(static child => child.Value)
            .ToArray();
    }

    /// <summary>
    /// Gets all direct numeric child values.
    /// </summary>
    /// <returns>The numeric child values.</returns>
    internal IReadOnlyList<double> GetAllNumbers()
    {
        return this.Children
            .Where(IsNumericNode)
            .Select(GetNumericValue)
            .ToArray();
    }

    /// <summary>
    /// Gets the authority tuple from a nested <c>ID[...]</c> or <c>AUTHORITY[...]</c> child.
    /// </summary>
    /// <returns>The authority tuple when present; otherwise <see langword="null"/>.</returns>
    internal (string Authority, string Code)? GetAuthority()
    {
        WktKeywordNode? authorityNode = this.FindChild("ID", "AUTHORITY");
        if (authorityNode is null || authorityNode.Children.Count < 2)
        {
            return null;
        }

        return (GetNodeText(authorityNode.Children[0]), GetNodeText(authorityNode.Children[1]));
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

    private static string GetNodeText(WktNode node)
    {
        return node switch
        {
            WktQuotedString quotedString => quotedString.Value,
            WktIdentifier identifier => identifier.Name,
            WktInteger integer => integer.Value.ToString(CultureInfo.InvariantCulture),
            WktNumber number => number.Value.ToString(CultureInfo.InvariantCulture),
            _ => throw new ArgumentException($"Expected a leaf WKT value node but found '{node.GetType().Name}'.", nameof(node)),
        };
    }

    private static bool IsNumericNode(WktNode node)
    {
        return node is WktNumber or WktInteger;
    }

    private static double GetNumericValue(WktNode node)
    {
        return node switch
        {
            WktNumber number => number.Value,
            WktInteger integer => integer.Value,
            _ => throw new ArgumentException($"Expected a numeric WKT node but found '{node.GetType().Name}'.", nameof(node)),
        };
    }

    private T GetLeafChild<T>(
        int index,
        Func<WktNode, bool> predicate,
        Func<WktNode, T> selector,
        string paramName)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, index, "The occurrence index cannot be negative.");
        }

        int currentIndex = 0;
        foreach (WktNode child in this.Children)
        {
            if (!predicate(child))
            {
                continue;
            }

            if (currentIndex == index)
            {
                return selector(child);
            }

            currentIndex++;
        }

        throw new ArgumentOutOfRangeException(paramName, index, $"No child with occurrence index {index} matched the requested node type.");
    }
}
