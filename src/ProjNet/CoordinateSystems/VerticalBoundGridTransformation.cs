// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;

/// <summary>
/// Retains the WKT2 vertical <c>BOUNDCRS</c> grid-binding metadata attached to a vertical CRS source.
/// </summary>
internal sealed class VerticalBoundGridTransformation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerticalBoundGridTransformation"/> class.
    /// </summary>
    /// <param name="methodName">Transformation method name from the WKT2 abridged transformation.</param>
    /// <param name="parameterFileName">Grid file reference from the WKT2 <c>PARAMETERFILE</c> block.</param>
    /// <param name="hubCoordinateSystem">Operational hub compound coordinate system used for runtime conversion.</param>
    internal VerticalBoundGridTransformation(string methodName, string parameterFileName, CompoundCoordinateSystem hubCoordinateSystem)
    {
        this.MethodName = ArgumentGuard.ThrowIfNull(methodName, nameof(methodName));
        this.ParameterFileName = ArgumentGuard.ThrowIfNull(parameterFileName, nameof(parameterFileName));
        this.HubCoordinateSystem = ArgumentGuard.ThrowIfNull(hubCoordinateSystem, nameof(hubCoordinateSystem));
    }

    /// <summary>
    /// Gets the transformation method name from the WKT2 abridged transformation block.
    /// </summary>
    internal string MethodName { get; }

    /// <summary>
    /// Gets the referenced grid file name or path from the WKT2 <c>PARAMETERFILE</c> block.
    /// </summary>
    internal string ParameterFileName { get; }

    /// <summary>
    /// Gets the operational hub compound coordinate system used for runtime conversion.
    /// </summary>
    internal CompoundCoordinateSystem HubCoordinateSystem { get; }
}
