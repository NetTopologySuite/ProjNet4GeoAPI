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
/// Reads a stream of Well Known Text (wkt) string and returns a stream of tokens.
/// </summary>
internal class WktStreamTokenizer : StreamTokenizer
{
    private readonly NumberFormatInfo nfi = CultureInfo.InvariantCulture.NumberFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="WktStreamTokenizer"/> class.
    /// </summary>
    /// <remarks>Whitespace is ignored by default; internal whitespace inside quoted strings is preserved.</remarks>
    /// <param name="reader">A <see cref="TextReader"/> providing the WKT character stream.</param>
    public WktStreamTokenizer(TextReader reader)
        : base(reader, true)
    {
    }

    /// <summary>
    /// Reads a token and checks it is what is expected.
    /// </summary>
    /// <param name="expectedToken">The expected token.</param>
    internal void ReadToken(string expectedToken)
    {
        this.NextToken();
        if (this.GetStringValue() != expectedToken)
        {
            string s = string.Format(this.nfi, "Expecting ('{3}') but got a '{0}' at line {1} column {2}.", this.GetStringValue(), this.LineNumber, this.Column, expectedToken);
            throw new ArgumentException(s);
        }
    }

    /// <summary>
    /// Reads a string inside double quotes.
    /// </summary>
    /// <remarks>
    /// White space inside quotes is preserved.
    /// </remarks>
    /// <returns>The string inside the double quotes.</returns>
    public string ReadDoubleQuotedWord()
    {
        if (this.GetStringValue() != "\"")
        {
            this.ReadToken("\"");
        }

        var wordBuilder = new StringBuilder();
        this.NextToken(false);
        while (this.GetStringValue() != "\"")
        {
            wordBuilder.Append(this.GetStringValue());
            this.NextToken(false);
        }

        return wordBuilder.ToString();
    }

    /// <summary>
    /// Reads an opener.
    /// </summary>
    /// <param name="expectedBracket">The expected bracket type.</param>
    /// <returns>The bracket type encountered.</returns>
    public WktBracket ReadOpener(WktBracket expectedBracket = WktBracket.DontCare)
    {
        this.NextToken();
        string stringValue = this.GetStringValue();
        if (stringValue == "[")
        {
            if (expectedBracket == WktBracket.Square || expectedBracket == WktBracket.DontCare)
            {
                return WktBracket.Square;
            }
        }
        else if (stringValue == "(")
        {
            if (expectedBracket == WktBracket.Round || expectedBracket == WktBracket.DontCare)
            {
                return WktBracket.Round;
            }
        }

        string expectedToken = expectedBracket == WktBracket.Square ? "[" : "(";
        string s = string.Format(this.nfi, "Expecting ('{3}') but got a '{0}' at line {1} column {2}.", stringValue, this.LineNumber, this.Column, expectedToken);
        throw new ArgumentException(s);
    }

    /// <summary>
    /// Reads a closing bracket that matches the type used by the corresponding opener.
    /// </summary>
    /// <param name="expectedBracket">The expected bracket type.</param>
    public void ReadCloser(WktBracket expectedBracket)
    {
        this.NextToken();
        this.CheckCloser(expectedBracket);
    }

    /// <summary>
    /// Checks if the current token is a closer of expected type.
    /// </summary>
    /// <param name="expectedBracket">The expected bracket type.</param>
    public void CheckCloser(WktBracket expectedBracket)
    {
        string stringValue = this.GetStringValue();
        if (stringValue == "]")
        {
            if (expectedBracket == WktBracket.Square || expectedBracket == WktBracket.DontCare)
            {
                return;
            }
        }
        else if (stringValue == ")")
        {
            if (expectedBracket == WktBracket.Round || expectedBracket == WktBracket.DontCare)
            {
                return;
            }
        }

        string expectedToken = expectedBracket == WktBracket.Square ? "]" : ")";
        string s = string.Format(this.nfi, "Expecting ('{3}') but got a '{0}' at line {1} column {2}.", stringValue, this.LineNumber, this.Column, expectedToken);
        throw new ArgumentException(s);
    }

    /// <summary>
    /// Reads the authority and authority code.
    /// </summary>
    /// <param name="authority">String to place the authority in.</param>
    /// <param name="authorityCode">String to place the authority code in.</param>
    public void ReadAuthority(out string authority, out long authorityCode)
    {
        // AUTHORITY["EPGS","9102"]]
        if (this.GetStringValue() != "AUTHORITY")
        {
            this.ReadToken("AUTHORITY");
        }

        var bracket = this.ReadOpener();
        authority = this.ReadDoubleQuotedWord();
        this.ReadToken(",");
        this.NextToken();
        if (this.GetTokenType() == TokenType.Number)
        {
            authorityCode = (long)this.GetNumericValue();
        }
        else
        {
            long.TryParse(this.ReadDoubleQuotedWord(), NumberStyles.Any, this.nfi, out authorityCode);
        }

        this.ReadCloser(bracket);
    }
}
