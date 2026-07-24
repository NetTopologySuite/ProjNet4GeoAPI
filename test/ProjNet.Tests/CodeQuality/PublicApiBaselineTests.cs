// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.IO;
using ProjNet;
using PublicApiGenerator;
using Xunit;

/// <summary>
/// Tests that verify the public API surface of the ProjNet assembly matches the committed baseline.
/// </summary>
public class PublicApiBaselineTests
{
    private const string BaselineFileName = "PublicAPI.Shipped.txt";
    private const string UpdateBaselineEnvironmentVariable = "PROJNET_UPDATE_PUBLIC_API_BASELINE";
    private static readonly string[] ExcludedPublicApiAttributes =
    [
        "System.Runtime.Versioning.TargetFrameworkAttribute",
        "System.Reflection.AssemblyMetadataAttribute",
    ];

    /// <summary>
    /// Verifies that the current public API of the ProjNet assembly matches the committed baseline file.
    /// </summary>
    [Fact]
    public void PublicApiMatchesBaseline()
    {
        string baselinePath = GetBaselinePath();
        string currentPublicApi = GenerateNormalizedPublicApi();

        if (Environment.GetEnvironmentVariable(UpdateBaselineEnvironmentVariable) == "1")
        {
            File.WriteAllText(baselinePath, currentPublicApi + Environment.NewLine);
            return;
        }

        if (!File.Exists(baselinePath))
        {
            throw new InvalidOperationException($"Public API baseline file was not found at '{baselinePath}'. Set {UpdateBaselineEnvironmentVariable}=1 and run this test to generate it.");
        }

        string baseline = NormalizeLineEndings(File.ReadAllText(baselinePath));
        Assert.Equal(baseline, currentPublicApi);
    }

    /// <summary>
    /// Verifies that multidimensional array members are normalized to their correct public API signatures.
    /// </summary>
    [Fact]
    public void GeneratedPublicApiPreservesMultiDimensionalArrayRanks()
    {
        string currentPublicApi = GenerateNormalizedPublicApi();

        Assert.Contains("public AffineTransform(double[,] matrix) { }", currentPublicApi, StringComparison.Ordinal);
        Assert.Contains("public double[,] GetMatrix() { }", currentPublicApi, StringComparison.Ordinal);
        Assert.Contains("public virtual double[,] Derivative(double[] point) { }", currentPublicApi, StringComparison.Ordinal);
    }

    private static string GetBaselinePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string solutionPath = Path.Combine(directory.FullName, "ProjNet4GeoAPI.sln");
            if (File.Exists(solutionPath))
            {
                return Path.Combine(directory.FullName, "src", "ProjNet", BaselineFileName);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate repository root from test output directory.");
    }

    private static string NormalizeLineEndings(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal).TrimEnd();
    }

    private static string NormalizeGeneratedPublicApi(string text)
    {
        return NormalizeLineEndings(text)
            .Replace("public AffineTransform(double[] matrix) { }", "public AffineTransform(double[,] matrix) { }", StringComparison.Ordinal)
            .Replace("public double[] GetMatrix() { }", "public double[,] GetMatrix() { }", StringComparison.Ordinal)
            .Replace("public virtual double[] Derivative(double[] point) { }", "public virtual double[,] Derivative(double[] point) { }", StringComparison.Ordinal);
    }

    private static string GenerateNormalizedPublicApi()
    {
        return NormalizeGeneratedPublicApi(typeof(CoordinateSystemServices).Assembly.GeneratePublicApi(new ApiGeneratorOptions
        {
            IncludeAssemblyAttributes = false,
            ExcludeAttributes = ExcludedPublicApiAttributes,
        }));
    }
}
