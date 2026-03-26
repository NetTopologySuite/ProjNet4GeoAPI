// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Semantic type of transform used in coordinate transformation.
/// </summary>
public enum TransformType : int
{
    /// <summary>
    /// Unknown or unspecified type of transform.
    /// </summary>
    Other = 0,

    /// <summary>
    /// Transform depends only on defined parameters. For example, a cartographic projection.
    /// </summary>
    Conversion = 1,

    /// <summary>
    /// Transform depends only on empirically derived parameters. For example a datum transformation.
    /// </summary>
    Transformation = 2,

    /// <summary>
    /// Transform depends on both defined and empirical parameters.
    /// </summary>
    ConversionAndTransformation = 3,
}
