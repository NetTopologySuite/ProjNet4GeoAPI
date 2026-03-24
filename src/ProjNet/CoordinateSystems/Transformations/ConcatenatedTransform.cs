// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a transformation that executes a sequence of coordinate transformations in order.
/// </summary>
[Serializable]
internal class ConcatenatedTransform : MathTransform, ICoordinateTransformationCore
{
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory =
            new CoordinateTransformationFactory();

    private readonly List<ICoordinateTransformationCore> coordinateTransformationList;

    /// <summary>
    /// Cached inverse transform.
    /// </summary>
    private ConcatenatedTransform inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcatenatedTransform"/> class.
    /// </summary>
    public ConcatenatedTransform()
    {
        this.coordinateTransformationList = new List<ICoordinateTransformationCore>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcatenatedTransform"/> class.
    /// </summary>
    /// <param name="transformList">Ordered sequence of coordinate transformations to concatenate.</param>
    public ConcatenatedTransform(IEnumerable<ICoordinateTransformationCore> transformList)
        : this()
    {
        if (transformList is null)
        {
            throw new ArgumentNullException(nameof(transformList));
        }

        if (transformList is ICollection<ICoordinateTransformationCore> collection)
        {
            this.coordinateTransformationList.Capacity = collection.Count;
        }

        this.coordinateTransformationList.AddRange(transformList);
    }

    /// <summary>
    /// Gets the ordered list of transformations that form this concatenated transform.
    /// </summary>
    public IList<ICoordinateTransformationCore> CoordinateTransformationList => this.coordinateTransformationList;

    /// <inheritdoc/>
    public override int DimSource => this.coordinateTransformationList[0].SourceCS.Dimension;

    /// <inheritdoc/>
    public override int DimTarget => this.coordinateTransformationList[this.coordinateTransformationList.Count - 1].TargetCS.Dimension;

    /// <inheritdoc/>
    public CoordinateSystem SourceCS { get => this.CoordinateTransformationList[0].SourceCS; }

    /// <inheritdoc/>
    public CoordinateSystem TargetCS { get => this.CoordinateTransformationList[this.CoordinateTransformationList.Count - 1].TargetCS; }

    /// <summary>
    /// Gets a Well-Known Text representation of this object.
    /// </summary>
    /// <value>The value.</value>
    public override string WKT
    {
        get { throw new NotImplementedException(); }
    }

    /// <summary>
    /// Gets an XML representation of this object.
    /// </summary>
    /// <value>The value.</value>
    public override string XML
    {
        get { throw new NotImplementedException(); }
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        foreach (var ctc in this.coordinateTransformationList)
        {
            TransformCore(ctc, ref x, ref y, ref z);
        }
    }

    /// <summary>
    /// Returns the inverse of this conversion.
    /// </summary>
    /// <returns>A <see cref="MathTransform"/> that reverses this concatenated transform.</returns>
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = this.Clone();
            this.inverse.Invert();
        }

        return this.inverse;
    }

    /// <summary>
    /// Reverses the transformation.
    /// </summary>
    public override void Invert()
    {
        this.coordinateTransformationList.Reverse();
        foreach (var ic in this.coordinateTransformationList)
        {
            if (ic is CoordinateTransformation ct)
            {
                ct.MathTransform.Invert();
            }
            else if (ic is ConcatenatedTransform cct)
            {
                cct.Invert();
            }
        }
    }

    /// <summary>
    /// Creates a deep clone of this concatenated transform with freshly resolved sub-transformations.
    /// </summary>
    /// <returns>A new <see cref="ConcatenatedTransform"/> with cloned sub-transformations.</returns>
    public ConcatenatedTransform Clone()
    {
        var clonedList = new List<ICoordinateTransformationCore>(this.coordinateTransformationList.Count);
        foreach (var ct in this.coordinateTransformationList)
        {
            clonedList.Add(CloneCoordinateTransformation(ct));
        }

        return new ConcatenatedTransform(clonedList);
    }

    /// <inheritdoc />
    internal override void Transform(ref double x, ref double y, ref double z, ref double t)
    {
        foreach (var ctc in this.coordinateTransformationList)
        {
            TransformCore(ctc, ref x, ref y, ref z, ref t);
        }
    }

    private static ICoordinateTransformationCore CloneCoordinateTransformation(ICoordinateTransformationCore ict)
    {
        return CoordinateTransformationFactory.CreateFromCoordinateSystems(ict.SourceCS, ict.TargetCS);
    }

    private static void TransformCore(ICoordinateTransformationCore transformation, ref double x, ref double y, ref double z)
    {
        if (transformation is CoordinateTransformation coordinateTransformation)
        {
            coordinateTransformation.MathTransform.Transform(ref x, ref y, ref z);
        }
        else if (transformation is ConcatenatedTransform concatenatedTransform)
        {
            concatenatedTransform.Transform(ref x, ref y, ref z);
        }
    }

    private static void TransformCore(ICoordinateTransformationCore transformation, ref double x, ref double y, ref double z, ref double t)
    {
        if (transformation is CoordinateTransformation coordinateTransformation)
        {
            coordinateTransformation.MathTransform.Transform(ref x, ref y, ref z, ref t);
        }
        else if (transformation is ConcatenatedTransform concatenatedTransform)
        {
            concatenatedTransform.Transform(ref x, ref y, ref z, ref t);
        }
    }
}
