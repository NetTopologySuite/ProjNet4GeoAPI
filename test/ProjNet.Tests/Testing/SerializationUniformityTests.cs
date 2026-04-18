// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ProjNet.CoordinateSystems.Projections;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Guards the iteration-5 serialization unification invariants against source-level regressions.
/// </summary>
public class SerializationUniformityTests
{
    private const int WindowLinesBeforeMember = 3;
    private const int WindowLinesAfterMember = 7;

    /// <summary>
    /// Verifies that WKT/XML serialization members no longer build their output with <see cref="System.Text.StringBuilder"/>.
    /// </summary>
    [Fact]
    public void WktAndXmlSerializationMembers_DoNotUseStringBuilder()
    {
        List<string> offenders = FindSerializationSourceWindows(window => window.Contains("StringBuilder", StringComparison.Ordinal));

        Assert.True(
            offenders.Count == 0,
            "WKT/XML serialization members should delegate through WktNode/XElement rather than building strings manually:" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>
    /// Verifies that serialization members no longer reference <see cref="NotImplementedException"/>.
    /// </summary>
    [Fact]
    public void SerializationMembers_DoNotReferenceNotImplementedException()
    {
        List<string> offenders = FindSerializationSourceWindows(window => window.Contains("NotImplementedException", StringComparison.Ordinal));

        Assert.True(
            offenders.Count == 0,
            "Serialization members should use NotSupportedException when a format is unsupported:" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>
    /// Verifies that any math transform with non-default serialization remains an explicitly classified special case.
    /// </summary>
    [Fact]
    public void ConcreteMathTransformSerializationShapesRemainExplicitlyClassified()
    {
        List<string> uncovered = [];

        foreach (Type type in typeof(MathTransform).Assembly.GetTypes())
        {
            if (!typeof(MathTransform).IsAssignableFrom(type) || type.IsAbstract)
            {
                continue;
            }

            if (!UsesNonDefaultSerialization(type))
            {
                continue;
            }

            if (!IsExpectedSerializationShape(type))
            {
                uncovered.Add(type.FullName ?? type.Name);
            }
        }

        Assert.True(
            uncovered.Count == 0,
            "New math transforms changed serialization behavior without an explicit test classification:" + Environment.NewLine + string.Join(Environment.NewLine, uncovered));
    }

    private static bool IsExpectedSerializationShape(Type type)
    {
        return typeof(MapProjection).IsAssignableFrom(type)
            || type == typeof(AffineTransform)
            || type == typeof(GeographicTransform)
            || type == typeof(IdentityMathTransform);
    }

    private static bool UsesNonDefaultSerialization(Type type)
    {
        return GetRequiredProperty(type, nameof(MathTransform.WKT)).GetMethod!.DeclaringType != typeof(MathTransform)
            || GetRequiredProperty(type, nameof(MathTransform.XML)).GetMethod!.DeclaringType != typeof(MathTransform)
            || GetRequiredMethod(type, nameof(MathTransform.ToWktNode), Type.EmptyTypes).DeclaringType != typeof(MathTransform)
            || GetRequiredMethod(type, nameof(MathTransform.ToXml), Type.EmptyTypes).DeclaringType != typeof(MathTransform);
    }

    private static List<string> FindSerializationSourceWindows(Func<string, bool> matches)
    {
        string repositoryRoot = GetRepositoryRoot();
        string sourceRoot = Path.Combine(repositoryRoot, "src", "ProjNet");
        List<string> offenders = [];

        foreach (string filePath in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            string[] lines = File.ReadAllLines(filePath);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                if (!IsSerializationMemberDeclaration(lines[lineIndex]))
                {
                    continue;
                }

                string window = GetWindow(lines, lineIndex);
                if (matches(window))
                {
                    offenders.Add(FormattableString.Invariant($"{Path.GetRelativePath(repositoryRoot, filePath)}:{lineIndex + 1}"));
                }
            }
        }

        return offenders;
    }

    private static string GetRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string solutionPath = Path.Combine(directory.FullName, "ProjNet4GeoAPI.sln");
            if (File.Exists(solutionPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate repository root from test output directory.");
    }

    private static string GetWindow(string[] lines, int lineIndex)
    {
        int start = Math.Max(0, lineIndex - WindowLinesBeforeMember);
        int end = Math.Min(lines.Length - 1, lineIndex + WindowLinesAfterMember);
        string[] windowLines = new string[(end - start) + 1];
        Array.Copy(lines, start, windowLines, 0, windowLines.Length);
        return string.Join("\n", windowLines);
    }

    private static bool IsSerializationMemberDeclaration(string line)
    {
        return line.Contains(" string WKT", StringComparison.Ordinal)
            || line.Contains(" string XML", StringComparison.Ordinal)
            || line.Contains(" WktNode ToWktNode(", StringComparison.Ordinal)
            || line.Contains(" XElement ToXml(", StringComparison.Ordinal);
    }

    private static MethodInfo GetRequiredMethod(Type type, string name, Type[] parameterTypes)
    {
        MethodInfo? method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance, binder: null, types: parameterTypes, modifiers: null);
        return method ?? throw new InvalidOperationException($"Method '{name}' was not found on '{type.FullName}'.");
    }

    private static PropertyInfo GetRequiredProperty(Type type, string name)
    {
        PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        return property ?? throw new InvalidOperationException($"Property '{name}' was not found on '{type.FullName}'.");
    }
}
