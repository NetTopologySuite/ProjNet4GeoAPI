// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2002 Urban Science Applications, Inc.
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from GeoTools.NET.

namespace ProjNet.IO.CoordinateSystems;

using System;
using System.Globalization;
using System.IO;
using System.Text;

/// <summary>
/// The StreamTokenizer class takes an input stream and parses it into "tokens", allowing the tokens to be read one at a time. The parsing process is controlled by a table and a number of flags that can be set to various states. The stream tokenizer can recognize identifiers, numbers, quoted strings, and various comment style.
/// </summary>
/// <remarks>
/// This is a crude C# port of the Java <c>StreamTokenizer</c> class.
/// </remarks>
internal class StreamTokenizer
{
    private readonly NumberFormatInfo nfi = CultureInfo.InvariantCulture.NumberFormat;
    private readonly TextReader reader;
    private readonly bool ignoreWhitespace;

    private TokenType currentTokenType;
    private string currentToken;

    private int lineNumber = 1;
    private int colNumber = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamTokenizer"/> class.
    /// </summary>
    /// <param name="reader">A TextReader with some text to read.</param>
    /// <param name="ignoreWhitespace">Flag indicating whether whitespace should be ignored.</param>
    public StreamTokenizer(TextReader reader, bool ignoreWhitespace)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        this.reader = reader;
        this.ignoreWhitespace = ignoreWhitespace;
    }

    /// <summary>
    /// Gets the current line number of the stream being read.
    /// </summary>
    public int LineNumber => this.lineNumber;

    /// <summary>
    /// Gets the current column number of the stream being read.
    /// </summary>
    public int Column => this.colNumber;

    /// <summary>
    /// Gets a value indicating whether whitespace tokens are skipped by <see cref="NextToken()"/>.
    /// </summary>
    public bool IgnoreWhitespace => this.ignoreWhitespace;

    /// <summary>
    /// Parses and returns the current token as a double-precision floating-point number.
    /// </summary>
    /// <remarks>
    /// The current token must be of type <see cref="TokenType.Number"/>; otherwise an <see cref="ArgumentException"/> is thrown.
    /// </remarks>
    /// <returns>The numeric value of the current token.</returns>
    /// <exception cref="FormatException">Current token is not a number in a valid format.</exception>
    public double GetNumericValue()
    {
        string number = this.GetStringValue();
        if (this.GetTokenType() == TokenType.Number)
        {
            return double.Parse(number, this.nfi);
        }

        string s = string.Format(
            this.nfi,
            "The token '{0}' is not a number at line {1} column {2}.",
            number,
            this.LineNumber,
            this.Column);
        throw new ArgumentException(s);
    }

    /// <summary>
    /// Returns the text content of the current token.
    /// </summary>
    /// <returns>The string value of the current token.</returns>
    public string GetStringValue() => this.currentToken;

    /// <summary>
    /// Gets the token type of the current token.
    /// </summary>
    /// <returns>The <see cref="TokenType"/> of the current token.</returns>
    public TokenType GetTokenType() => this.currentTokenType;

    /// <summary>
    /// Returns the next token.
    /// </summary>
    /// <param name="ignoreWhitespace">Determines is whitespace is ignored. True if whitespace is to be ignored.</param>
    /// <returns>The TokenType of the next token.</returns>
    public TokenType NextToken(bool ignoreWhitespace) => ignoreWhitespace ? this.NextNonWhitespaceToken() : this.NextTokenAny();

    /// <summary>
    /// Returns the next token.
    /// </summary>
    /// <returns>The TokenType of the next token.</returns>
    public TokenType NextToken() => this.NextToken(this.IgnoreWhitespace);

    private TokenType NextTokenAny()
    {
        var tokenBuilder = new StringBuilder();
        this.currentToken = string.Empty;
        this.currentTokenType = TokenType.Eof;
        int finished = this.reader.Read();

        bool isNumber = false;
        bool isWord = false;

        while (finished != -1)
        {
            char currentCharacter = (char)finished;
            char nextCharacter = (char)this.reader.Peek();
            this.currentTokenType = GetType(currentCharacter);
            var nextTokenType = GetType(nextCharacter);

            // handling of words with _
            if (isWord && currentCharacter == '_')
            {
                this.currentTokenType = TokenType.Word;
            }

            // handing of words ending in numbers
            if (isWord && this.currentTokenType == TokenType.Number)
            {
                this.currentTokenType = TokenType.Word;
            }

            if (!isNumber)
            {
                if (this.currentTokenType == TokenType.Word && nextCharacter == '_')
                {
                    // enable words with _ inbetween
                    nextTokenType = TokenType.Word;
                    isWord = true;
                }

                if (this.currentTokenType == TokenType.Word && nextTokenType == TokenType.Number)
                {
                    // enable words ending with numbers
                    nextTokenType = TokenType.Word;
                    isWord = true;
                }
            }

            // handle negative numbers
            if (currentCharacter == '-' && nextTokenType == TokenType.Number && isNumber == false)
            {
                this.currentTokenType = TokenType.Number;
                nextTokenType = TokenType.Number;
            }

            // this handles numbers with a decimal point
            if (isNumber && nextTokenType == TokenType.Number && currentCharacter == '.')
            {
                this.currentTokenType = TokenType.Number;
            }

            if (this.currentTokenType == TokenType.Number && nextCharacter == '.' && isNumber == false)
            {
                nextTokenType = TokenType.Number;
                isNumber = true;
            }

            // this handles numbers with a scientific notation
            if (isNumber)
            {
                if (this.currentTokenType == TokenType.Number && nextCharacter == 'E')
                {
                    nextTokenType = TokenType.Number;
                }

                if (currentCharacter == 'E' && (nextCharacter == '-' || nextCharacter == '+'))
                {
                    this.currentTokenType = TokenType.Number;
                    nextTokenType = TokenType.Number;
                }

                if ((currentCharacter == 'E' || currentCharacter == '-' || currentCharacter == '+') && nextTokenType == TokenType.Number)
                {
                    this.currentTokenType = TokenType.Number;
                }
            }

            this.colNumber++;
            if (this.currentTokenType == TokenType.Eol)
            {
                this.lineNumber++;
                this.colNumber = 1;
            }

            tokenBuilder.Append(currentCharacter);
            if (this.currentTokenType != nextTokenType)
            {
                finished = -1;
            }
            else if (this.currentTokenType == TokenType.Symbol && currentCharacter != '-')
            {
                finished = -1;
            }
            else
            {
                finished = this.reader.Read();
            }
        }

        this.currentToken = tokenBuilder.ToString();
        return this.currentTokenType;
    }

    /// <summary>
    /// Determines a characters type (e.g. number, symbols, character).
    /// </summary>
    /// <param name="character">The character to determine.</param>
    /// <returns>The TokenType the character is.</returns>
    private static TokenType GetType(char character)
    {
        if (char.IsDigit(character))
        {
            return TokenType.Number;
        }

        if (char.IsLetter(character))
        {
            return TokenType.Word;
        }

        if (character == '\n')
        {
            return TokenType.Eol;
        }

        if (char.IsWhiteSpace(character) || char.IsControl(character))
        {
            return TokenType.Whitespace;
        }

        return TokenType.Symbol;
    }

    /// <summary>
    /// Returns the next token that is not whitespace.
    /// </summary>
    /// <returns>The <see cref="TokenType"/> of the next non-whitespace token.</returns>
    private TokenType NextNonWhitespaceToken()
    {
        var tokenType = this.NextTokenAny();
        while (tokenType == TokenType.Whitespace || tokenType == TokenType.Eol)
        {
            tokenType = this.NextTokenAny();
        }

        return tokenType;
    }
}
