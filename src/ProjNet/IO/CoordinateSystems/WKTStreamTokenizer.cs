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

namespace ProjNet.IO.CoordinateSystems
{
    /// <summary>
    /// Reads a stream of Well Known Text (wkt) string and returns a stream of tokens.
    /// </summary>
    internal class WktStreamTokenizer : StreamTokenizer
    {
        private readonly NumberFormatInfo _nfi = CultureInfo.InvariantCulture.NumberFormat;

        /// <summary>
        /// Initializes a new instance of the WktStreamTokenizer class.
        /// </summary>
        /// <remarks>The WktStreamTokenizer class ais in reading WKT streams.</remarks>
        /// <param name="reader">A TextReader that contains </param>
        public WktStreamTokenizer(TextReader reader) : base(reader, true) { }

        /// <summary>
        /// Reads a token and checks it is what is expected.
        /// </summary>
        /// <param name="expectedToken">The expected token.</param>
        internal void ReadToken(string expectedToken)
        {
            NextToken();
            if (GetStringValue() != expectedToken)
            {
                string s = string.Format(_nfi, "Expecting ('{3}') but got a '{0}' at line {1} column {2}.", GetStringValue(), LineNumber, Column, expectedToken);
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
            string word = "";

            if (GetStringValue()!="\"")
                ReadToken("\"");
            NextToken(false);
            while (GetStringValue() != "\"")
            {
                word = word + GetStringValue();
                NextToken(false);
            }
            return word;
        }

        /// <summary>
        /// Reads an opener
        /// </summary>
        /// <param name="expectedBracket">The expected bracket type.</param>
        /// <returns>The bracket type encountered</returns>
        public WktBracket ReadOpener(WktBracket expectedBracket = WktBracket.DontCare)
        {
            NextToken();
            string stringValue = GetStringValue();
            if (stringValue == "[")
            {
                if (expectedBracket == WktBracket.Square || expectedBracket == WktBracket.DontCare)
                    return WktBracket.Square;
            }
            else if (stringValue == "(")
            {
                if (expectedBracket == WktBracket.Round || expectedBracket == WktBracket.DontCare)
                    return WktBracket.Round;
            }

            string expectedToken = expectedBracket == WktBracket.Square ? "[" : "(";
            string s = string.Format(_nfi, "Expecting ('{3}') but got a '{0}' at line {1} column {2}.", stringValue, LineNumber, Column, expectedToken);
            throw new ArgumentException(s);
        }

        /// <summary>
        /// Reads an closer
        /// </summary>
        /// <param name="expectedBracket">The expected bracket type.</param>
        public void ReadCloser(WktBracket expectedBracket)
        {
            NextToken();
            CheckCloser(expectedBracket);
        }

        /// <summary>
        /// Checks if the current token is a closer of expected type.
        /// </summary>
        /// <param name="expectedBracket">The expected bracket type.</param>
        public void CheckCloser(WktBracket expectedBracket)
        {
            string stringValue = GetStringValue();
            if (stringValue == "]")
            {
                if (expectedBracket == WktBracket.Square || expectedBracket == WktBracket.DontCare)
                    return;
            }
            else if (stringValue == ")")
            {
                if (expectedBracket == WktBracket.Round || expectedBracket == WktBracket.DontCare)
                    return;
            }

            string expectedToken = expectedBracket == WktBracket.Square ? "]" : ")";
            string s = string.Format(_nfi, "Expecting ('{3}') but got a '{0}' at line {1} column {2}.", stringValue, LineNumber, Column, expectedToken);
            throw new ArgumentException(s);
        }

        /// <summary>
        /// Reads the authority and authority code.
        /// </summary>
        /// <param name="authority">String to place the authority in.</param>
        /// <param name="authorityCode">String to place the authority code in.</param>
        public void ReadAuthority(out string authority, out long authorityCode)
        {
            //AUTHORITY["EPGS","9102"]]
            if (GetStringValue() != "AUTHORITY")
                ReadToken("AUTHORITY");
            var bracket = ReadOpener();
            authority = ReadDoubleQuotedWord();
            ReadToken(",");
            NextToken();
            if (GetTokenType() == TokenType.Number)
                authorityCode = (long) GetNumericValue();
            else
                long.TryParse(ReadDoubleQuotedWord(), NumberStyles.Any, _nfi, out authorityCode);
            ReadCloser(bracket);
        }
    }
}
