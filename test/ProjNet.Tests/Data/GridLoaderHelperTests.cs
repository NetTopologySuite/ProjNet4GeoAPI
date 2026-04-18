// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Verifies the shared grid loader helper used by grid-backed math transforms.
/// </summary>
public class GridLoaderHelperTests
{
    /// <summary>
    /// Verifies the helper skips blank path entries and flattens the loaded items in input order.
    /// </summary>
    [Fact]
    public void LoadMulti_SkipsBlankPaths_AndFlattensLoadedItems()
    {
        IReadOnlyList<string> paths = ["first", string.Empty, " ", "second"];

        System.Collections.ObjectModel.ReadOnlyCollection<int> loaded = GridLoaderHelper.LoadMulti(
            paths,
            "gridPaths",
            "No grids loaded.",
            static path => path switch
            {
                "first" => [2, 1],
                "second" => [3],
                _ => Array.Empty<int>(),
            });

        Assert.Equal([2, 1, 3], loaded);
    }

    /// <summary>
    /// Verifies the helper sorts the loaded items when a comparison is supplied.
    /// </summary>
    [Fact]
    public void LoadMulti_SortsLoadedItems_WhenComparisonProvided()
    {
        IReadOnlyList<string> paths = ["first", "second"];

        System.Collections.ObjectModel.ReadOnlyCollection<int> loaded = GridLoaderHelper.LoadMulti(
            paths,
            "gridPaths",
            "No grids loaded.",
            static path => path switch
            {
                "first" => [5, 1],
                "second" => [4, 2, 3],
                _ => Array.Empty<int>(),
            },
            static (left, right) => left.CompareTo(right));

        Assert.Equal([1, 2, 3, 4, 5], loaded);
    }

    /// <summary>
    /// Verifies the helper reports an argument error when no grid items could be loaded.
    /// </summary>
    [Fact]
    public void LoadMulti_NoLoadedItems_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => GridLoaderHelper.LoadMulti<int>(
                [string.Empty, " "],
                "gridPaths",
                "No grids loaded.",
                static _ => Array.Empty<int>()));

        Assert.Equal("gridPaths", exception.ParamName);
        Assert.Contains("No grids loaded.", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies the helper rejects a <see langword="null"/> path collection.
    /// </summary>
    [Fact]
    public void LoadMulti_NullPaths_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => GridLoaderHelper.LoadMulti<int>(
                null!,
                "gridPaths",
                "No grids loaded.",
                static _ => Array.Empty<int>()));
    }
}
