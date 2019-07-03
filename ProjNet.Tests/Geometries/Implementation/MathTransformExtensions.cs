using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems.Transformations;

namespace ProjNET.Tests.Geometries.Implementation
{
    public static class MathTransformExtensions
    {
        public static bool UseConverter { get; set; }= false;

        public static CoordinateSequence Transform(this MathTransform self, CoordinateSequence sequence)
        {
            if (UseConverter)
                return Transform(self, sequence, (SequenceCoordinateConverterBase)null);

            return Transform(self, sequence, (SequenceTransformerBase)null);
        }

        public static CoordinateSequence Transform(this MathTransform self, CoordinateSequence sequence, SequenceCoordinateConverterBase sc = null)
        {
            if (sc == null)
                sc = new SequenceCoordinateConverterBase();

            var res = sequence.Copy();
            var a = sc.ExtractRawCoordinatesFromSequence(res, out var xs, out int strideX,
                out var ys, out int strideY,
                out var zs, out int strideZ);

            self.Transform(xs, ys, zs, strideX, strideY, strideZ);

            sc.CopyRawCoordinatesToSequence(xs, strideX, ys, strideY, zs, strideZ, res);

            a.Invoke();

            return res;
            //throw new NotSupportedException();
        }

        public static CoordinateSequence Transform(this MathTransform self, CoordinateSequence sequence, SequenceTransformerBase st = null)
        {
            if (st == null)
                st = new SequenceTransformerBase();

            var res = sequence.Copy();
            st.Transform(self, res);

            return res;
        }



        public static Coordinate Transform(this MathTransform self, Coordinate coordinate)
        {
            var result = coordinate.Copy();
            if (coordinate is CoordinateZ)
                (result.X, result.Y, result.Z) = self.Transform(coordinate.X, coordinate.Y, coordinate.Z);
            else
                (result.X, result.Y) = self.Transform(coordinate.X, coordinate.Y);

            return result;
        }

        public static IList<Coordinate> TransformList(this MathTransform self, IList<Coordinate> coordinates)
        {
            var result = new List<Coordinate>(coordinates.Count);
            foreach (var coordinate in coordinates)
                result.Add(Transform(self, coordinate));

            return result;
        }

    }
}
