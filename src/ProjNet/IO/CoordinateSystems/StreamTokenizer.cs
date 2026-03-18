// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

// SOURCECODE IS MODIFIED FROM ANOTHER WORK AND IS ORIGINALLY BASED ON GeoTools.NET:
namespace ProjNet.IO.CoordinateSystems
{
    // SOURCECODE IS MODIFIED FROM ANOTHER WORK AND IS ORIGINALLY BASED ON GeoTools.NET:
    /*
     *  Copyright (C) 2002 Urban Science Applications, Inc.
     *
     *  This library is free software; you can redistribute it and/or
     *  modify it under the terms of the GNU Lesser General Public
     *  License as published by the Free Software Foundation; either
     *  version 2.1 of the License, or (at your option) any later version.
     *
     *  This library is distributed in the hope that it will be useful,
     *  but WITHOUT ANY WARRANTY; without even the implied warranty of
     *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
     *  Lesser General Public License for more details.
     *
     *  You should have received a copy of the GNU Lesser General Public
     *  License along with this library; if not, write to the Free Software
     *  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
     *
     */

    using System;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// The StreamTokenizer class takes an input stream and parses it into "tokens", allowing the tokens to be read one at a time. The parsing process is controlled by a table and a number of flags that can be set to various states. The stream tokenizer can recognize identifiers, numbers, quoted strings, and various comment style.
    /// </summary>
    /// <remarks>
    /// This is a crude c# implementation of Java's <a href="http://java.sun.com/products/jdk/1.2/docs/api/java/io/StreamTokenizer.html">StreamTokenizer</a> class.
    /// </remarks>
    internal class StreamTokenizer
    {
        private readonly NumberFormatInfo nfi = CultureInfo.InvariantCulture.NumberFormat;

        private TokenType currentTokenType;
        private readonly TextReader reader;
        private string currentToken;

        private int lineNumber = 1;
        private int colNumber = 1;
        private readonly bool ignoreWhitespace;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamTokenizer"/> class.
        /// </summary>
        /// <param name="reader">A TextReader with some text to read.</param>
        /// <param name="ignoreWhitespace">Flag indicating whether whitespace should be ignored.</param>
        public StreamTokenizer(TextReader reader, bool ignoreWhitespace)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            this.reader = reader;
            this.ignoreWhitespace = ignoreWhitespace;
        }

        /// <summary>
        /// Gets the current line number of the stream being read.
        /// </summary>
        public int LineNumber
        {
            get { return this.lineNumber; }
        }

        /// <summary>
        /// Gets the current column number of the stream being read.
        /// </summary>
        public int Column
        {
            get { return this.colNumber; }
        }

        public bool IgnoreWhitespace
        {
            get { return this.ignoreWhitespace; }
        }

        /// <summary>
        /// If the current token is a number, this field contains the value of that number.
        /// </summary>
        /// <remarks>
        /// If the current token is a number, this field contains the value of that number. The current token is a number when the value of the ttype field is TT_NUMBER.
        /// </remarks>
        /// <exception cref="FormatException">Current token is not a number in a valid format.</exception>
        public double GetNumericValue()
        {
            string number = this.GetStringValue();
            if (this.GetTokenType() == TokenType.Number)
            {
                return double.Parse(number, this.nfi);
            }

            string s = string.Format(this.nfi, "The token '{0}' is not a number at line {1} column {2}.",
                number, this.LineNumber, this.Column);
            throw new ArgumentException(s);
        }

        /// <summary>
        /// If the current token is a word token, this field contains a string giving the characters of the word token.
        /// </summary>
        public string GetStringValue()
        {
            return this.currentToken;
        }

        /// <summary>
        /// Gets the token type of the current token.
        /// </summary>
        /// <returns></returns>
        public TokenType GetTokenType()
        {
            return this.currentTokenType;
        }

        /// <summary>
        /// Returns the next token.
        /// </summary>
        /// <param name="ignoreWhitespace">Determines is whitespace is ignored. True if whitespace is to be ignored.</param>
        /// <returns>The TokenType of the next token.</returns>
        public TokenType NextToken(bool ignoreWhitespace)
        {
            return ignoreWhitespace ? this.NextNonWhitespaceToken() : this.NextTokenAny();
        }

        /// <summary>
        /// Returns the next token.
        /// </summary>
        /// <returns>The TokenType of the next token.</returns>
        public TokenType NextToken()
        {
            return this.NextToken(this.IgnoreWhitespace);
        }

        private TokenType NextTokenAny()
        {
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

                this.currentToken = this.currentToken + currentCharacter;
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
        /// Returns next token that is not whitespace.
        /// </summary>
        /// <returns></returns>
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
}
