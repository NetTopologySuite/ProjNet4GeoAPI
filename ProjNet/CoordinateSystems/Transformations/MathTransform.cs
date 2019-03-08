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

namespace ProjNet.CoordinateSystems.Transformations
{
    public struct XY
    {
        public double X;

        public double Y;
    }

    public struct XYZ
    {
        public double X;

        public double Y;

        public double Z;
    }

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

        public abstract (double x, double y, double z) Transform(double x, double y, double z);

        public void Transform(ReadOnlySpan<double> xs, ReadOnlySpan<double> ys, ReadOnlySpan<double> zs, Span<double> outXs, Span<double> outYs, Span<double> outZs)
        {
            if (xs.Length != ys.Length ||
                xs.Length != outXs.Length ||
                xs.Length != outYs.Length ||
                (DimSource > 2 && xs.Length != zs.Length) ||
                (DimTarget > 2 && xs.Length != outZs.Length))
            {
                throw new ArgumentException("Observed spans must be the same length.");
            }

            TransformCore(xs, ys, zs, outXs, outYs, outZs);
        }

        protected virtual void TransformCore(ReadOnlySpan<double> xs, ReadOnlySpan<double> ys, ReadOnlySpan<double> zs, Span<double> outXs, Span<double> outYs, Span<double> outZs)
        {
            bool readZ = DimSource > 2;
            bool writeZ = DimTarget > 2;
            for (int i = 0; i < xs.Length; i++)
            {
                double z = readZ ? zs[i] : 0;
                (outXs[i], outYs[i], z) = Transform(xs[i], ys[i], z);
                if (writeZ)
                {
                    outZs[i] = z;
                }
            }
        }

        public void Transform(ReadOnlySpan<XY> xys, ReadOnlySpan<double> zs, Span<XY> outXys, Span<double> outZs)
        {
            if (xys.Length != outXys.Length ||
                (DimSource > 2 && xys.Length != zs.Length) ||
                (DimTarget > 2 && xys.Length != outZs.Length))
            {
                throw new ArgumentException("Observed spans must be the same length.");
            }

            TransformCore(xys, zs, outXys, outZs);
        }

        protected virtual void TransformCore(ReadOnlySpan<XY> xys, ReadOnlySpan<double> zs, Span<XY> outXys, Span<double> outZs)
        {
            bool readZ = DimSource > 2;
            bool writeZ = DimTarget > 2;
            for (int i = 0; i < xys.Length; i++)
            {
                double z = readZ ? zs[i] : 0;
                (outXys[i].X, outXys[i].Y, z) = this.Transform(xys[i].X, xys[i].Y, z);
                if (writeZ)
                {
                    outZs[i] = z;
                }
            }
        }

        public void Transform(ReadOnlySpan<XYZ> xyzs, Span<XYZ> outXyzs)
        {
            if (xyzs.Length != outXyzs.Length)
            {
                throw new ArgumentException("Observed spans must be the same length.");
            }

            TransformCore(xyzs, outXyzs);
        }

        protected virtual void TransformCore(ReadOnlySpan<XYZ> xyzs, Span<XYZ> outXyzs)
        {
            for (int i = 0; i < xyzs.Length; i++)
            {
                (outXyzs[i].X, outXyzs[i].Y, outXyzs[i].Z) = this.Transform(xyzs[i].X, xyzs[i].Y, xyzs[i].Z);
            }
        }

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

        ICoordinateSequence IMathTransform.Transform(ICoordinateSequence coordinateSequence) => TransformCopy(coordinateSequence);

        public virtual void TransformInPlace(ICoordinateSequence coordinateSequence)
        {
            // shortcout, no matter what
            if (coordinateSequence == null || coordinateSequence.Count == 0)
            {
                return;
            }

            var converter = this.SequenceCoordinateConverter;
            converter.ExtractRawCoordinatesFromSequence(coordinateSequence, out var xys, out var zs);
            Transform(xys, zs, xys, zs);
            converter.CopyRawCoordinatesToSequence(xys, zs, coordinateSequence, 0);
        }

        public virtual ICoordinateSequence TransformCopy(ICoordinateSequence coordinateSequence)
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

        public SequenceCoordinateConverterBase SequenceCoordinateConverter { get; set; } = new SequenceCoordinateConverterBase();

