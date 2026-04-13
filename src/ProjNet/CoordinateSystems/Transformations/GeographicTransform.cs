// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// The GeographicTransform class is implemented on geographic transformation objects and
/// implements datum transformations between geographic coordinate systems.
/// </summary>
/// <remarks>
/// When the source and target geographic coordinate systems share the same
/// horizontal datum, this transform represents only the prime-meridian
/// conversion between them. It adjusts the longitude ordinate by removing the
/// source prime-meridian offset and applying the target prime-meridian offset,
/// while leaving latitude and height unchanged.
/// </remarks>
/// <seealso href="https://proj.org/en/stable/glossary.html">PROJ glossary: ballpark transformation.</seealso>
public sealed class GeographicTransform : MathTransform
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
    /// Gets the source geographic coordinate system for the transformation.
    /// </summary>
    public GeographicCoordinateSystem SourceGCS { get; }

    /// <summary>
    /// Gets the target geographic coordinate system for the transformation.
    /// </summary>
    public GeographicCoordinateSystem TargetGCS { get; }

    /// <inheritdoc />
    public override string WKT => base.WKT;

    /// <inheritdoc />
    public override string XML => base.XML;

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
    /// <remarks>
    /// This transform applies only the prime-meridian longitude shift between
    /// the two geographic coordinate systems. The longitude is first normalized
    /// with the source angular unit and then restored using the target prime-
    /// meridian longitude, while unit conversion itself remains the caller's
    /// responsibility in the broader transformation chain.
    /// </remarks>
    public override MathTransform Inverse() => new GeographicTransform(this.TargetGCS, this.SourceGCS);

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
    public override void Invert() => throw new NotSupportedException("GeographicTransform is immutable. Use Inverse() to obtain inverted transform.");
}
