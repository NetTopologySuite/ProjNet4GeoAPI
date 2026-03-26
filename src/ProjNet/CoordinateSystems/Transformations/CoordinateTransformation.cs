// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// Describes a coordinate transformation. This class only describes a
/// coordinate transformation, it does not actually perform the transform
/// operation on points. To transform points you must use a <see cref="MathTransform"/>.
/// </summary>
[Serializable]
public class CoordinateTransformation : ICoordinateTransformation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateTransformation"/> class.
    /// </summary>
    /// <param name="sourceCS">Source coordinate system.</param>
    /// <param name="targetCS">Target coordinate system.</param>
    /// <param name="transformType">Transformation type.</param>
    /// <param name="mathTransform">Math transform.</param>
    /// <param name="name">Name of transform.</param>
    /// <param name="authority">Authority.</param>
    /// <param name="authorityCode">Authority code.</param>
    /// <param name="areaOfUse">Area of use.</param>
    /// <param name="remarks">Remarks.</param>
    internal CoordinateTransformation(
        CoordinateSystem sourceCS,
        CoordinateSystem targetCS,
        TransformType transformType,
        MathTransform mathTransform,
        string name,
        string authority,
        long authorityCode,
        string areaOfUse,
        string remarks)
    {
        this.TargetCS = targetCS;
        this.SourceCS = sourceCS;
        this.TransformType = transformType;
        this.MathTransform = mathTransform;
        this.Name = name;
        this.Authority = authority;
        this.AuthorityCode = authorityCode;
        this.AreaOfUse = areaOfUse;
        this.Remarks = remarks;
    }

    /// <summary>
    /// Gets human readable description of domain in source coordinate system.
    /// </summary>
    public string AreaOfUse { get; }

    /// <summary>
    /// Gets authority which defined transformation and parameter values.
    /// </summary>
    /// <remarks>
    /// An Authority is an organization that maintains definitions of Authority Codes. For example the European Petroleum Survey Group (EPSG) maintains a database of coordinate systems, and other spatial referencing objects, where each object has a code number ID. For example, the EPSG code for a WGS84 Lat/Lon coordinate system is �4326�.
    /// </remarks>
    public string Authority { get; }

    /// <summary>
    /// Gets code used by authority to identify transformation. An empty string is used for no code.
    /// </summary>
    /// <remarks>The AuthorityCode is a compact string defined by an Authority to reference a particular spatial reference object. For example, the European Survey Group (EPSG) authority uses 32 bit integers to reference coordinate systems, so all their code strings will consist of a few digits. The EPSG code for WGS84 Lat/Lon is �4326�.</remarks>
    public long AuthorityCode { get; }

    /// <summary>
    /// Gets math transform.
    /// </summary>
    public MathTransform MathTransform { get; }

    /// <summary>
    /// Gets name of transformation.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the provider-supplied remarks.
    /// </summary>
    public string Remarks { get; }

    /// <summary>
    /// Gets source coordinate system.
    /// </summary>
    public CoordinateSystem SourceCS { get; }

    /// <summary>
    /// Gets target coordinate system.
    /// </summary>
    public CoordinateSystem TargetCS { get; }

    /// <summary>
    /// Gets semantic type of transform. For example, a datum transformation or a coordinate conversion.
    /// </summary>
    public TransformType TransformType { get; }
}
