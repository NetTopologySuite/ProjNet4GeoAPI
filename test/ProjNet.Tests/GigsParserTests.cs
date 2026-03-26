// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class GigsParserTests
{
    /// <summary>
    /// Gets local non-failing GIGS fixture files.
    /// </summary>
    /// <value>Fixture file entries with file name and full path.</value>
    public static IEnumerable<object[]> NonFailingFixtureFiles
    {
        get
        {
            string gigsDirectory = FindGigsDirectory();
            if (gigsDirectory is null)
            {
                yield break;
            }

            foreach (string file in Directory.GetFiles(gigsDirectory, "*.gie")
                         .Where(path => !path.EndsWith(".failing", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                yield return new object[] { Path.GetFileName(file), file };
            }
        }
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="fileName">Fixture file name.</param>
    /// <param name="filePath">Fixture file full path.</param>
    [Theory]
    [MemberData(nameof(NonFailingFixtureFiles))]
    public void ParseGigsFixtureFileProducesCases(string fileName, string filePath)
    {
        IReadOnlyList<GieCase> parsed = GieParser.ParseFile(
            filePath,
            new GieParserOptions
            {
                IgnoreUnknownDirectives = true,
                AllowOperationContinuation = true,
            });

        Assert.NotNull(parsed);
        Assert.NotEmpty(parsed);
        Assert.False(string.IsNullOrWhiteSpace(fileName));
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void ParseGigsFixturesParsesAllNonFailingFiles()
    {
        string gigsDirectory = FindGigsDirectory();
        if (gigsDirectory is null)
        {
            Assert.Skip("GIGS fixtures were not found under test\\ProjNet.Tests\\Fixtures\\gigs.");
        }

        var files = Directory.GetFiles(gigsDirectory, "*.gie")
            .Where(path => !path.EndsWith(".failing", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.NotEmpty(files);

        int totalCases = 0;
        foreach (string file in files)
        {
            IReadOnlyList<GieCase> parsed = GieParser.ParseFile(
                file,
                new GieParserOptions
                {
                    IgnoreUnknownDirectives = true,
                    AllowOperationContinuation = true,
                });

            Assert.NotNull(parsed);
            Assert.NotEmpty(parsed);
            totalCases += parsed.Count;
        }

        Assert.True(totalCases > 100, "Expected substantial GIGS coverage from parsed cases.");
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void ParseGigsFixturesPreservesPipelineOperations()
    {
        string gigsDirectory = FindGigsDirectory();
        if (gigsDirectory is null)
        {
            Assert.Skip("GIGS fixtures were not found under test\\ProjNet.Tests\\Fixtures\\gigs.");
        }

        var files = Directory.GetFiles(gigsDirectory, "*.gie")
            .Where(path => !path.EndsWith(".failing", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        int pipelineCaseCount = 0;
        foreach (string file in files)
        {
            IReadOnlyList<GieCase> parsed = GieParser.ParseFile(
                file,
                new GieParserOptions
                {
                    IgnoreUnknownDirectives = true,
                    AllowOperationContinuation = true,
                });

            pipelineCaseCount += parsed.Count(item =>
                item.Operation is not null
                && item.Operation.Contains("+proj=pipeline", StringComparison.OrdinalIgnoreCase));
        }

        Assert.True(pipelineCaseCount > 0, "Expected parsed GIGS cases to include pipeline operations.");
    }

    private static string FindGigsDirectory()
    {
        string direct = Path.Combine(AppContext.BaseDirectory, "Fixtures", "gigs");
        if (Directory.Exists(direct))
        {
            return direct;
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "test", "ProjNet.Tests", "Fixtures", "gigs");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return default!;
    }
}
