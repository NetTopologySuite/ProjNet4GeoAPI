// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// The GeographicTransform class is implemented on geographic transformation objects and
/// implements datum transformations between geographic coordinate systems.
/// </summary>
[Serializable]
public class GeographicTransform : MathTransform
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeographicTransform"/> class.
    /// </summary>
    /// <param name="sourceGCS">Source geographic coordinate system.</param>
    /// <param name="targetGCS">Target geographic coordinate system.</param>
    internal GeographicTransform(GeographicCoordinateSystem sourceGCS, GeographicCoordinateSystem targetGCS)
    {
        this.SourceGCS = sourceGCS;
        this.TargetGCS = targetGCS;
    }

    /// <summary>
    /// Gets or sets the source geographic coordinate system for the transformation.
    /// </summary>
    public GeographicCoordinateSystem SourceGCS { get; set; }

    /// <summary>
    /// Gets or sets the target geographic coordinate system for the transformation.
    /// </summary>
    public GeographicCoordinateSystem TargetGCS { get; set; }

    /// <summary>
    /// Gets the Well-known text for this object
    /// as defined in the simple features specification. [NOT IMPLEMENTED].
    /// </summary>
    public override string WKT
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Gets an XML representation of this object [NOT IMPLEMENTED].
    /// </summary>
    public override string XML => throw new NotImplementedException();

    /// <summary>
    /// Gets the dimension of input points.
    /// </summary>
    public override int DimSource => this.SourceGCS.Dimension;

    /// <summary>
    /// Gets the dimension of output points.
    /// </summary>
    public override int DimTarget => this.TargetGCS.Dimension;

    /// <summary>
    /// Creates the inverse transform of this object.
    /// </summary>
    /// <returns>A <see cref="MathTransform"/> that reverses this geographic transformation.</returns>
    /// <remarks>This method may fail if the transform is not one to one. However, all cartographic projections should succeed.</remarks>
    public override MathTransform Inverse()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public sealed override void Transform(ref double x, ref double y, ref double z)
    {
        x /= this.SourceGCS.AngularUnit.RadiansPerUnit;
        x -= this.SourceGCS.PrimeMeridian.Longitude / this.SourceGCS.PrimeMeridian.AngularUnit.RadiansPerUnit;
        x += this.TargetGCS.PrimeMeridian.Longitude / this.TargetGCS.PrimeMeridian.AngularUnit.RadiansPerUnit;
        x *= this.SourceGCS.AngularUnit.RadiansPerUnit;
    }

    /// <summary>
    /// Reverses the transformation.
    /// </summary>
    public override void Invert()
    {
        throw new NotImplementedException();
    }
}
