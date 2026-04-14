// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.Wkt;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ProjNet;
using ProjNet.IO.CoordinateSystems;

/// <summary>
/// Represents a WKT keyword node with children, e.g. <c>GEOGCS["WGS 84", ...]</c>.
/// </summary>
public sealed class WktKeywordNode : WktNode
{
    private readonly WktTextSlice keywordText;
    private readonly WktNode[] children;
    private string? keyword;

    /// <summary>
    /// Initializes a new instance of the <see cref="WktKeywordNode"/> class.
    /// </summary>
    /// <param name="keyword">The WKT keyword.</param>
    /// <param name="children">The child nodes.</param>
    public WktKeywordNode(string keyword, params WktNode[] children)
        : this(new WktTextSlice(ArgumentGuard.ThrowIfNull(keyword, nameof(keyword))), CopyChildren(children), keyword)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WktKeywordNode"/> class.
    /// </summary>
    /// <param name="keyword">The WKT keyword.</param>
    /// <param name="children">The child nodes.</param>
    public WktKeywordNode(string keyword, IReadOnlyList<WktNode> children)
        : this(new WktTextSlice(ArgumentGuard.ThrowIfNull(keyword, nameof(keyword))), CopyChildren(children), keyword)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WktKeywordNode"/> class from a source-backed keyword slice.
    /// </summary>
    /// <param name="source">The WKT source string.</param>
    /// <param name="keywordStart">The zero-based start index of the keyword text.</param>
    /// <param name="keywordLength">The keyword length.</param>
    /// <param name="children">The child node array to attach directly.</param>
    internal WktKeywordNode(string source, int keywordStart, int keywordLength, WktNode[] children)
        : this(new WktTextSlice(source, keywordStart, keywordLength), TakeChildren(children), keyword: null)
    {
    }

    private WktKeywordNode(WktTextSlice keywordText, WktNode[] children, string? keyword)
    {
        this.keywordText = keywordText;
        this.children = children;
        this.keyword = keyword;
    }

    /// <summary>
    /// Gets the WKT keyword, e.g. <c>GEOGCS</c>, <c>DATUM</c>.
    /// </summary>
    public string Keyword => this.keyword ??= this.keywordText.ToText();

