// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Describes core information of a coordinate transformation.
/// </summary>
public interface ICoordinateTransformationCore
{
    /// <summary>
    /// Gets source coordinate system.
    /// </summary>
    CoordinateSystem SourceCS { get; }

    /// <summary>
    /// Gets target coordinate system.
    /// </summary>
    CoordinateSystem TargetCS { get; }
}
