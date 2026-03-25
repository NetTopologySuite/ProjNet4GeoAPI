// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from GeoTools.NET.

namespace ProjNet.IO.CoordinateSystems;

using System;
using System.IO;

/// <summary>
/// Tokenizes a buffered Well Known Text (WKT) input stream.
/// </summary>
/// <remarks>
/// This tokenizer operates on a fully buffered source string and exposes token slices
/// as <see cref="ReadOnlySpan{T}"/> without requiring per-token <see cref="string"/>
/// builders in the scanning path.
/// </remarks>
internal sealed class WktTokenizer
{
    private readonly string source;
    private readonly bool ignoreWhitespaceByDefault;

    private int index;
    private int lineNumber = 1;
    private int column = 1;

    private int tokenStartIndex;
    private int tokenLength;
    private int tokenLine = 1;
    private int tokenColumn = 1;
    private TokenType tokenType = TokenType.Eof;

    /// <summary>
    /// Initializes a new instance of the <see cref="WktTokenizer"/> class.
    /// </summary>
    /// <param name="source">Fully buffered WKT source text.</param>
    /// <param name="ignoreWhitespaceByDefault">
    /// When <see langword="true"/>, <see cref="NextToken()"/> skips whitespace and end-of-line tokens.
    /// </param>
    internal WktTokenizer(string source, bool ignoreWhitespaceByDefault = true)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        this.source = source;
        this.ignoreWhitespaceByDefault = ignoreWhitespaceByDefault;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WktTokenizer"/> class.
    /// </summary>
    /// <param name="reader">Reader providing WKT source text.</param>
    /// <param name="ignoreWhitespaceByDefault">
    /// When <see langword="true"/>, <see cref="NextToken()"/> skips whitespace and end-of-line tokens.
    /// </param>
    internal WktTokenizer(TextReader reader, bool ignoreWhitespaceByDefault = true)
        : this(reader is null ? throw new ArgumentNullException(nameof(reader)) : reader.ReadToEnd(), ignoreWhitespaceByDefault)
    {
    }

    /// <summary>
    /// Gets the token type of the current token.
    /// </summary>
    internal TokenType TokenType => this.tokenType;

    /// <summary>
    /// Gets the one-based line number where the current token starts.
    /// </summary>
    internal int LineNumber => this.tokenLine;

    /// <summary>
    /// Gets the one-based column number where the current token starts.
    /// </summary>
    internal int Column => this.tokenColumn;

    /// <summary>
    /// Gets a value indicating whether the tokenizer reached end of input.
    /// </summary>
    internal bool IsEndOfInput => this.tokenType == TokenType.Eof;

    /// <summary>
    /// Gets the current token as a span over the buffered source text.
    /// </summary>
    /// <returns>Current token span.</returns>
    internal ReadOnlySpan<char> GetTokenSpan()
    {
        return this.source.AsSpan(this.tokenStartIndex, this.tokenLength);
    }

    /// <summary>
    /// Gets the current token as a string.
    /// </summary>
    /// <returns>The current token string value.</returns>
    internal string GetTokenString()
    {
        return this.tokenLength == 0
            ? string.Empty
            : this.source.Substring(this.tokenStartIndex, this.tokenLength);
    }

    /// <summary>
    /// Reads the next token using the default whitespace behavior.
    /// </summary>
    /// <returns>The type of the next token.</returns>
    internal TokenType NextToken()
    {
        return this.NextToken(this.ignoreWhitespaceByDefault);
    }

    /// <summary>
    /// Reads the next token.
    /// </summary>
    /// <param name="ignoreWhitespace">
    /// When <see langword="true"/>, whitespace and end-of-line tokens are skipped.
    /// </param>
    /// <returns>The type of the next token.</returns>
    internal TokenType NextToken(bool ignoreWhitespace)
    {
        while (true)
        {
            this.tokenStartIndex = this.index;
            this.tokenLine = this.lineNumber;
            this.tokenColumn = this.column;

            if (this.index >= this.source.Length)
            {
                this.tokenLength = 0;
                this.tokenType = TokenType.Eof;
                return this.tokenType;
            }

            char current = this.source[this.index];
            TokenType nextTokenType = GetCharacterType(current);

            switch (nextTokenType)
            {
                case TokenType.Word:
                    this.ConsumeWord();
                    break;
                case TokenType.Number:
                    this.ConsumeNumber();
                    break;
                case TokenType.Eol:
                    this.ConsumeEol();
                    break;
                case TokenType.Whitespace:
                    this.ConsumeWhitespace();
                    break;
                default:
                    this.ConsumeSymbol();
                    break;
            }

            this.tokenType = nextTokenType;
            if (!ignoreWhitespace || (nextTokenType != TokenType.Whitespace && nextTokenType != TokenType.Eol))
            {
                return this.tokenType;
            }
        }
    }

    private static TokenType GetCharacterType(char value)
    {
        if (value == '\r' || value == '\n')
        {
            return TokenType.Eol;
        }

        if (char.IsDigit(value))
        {
            return TokenType.Number;
        }

        if (char.IsLetter(value))
        {
            return TokenType.Word;
        }

        if (char.IsWhiteSpace(value) || char.IsControl(value))
        {
            return TokenType.Whitespace;
        }

        return TokenType.Symbol;
    }

    private void ConsumeWord()
    {
        this.AdvanceNonEolCharacter();
        while (this.index < this.source.Length)
        {
            char current = this.source[this.index];
            if (char.IsLetter(current) || char.IsDigit(current) || current == '_')
            {
                this.AdvanceNonEolCharacter();
                continue;
            }

            break;
        }

        this.tokenLength = this.index - this.tokenStartIndex;
    }

    private void ConsumeNumber()
    {
        this.AdvanceNonEolCharacter();
        while (this.index < this.source.Length && char.IsDigit(this.source[this.index]))
        {
            this.AdvanceNonEolCharacter();
        }

        this.tokenLength = this.index - this.tokenStartIndex;
    }

    private void ConsumeSymbol()
    {
        this.AdvanceNonEolCharacter();
        this.tokenLength = this.index - this.tokenStartIndex;
    }

    private void ConsumeWhitespace()
    {
        while (this.index < this.source.Length)
        {
            char current = this.source[this.index];
            if (current == '\r' || current == '\n')
            {
                break;
            }

            if (!(char.IsWhiteSpace(current) || char.IsControl(current)))
            {
                break;
            }

            this.AdvanceNonEolCharacter();
        }

        this.tokenLength = this.index - this.tokenStartIndex;
    }

    private void ConsumeEol()
    {
        if (this.source[this.index] == '\r')
        {
            this.index++;
            if (this.index < this.source.Length && this.source[this.index] == '\n')
            {
                this.index++;
            }
        }
        else
        {
            this.index++;
        }

        this.lineNumber++;
        this.column = 1;
        this.tokenLength = this.index - this.tokenStartIndex;
    }

    private void AdvanceNonEolCharacter()
    {
        this.index++;
        this.column++;
    }
}
