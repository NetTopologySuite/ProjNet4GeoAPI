// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable
namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;

/// <summary>
/// Holds pipeline execution state shared by stack-aware runtime steps.
/// </summary>
[Serializable]
internal sealed class PipelineExecutionContext
{
    private readonly Stack<double>[] stacks =
    [
        new Stack<double>(),
        new Stack<double>(),
        new Stack<double>(),
        new Stack<double>(),
    ];

    /// <summary>
    /// Pushes an ordinate value for a coordinate component.
    /// </summary>
    /// <param name="coordinateIndex">Zero-based coordinate component index (0..3).</param>
    /// <param name="value">Value to push.</param>
    internal void Push(int coordinateIndex, double value)
    {
        this.stacks[coordinateIndex].Push(value);
    }

    /// <summary>
    /// Attempts to pop an ordinate value for a coordinate component.
    /// </summary>
    /// <param name="coordinateIndex">Zero-based coordinate component index (0..3).</param>
    /// <param name="value">Popped value when available.</param>
    /// <returns><see langword="true"/> when a value was popped; otherwise <see langword="false"/>.</returns>
    internal bool TryPop(int coordinateIndex, out double value)
    {
        Stack<double> stack = this.stacks[coordinateIndex];
        if (stack.Count == 0)
        {
            value = 0d;
            return false;
        }

        value = stack.Pop();
        return true;
    }

    /// <summary>
    /// Clears all stack contents for a new coordinate transformation run.
    /// </summary>
    internal void Clear()
    {
        for (int i = 0; i < this.stacks.Length; i++)
        {
            this.stacks[i].Clear();
        }
    }
}