        public class SequenceCoordinateConverterBase
        {
            public virtual void ExtractRawCoordinatesFromSequence(ICoordinateSequence sequence, out Span<XY> xys, out Span<double> zs)
            {
                if (sequence == null || sequence.Count < 1)
                {
                    xys = default;
                    zs = default;
                    return;
                }

                var xysArray = new XY[sequence.Count];
                var zsArray = new double[xysArray.Length];

                xys = xysArray;
                zs = zsArray;

                bool hasZ = sequence.HasZ;
                for (int i = 0; i < xys.Length; i++)
                {
                    xys[i].X = sequence.GetX(i);
                    xys[i].Y = sequence.GetY(i);
                    zs[i] = sequence.GetZ(i);
                }
            }

            public void CopyRawCoordinatesToSequence(Span<XY> xys, Span<double> zs, ICoordinateSequence sequence, int offset)
            {
                if (offset < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset), offset, "must be non-negative");
                }

                if (sequence == null || sequence.Count < 1)
                {
                    return;
                }

                if (sequence.Count - offset < xys.Length)
                {
                    throw new ArgumentException("Not enough room in the sequence for the coordinates.");
                }

                if (xys.Length == 0)
                {
                    return;
                }

                if (zs.Length == 0)
                {
                    if (sequence.HasZ)
                    {
                        throw new ArgumentException("can only be empty when sequence does not have Z", nameof(zs));
                    }
                }
                else if (xys.Length != zs.Length)
                {
                    throw new ArgumentException("spans must be the same length.");
                }

                CopyRawCoordinatesToSequenceCore(xys, zs, sequence, offset);
            }

            protected virtual void CopyRawCoordinatesToSequenceCore(Span<XY> xys, Span<double> zs, ICoordinateSequence sequence, int offset)
            {
                bool hasZ = sequence.HasZ;
                for (int i = 0; i < xys.Length; i++)
                {
                    sequence.SetOrdinate(i + offset, Ordinate.X, xys[i].X);
                    sequence.SetOrdinate(i + offset, Ordinate.Y, xys[i].Y);

                    // documentation says that the sequence MUST NOT throw if it doesn't support Z
                    // and that it SHOULD ignore the call... PackedCoordinateSequence instances will
                    // overwrite other values, so we do need to skip.
                    if (hasZ)
                    {
                        sequence.SetOrdinate(i + offset, Ordinate.Z, zs[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Reverses the transformation
        /// </summary>
        public abstract void Invert();

	    /// <summary>
		/// To convert degrees to radians, multiply degrees by pi/180. 
		/// </summary>
		protected static double Degrees2Radians(double deg)
		{
			return (D2R * deg);

		}

        protected void DegreesToRadians(ReadOnlySpan<XY> inputs, Span<XY> outputs)
        {
            DegreesToRadians(MemoryMarshal.Cast<XY, double>(inputs), MemoryMarshal.Cast<XY, double>(outputs));
        }

        protected static void DegreesToRadians(ReadOnlySpan<double> degrees, Span<double> radians)
        {
            for (int i = 0; i < degrees.Length; i++)
            {
                radians[i] = degrees[i] * D2R;
            }
        }

        protected void DegreesToRadians(ReadOnlySpan<XYZ> inputs, Span<XYZ> outputs)
        {
            for (int i = 0; i < inputs.Length; i++)
            {
                outputs[i].X = D2R * inputs[i].X;
                outputs[i].Y = D2R * inputs[i].Y;
            }
        }

        /// <summary>
        /// R2D
        /// </summary>
        protected const double R2D = 180 / Math.PI;

		/// <summary>
		/// D2R
		/// </summary>
		protected const double D2R = Math.PI / 180;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rad"></param>
		/// <returns></returns>
		protected static double Radians2Degrees(double rad)
		{
			return (R2D * rad);
		}

        protected void RadiansToDegrees(ReadOnlySpan<XY> inputs, Span<XY> outputs)
        {
            RadiansToDegrees(MemoryMarshal.Cast<XY, double>(inputs), MemoryMarshal.Cast<XY, double>(outputs));
        }

        protected static void RadiansToDegrees(ReadOnlySpan<double> radians, Span<double> degrees)
        {
            for (int i = 0; i < radians.Length; i++)
            {
                degrees[i] = radians[i] * R2D;
            }
        }

        protected void RadiansToDegrees(ReadOnlySpan<XYZ> inputs, Span<XYZ> outputs)
        {
            for (int i = 0; i < inputs.Length; i++)
            {
                outputs[i].X = D2R * inputs[i].X;
                outputs[i].Y = D2R * inputs[i].Y;
            }
        }

        #endregion
    }
}
