// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// Adjusts target Prime Meridian.
/// </summary>
/// <remarks>
/// Prime-meridian adjustment is a simple longitude translation by the angular
/// difference between the source and target prime meridians:
/// <c>x += source.Longitude - target.Longitude</c>. The inverse applies the
/// same difference with reversed sign.
/// </remarks>
/// <seealso href="https://proj.org/en/stable/usage/projections.html">PROJ usage: prime meridian and axis orientation.</seealso>
internal sealed class PrimeMeridianTransform : MathTransform
{
    private readonly PrimeMeridian source;
    private readonly PrimeMeridian target;
    private readonly bool isInverted;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrimeMeridianTransform"/> class.
    /// </summary>
    /// <param name="source">Source prime meridian.</param>
    /// <param name="target">Target prime meridian.</param>
    public PrimeMeridianTransform(PrimeMeridian source, PrimeMeridian target)
        : this(source, target, false)
    {
    }

    private PrimeMeridianTransform(PrimeMeridian source, PrimeMeridian target, bool isInverted)
    {
        if (!source.AngularUnit.EqualParams(target.AngularUnit))
        {
            throw new NotSupportedException("Prime meridian transformation requires matching angular units.");
        }

        this.source = source;
        this.target = target;
        this.isInverted = isInverted;
    }

    /// <summary>
    /// Gets a Well-Known Text representation of this prime meridian transformation.
    /// </summary>
    public override string WKT => throw new NotImplementedException("The method or operation is not implemented.");

    /// <summary>
    /// Gets an XML representation of this prime meridian transformation.
    /// </summary>
    public override string XML => throw new NotImplementedException("The method or operation is not implemented.");

    /// <summary>
    /// Gets the dimension of input points.
    /// </summary>
    public override int DimSource => 3;

    /// <summary>
    /// Gets the dimension of output points.
    /// </summary>
    public override int DimTarget => 3;

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return new PrimeMeridianTransform(this.source, this.target, !this.isInverted);
    }

    /// <inheritdoc />
    public sealed override void Transform(ref double x, ref double y, ref double z)
    {
        if (this.isInverted)
        {
            x += this.target.Longitude - this.source.Longitude;
        }
        else
        {
            x += this.source.Longitude - this.target.Longitude;
        }
    }

    /// <inheritdoc />
    protected sealed override void TransformCore(
        Span<double> xs,
        Span<double> ys,
        Span<double> zs,
        int strideX,
        int strideY,
        int strideZ)
    {
        double addend = this.isInverted
            ? this.target.Longitude - this.source.Longitude
            : this.source.Longitude - this.target.Longitude;
        AddInPlace(xs, strideX, addend);
    }

    /// <inheritdoc />
    public override void Invert()
    {
        throw new NotSupportedException("PrimeMeridianTransform is immutable. Use Inverse() to obtain inverted transform.");
    }
}
