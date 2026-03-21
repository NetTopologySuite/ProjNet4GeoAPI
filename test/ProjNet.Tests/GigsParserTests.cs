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
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void ParseGigsFixturesParsesAllNonFailingFiles()
    {
        string gigsDirectory = FindGigsDirectory();
        if (gigsDirectory is null)
        {
            Assert.Skip("GIGS fixtures were not found under spec\\PROJ\\test\\gigs.");
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
            Assert.Skip("GIGS fixtures were not found under spec\\PROJ\\test\\gigs.");
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
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "spec", "PROJ", "test", "gigs");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }
}
