// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a transformation that executes a sequence of coordinate transformations in order.
/// </summary>
/// <remarks>
/// Concatenated transforms model a chained coordinate-operation pipeline over
/// resolved <see cref="ICoordinateTransformationCore"/> instances. The inverse
/// cache is intentionally cleared during in-place inversion so reversed child
/// transforms are rebuilt from the updated traversal order.
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/operations_computation.html">PROJ: computation of coordinate operations between two CRS.</seealso>
internal sealed class ConcatenatedTransform : MathTransform, ICoordinateTransformationCore
{
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory =
            new();

    private readonly List<ICoordinateTransformationCore> coordinateTransformationList;

    /// <summary>
    /// Cached inverse transform.
    /// </summary>
    private ConcatenatedTransform? inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcatenatedTransform"/> class.
    /// </summary>
    public ConcatenatedTransform()
    {
        this.coordinateTransformationList = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcatenatedTransform"/> class.
    /// </summary>
    /// <param name="transformList">Ordered sequence of coordinate transformations to concatenate.</param>
    public ConcatenatedTransform(IEnumerable<ICoordinateTransformationCore> transformList)
        : this()
    {
        transformList = ArgumentGuard.ThrowIfNull(transformList, nameof(transformList));
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
    public override int DimSource => this.GetFirstTransform().SourceCS.Dimension;

    /// <inheritdoc/>
    public override int DimTarget => this.GetLastTransform().TargetCS.Dimension;

    /// <inheritdoc/>
    public CoordinateSystem SourceCS { get => this.GetFirstTransform().SourceCS; }

    /// <inheritdoc/>
    public CoordinateSystem TargetCS { get => this.GetLastTransform().TargetCS; }

    /// <summary>
    /// Gets an XML representation of this object.
    /// </summary>
    /// <value>The value.</value>
    public override string XML => throw new NotImplementedException();

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        foreach (ICoordinateTransformationCore ctc in this.coordinateTransformationList)
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
            this.inverse = new ConcatenatedTransform(BuildInvertedCoordinateTransformations(this.coordinateTransformationList));
        }

        return this.inverse;
    }

    /// <summary>
    /// Reverses the transformation.
    /// </summary>
    public override void Invert()
    {
        this.inverse = null;
        List<ICoordinateTransformationCore> inverted = BuildInvertedCoordinateTransformations(this.coordinateTransformationList);
        this.coordinateTransformationList.Clear();
        foreach (ICoordinateTransformationCore transformation in inverted)
        {
            this.coordinateTransformationList.Add(transformation);
        }
    }

    /// <summary>
    /// Creates a deep clone of this concatenated transform with freshly resolved sub-transformations.
    /// </summary>
    /// <returns>A new <see cref="ConcatenatedTransform"/> with cloned sub-transformations.</returns>
    public ConcatenatedTransform Clone()
    {
        var clonedList = new List<ICoordinateTransformationCore>(this.coordinateTransformationList.Count);
        foreach (ICoordinateTransformationCore ct in this.coordinateTransformationList)
        {
            clonedList.Add(CloneCoordinateTransformation(ct));
        }

        return new ConcatenatedTransform(clonedList);
    }

    /// <inheritdoc />
    internal override void Transform(ref double x, ref double y, ref double z, ref double t)
    {
        foreach (ICoordinateTransformationCore ctc in this.coordinateTransformationList)
        {
            TransformCore(ctc, ref x, ref y, ref z, ref t);
        }
    }

    private static ICoordinateTransformationCore CloneCoordinateTransformation(ICoordinateTransformationCore ict)
    {
        return CoordinateTransformationFactory.CreateFromCoordinateSystems(ict.SourceCS, ict.TargetCS);
    }

    private static List<ICoordinateTransformationCore> BuildInvertedCoordinateTransformations(List<ICoordinateTransformationCore> transformations)
    {
        var inverted = new List<ICoordinateTransformationCore>(transformations.Count);
        for (int i = transformations.Count - 1; i >= 0; i--)
        {
            inverted.Add(InvertCoordinateTransformation(transformations[i]));
        }

        return inverted;
    }

    private static ICoordinateTransformationCore InvertCoordinateTransformation(ICoordinateTransformationCore transformation)
    {
        if (transformation is CoordinateTransformation coordinateTransformation)
        {
            return new CoordinateTransformation(
                coordinateTransformation.TargetCS,
                coordinateTransformation.SourceCS,
                coordinateTransformation.TransformType,
                coordinateTransformation.MathTransform.Inverse(),
                coordinateTransformation.Name,
                coordinateTransformation.Authority,
                coordinateTransformation.AuthorityCode,
                coordinateTransformation.AreaOfUse,
                coordinateTransformation.Remarks);
        }

        if (transformation is ConcatenatedTransform concatenatedTransform)
        {
            return AssertConcatenatedInverse(concatenatedTransform.Inverse());
        }

        throw new NotSupportedException($"Unsupported concatenated child type '{transformation.GetType().FullName}'.");
    }

    private static ConcatenatedTransform AssertConcatenatedInverse(MathTransform inverse)
    {
        if (inverse is ConcatenatedTransform concatenatedTransform)
        {
            return concatenatedTransform;
        }

        throw new InvalidOperationException("Concatenated child inverse did not return a ConcatenatedTransform.");
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

    private ICoordinateTransformationCore GetFirstTransform()
    {
        if (this.coordinateTransformationList.Count == 0)
        {
            throw new InvalidOperationException("Concatenated transform does not contain any child transformations.");
        }

        return this.coordinateTransformationList[0];
    }

    private ICoordinateTransformationCore GetLastTransform()
    {
        if (this.coordinateTransformationList.Count == 0)
        {
            throw new InvalidOperationException("Concatenated transform does not contain any child transformations.");
        }

        return this.coordinateTransformationList[^1];
    }
}
