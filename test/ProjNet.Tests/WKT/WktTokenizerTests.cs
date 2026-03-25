// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNET.Tests.WKT;

using ProjNet.IO.CoordinateSystems;
using System;
using System.Text;
using Xunit;

/// <summary>
/// Verifies tokenizer behavior for edge-case WKT inputs.
/// </summary>
public class WktTokenizerTests
{
    /// <summary>
    /// Verifies that empty input immediately reports end-of-file.
    /// </summary>
    [Fact]
    public void NextTokenOnEmptyInputReturnsEof()
    {
        var tokenizer = new WktTokenizer(string.Empty);

        TokenType token = tokenizer.NextToken();

        Assert.Equal(TokenType.Eof, token);
        Assert.True(tokenizer.IsEndOfInput);
        Assert.Equal(string.Empty, tokenizer.GetStringValue());
    }

    /// <summary>
    /// Verifies that whitespace and end-of-line tokens are emitted when whitespace is significant.
    /// </summary>
    [Fact]
    public void NextTokenWhenWhitespaceIsSignificantReturnsWhitespaceAndEolTokens()
    {
        var tokenizer = new WktTokenizer("A \r\nB", ignoreWhitespaceByDefault: false);

        Assert.Equal(TokenType.Word, tokenizer.NextToken());
        Assert.Equal("A", tokenizer.GetStringValue());

        Assert.Equal(TokenType.Whitespace, tokenizer.NextToken());
        Assert.Equal(TokenType.Eol, tokenizer.NextToken());

        Assert.Equal(TokenType.Word, tokenizer.NextToken());
        Assert.Equal("B", tokenizer.GetStringValue());
        Assert.Equal(2, tokenizer.LineNumber);
        Assert.Equal(1, tokenizer.Column);
    }

    /// <summary>
    /// Verifies numeric parsing for precision-boundary and signed scientific values.
    /// </summary>
    [Theory]
    [InlineData("-1.7976931348623157E+308", -1.7976931348623157E+308)]
    [InlineData("2.2250738585072014E-308", 2.2250738585072014E-308)]
    [InlineData("5.235E+4", 52350d)]
    [InlineData("-.5", -0.5d)]
    [InlineData("+.75", 0.75d)]
    [InlineData("-0.0", -0d)]
    public void GetNumericValueParsesBoundaryNumbers(string tokenText, double expected)
    {
        var tokenizer = new WktTokenizer(tokenText);

        Assert.Equal(TokenType.Number, tokenizer.NextToken());
        Assert.True(tokenizer.TryGetNumericValue(out double parsed));
        Assert.Equal(expected, parsed);
        Assert.Equal(expected, tokenizer.GetNumericValue());
    }

    /// <summary>
    /// Verifies malformed quoted input surfaces an explicit parse error.
    /// </summary>
    [Fact]
    public void ReadDoubleQuotedWordWithUnterminatedInputThrows()
    {
        var tokenizer = new WktTokenizer("\"unterminated");

        ArgumentException exception = Assert.Throws<ArgumentException>(() => tokenizer.ReadDoubleQuotedWord());
        Assert.Contains("Unterminated quoted string", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies malformed bracket closure reports the expected mismatch.
    /// </summary>
    [Fact]
    public void ReadCloserWithMismatchedBracketThrows()
    {
        var tokenizer = new WktTokenizer("(]");

        WktBracket opener = tokenizer.ReadOpener(WktBracket.Round);
        Assert.Equal(WktBracket.Round, opener);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => tokenizer.ReadCloser(WktBracket.Round));
        Assert.Contains("Expecting (')')", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies AUTHORITY parsing for both numeric and quoted authority codes.
    /// </summary>
    [Theory]
    [InlineData("AUTHORITY[\"EPSG\",4326]", "EPSG", 4326L)]
    [InlineData("AUTHORITY[\"EPSG\",\"3857\"]", "EPSG", 3857L)]
    [InlineData("AUTHORITY[\"LOCAL\",\"abc\"]", "LOCAL", 0L)]
    public void ReadAuthorityParsesNumericAndQuotedCodes(string authorityWkt, string expectedAuthority, long expectedCode)
    {
        var tokenizer = new WktTokenizer(authorityWkt);

        tokenizer.ReadAuthority(out string authority, out long authorityCode);

        Assert.Equal(expectedAuthority, authority);
        Assert.Equal(expectedCode, authorityCode);
    }

    /// <summary>
    /// Verifies deeply nested WKT-like bracket sequences are tokenized without losing bracket balance.
    /// </summary>
    [Fact]
    public void NextTokenOnDeeplyNestedInputMaintainsBracketBalance()
    {
        const int depth = 256;
        var builder = new StringBuilder("ROOT");
        for (int i = 0; i < depth; i++)
        {
            builder.Append('[');
        }

        builder.Append("\"N\"");

        for (int i = 0; i < depth; i++)
        {
            builder.Append(']');
        }

        var tokenizer = new WktTokenizer(builder.ToString());
        int openCount = 0;
        int closeCount = 0;
        while (tokenizer.NextToken() != TokenType.Eof)
        {
            string token = tokenizer.GetStringValue();
            if (token == "[")
            {
                openCount++;
            }
            else if (token == "]")
            {
                closeCount++;
            }
        }

        Assert.Equal(depth, openCount);
        Assert.Equal(depth, closeCount);
    }

    /// <summary>
    /// Verifies tokenization remains stable for large WKT strings with long quoted names.
    /// </summary>
    [Fact]
    public void ReadDoubleQuotedWordHandlesLargeInput()
    {
        const int nameLength = 32768;
        string longName = new('X', nameLength);
        string wkt = $"GEOGCS[\"{longName}\"]";
        var tokenizer = new WktTokenizer(wkt);

        Assert.Equal(TokenType.Word, tokenizer.NextToken());
        Assert.Equal("GEOGCS", tokenizer.GetStringValue());
        Assert.Equal(WktBracket.Square, tokenizer.ReadOpener());

        string parsedName = tokenizer.ReadDoubleQuotedWord();
        Assert.Equal(nameLength, parsedName.Length);
        Assert.Equal(longName, parsedName);

        tokenizer.ReadCloser(WktBracket.Square);
        Assert.Equal(TokenType.Eof, tokenizer.NextToken());
    }

    /// <summary>
    /// Verifies malformed WKT with a missing closing bracket throws for both string and span parse paths.
    /// </summary>
    [Fact]
    public void ParseMalformedWktWithMissingCloserThrowsForStringAndSpan()
    {
        const string malformedWkt = "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563]]";

        ArgumentException stringException = Assert.Throws<ArgumentException>(() => CoordinateSystemWktReader.Parse(malformedWkt));
        ArgumentException spanException = Assert.Throws<ArgumentException>(() => CoordinateSystemWktReader.Parse(malformedWkt.AsSpan()));

        Assert.Contains("Expecting", stringException.Message, StringComparison.Ordinal);
        Assert.Contains("Expecting", spanException.Message, StringComparison.Ordinal);
    }
}
