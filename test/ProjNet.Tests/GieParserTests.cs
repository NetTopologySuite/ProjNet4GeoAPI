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
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNET.Tests;

using System;
using System.IO;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class GieParserTests
{
    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void ParseWithForwardAndInversePairsProducesExpectedCases()
    {
        const string content = @"
<gie-strict>
operation  +proj=tmerc +lat_0=49 +lon_0=-2 +k_0=0.9996 +ellps=WGS84
tolerance  0.03 m
accept     3 80
expect     496813.178 3358297.326
direction  inverse
accept     496813.178 3358297.326
expect     3 80
";

        var parsed = GieParser.Parse(content);

        Assert.Equal(2, parsed.Count);
        Assert.Equal("+proj=tmerc +lat_0=49 +lon_0=-2 +k_0=0.9996 +ellps=WGS84", parsed[0].Operation);
        Assert.Equal(GieDirection.Forward, parsed[0].Direction);
        Assert.Equal(0.03d, parsed[0].ToleranceValue, 12);
        Assert.Equal("m", parsed[0].ToleranceUnit);
        Assert.Equal(2, parsed[0].Accept.Length);
        Assert.Equal(2, parsed[0].Expect.Length);
        Assert.Equal(GieDirection.Inverse, parsed[1].Direction);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void ParseIgnoresCommentsAndTags()
    {
        const string content = @"
<gie-strict>
# comment line
operation +proj=eqearth +ellps=WGS84
tolerance 10 m
accept 10 20 # inline comment
expect 1000 2000
";

        var parsed = GieParser.Parse(content);

        Assert.Single(parsed);
        Assert.Equal("+proj=eqearth +ellps=WGS84", parsed[0].Operation);
        Assert.Equal(10d, parsed[0].ToleranceValue, 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void ParseWithoutAcceptBeforeExpectThrowsFormatException()
    {
        const string content = @"
operation +proj=moll +ellps=WGS84
expect 1 2
";

        Assert.Throws<FormatException>(() => GieParser.Parse(content));
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void ParseUnknownDirectiveThrowsFormatException()
    {
        const string content = @"
operation +proj=aeqd +ellps=WGS84
tolerance 1 m
foobar 1 2
";

        Assert.Throws<FormatException>(() => GieParser.Parse(content));
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void ParseFileWithTemporaryInputProducesCases()
    {
        string filePath = Path.GetTempFileName();
        try
        {
            string content = "operation +proj=gnom +ellps=WGS84\n" +
                             "tolerance 0.5 m\n" +
                             "accept 7 8\n" +
                             "expect 700 800\n";
            File.WriteAllText(filePath, content);

            var parsed = GieParser.ParseFile(filePath);

            Assert.Single(parsed);
            Assert.Equal("+proj=gnom +ellps=WGS84", parsed[0].Operation);
            Assert.Equal(0.5d, parsed[0].ToleranceValue, 12);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void ParseWithContinuationLineAppendsOperation()
    {
        const string content = @"
operation +proj=tmerc +ellps=WGS84 \
          +lat_0=0 +lon_0=9
accept 1 2
expect 3 4
";

        var parsed = GieParser.Parse(content);

        Assert.Single(parsed);
        Assert.Contains("+lat_0=0", parsed[0].Operation, StringComparison.Ordinal);
        Assert.Contains("+lon_0=9", parsed[0].Operation, StringComparison.Ordinal);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void ParseWithFailureExpectationSetsFailureMetadata()
    {
        const string content = @"
operation +proj=aea +lat_1=900
expect failure errno invalid_op_illegal_arg_value
";

        var parsed = GieParser.Parse(content);

        Assert.Single(parsed);
        Assert.True(parsed[0].ExpectsFailure);
        Assert.Equal("invalid_op_illegal_arg_value", parsed[0].ExpectedErrorCode);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void ParseWithIgnoreUnknownDirectivesEnabledSkipsUnknownDirective()
    {
        const string content = @"
operation +proj=merc +ellps=WGS84
foobar this should be ignored
accept 1 2
expect 3 4
";

        var parsed = GieParser.Parse(content, new GieParserOptions { IgnoreUnknownDirectives = true });

        Assert.Single(parsed);
        Assert.False(parsed[0].ExpectsFailure);
    }
}
