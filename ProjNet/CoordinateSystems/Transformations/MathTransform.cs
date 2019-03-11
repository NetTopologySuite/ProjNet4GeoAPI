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
using System.Globalization;
using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.Geometries;

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
        private static ISequenceToSpanConverter _defaultSequenceToSpanConverter;

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

        protected internal abstract void Transform(ref Span<double> points, ref Span<double> altitudes);

        /// <summary>
        /// Transforms a coordinate point. The passed parameter point should not be modified.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public virtual double[] Transform(double[] point)
        {
            var points = new Span<double>(new []{point[0], point[1]});
            Span<double> altitudes = null;
            if (DimSource == 3 || point.Length > 2)
            {
                altitudes = new Span<double>(new double[1]);
                if (point.Length > 2) altitudes[0] = point[2];
            }

            Transform(ref points, ref altitudes);

            if (DimTarget == 2)
                return points.ToArray();

            return new[] {points[0], points[1], altitudes[0]};
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
		public virtual IList<double[]> TransformList(IList<double[]> points)
		{
            var result = new List<double[]>(points.Count);

            foreach (var c in points)
                result.Add(Transform(c));
            
            return result;
        }

	    public virtual IList<Coordinate> TransformList(IList<Coordinate> points)
	    {
            var result = new List<Coordinate>(points.Count);

            foreach (var c in points)
                result.Add(Transform(c));
            return result;
        }

        public Coordinate Transform(Coordinate coordinate)
        {
            var ordinates = DimSource == 2
                                     ? new[] {coordinate.X, coordinate.Y}
                                     : new[] {coordinate.X, coordinate.Y, coordinate.Z};

                var ret = Transform(ordinates);

            return (DimTarget == 2)
                ? new Coordinate(ret[0], ret[1])
                : new CoordinateZ(ret[0], ret[1], ret[2]);
        }

        public virtual ICoordinateSequence Transform(ICoordinateSequence coordinateSequence)
        {
            return Transform(coordinateSequence, true);
        }

        public virtual ICoordinateSequence Transform(ICoordinateSequence coordinateSequence, bool inPlace)
        {
            // shortcout, no matter what
            if (coordinateSequence == null || coordinateSequence.Count == 0)
                return coordinateSequence;

            ICoordinateSequence res = null;
            if (!coordinateSequence.HasZ && DimTarget > 2)
            {
                res = ProjNet.CoordinateSystemServices.CoordinateSequenceFactory.Create(coordinateSequence.Count, Ordinates.XYZ);
                for (int i = 0; i < coordinateSequence.Count; i++){
                    res.SetOrdinate(i, Ordinate.X, coordinateSequence.GetX(i));
                    res.SetOrdinate(i, Ordinate.Y, coordinateSequence.GetY(i));
                }
            }

            // make a copy if required
            if (res == null)
                res = inPlace ? coordinateSequence : coordinateSequence.Copy();

            SequenceToSpanConverter.Convert(ref res, out var points, out var altitudes);
            Transform(ref points, ref altitudes);
            SequenceToSpanConverter.Convert(ref points, ref altitudes, ref res);

            return res;
	    }

        public static ISequenceToSpanConverter SequenceToSpanConverter
        {
            get => _defaultSequenceToSpanConverter ?? new DefaultSequenceSpanConverter();
            set => _defaultSequenceToSpanConverter = value;
        }

        public interface ISequenceToSpanConverter
        {
            void Convert(ref ICoordinateSequence sequence, out Span<double> pointData, out Span<double> altitudes);
            void Convert(ref Span<double> pointData, ref Span<double> altitudes, ref ICoordinateSequence sequence);
        }

        private class DefaultSequenceSpanConverter : ISequenceToSpanConverter
        {

            public void Convert(ref ICoordinateSequence sequence, out Span<double> pointData, out Span<double> altitudes)
            {
                altitudes = null;
                if (sequence == null || sequence.Count == 0)
                {
                    pointData = new Span<double>(new double[0]);
                    if (sequence != null && sequence.HasZ)
                        altitudes = new Span<double>(new double[0]);
                    return;
                }

                pointData = new Span<double>(new double[2* sequence.Count]);
                for (int i = 0, j = 0; i < sequence.Count; i++) {
                    pointData[j++] = sequence.GetX(i);
                    pointData[j++] = sequence.GetY(i);
                }

                if (!sequence.HasZ) return;

                altitudes = new Span<double>(new double[sequence.Count]);
                for (int i = 0, j = 0; i < sequence.Count; i++)
                    altitudes[i] = sequence.GetZ(i);
            }

            public void Convert(ref Span<double> pointData, ref Span<double> altitudes, ref ICoordinateSequence sequence)
            {
                for (int i = 0, j = 0; i < sequence.Count; i++)
                {
                    sequence.SetOrdinate(i, Ordinate.X, pointData[j++]);
                    sequence.SetOrdinate(i, Ordinate.Y, pointData[j++]);
                }

                if (altitudes != null && altitudes.Length == sequence.Count)
                {
                    for (int i = 0; i < sequence.Count; i++)
                        sequence.SetOrdinate(i, Ordinate.Z, altitudes[i]);

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

        protected static void DegreesToRadians(ref Span<double> degrees)
        {
            for (int i = 0; i < degrees.Length; i++)
                degrees[i] *= D2R;
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

        protected static void RadiansToDegrees(ref Span<double> rad)
        {
            if (rad == null) return;
            for (int i = 0; i < rad.Length; i++)
                rad[i] *= R2D;
        }
        #endregion
    }
}
