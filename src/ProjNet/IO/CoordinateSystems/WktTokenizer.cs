// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from GeoTools.NET.

namespace ProjNet.IO.CoordinateSystems;

using System;
using System.Globalization;
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
    /// Gets the current token parsed as a number.
    /// </summary>
    /// <returns>The current token parsed as <see cref="double"/>.</returns>
    /// <exception cref="ArgumentException">Current token is not a number token.</exception>
    /// <exception cref="FormatException">Current token text is not a valid floating-point number.</exception>
    internal double GetNumericValue()
    {
        if (this.tokenType != TokenType.Number)
        {
            throw new ArgumentException($"The token '{this.GetTokenString()}' is not a number at line {this.LineNumber} column {this.Column}.");
        }

        if (this.TryGetNumericValue(out double value))
        {
            return value;
        }

        throw new FormatException($"The token '{this.GetTokenString()}' is not a valid number at line {this.LineNumber} column {this.Column}.");
    }

    /// <summary>
    /// Tries to parse the current token as a number.
    /// </summary>
    /// <param name="value">The parsed value, when successful.</param>
    /// <returns>
    /// <see langword="true"/> when the current token is numeric and parsing succeeded;
    /// otherwise <see langword="false"/>.
    /// </returns>
    internal bool TryGetNumericValue(out double value)
    {
        if (this.tokenType != TokenType.Number)
        {
            value = default;
            return false;
        }

        ReadOnlySpan<char> tokenSpan = this.GetTokenSpan();
#if NETSTANDARD2_0
        return double.TryParse(
            tokenSpan.ToString(),
            NumberStyles.Float | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out value);
#else
        return double.TryParse(
            tokenSpan,
            NumberStyles.Float | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out value);
#endif
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
            TokenType nextTokenType;
            if (char.IsLetter(current))
            {
                nextTokenType = TokenType.Word;
                this.ConsumeWord();
            }
            else if (this.TryConsumeNumber())
            {
                nextTokenType = TokenType.Number;
            }
            else if (current == '\r' || current == '\n')
            {
                nextTokenType = TokenType.Eol;
                this.ConsumeEol();
            }
            else if (char.IsWhiteSpace(current) || char.IsControl(current))
            {
                nextTokenType = TokenType.Whitespace;
                this.ConsumeWhitespace();
            }
            else
            {
                nextTokenType = TokenType.Symbol;
                this.ConsumeSymbol();
            }

            this.tokenType = nextTokenType;
            if (!ignoreWhitespace || (nextTokenType != TokenType.Whitespace && nextTokenType != TokenType.Eol))
            {
                return this.tokenType;
            }
        }
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

    private bool TryConsumeNumber()
    {
        int scanIndex = this.index;

        if (this.source[scanIndex] == '-' || this.source[scanIndex] == '+')
        {
            if (!this.IsSignPrefixForNumber(scanIndex))
            {
                return false;
            }

            scanIndex++;
        }

        bool hasDigits = false;
        while (scanIndex < this.source.Length && char.IsDigit(this.source[scanIndex]))
        {
            scanIndex++;
            hasDigits = true;
        }

        if (scanIndex < this.source.Length && this.source[scanIndex] == '.')
        {
            scanIndex++;
            while (scanIndex < this.source.Length && char.IsDigit(this.source[scanIndex]))
            {
                scanIndex++;
                hasDigits = true;
            }
        }

        if (!hasDigits)
        {
            return false;
        }

        if (scanIndex < this.source.Length && (this.source[scanIndex] == 'E' || this.source[scanIndex] == 'e'))
        {
            int exponentIndex = scanIndex + 1;
            if (exponentIndex < this.source.Length && (this.source[exponentIndex] == '+' || this.source[exponentIndex] == '-'))
            {
                exponentIndex++;
            }

            int exponentDigitsStart = exponentIndex;
            while (exponentIndex < this.source.Length && char.IsDigit(this.source[exponentIndex]))
            {
                exponentIndex++;
            }

            if (exponentIndex > exponentDigitsStart)
            {
                scanIndex = exponentIndex;
            }
        }

        while (this.index < scanIndex)
        {
            this.AdvanceNonEolCharacter();
        }

        this.tokenLength = this.index - this.tokenStartIndex;
        return true;
    }

    private bool IsSignPrefixForNumber(int signIndex)
    {
        int nextIndex = signIndex + 1;
        if (nextIndex >= this.source.Length)
        {
            return false;
        }

        char nextCharacter = this.source[nextIndex];
        if (char.IsDigit(nextCharacter))
        {
            return true;
        }

        if (nextCharacter != '.')
        {
            return false;
        }

        int fractionalStartIndex = nextIndex + 1;
        return fractionalStartIndex < this.source.Length && char.IsDigit(this.source[fractionalStartIndex]);
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
