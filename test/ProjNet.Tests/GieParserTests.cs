// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

/// <summary>
/// Contains unit tests for the GieParser class.
/// </summary>
public class GieParserTests
{
    /// <summary>
    /// Verifies that parsing content with forward and inverse direction pairs produces correctly populated cases.
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

        IReadOnlyList<GieCase> parsed = GieParser.Parse(content);

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
    /// Verifies that comment lines and XML-style tags are ignored during parsing.
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

        IReadOnlyList<GieCase> parsed = GieParser.Parse(content);

        Assert.Single(parsed);
        Assert.Equal("+proj=eqearth +ellps=WGS84", parsed[0].Operation);
        Assert.Equal(10d, parsed[0].ToleranceValue, 12);
    }

    /// <summary>
    /// Verifies that an <c>expect</c> directive without a preceding <c>accept</c> directive throws a <see cref="System.FormatException"/>.
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
    /// Verifies that an unknown directive throws a <see cref="System.FormatException"/>.
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
    /// Verifies that parsing a GIE fixture written to a temporary file produces the expected cases.
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

            IReadOnlyList<GieCase> parsed = GieParser.ParseFile(filePath);

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
    /// Verifies that a backslash continuation line is appended to the preceding operation string.
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

        IReadOnlyList<GieCase> parsed = GieParser.Parse(content);

        Assert.Single(parsed);
        Assert.Contains("+lat_0=0", parsed[0].Operation, StringComparison.Ordinal);
        Assert.Contains("+lon_0=9", parsed[0].Operation, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that a failure expectation directive sets the failure flag and error code on the parsed case.
    /// </summary>
    [Fact]
    public void ParseWithFailureExpectationSetsFailureMetadata()
    {
        const string content = @"
operation +proj=aea +lat_1=900
expect failure errno invalid_op_illegal_arg_value
";

        IReadOnlyList<GieCase> parsed = GieParser.Parse(content);

        Assert.Single(parsed);
        Assert.True(parsed[0].ExpectsFailure);
        Assert.Equal("invalid_op_illegal_arg_value", parsed[0].ExpectedErrorCode);
    }

    /// <summary>
    /// Verifies that GIE numeric sentinels used in failure-expectation cases do not abort parsing.
    /// </summary>
    [Fact]
    public void ParseWithHugeValSentinelParsesFailureCase()
    {
        const string content = @"
operation +proj=defmodel +model=tests/simple_model_degree_horizontal.json
accept 2 49 30 HUGE_VAL
expect failure errno coord_transfm_missing_time
";

        IReadOnlyList<GieCase> parsed = GieParser.Parse(content);

        Assert.Single(parsed);
        Assert.True(parsed[0].ExpectsFailure);
        Assert.Equal(4, parsed[0].Accept.Length);
        Assert.True(double.IsPositiveInfinity(parsed[0].Accept[3]));
    }

    /// <summary>
    /// Verifies that a blank <c>operation</c> directive can still be represented for failure-expectation rows.
    /// </summary>
    [Fact]
    public void ParseWithBlankOperationFailureCasePreservesFollowingOperations()
    {
        const string content = @"
operation
expect failure
operation cobra
expect failure
";

        IReadOnlyList<GieCase> parsed = GieParser.Parse(content);

        Assert.Equal(2, parsed.Count);
        Assert.Equal(string.Empty, parsed[0].Operation);
        Assert.True(parsed[0].ExpectsFailure);
        Assert.Equal("cobra", parsed[1].Operation);
        Assert.True(parsed[1].ExpectsFailure);
    }

    /// <summary>
    /// Verifies that unknown directives are skipped without error when <see cref="GieParserOptions.IgnoreUnknownDirectives"/> is enabled.
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

        IReadOnlyList<GieCase> parsed = GieParser.Parse(content, new GieParserOptions { IgnoreUnknownDirectives = true });

        Assert.Single(parsed);
        Assert.False(parsed[0].ExpectsFailure);
    }

    /// <summary>
    /// Verifies that DMS coordinates with seconds but without a trailing quote preserve the seconds component.
    /// </summary>
    [Fact]
    public void ParseWithUnquotedDmsSecondsPreservesSeconds()
    {
        const string content = @"
operation +proj=latlong +datum=NAD27
accept -80d32'30.000 34d32'30.000 0.0
expect 1 2 3
";

        IReadOnlyList<GieCase> parsed = GieParser.Parse(content);

        Assert.Single(parsed);
        Assert.Equal(-80.54166666666667d, parsed[0].Accept[0], 12);
        Assert.Equal(34.54166666666667d, parsed[0].Accept[1], 12);
    }

    /// <summary>
    /// Verifies that hemisphere suffices still combine correctly with unquoted DMS seconds.
    /// </summary>
    [Fact]
    public void ParseWithUnquotedDmsSecondsAndHemispherePreservesSign()
    {
        const string content = @"
operation +proj=latlong +datum=WGS84
accept 1d2'3.5W 4d5'6.25S
expect 1 2
";

        IReadOnlyList<GieCase> parsed = GieParser.Parse(content);

        Assert.Single(parsed);
        Assert.Equal(-(1d + (2d / 60d) + (3.5d / 3600d)), parsed[0].Accept[0], 12);
        Assert.Equal(-(4d + (5d / 60d) + (6.25d / 3600d)), parsed[0].Accept[1], 12);
    }
}
