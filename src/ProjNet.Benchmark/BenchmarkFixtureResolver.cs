// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System;
using System.IO;

/// <summary>
/// Resolves repository-local benchmark fixtures from BenchmarkDotNet child-process output directories.
/// </summary>
internal static class BenchmarkFixtureResolver
{
    /// <summary>
    /// Resolves a test grid fixture path under <c>test\ProjNet.Tests\Fixtures\grids</c>.
    /// </summary>
    /// <param name="fileName">Fixture file name.</param>
    /// <returns>The absolute grid fixture path.</returns>
    internal static string ResolveGridPath(string fileName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "test", "ProjNet.Tests", "Fixtures", "grids", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException("Could not locate local test grid fixture under test\\ProjNet.Tests\\Fixtures\\grids.", fileName);
    }
}
