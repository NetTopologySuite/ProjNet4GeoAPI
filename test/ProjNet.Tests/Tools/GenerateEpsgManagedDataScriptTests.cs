// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.Tools;

using System;
using System.Diagnostics;
using System.IO;
using Xunit;

/// <summary>
/// Tests for the PowerShell EPSG catalog generation wrapper.
/// </summary>
public class GenerateEpsgManagedDataScriptTests
{
    /// <summary>
    /// Verifies that the wrapper surfaces a non-zero Python exit code when the generator fails after argument validation.
    /// </summary>
    [Fact]
    public void WrapperShouldSurfaceGeneratorExitCode()
    {
        string projectRoot = GetProjectRoot();
        string toolsRoot = Path.Combine(projectRoot, "tools");
        string scriptPath = Path.Combine(toolsRoot, "Generate-EpsgManagedData.ps1");
        string tempDirectory = Path.Combine(Path.GetTempPath(), "ProjNet.Tests", nameof(GenerateEpsgManagedDataScriptTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            string bogusWktZipPath = Path.Combine(tempDirectory, "bogus-wkt.zip");
            string bogusPgZipPath = Path.Combine(tempDirectory, "bogus-pg.zip");
            string outputPath = Path.Combine(tempDirectory, "generated.cs");
            File.WriteAllText(bogusWktZipPath, "not a zip archive");
            File.WriteAllText(bogusPgZipPath, "not a zip archive");

            string zipArgument = Path.GetRelativePath(toolsRoot, bogusWktZipPath);
            string pgZipArgument = Path.GetRelativePath(toolsRoot, bogusPgZipPath);
            string outputArgument = Path.GetRelativePath(toolsRoot, outputPath);

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = FormattableString.Invariant($"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{scriptPath}\" -ZipPath \"{zipArgument}\" -PgZipPath \"{pgZipArgument}\" -OutputPath \"{outputArgument}\""),
                WorkingDirectory = toolsRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            process.Start();
            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            string combinedOutput = standardOutput + Environment.NewLine + standardError;
            Assert.NotEqual(0, process.ExitCode);
            Assert.Contains("Generator failed with exit code", combinedOutput, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private static string GetProjectRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ProjNet4GeoAPI.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate repository root from test output directory.");
    }
}
