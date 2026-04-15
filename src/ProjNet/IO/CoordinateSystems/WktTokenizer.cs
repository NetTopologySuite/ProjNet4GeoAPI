// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from GeoTools.NET.

namespace ProjNet.IO.CoordinateSystems;

using System;
using System.Globalization;
using System.IO;
using System.Text;
using ProjNet.IO.Wkt;

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
        this.source = ArgumentGuard.ThrowIfNull(source, nameof(source));
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
    {
        this.source = ArgumentGuard.ThrowIfNull(reader, nameof(reader)).ReadToEnd();
        this.ignoreWhitespaceByDefault = ignoreWhitespaceByDefault;
    }

    /// <summary>
    /// Gets the token type of the current token.
    /// </summary>
    internal TokenType TokenType => this.tokenType;

    /// <summary>
    /// Gets the buffered WKT source string.
    /// </summary>
    internal string Source => this.source;

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
    /// Gets the zero-based start index of the current token within the buffered source string.
    /// </summary>
    internal int TokenStartIndex => this.tokenStartIndex;

    /// <summary>
    /// Gets the length of the current token.
    /// </summary>
    internal int TokenLength => this.tokenLength;

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
    internal string GetStringValue()
    {
        return this.GetTokenString();
    }

    /// <summary>
    /// Gets the token type of the current token.
    /// </summary>
    /// <returns>The current <see cref="TokenType"/>.</returns>
    internal TokenType GetTokenType()
    {
        return this.tokenType;
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
    /// <exception cref="WktParseException">
    /// Thrown when the current token is not a valid numeric token.
    /// </exception>
    internal double GetNumericValue()
    {
        if (this.tokenType != TokenType.Number)
        {
            throw new WktParseException($"The token '{this.GetTokenString()}' is not a number at line {this.LineNumber} column {this.Column}.");
        }

        return this.TryGetNumericValue(out double value)
            ? value
            : throw new WktParseException($"The token '{this.GetTokenString()}' is not a valid number at line {this.LineNumber} column {this.Column}.");
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
    /// Tries to parse the current token as a 32-bit integer.
    /// </summary>
    /// <param name="value">The parsed integer value, when successful.</param>
    /// <returns>
    /// <see langword="true"/> when the current token is numeric and parsing as an integer succeeded;
    /// otherwise <see langword="false"/>.
    /// </returns>
    internal bool TryGetInt32Value(out int value)
    {
        if (this.tokenType != TokenType.Number)
        {
            value = default;
            return false;
        }

#if NETSTANDARD2_0
        return int.TryParse(
            this.GetTokenString(),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out value);
#else
        return int.TryParse(
            this.GetTokenSpan(),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out value);
#endif
    }

    /// <summary>
    /// Determines whether the current token is the specified symbol.
    /// </summary>
    /// <param name="symbol">Expected symbol character.</param>
    /// <returns><see langword="true"/> when the current token is the requested symbol; otherwise <see langword="false"/>.</returns>
    internal bool IsCurrentSymbol(char symbol)
    {
        return this.IsCurrentSymbolCore(symbol);
    }

    /// <summary>
    /// Reads a token and verifies that it matches the expected token text.
    /// </summary>
    /// <param name="expectedToken">Expected token text.</param>
    /// <exception cref="WktParseException">Thrown when the token does not match.</exception>
    internal void ReadToken(string expectedToken)
    {
        this.NextToken();
        if (!this.IsCurrentToken(expectedToken.AsSpan()))
        {
            throw new WktParseException(
                $"Expecting ('{expectedToken}') but got a '{this.GetTokenString()}' at line {this.LineNumber} column {this.Column}.");
        }
    }

    /// <summary>
    /// Reads a string value enclosed in double quotes.
    /// </summary>
    /// <returns>The unquoted string value.</returns>
    /// <exception cref="WktParseException">Thrown when the quoted value is not terminated.</exception>
    internal string ReadDoubleQuotedWord()
    {
        if (!this.IsCurrentSymbol('"'))
        {
            this.ReadToken("\"");
        }

        var builder = new StringBuilder();
        this.NextToken(false);

        while (true)
        {
            if (this.tokenType == TokenType.Eof)
            {
                throw new WktParseException(
                    $"Unterminated quoted string at line {this.LineNumber} column {this.Column}.");
            }

            if (this.IsCurrentSymbol('"'))
            {
                if (this.index < this.source.Length && this.source[this.index] == '"')
                {
                    this.NextToken(false);
                    builder.Append('"');
                    this.NextToken(false);
                    continue;
                }

                return builder.ToString();
            }

            builder.Append(this.GetTokenString());
            this.NextToken(false);
        }
    }

    /// <summary>
    /// Reads a double-quoted token and returns the raw content range without materializing the unescaped string.
    /// </summary>
    /// <returns>The start index and length of the content inside the surrounding double quotes.</returns>
    /// <exception cref="WktParseException">Thrown when the quoted value is not terminated.</exception>
    internal (int ContentStartIndex, int ContentLength) ReadDoubleQuotedContentRange()
    {
        if (!this.IsCurrentSymbol('"'))
        {
            this.ReadToken("\"");
        }

        int contentStartIndex = this.index;
        this.NextToken(false);

        while (true)
        {
            if (this.tokenType == TokenType.Eof)
            {
                throw new WktParseException(
                    $"Unterminated quoted string at line {this.LineNumber} column {this.Column}.");
            }

            if (this.IsCurrentSymbol('"'))
            {
                if (this.index < this.source.Length && this.source[this.index] == '"')
                {
                    this.NextToken(false);
                    this.NextToken(false);
                    continue;
                }

                return (contentStartIndex, this.tokenStartIndex - contentStartIndex);
            }

            this.NextToken(false);
        }
    }

    /// <summary>
    /// Reads an opening bracket token.
    /// </summary>
    /// <param name="expectedBracket">Expected opening bracket type.</param>
    /// <returns>The encountered bracket type.</returns>
    /// <exception cref="WktParseException">Thrown when the bracket does not match.</exception>
    internal WktBracket ReadOpener(WktBracket expectedBracket = WktBracket.DontCare)
    {
        this.NextToken();
        if (this.IsCurrentSymbol('['))
        {
            if (expectedBracket == WktBracket.Square || expectedBracket == WktBracket.DontCare)
            {
                return WktBracket.Square;
            }
        }
        else if (this.IsCurrentSymbol('('))
        {
            if (expectedBracket == WktBracket.Round || expectedBracket == WktBracket.DontCare)
            {
                return WktBracket.Round;
            }
        }

        string expectedToken = expectedBracket == WktBracket.Square ? "[" : "(";
        throw new WktParseException(
            $"Expecting ('{expectedToken}') but got a '{this.GetTokenString()}' at line {this.LineNumber} column {this.Column}.");
    }

    /// <summary>
    /// Reads and validates a closing bracket token.
    /// </summary>
    /// <param name="expectedBracket">Expected closing bracket type.</param>
    internal void ReadCloser(WktBracket expectedBracket)
    {
        this.NextToken();
        this.CheckCloser(expectedBracket);
    }

    /// <summary>
    /// Validates that the current token is a matching closing bracket token.
    /// </summary>
    /// <param name="expectedBracket">Expected closing bracket type.</param>
    /// <exception cref="WktParseException">Thrown when the bracket does not match.</exception>
    internal void CheckCloser(WktBracket expectedBracket)
    {
        if (this.IsCurrentSymbol(']'))
        {
            if (expectedBracket == WktBracket.Square || expectedBracket == WktBracket.DontCare)
            {
                return;
            }
        }
        else if (this.IsCurrentSymbol(')'))
        {
            if (expectedBracket == WktBracket.Round || expectedBracket == WktBracket.DontCare)
            {
                return;
            }
        }

        string expectedToken = expectedBracket == WktBracket.Square ? "]" : ")";
        throw new WktParseException(
            $"Expecting ('{expectedToken}') but got a '{this.GetTokenString()}' at line {this.LineNumber} column {this.Column}.");
    }

    /// <summary>
    /// Reads an AUTHORITY token block.
    /// </summary>
    /// <param name="authority">Parsed authority name.</param>
    /// <param name="authorityCode">Parsed authority code.</param>
    internal void ReadAuthority(out string authority, out long authorityCode)
    {
        this.ReadAuthority(out authority, out authorityCode, out _);
    }

    /// <summary>
    /// Reads an AUTHORITY token block and reports whether the authority code token was numeric.
    /// </summary>
    /// <param name="authority">Parsed authority name.</param>
    /// <param name="authorityCode">Parsed authority code.</param>
    /// <param name="hasNumericAuthorityCode">
    /// <see langword="true"/> when the authority code token was numeric or parseable as integer; otherwise <see langword="false"/>.
    /// </param>
    internal void ReadAuthority(out string authority, out long authorityCode, out bool hasNumericAuthorityCode)
    {
        if (!this.IsCurrentToken("AUTHORITY".AsSpan()))
        {
            this.ReadToken("AUTHORITY");
        }

        WktBracket bracket = this.ReadOpener();
        authority = this.ReadDoubleQuotedWord();
        this.ReadToken(",");
        this.NextToken();

        if (this.tokenType == TokenType.Number)
        {
            authorityCode = (long)this.GetNumericValue();
            hasNumericAuthorityCode = true;
        }
        else
        {
            hasNumericAuthorityCode = long.TryParse(
                this.ReadDoubleQuotedWord(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out authorityCode);
        }

        this.ReadCloser(bracket);
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

    private bool IsCurrentSymbolCore(char symbol)
    {
        return this.tokenType == TokenType.Symbol &&
            this.tokenLength == 1 &&
            this.source[this.tokenStartIndex] == symbol;
    }

    private bool IsCurrentToken(ReadOnlySpan<char> expectedToken)
    {
        return this.GetTokenSpan().SequenceEqual(expectedToken);
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