    /// <summary>
    /// Gets the child nodes of this keyword node.
    /// </summary>
    public IReadOnlyList<WktNode> Children => this.children;

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder();
        this.AppendTo(sb);
        return sb.ToString();
    }

    /// <inheritdoc />
    public override string ToFormattedString(int indentLevel = 0, int indentSize = 4)
    {
        var sb = new StringBuilder();
        this.AppendFormattedTo(sb, indentLevel, indentSize);
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
        WktKeywordNode root = ParseKeywordNode(tokenizer, advancePastNode: true);
        if (!tokenizer.IsEndOfInput)
        {
            throw new ArgumentException(
                $"Unexpected token '{tokenizer.GetTokenString()}' at line {tokenizer.LineNumber} column {tokenizer.Column} after the root WKT node.",
                nameof(tokenizer));
        }

        return root;
    }

    /// <summary>
    /// Parses a single keyword-node subtree from the current tokenizer position.
    /// </summary>
    /// <param name="tokenizer">The tokenizer positioned on a keyword token.</param>
    /// <returns>The parsed keyword node.</returns>
    internal static WktKeywordNode ParseSubtree(WktTokenizer tokenizer)
    {
        ArgumentGuard.ThrowIfNull(tokenizer, nameof(tokenizer));
        return ParseKeywordNode(tokenizer, advancePastNode: false);
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

        for (int childIndex = 0; childIndex < this.children.Length; childIndex++)
        {
            if (this.children[childIndex] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            for (int i = 0; i < keywords.Length; i++)
            {
                if (keywordChild.KeywordEquals(keywords[i]))
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
        for (int i = 0; i < this.children.Length; i++)
        {
            if (this.children[i] is WktKeywordNode keywordChild &&
                keywordChild.KeywordEquals(keyword))
            {
                matches.Add(keywordChild);
            }
        }

        return matches.Count == 0 ? Array.Empty<WktKeywordNode>() : matches;
    }

    /// <summary>
    /// Gets all direct quoted-string child values.
    /// </summary>
    /// <returns>The quoted-string child values.</returns>
    internal IReadOnlyList<string> GetAllStrings()
    {
        var values = new List<string>();
        for (int i = 0; i < this.children.Length; i++)
        {
            if (this.children[i] is WktQuotedString quotedString)
            {
                values.Add(quotedString.Value);
            }
        }

        return values.Count == 0 ? Array.Empty<string>() : values;
    }

    /// <summary>
    /// Gets all direct numeric child values.
    /// </summary>
    /// <returns>The numeric child values.</returns>
    internal IReadOnlyList<double> GetAllNumbers()
    {
        var values = new List<double>();
        for (int i = 0; i < this.children.Length; i++)
        {
            if (IsNumericNode(this.children[i]))
            {
                values.Add(GetNumericValue(this.children[i]));
            }
        }

        return values.Count == 0 ? Array.Empty<double>() : values;
    }

    /// <summary>
    /// Gets the authority tuple from a nested <c>ID[...]</c> or <c>AUTHORITY[...]</c> child.
    /// </summary>
    /// <returns>The authority tuple when present; otherwise <see langword="null"/>.</returns>
    internal (string Authority, string Code)? GetAuthority()
    {
        WktKeywordNode? authorityNode = this.FindChild("ID", "AUTHORITY");
        if (authorityNode is null || authorityNode.children.Length < 2)
        {
            return null;
        }

        return (GetNodeText(authorityNode.children[0]), GetNodeText(authorityNode.children[1]));
    }

    /// <summary>
    /// Returns the child nodes as a span for allocation-free internal iteration.
    /// </summary>
    /// <returns>The direct child nodes.</returns>
    internal ReadOnlySpan<WktNode> GetChildrenSpan()
    {
        return this.children;
    }

    /// <summary>
    /// Appends the compact WKT representation of this node to the provided builder.
    /// </summary>
    /// <param name="builder">The target string builder.</param>
    internal override void AppendTo(StringBuilder builder)
    {
        ArgumentGuard.ThrowIfNull(builder, nameof(builder));
        this.keywordText.AppendTo(builder);
        builder.Append('[');
        for (int i = 0; i < this.children.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            this.children[i].AppendTo(builder);
        }

        builder.Append(']');
    }

    /// <summary>
    /// Appends the formatted WKT representation of this node to the provided builder.
    /// </summary>
    /// <param name="builder">The target string builder.</param>
    /// <param name="indentLevel">The current indentation level.</param>
    /// <param name="indentSize">The number of spaces per indentation level.</param>
    internal override void AppendFormattedTo(StringBuilder builder, int indentLevel, int indentSize)
    {
        ArgumentGuard.ThrowIfNull(builder, nameof(builder));
        AppendIndent(builder, indentLevel, indentSize);
        this.keywordText.AppendTo(builder);
        builder.Append('[');

        bool hasComplexChildren = HasKeywordChildren(this.children);
        if (hasComplexChildren && this.children.Length > 0)
        {
            builder.AppendLine();
            for (int i = 0; i < this.children.Length; i++)
            {
                if (this.children[i] is WktKeywordNode keywordChild)
                {
                    keywordChild.AppendFormattedTo(builder, indentLevel + 1, indentSize);
                }
                else
                {
                    AppendIndent(builder, indentLevel + 1, indentSize);
                    this.children[i].AppendTo(builder);
                }

                if (i < this.children.Length - 1)
                {
                    builder.Append(',');
                }

                builder.AppendLine();
            }

            AppendIndent(builder, indentLevel, indentSize);
        }
        else
        {
            for (int i = 0; i < this.children.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                this.children[i].AppendTo(builder);
            }
        }

        builder.Append(']');
    }

    private static WktKeywordNode ParseKeywordNode(WktTokenizer tokenizer, bool advancePastNode)
    {
        if (tokenizer.GetTokenType() != TokenType.Word)
        {
            throw new ArgumentException(
                $"Expected a WKT keyword at line {tokenizer.LineNumber} column {tokenizer.Column}, but found '{tokenizer.GetTokenString()}'.",
                nameof(tokenizer));
        }

        int keywordStart = tokenizer.TokenStartIndex;
        int keywordLength = tokenizer.TokenLength;
        string source = tokenizer.Source;
        tokenizer.NextToken();
        return ParseKeywordNodeAfterKeyword(tokenizer, source, keywordStart, keywordLength, advancePastNode);
    }

    private static WktKeywordNode ParseKeywordNodeAfterKeyword(WktTokenizer tokenizer, string source, int keywordStart, int keywordLength, bool advancePastNode)
    {
        var keywordText = new WktTextSlice(source, keywordStart, keywordLength);
        WktBracket bracket = GetCurrentOpener(tokenizer);
        WktNode[] children = Array.Empty<WktNode>();
        int childCount = 0;
        tokenizer.NextToken();

        while (!IsCloser(tokenizer, bracket))
        {
            if (tokenizer.IsEndOfInput)
            {
                throw new ArgumentException(
                    $"Unexpected end of input while parsing '{keywordText.ToText()}' at line {tokenizer.LineNumber} column {tokenizer.Column}.",
                    nameof(tokenizer));
            }

            AddChild(ref children, ref childCount, ParseNodeAndAdvance(tokenizer));
            if (IsComma(tokenizer))
            {
                tokenizer.NextToken();
                continue;
            }

            tokenizer.CheckCloser(bracket);
        }

        var node = new WktKeywordNode(source, keywordStart, keywordLength, TrimChildren(children, childCount));
        if (advancePastNode)
        {
            tokenizer.NextToken();
        }

        return node;
    }

    private static WktNode ParseNodeAndAdvance(WktTokenizer tokenizer)
    {
        switch (tokenizer.GetTokenType())
        {
            case TokenType.Symbol when tokenizer.IsCurrentSymbol('"'):
                (int quotedContentStart, int quotedContentLength) = tokenizer.ReadDoubleQuotedContentRange();
                tokenizer.NextToken();
                return new WktQuotedString(tokenizer.Source, quotedContentStart, quotedContentLength);

            case TokenType.Number:
                WktNode numericNode = CreateNumericNode(tokenizer);
                tokenizer.NextToken();
                return numericNode;

            case TokenType.Word:
                int wordStart = tokenizer.TokenStartIndex;
                int wordLength = tokenizer.TokenLength;
                string source = tokenizer.Source;
                tokenizer.NextToken();
                return IsOpener(tokenizer)
                    ? ParseKeywordNodeAfterKeyword(tokenizer, source, wordStart, wordLength, advancePastNode: true)
                    : new WktIdentifier(source, wordStart, wordLength);

            default:
                throw new ArgumentException(
                    $"Unexpected token '{tokenizer.GetTokenString()}' at line {tokenizer.LineNumber} column {tokenizer.Column} while parsing WKT.",
                    nameof(tokenizer));
        }
    }

    private static WktNode CreateNumericNode(WktTokenizer tokenizer)
    {
        if (tokenizer.TryGetInt32Value(out int integerValue))
        {
            return new WktInteger(integerValue);
        }

        return new WktNumber(tokenizer.GetNumericValue());
    }

    private static bool IsComma(WktTokenizer tokenizer)
    {
        return tokenizer.IsCurrentSymbol(',');
    }

    private static bool IsOpener(WktTokenizer tokenizer)
    {
        return tokenizer.IsCurrentSymbol('[') || tokenizer.IsCurrentSymbol('(');
    }

    private static bool IsCloser(WktTokenizer tokenizer, WktBracket bracket)
    {
        return bracket == WktBracket.Square
            ? tokenizer.IsCurrentSymbol(']')
            : tokenizer.IsCurrentSymbol(')');
    }

    private static WktBracket GetCurrentOpener(WktTokenizer tokenizer)
    {
        if (tokenizer.GetTokenType() != TokenType.Symbol)
        {
            throw new ArgumentException(
                $"Expected an opening bracket after a WKT keyword at line {tokenizer.LineNumber} column {tokenizer.Column}, but found '{tokenizer.GetTokenString()}'.",
                nameof(tokenizer));
        }

        if (tokenizer.IsCurrentSymbol('['))
        {
            return WktBracket.Square;
        }

        if (tokenizer.IsCurrentSymbol('('))
        {
            return WktBracket.Round;
        }

        throw new ArgumentException(
            $"Expected an opening bracket after a WKT keyword at line {tokenizer.LineNumber} column {tokenizer.Column}, but found '{tokenizer.GetTokenString()}'.",
            nameof(tokenizer));
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

    private static bool HasKeywordChildren(WktNode[] children)
    {
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is WktKeywordNode)
            {
                return true;
            }
        }

        return false;
    }

    private static WktNode[] CopyChildren(IReadOnlyList<WktNode>? children)
    {
        children = ArgumentGuard.ThrowIfNull(children, nameof(children));

        if (children.Count == 0)
        {
            return Array.Empty<WktNode>();
        }

        var copy = new WktNode[children.Count];
        for (int i = 0; i < children.Count; i++)
        {
            copy[i] = children[i];
        }

        return copy;
    }

    private static WktNode[] TakeChildren(WktNode[]? children)
    {
        return ArgumentGuard.ThrowIfNull(children, nameof(children));
    }

    private static void AddChild(ref WktNode[] children, ref int count, WktNode child)
    {
        if (count == children.Length)
        {
            int newLength = children.Length == 0 ? 4 : children.Length * 2;
            Array.Resize(ref children, newLength);
        }

        children[count] = child;
        count++;
    }

    private static WktNode[] TrimChildren(WktNode[] children, int count)
    {
        if (count == 0)
        {
            return Array.Empty<WktNode>();
        }

        if (count == children.Length)
        {
            return children;
        }

        var trimmedChildren = new WktNode[count];
        Array.Copy(children, trimmedChildren, count);
        return trimmedChildren;
    }

    private static void AppendIndent(StringBuilder builder, int indentLevel, int indentSize)
    {
        builder.Append(' ', indentLevel * indentSize);
    }

    private bool KeywordEquals(string value)
    {
        return this.keywordText.EqualsOrdinalIgnoreCase(value);
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
        for (int i = 0; i < this.children.Length; i++)
        {
            if (!predicate(this.children[i]))
            {
                continue;
            }

            if (currentIndex == index)
            {
                return selector(this.children[i]);
            }

            currentIndex++;
        }

        throw new ArgumentOutOfRangeException(paramName, index, $"No child with occurrence index {index} matched the requested node type.");
    }
}
