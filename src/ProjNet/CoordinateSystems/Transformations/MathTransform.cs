// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using ProjNet.Geometries;

/// <summary>
/// Abstract class for creating multi-dimensional coordinate points transformations.
/// </summary>
/// <remarks>
/// If a client application wishes to query the source and target coordinate
/// systems of a transformation, then it should keep hold of the
/// <see cref="CoordinateTransformation"/> object, and use the contained
/// math transform object whenever it wishes to perform a transform.
/// </remarks>
[Serializable]
public abstract class MathTransform
{
    /// <summary>
    /// Constant for converting Degrees to Radians.
    /// </summary>
    protected const double D2R = Math.PI / 180;

    /// <summary>
    /// Constant for converting Radians to Degrees.
    /// </summary>
    protected const double R2D = 180 / Math.PI;

    /// <summary>
    /// Gets the dimension of input points.
    /// </summary>
    public abstract int DimSource { get; }

    /// <summary>
    /// Gets the dimension of output points.
    /// </summary>
    public abstract int DimTarget { get; }

    /// <summary>
    /// Gets a Well-Known text representation of this object.
    /// </summary>
    public abstract string WKT { get; }

    /// <summary>
    /// Gets an XML representation of this object.
    /// </summary>
    public abstract string XML { get; }

    /// <summary>
    /// Tests whether this transform does not move any points.
    /// </summary>
    /// <returns><see langword="true"/> when the transform is an identity; otherwise <see langword="false"/>.</returns>
    public virtual bool Identity()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the derivative of this transform at a point. If the transform does
    /// not have a well-defined derivative at the point, then this function should
    /// fail in the usual way for the DCP. The derivative is the matrix of the
    /// non-translating portion of the approximate affine map at the point. The
    /// matrix will have dimensions corresponding to the source and target
    /// coordinate systems. If the input dimension is M, and the output dimension
    /// is N, then the matrix will have size [M][N]. The elements of the matrix
    /// {elt[n][m] : n=0..(N-1)} form a vector in the output space which is
    /// parallel to the displacement caused by a small change in the m'th ordinate
    /// in the input space.
    /// </summary>
    /// <param name="point">The ordinate values of the point at which to compute the derivative.</param>
    /// <returns>An [N][M] matrix of partial derivatives where N is the output dimension and M is the input dimension.</returns>
    public virtual double[,] Derivative(double[] point)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets transformed convex hull.
    /// </summary>
    /// <remarks>
    /// <para>The supplied ordinates are interpreted as a sequence of points, which generates a convex
    /// hull in the source space. The returned sequence of ordinates represents a convex hull in the
    /// output space. The number of output points will often be different from the number of input
    /// points. Each of the input points should be inside the valid domain (this can be checked by
    /// testing the points' domain flags individually). However, the convex hull of the input points
    /// may go outside the valid domain. The returned convex hull should contain the transformed image
    /// of the intersection of the source convex hull and the source domain.</para>
    /// <para>A convex hull is a shape in a coordinate system, where if two positions A and B are
    /// inside the shape, then all positions in the straight line between A and B are also inside
    /// the shape. So in 3D a cube and a sphere are both convex hulls. Other less obvious examples
    /// of convex hulls are straight lines, and single points. (A single point is a convex hull,
    /// because the positions A and B must both be the same - i.e. the point itself. So the straight
    /// line between A and B has zero length.)</para>
    /// <para>Some examples of shapes that are NOT convex hulls are donuts, and horseshoes.</para>
    /// </remarks>
    /// <param name="points">Packed ordinate values representing the source convex hull.</param>
    /// <returns>Packed ordinate values representing the transformed convex hull in the output space.</returns>
    public virtual List<double> GetCodomainConvexHull(List<double> points)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets transformed convex hull.
    /// </summary>
    /// <param name="points">Packed ordinate values representing the source convex hull.</param>
    /// <returns>Packed ordinate values representing the transformed convex hull in the output space.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1002:Do not expose generic lists",
        Justification = "Legacy API surface; span overload added as a non-breaking alternative.")]
    public virtual List<double> GetCodomainConvexHull(ReadOnlySpan<double> points)
    {
        return this.GetCodomainConvexHull(CreatePointList(points));
    }

    /// <summary>
    /// Gets flags classifying domain points within a convex hull.
    /// </summary>
    /// <remarks>
    /// The supplied ordinates are interpreted as a sequence of points, which
    /// generates a convex hull in the source space. Conceptually, each of the
    /// (usually infinite) points inside the convex hull is then tested against
    /// the source domain. The flags of all these tests are then combined. In
    /// practice, implementations of different transforms will use different
    /// short-cuts to avoid doing an infinite number of tests.
    /// </remarks>
    /// <param name="points">Packed ordinate values representing the source convex hull.</param>
    /// <returns>Combined <see cref="DomainFlags"/> for all points inside the source convex hull.</returns>
    public virtual DomainFlags GetDomainFlags(List<double> points)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets flags classifying domain points within a convex hull.
    /// </summary>
    /// <param name="points">Packed ordinate values representing the source convex hull.</param>
    /// <returns>Combined <see cref="DomainFlags"/> for all points inside the source convex hull.</returns>
    public virtual DomainFlags GetDomainFlags(ReadOnlySpan<double> points)
    {
        return this.GetDomainFlags(CreatePointList(points));
    }

    /// <summary>
    /// Creates the inverse transform of this object.
    /// </summary>
    /// <remarks>This method may fail if the transform is not one to one. However, all cartographic projections should succeed.</remarks>
    /// <returns>A <see cref="MathTransform"/> that reverses this transform.</returns>
    public abstract MathTransform Inverse();

    /// <summary>
    /// Reverses the transformation.
    /// </summary>
    public abstract void Invert();

    /// <summary>
    /// Transforms a coordinate point. The passed parameter point should not be modified.
    /// </summary>
    /// <param name="point">The input coordinate as an array of ordinate values.</param>
    /// <returns>The transformed coordinate as an array of ordinate values.</returns>
    public double[] Transform(double[] point)
    {
        ArgumentGuard.ThrowIfNull(point, nameof(point));

        int pointLength = point.Length;
        int resultDimensions = this.GetResultDimensions(pointLength);
        if (resultDimensions <= 4)
        {
            Span<double> scratch = stackalloc double[4];
            Span<double> scratchResult = scratch.Slice(0, resultDimensions);
            this.TransformPoint(point, pointLength, scratchResult, resultDimensions);
            var transformedSmall = new double[resultDimensions];
            scratchResult.CopyTo(transformedSmall);
            return transformedSmall;
        }

        var transformed = new double[resultDimensions];
        this.TransformPoint(point, pointLength, transformed, resultDimensions);
        return transformed;
    }

    /// <summary>
    /// Transforms a coordinate point from <paramref name="point"/> into <paramref name="result"/>.
    /// </summary>
    /// <param name="point">The input coordinate as a readonly span of ordinate values.</param>
    /// <param name="result">Destination span that receives the transformed ordinate values.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="point"/> has fewer than two ordinates or
    /// <paramref name="result"/> is too small to hold the transformed ordinates.
    /// </exception>
    public void Transform(ReadOnlySpan<double> point, Span<double> result)
    {
        int pointLength = point.Length;
        if (pointLength < 2)
        {
            ArgumentGuard.ThrowArgument("At least two ordinate values are required.", nameof(point));
        }

        int resultDimensions = this.GetResultDimensions(pointLength);
        if (result.Length < resultDimensions)
        {
            ArgumentGuard.ThrowArgument(
                "Destination span is too small to store the transformed coordinate.",
                nameof(result));
        }

        this.TransformPoint(point, pointLength, result, resultDimensions);
    }

    /// <summary>
    /// Transforms a list of coordinate point ordinal values.
    /// </summary>
    /// <remarks>
    /// This method is provided for efficiently transforming many points. The supplied array
    /// of ordinal values will contain packed ordinal values. For example, if the source
    /// dimension is 3, then the ordinals will be packed in this order (x0,y0,z0,x1,y1,z1 ...).
    /// The size of the passed array must be an integer multiple of DimSource. The returned
    /// ordinal values are packed in a similar way. In some DCPs. the ordinals may be
    /// transformed in-place, and the returned array may be the same as the passed array.
    /// So any client code should not attempt to reuse the passed ordinal values (although
    /// they can certainly reuse the passed array). If there is any problem then the server
    /// implementation will throw an exception. If this happens then the client should not
    /// make any assumptions about the state of the ordinal values.
    /// </remarks>
    /// <param name="points">The packed ordinate values to transform.</param>
    /// <returns>The transformed packed ordinate values.</returns>
    public IList<double[]> TransformList(IList<double[]> points)
    {
        ArgumentGuard.ThrowIfNull(points, nameof(points));

        int minimumDimensions = this.DimTarget == 2 ? 2 : 3;
        var result = new List<double[]>(points.Count);
        for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
        {
            double[] point = points[pointIndex];
            int pointLength = point.Length;
            int resultDimensions = pointLength <= 3
                ? minimumDimensions
                : Math.Max(minimumDimensions, pointLength);
            var transformed = new double[resultDimensions];
            this.TransformPoint(point, pointLength, transformed, resultDimensions);
            result.Add(transformed);
        }

        return result;
    }

    /// <summary>
    /// Transforms a single 2-dimensional point.
    /// </summary>
    /// <param name="x">The ordinate value on the first axis, either x or longitude.</param>
    /// <param name="y">The ordinate value on the second axis, either y or latitude.</param>
    /// <returns>The transformed x- and y-ordinate values.</returns>
    public (double X, double Y) Transform(double x, double y)
    {
        double z = 0;
        this.Transform(ref x, ref y, ref z);
        return (x, y);
    }

    /// <summary>
    /// Transforms a single 3-dimensional point.
    /// </summary>
    /// <param name="x">The ordinate value on the first axis, either x or longitude.</param>
    /// <param name="y">The ordinate value on the second axis, either y or latitude.</param>
    /// <param name="z">The ordinate value on the third axis, either z, height or altitude.</param>
    /// <returns>The transformed x-, y- and z-ordinate values.</returns>
    public (double O1, double O2, double O3) Transform(double x, double y, double z)
    {
        this.Transform(ref x, ref y, ref z);
        return (x, y, z);
    }

    /// <summary>
    /// Transforms a single 2-dimensional point in-place.
    /// </summary>
    /// <param name="x">The ordinate value on the first axis, either x or longitude.</param>
    /// <param name="y">The ordinate value on the second axis, either y or latitude.</param>
    public void Transform(ref double x, ref double y)
    {
        double z = 0d;
        this.Transform(ref x, ref y, ref z);
    }

    /// <summary>
    /// Transforms a single 3-dimensional point in-place.
    /// </summary>
    /// <param name="x">The ordinate value on the first axis, either x or longitude.</param>
    /// <param name="y">The ordinate value on the second axis, either y or latitude.</param>
    /// <param name="z">The ordinate value on the third axis, either z, height or altitude.</param>
    public abstract void Transform(ref double x, ref double y, ref double z);

    /// <summary>
    /// Core method to transform a series of points defined by their ordinates.
    /// The transformation is performed in-place.
    /// </summary>
    /// <param name="xs">A series of x-ordinate values.</param>
    /// <param name="ys">A series of y-ordinate values.</param>
    /// <param name="strideX">A stride value for the x-ordinate series.</param>
    /// <param name="strideY">A stride value for the y-ordinate series.</param>
    /// <exception cref="ArgumentException">If the provided span and stride values don't result in matching number of ordinates.</exception>
    public void Transform(Span<double> xs, Span<double> ys, int strideX = 1, int strideY = 1)
    {
        int elementsX = (xs.Length / strideX) + (xs.Length % strideX) != 0 ? 1 : 0;
        int elementsY = (ys.Length / strideY) + (ys.Length % strideY) != 0 ? 1 : 0;

        if (elementsX != elementsY)
        {
            ArgumentGuard.ThrowArgument("Spans of ordinate values don't match in size.");
        }

        Span<double> dummyZ = stackalloc double[] { 0 };
        this.TransformCore(xs, ys, dummyZ, strideX, strideY, 0);
    }

    /// <summary>
    /// Core method to transform a series of points defined by their ordinates.
    /// The transformation is performed in-place.
    /// </summary>
    /// <param name="xs">A series of x-ordinate values.</param>
    /// <param name="ys">A series of y-ordinate values.</param>
    /// <param name="zs">A series of z-ordinate values.</param>
    /// <param name="strideX">A stride value for the x-ordinate series.</param>
    /// <param name="strideY">A stride value for the y-ordinate series.</param>
    /// <param name="strideZ">A stride value for the z-ordinate series.</param>
    /// <exception cref="ArgumentException">If the provided span and stride values don't result in matching number of ordinates.</exception>
    public void Transform(
        Span<double> xs,
        Span<double> ys,
        Span<double> zs,
        int strideX = 1,
        int strideY = 1,
        int strideZ = 1)
    {
        int elementsX = (xs.Length / strideX) + (xs.Length % strideX) != 0 ? 1 : 0;
        int elementsY = (ys.Length / strideY) + (ys.Length % strideY) != 0 ? 1 : 0;
        if (elementsX != elementsY)
        {
            ArgumentGuard.ThrowArgument("Spans of ordinate values don't match in size.");
        }

        if (zs.IsEmpty)
        {
            Span<double> dummyZ = stackalloc double[] { 0 };
            this.TransformCore(xs, ys, dummyZ, strideX, strideY, 0);
            return;
        }

        int elementsZ = (zs.Length / strideZ) + (zs.Length % strideZ) != 0 ? 1 : 0;
        if (elementsZ != elementsX)
        {
            ArgumentGuard.ThrowArgument("Spans of ordinate values don't match in size.");
        }

        this.TransformCore(xs, ys, zs, strideX, strideY, strideZ);
    }

    /// <summary>
    /// Transforms a series of 2-dimensional <see cref="XY"/>-points and (optionally) a series of z-ordinate values.
    /// </summary>
    /// <param name="xys">A series of <see cref="XY"/> points.</param>
    /// <param name="zs">A series of z-ordinate values.</param>
    /// <param name="strideZ">A stride value for z-ordinates.</param>
    /// <exception cref="ArgumentException">If the provided series' and buffers don't match in size.</exception>
    public void Transform(Span<XY> xys, Span<double> zs = default, int strideZ = 0)
    {
        if (!zs.IsEmpty)
        {
            if (strideZ <= 0)
            {
                strideZ = 1;
            }

            if (xys.Length != ((zs.Length / strideZ) + (zs.Length % strideZ != 0 ? 1 : 0)))
            {
                ArgumentGuard.ThrowArgument("Provided spans don't match in size.");
            }
        }

        var read = MemoryMarshal.Cast<XY, double>(xys);
        var inXs = read.Slice(0);
        var inYs = read.Slice(1);

        if (zs.IsEmpty)
        {
            Span<double> dummyZ = stackalloc double[] { 0 };
            this.TransformCore(inXs, inYs, dummyZ, 2, 2, 0);
        }
        else
        {
            this.TransformCore(inXs, inYs, zs, 2, 2, strideZ);
        }
    }

    /// <summary>
    /// Transforms a series of 3-dimensional <see cref="XYZ"/>-points.
    /// </summary>
    /// <param name="xyzs">A series of <see cref="XYZ"/> points.</param>
    public void Transform(Span<XYZ> xyzs)
    {
        var read = MemoryMarshal.Cast<XYZ, double>(xyzs);
        var inXs = read.Slice(0); // , read.Length - 2);
        var inYs = read.Slice(1); // , read.Length - 2);
        var inZs = read.Slice(2); // , read.Length - 2);

        this.TransformCore(inXs, inYs, inZs, 3, 3, 3);
    }

    /// <summary>
    /// Transforms a single 4-dimensional point in-place.
    /// </summary>
    /// <param name="x">The ordinate value on the first axis, either x or longitude.</param>
    /// <param name="y">The ordinate value on the second axis, either y or latitude.</param>
    /// <param name="z">The ordinate value on the third axis, either z, height or altitude.</param>
    /// <param name="t">The ordinate value on the fourth axis, typically observation epoch.</param>
    internal virtual void Transform(ref double x, ref double y, ref double z, ref double t)
    {
        this.Transform(ref x, ref y, ref z);
    }

    /// <summary>
    /// Core method to transform a series of points defined by their ordinates.
    /// The transformation is performed in-place.
    /// </summary>
    /// <param name="xs">A series of x-ordinate values.</param>
    /// <param name="ys">A series of y-ordinate values.</param>
    /// <param name="zs">A series of z-ordinate values.</param>
    /// <param name="strideX">A stride value for the x-ordinate series.</param>
    /// <param name="strideY">A stride value for the y-ordinate series.</param>
    /// <param name="strideZ">A stride value for the z-ordinate series.</param>
    protected virtual void TransformCore(
        Span<double> xs,
        Span<double> ys,
        Span<double> zs,
        int strideX,
        int strideY,
        int strideZ)
    {
        for (int i = 0, j = 0, k = 0; i < xs.Length; i += strideX, j += strideY, k += strideZ)
        {
            this.Transform(ref xs[i], ref ys[j], ref zs[k]);
        }
    }

    /// <summary>
    /// Converts a degree value to radians by multiplying it with <c><see cref="Math.PI"/> / 180.0</c>.
    /// </summary>
    /// <param name="deg">The value in degrees to convert.</param>
    /// <returns>The equivalent value in radians.</returns>
    protected static double DegreesToRadians(double deg) => D2R * deg;

    /// <summary>
    /// Converts a series of degree-values (<paramref name="degrees"/>) to a radian-values by multiplying them with <c><see cref="Math.PI"/> / 180.0</c>.
    /// </summary>
    /// <param name="degrees">A series of degree-values.</param>
    /// <param name="stride">A stride value.</param>
    protected static void DegreesToRadians(Span<double> degrees, int stride)
    {
        MultiplyInPlace(degrees, stride, D2R);
    }

    /// <summary>
    /// Converts a radian value to degrees by multiplying it with <c>180.0 / <see cref="Math.PI"/></c>.
    /// </summary>
    /// <param name="rad">The value in radians to convert.</param>
    /// <returns>The equivalent value in degrees.</returns>
    protected static double RadiansToDegrees(double rad) => R2D * rad;

    /// <summary>
    /// Converts a series of radian-values (<paramref name="radians"/>) to a degrees-values by multiplying them with <c>180.0 / <see cref="Math.PI"/></c>.
    /// </summary>
    /// <param name="radians">A series of radian-values.</param>
    /// <param name="stride">A stride value.</param>
    protected static void RadiansToDegrees(Span<double> radians, int stride)
    {
        MultiplyInPlace(radians, stride, R2D);
    }

    /// <summary>
    /// Adds a value to the elements of a <see cref="Span{T}"/> in-place, using SIMD when legal
    /// and effective.
    /// </summary>
    /// <param name="vals">A series of values to transform in-place.</param>
    /// <param name="stride">The spacing between elements.</param>
    /// <param name="addend">The value to add to each element in <paramref name="vals"/> in-place.</param>
    protected static void AddInPlace(Span<double> vals, int stride, double addend)
    {
        if (stride < 1)
        {
            ArgumentGuard.ThrowArgumentOutOfRange(nameof(stride), stride, "Must be greater than zero.");
        }

        if (addend == 0)
        {
            return;
        }

        int scalarStart = 0;
        if (Vector.IsHardwareAccelerated && stride == 1 && vals.Length >= Vector<double>.Count)
        {
            var valsVector = MemoryMarshal.Cast<double, Vector<double>>(vals);
            var addendVector = new Vector<double>(addend);
            for (int i = 0; i < valsVector.Length; i++)
            {
                valsVector[i] += addendVector;
            }

            scalarStart = valsVector.Length * Vector<double>.Count;
        }

        for (int i = scalarStart; i < vals.Length; i += stride)
        {
            vals[i] += addend;
        }
    }

    /// <summary>
    /// Multiplies the elements of a <see cref="Span{T}"/> in-place by a multiplier, using SIMD
    /// when legal and effective.
    /// </summary>
    /// <param name="vals">A series of values to transform in-place.</param>
    /// <param name="stride">The spacing between elements.</param>
    /// <param name="multiplier">The value by which to multiply each element in <paramref name="vals"/> in-place.</param>
    protected static void MultiplyInPlace(Span<double> vals, int stride, double multiplier)
    {
        if (stride < 1)
        {
            ArgumentGuard.ThrowArgumentOutOfRange(nameof(stride), stride, "Must be greater than zero.");
        }

        if (multiplier == 1)
        {
            return;
        }

        int scalarStart = 0;
        if (Vector.IsHardwareAccelerated && stride == 1 && vals.Length >= Vector<double>.Count)
        {
            var valsVector = MemoryMarshal.Cast<double, Vector<double>>(vals);
            var multiplierVector = new Vector<double>(multiplier);
            for (int i = 0; i < valsVector.Length; i++)
            {
                valsVector[i] *= multiplierVector;
            }

            scalarStart = valsVector.Length * Vector<double>.Count;
        }

        for (int i = scalarStart; i < vals.Length; i += stride)
        {
            vals[i] *= multiplier;
        }
    }

    /// <summary>
    /// Multiplies the elements of a <see cref="Span{T}"/> in-place by a multiplier, then adds a
    /// value to the product in-place, using SIMD when legal and effective.
    /// </summary>
    /// <param name="vals">A series of values to transform in-place.</param>
    /// <param name="stride">The spacing between elements.</param>
    /// <param name="multiplier">The value by which to multiply each element in <paramref name="vals"/> in-place.</param>
    /// <param name="addend">The value to add to each multiplied element in <paramref name="vals"/> in-place.</param>
    protected static void MultiplyThenAddInPlace(Span<double> vals, int stride, double multiplier, double addend)
    {
        if (stride < 1)
        {
            ArgumentGuard.ThrowArgumentOutOfRange(nameof(stride), stride, "Must be greater than zero.");
        }

        if (addend == 0)
        {
            MultiplyInPlace(vals, stride, multiplier);
            return;
        }

        if (multiplier == 1)
        {
            AddInPlace(vals, stride, addend);
            return;
        }

        int scalarStart = 0;
        if (Vector.IsHardwareAccelerated && stride == 1 && vals.Length >= Vector<double>.Count)
        {
            var valsVector = MemoryMarshal.Cast<double, Vector<double>>(vals);
            var multiplierVector = new Vector<double>(multiplier);
            var addendVector = new Vector<double>(addend);
            for (int i = 0; i < valsVector.Length; i++)
            {
                valsVector[i] = (valsVector[i] * multiplierVector) + addendVector;
            }

            scalarStart = valsVector.Length * Vector<double>.Count;
        }

        for (int i = scalarStart; i < vals.Length; i += stride)
        {
            vals[i] = (vals[i] * multiplier) + addend;
        }
    }

    /// <summary>
    /// Adds a value to the elements of a <see cref="Span{T}"/> in-place, then multiplies each
    /// sum by a multiplier in-place, using SIMD when legal and effective.
    /// </summary>
    /// <param name="vals">A series of values to transform in-place.</param>
    /// <param name="stride">The spacing between elements.</param>
    /// <param name="addend">The value to add to each element in <paramref name="vals"/> in-place.</param>
    /// <param name="multiplier">The value by which to multiply each summed element in <paramref name="vals"/> in-place.</param>
    protected static void AddThenMultiplyInPlace(Span<double> vals, int stride, double addend, double multiplier)
    {
        if (stride < 1)
        {
            ArgumentGuard.ThrowArgumentOutOfRange(nameof(stride), stride, "Must be greater than zero.");
        }

        if (addend == 0)
        {
            MultiplyInPlace(vals, stride, multiplier);
            return;
        }

        if (multiplier == 1)
        {
            AddInPlace(vals, stride, addend);
            return;
        }

        int scalarStart = 0;
        if (Vector.IsHardwareAccelerated && stride == 1 && vals.Length >= Vector<double>.Count)
        {
            var valsVector = MemoryMarshal.Cast<double, Vector<double>>(vals);
            var addendVector = new Vector<double>(addend);
            var multiplierVector = new Vector<double>(multiplier);
            for (int i = 0; i < valsVector.Length; i++)
            {
                valsVector[i] = (valsVector[i] + addendVector) * multiplierVector;
            }

            scalarStart = valsVector.Length * Vector<double>.Count;
        }

        for (int i = scalarStart; i < vals.Length; i += stride)
        {
            vals[i] = (vals[i] + addend) * multiplier;
        }
    }

    private static List<double> CreatePointList(ReadOnlySpan<double> points)
    {
        if (points.IsEmpty)
        {
            return new List<double>();
        }

        var list = new List<double>(points.Length);
        for (int i = 0; i < points.Length; i++)
        {
            list.Add(points[i]);
        }

        return list;
    }

    /// <summary>
    /// Transforms a single source coordinate into an already-sized destination span.
    /// </summary>
    /// <param name="point">Source ordinates.</param>
    /// <param name="pointLength">The number of source ordinates available in <paramref name="point"/>.</param>
    /// <param name="result">Destination ordinates.</param>
    /// <param name="resultDimensions">The number of result ordinates to write.</param>
    private void TransformPoint(ReadOnlySpan<double> point, int pointLength, Span<double> result, int resultDimensions)
    {
        double x = point[0];
        double y = point[1];
        double z = pointLength >= 3 ? point[2] : 0;
        double t = pointLength >= 4 ? point[3] : 0;

        if (pointLength >= 4)
        {
            this.Transform(ref x, ref y, ref z, ref t);
        }
        else
        {
            this.Transform(ref x, ref y, ref z);
        }

        result[0] = x;
        result[1] = y;
        if (resultDimensions >= 3)
        {
            result[2] = z;
        }

        if (resultDimensions >= 4)
        {
            result[3] = t;
            if (resultDimensions > 4)
            {
                point.Slice(4, resultDimensions - 4).CopyTo(result.Slice(4));
            }
        }
    }

    /// <summary>
    /// Gets the required number of ordinates in a transformed point.
    /// </summary>
    /// <param name="pointLength">Input point ordinate count.</param>
    /// <returns>Output ordinate count.</returns>
    private int GetResultDimensions(int pointLength)
    {
        int minimumDimensions = this.DimTarget == 2 ? 2 : 3;
        return pointLength <= 3
            ? minimumDimensions
            : Math.Max(minimumDimensions, pointLength);
    }
}
