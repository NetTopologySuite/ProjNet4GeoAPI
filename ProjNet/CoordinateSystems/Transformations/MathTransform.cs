// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.Geometries;
using ProjNet.Geometries;

namespace ProjNet.CoordinateSystems.Transformations
{
    /// <summary>
    /// Abstract class for creating multi-dimensional coordinate points transformations.
    /// </summary>
    /// <remarks>
    /// If a client application wishes to query the source and target coordinate 
    /// systems of a transformation, then it should keep hold of the 
    /// <see cref="ICoordinateTransformation"/> interface, and use the contained 
    /// math transform object whenever it wishes to perform a transform.
    /// </remarks>
    [Serializable]
    public abstract class MathTransform : IMathTransform
    {
        #region IMathTransform Members

        /// <summary>
        /// Gets the dimension of input points.
        /// </summary>
        public abstract int DimSource { get; }

        /// <summary>
        /// Gets the dimension of output points.
        /// </summary>
        public abstract int DimTarget { get; }

        /// <summary>
        /// Tests whether this transform does not move any points.
        /// </summary>
        /// <returns></returns>
        public virtual bool Identity()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a Well-Known text representation of this object.
        /// </summary>
        public abstract string WKT { get; }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        public abstract string XML { get; }

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
        /// <param name="point"></param>
        /// <returns></returns>
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
        /// <param name="points"></param>
        /// <returns></returns>
        public virtual List<double> GetCodomainConvexHull(List<double> points)
        {
            throw new NotImplementedException();
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
        /// <param name="points"></param>
        /// <returns></returns>
        public virtual DomainFlags GetDomainFlags(List<double> points)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates the inverse transform of this object.
        /// </summary>
        /// <remarks>This method may fail if the transform is not one to one. However, all cartographic projections should succeed.</remarks>
        /// <returns></returns>
        public abstract IMathTransform Inverse();

        /// <summary>
        /// Transforms a coordinate point. The passed parameter point should not be modified.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        double[] IMathTransform.Transform(double[] point)
        {
            double x = point[0];
            double y = point[1];
            double z = point.Length < 3 ? 0 : point[2];

            (x, y, z) = Transform(x, y, z);

            return DimTarget == 2
                ? new[] { x, y }
                : new[] { x, y, z };
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
        /// <param name="points"></param>
        /// <returns></returns>
        IList<double[]> IMathTransform.TransformList(IList<double[]> points)
        {
            var result = new List<double[]>(points.Count);
            foreach (double[] point in points)
            {
                double x = point[0];
                double y = point[1];
                double z = point.Length < 3 ? 0 : point[2];
                (x, y, z) = Transform(x, y, z);

                result.Add(DimTarget == 2
                    ? new[] { x, y }
                    : new[] { x, y, z });
            }

            return result;
        }

        IList<Coordinate> IMathTransform.TransformList(IList<Coordinate> points)
        {
            var result = new List<Coordinate>(points.Count);

            foreach (var point in points)
            {
                double x = point.X;
                double y = point.Y;
                double z = point.Z;
                (x, y, z) = Transform(x, y, z);
                result.Add(DimTarget == 2
                    ? new Coordinate(x, y)
                    : new CoordinateZ(x, y, z));
            }

            return result;
        }

        Coordinate IMathTransform.Transform(Coordinate coordinate)
        {
            double x = coordinate.X;
            double y = coordinate.Y;
            double z = coordinate.Z;
            (x, y, z) = Transform(x, y, z);
            return DimTarget == 2
                ? new Coordinate(x, y)
                : new CoordinateZ(x, y, z);
        }

        ICoordinateSequence IMathTransform.Transform(ICoordinateSequence coordinateSequence) =>
            TransformCopy(coordinateSequence);

        /// <summary>
        /// Transforms <paramref name="coordinateSequence"/>
        /// </summary>
        /// <param name="coordinateSequence">A coordinate sequence</param>
        public void TransformInPlace(ICoordinateSequence coordinateSequence)
        {
            // shortcout, no matter what
            if (coordinateSequence == null || coordinateSequence.Count == 0)
            {
                return;
            }

            var converter = _sequenceCoordinateConverter;
            if (converter != null)
            {
                var cleanup = converter.ExtractRawCoordinatesFromSequence(coordinateSequence, out var xys, out var zs);
                try
                {
                    Transform(xys, zs, xys, zs);
                    converter.CopyRawCoordinatesToSequence(xys, zs, coordinateSequence);
                }
                finally
                {
                    cleanup();
                }
            }
            else
            {
                var st = SequenceTransformer;
                st.Transform(this, coordinateSequence);
            }
        }

        /// <summary>
        /// Copies <paramref name="coordinateSequence"> and returns the transformed copy</paramref>
        /// </summary>
        /// <param name="coordinateSequence">A coordinate sequence</param>
        /// <returns>A transformed sequence</returns>
        public ICoordinateSequence TransformCopy(ICoordinateSequence coordinateSequence)
        {
            // shortcout, no matter what
            if (coordinateSequence == null || coordinateSequence.Count == 0)
            {
                return coordinateSequence;
            }

            coordinateSequence = coordinateSequence.Copy();
            TransformInPlace(coordinateSequence);
            return coordinateSequence;
        }


        /// <summary>
        /// Reverses the transformation
        /// </summary>
        public abstract void Invert();

        /// <summary>
        /// Constant for converting Degrees to Radians
        /// </summary>
        protected const double D2R = Math.PI / 180;

        /// <summary>
        /// Converts a degree-value (<paramref name="deg"/>) to a radian-value by multiplying it with <c><see cref="Math.PI"/> / 180.0</c> 
        /// </summary>
        protected static double Degrees2Radians(double deg)
        {
            return D2R * deg;

        }

        /// <summary>
        /// Converts a series of degree-values (<paramref name="degrees"/>) to a radian-values by multiplying them with <c><see cref="Math.PI"/> / 180.0</c> 
        /// </summary>
        /// <param name="degrees">A series of degree-values</param>
        /// <param name="radians">A buffer for the radian-values</param>
        /// <param name="stride">A stride value</param>
        protected static void DegreesToRadians(ReadOnlySpan<double> degrees, Span<double> radians, int stride)
        {
            for (int i = 0; i < degrees.Length; i += stride)
            {
                radians[i] = degrees[i] * D2R;
            }
        }

        /// <summary>
        /// Constant for converting Radians to Degrees
        /// </summary>
        protected const double R2D = 180 / Math.PI;

        /// <summary>
        /// Converts a radian-value (<paramref name="rad"/>) to a degree-value by multiplying it with <c>180.0 / <see cref="Math.PI"/></c> 
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        protected static double Radians2Degrees(double rad)
        {
            return R2D * rad;
        }

        /// <summary>
        /// Converts a series of radian-values (<paramref name="degrees"/>) to a degrees-values by multiplying them with <c>180.0 / <see cref="Math.PI"/></c> 
        /// </summary>
        /// <param name="radians">A series of redian-values</param>
        /// <param name="degrees">A buffer for the degree-values</param>
        protected static void RadiansToDegrees(ReadOnlySpan<double> radians, Span<double> degrees)
        {
            for (int i = 0; i < radians.Length; i++)
            {
                degrees[i] = radians[i] * R2D;
            }
        }

        /// <summary>
        /// Abstract transformation method for a single point
        /// </summary>
        /// <param name="x">The x-ordinate</param>
        /// <param name="y">The y-ordinate</param>
        /// <param name="z">The z-ordinate</param>
        /// <returns>The transformed x-, y- and z-ordinate values</returns>
        public abstract (double x, double y, double z) Transform(double x, double y, double z);


        /// <summary>
        /// Core method to transform a series of points defined by their ordinates
        /// </summary>
        /// <param name="inXs">A series of x-ordinate values</param>
        /// <param name="inYs">A series of y-ordinate values</param>
        /// <param name="inZs">A series of z-ordinate values</param>
        /// <param name="outXs">A buffer for x-ordinate values</param>
        /// <param name="outYs">A buffer for y-ordinate values</param>
        /// <param name="outZs">A buffer for z-ordinate values</param>
        /// <param name="strideX">A stride value for the x-ordinate series</param>
        /// <param name="strideY">A stride value for the y-ordinate series</param>
        /// <param name="strideZ">A stride value for the z-ordinate series</param>
        /// <remarks>The stride values apply to both input series and output buffers!</remarks>
        protected virtual void TransformCore(ReadOnlySpan<double> inXs, ReadOnlySpan<double> inYs, ReadOnlySpan<double> inZs,
            Span<double> outXs, Span<double> outYs, Span<double> outZs, int strideX, int strideY, int strideZ)
        {
            for (int i = 0, j = 0, k = 0; i < inXs.Length; i += strideX, j += strideY, k += strideZ)
            {
                (outXs[i], outYs[j], outZs[k]) = Transform(inXs[i], inYs[i], inZs[k]);
            }
        }

        private readonly double[] _inZs = {0d};
        private readonly double[] _outZs = {0d};

        /// <summary>
        /// Transforms a series of 2-dimensional points defined by their ordinates
        /// </summary>
        /// <param name="inXs">A series of x-ordinate values</param>
        /// <param name="inYs">A series of y-ordinate values</param>
        /// <param name="outXs">A buffer for x-ordinate values</param>
        /// <param name="outYs">A buffer for y-ordinate values</param>
        /// <param name="strideX">A stride value for the x-ordinate series</param>
        /// <param name="strideY">A stride value for the y-ordinate series</param>
        /// <remarks>All series and buffers must not be <value>null</value></remarks>
        /// <exception cref="ArgumentException">If the <paramref name="strideX"/> or <paramref name="strideY"/> values are <c>&lt;= 0</c></exception>
        public void Transform(ReadOnlySpan<double> inXs, ReadOnlySpan<double> inYs, 
            Span<double> outXs, Span<double> outYs, int strideX = 1, int strideY = 1)
        {
            if (strideX <= 0 || strideY <= 0)
                throw new ArgumentException("Strides for x- and y- ordinates must be positive");

            TransformCore(inXs, inYs, _inZs, outXs, outYs, _outZs, strideX, strideY, 0);
        }

        /// <summary>
        /// Transforms a series of 2-dimensional points defined by their ordinates
        /// </summary>
        /// <param name="inXs">A series of x-ordinate values</param>
        /// <param name="inYs">A series of y-ordinate values</param>
        /// <param name="inZs">A series of z-ordinate values</param>
        /// <param name="outXs">A buffer for x-ordinate values</param>
        /// <param name="outYs">A buffer for y-ordinate values</param>
        /// <param name="outZs">A buffer for y-ordinate values</param>
        /// <param name="strideX">A stride value for the x-ordinate series</param>
        /// <param name="strideY">A stride value for the y-ordinate series</param>
        /// <param name="strideZ">A stride value for the z-ordinate series</param>
        /// <remarks>All series and buffers must not be <value>null</value></remarks>
        /// <exception cref="ArgumentException">If the <paramref name="strideX"/> or <paramref name="strideY"/> values are <c>&lt;= 0</c></exception>
        public void Transform(ReadOnlySpan<double> inXs, ReadOnlySpan<double> inYs, ReadOnlySpan<double> inZs,
            Span<double> outXs, Span<double> outYs, Span<double> outZs, int strideX = 1, int strideY = 1, int strideZ = 1)
        {
            if (strideX <= 0 || strideY <= 0)
                throw new ArgumentException("Strides for x- and y- ordinates must be positive");

            // check span sizes
            int inXElements = inXs.Length / strideX;
            if (strideX > 1) inXElements += 1;
            int inYElements = inYs.Length / strideY;
            if (strideY > 1) inYElements += 1;

            if (inXElements != inYElements ||
                inXs.Length != outXs.Length ||
                inYs.Length != outYs.Length)
            {
                throw new ArgumentException("Provided x- and y-spans must be of the same length.");
            }

            // If a stride value for z is positive we must check this, too
            if (strideZ > 0 && inZs.Length > 0)
            {
                int inZElements = inZs.Length / strideZ;
                if (strideZ > 1) inZElements += 1;
                if (inXElements != inZElements || inZs.Length != outZs.Length)
                {
                    throw new ArgumentException("Provided z-spans are not of the same length.");
                }
            }

            TransformCore(inXs, inYs, inZs, outXs, outYs, outZs, strideX, strideY, strideZ);
        }


        /// <summary>
        /// Transforms a series of 2-dimensional <see cref="XY"/>-points and and a series of z-ordinate values.
        /// </summary>
        /// <param name="inXys">A series of <see cref="XY"/> points</param>
        /// <param name="inZs">A series of z-ordinate values.</param>
        /// <param name="outXys">A buffer for <see cref="XY"/> points</param>
        /// <param name="outZs">A buffer for z-ordinate values</param>
        /// <exception cref="ArgumentException">If the provided series' and buffers don't match in size.</exception>
        public void Transform(ReadOnlySpan<XY> inXys, ReadOnlySpan<double> inZs, Span<XY> outXys, Span<double> outZs)
        {
            if (inXys.Length != outXys.Length ||
                (inZs.Length != 0 && inXys.Length != outZs.Length) ||
                (outZs.Length != 0 && inXys.Length != outZs.Length))
            {
                throw new ArgumentException("Provided spans must be the same length.");
            }
            var read = MemoryMarshal.Cast<XY, double>(inXys);
            var inXs = read.Slice(0, read.Length - 1);
            var inYs = read.Slice(1, read.Length - 1);

            var write = MemoryMarshal.Cast<XY, double>(outXys);
            var outXs = write.Slice(0, write.Length - 1);
            var outYs = write.Slice(1, write.Length - 1);

            if (inZs.Length == 0)
                TransformCore(inXs, inYs, _inZs, outXs, outYs, _outZs, 2,2, 0);
            else
                TransformCore(inXs, inYs, inZs, outXs, outYs, outZs, 2, 2, 1);
        }

        /// <summary>
        /// Transforms a series of 3-dimensional <see cref="XYZ"/>-points.
        /// </summary>
        /// <param name="inXyzs">A series of <see cref="XYZ"/> points</param>
        /// <param name="outXyzs">A buffer for <see cref="XYZ"/> points</param>
        /// <exception cref="ArgumentException">If the provided series and buffer don't match in size.</exception>
        public void Transform(ReadOnlySpan<XYZ> inXyzs, Span<XYZ> outXyzs)
        {
            if (inXyzs.Length != outXyzs.Length)
            {
                throw new ArgumentException("Observed spans must be the same length.");
            }

            var read = MemoryMarshal.Cast<XYZ, double>(inXyzs);
            var inXs = read.Slice(0, read.Length - 2);
            var inYs = read.Slice(1, read.Length - 2);
            var inZs = read.Slice(2, read.Length - 2);

            var write = MemoryMarshal.Cast<XYZ, double>(outXyzs);
            var outXs = write.Slice(0, write.Length - 2);
            var outYs = write.Slice(1, write.Length - 2);
            var outZs = write.Slice(2, write.Length - 2);

            TransformCore(inXs, inYs, inZs, outXs, outYs, outZs,3,3,3);
        }

        #endregion

        private static SequenceCoordinateConverterBase _sequenceCoordinateConverter;

        /// <summary>
        /// Gets or sets a converter to extract coordinates from a sequence, transform them and copy the transformed to the sequence.
        /// </summary>
        public static SequenceCoordinateConverterBase SequenceCoordinateConverter
        {
            get { return _sequenceCoordinateConverter ?? (_sequenceCoordinateConverter = new SequenceCoordinateConverterBase()); }
            set { _sequenceCoordinateConverter = value; }
        }

        private static SequenceTransformerBase _sequenceTransformer;

        /// <summary>
        /// Gets or sets a transformer that transforms all coordinates in a sequence.
        /// </summary>
        public static SequenceTransformerBase SequenceTransformer
        {
            get { return _sequenceTransformer ?? (_sequenceTransformer = new SequenceTransformerBase()); }
            set { _sequenceTransformer = value; }
        }
    }
}


