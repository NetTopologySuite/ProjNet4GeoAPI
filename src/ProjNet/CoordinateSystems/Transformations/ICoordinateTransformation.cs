// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Describes a coordinate transformation. This interface only describes a
/// coordinate transformation, it does not actually perform the transform
/// operation on points. To transform points you must use a math transform.
/// </summary>
public interface ICoordinateTransformation : ICoordinateTransformationCore
{
    /// <summary>
    /// Gets human readable description of domain in source coordinate system.
    /// </summary>
    string AreaOfUse { get; }

    /// <summary>
    /// Gets authority which defined transformation and parameter values.
    /// </summary>
    /// <remarks>
    /// An Authority is an organization that maintains definitions of Authority Codes. For example the European Petroleum Survey Group (EPSG) maintains a database of coordinate systems, and other spatial referencing objects, where each object has a code number ID. For example, the EPSG code for a WGS84 Lat/Lon coordinate system is �4326�.
    /// </remarks>
    string Authority { get; }

    /// <summary>
    /// Gets code used by authority to identify transformation. An empty string is used for no code.
    /// </summary>
    /// <remarks>The AuthorityCode is a compact string defined by an Authority to reference a particular spatial reference object. For example, the European Survey Group (EPSG) authority uses 32 bit integers to reference coordinate systems, so all their code strings will consist of a few digits. The EPSG code for WGS84 Lat/Lon is �4326�.</remarks>
    long AuthorityCode { get; }

    /// <summary>
    /// Gets name of transformation.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the provider-supplied remarks.
    /// </summary>
    string Remarks { get; }

    /// <summary>
    /// Gets math transform.
    /// </summary>
    MathTransform MathTransform { get; }

    /// <summary>
    /// Gets semantic type of transform. For example, a datum transformation or a coordinate conversion.
    /// </summary>
    TransformType TransformType { get; }
}
