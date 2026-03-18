namespace ProjNet.CoordinateSystems.Transformations
{
    using System;
    using System.Collections.Generic;

    internal static class CoordinateOperationResolver
    {
        internal static ICoordinateTransformation Resolve(
            CoordinateSystem source,
            CoordinateSystem target,
            Func<CoordinateSystem, CoordinateSystem, ICoordinateTransformation> directResolver)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (directResolver == null)
            {
                throw new ArgumentNullException(nameof(directResolver));
            }

            var candidates = new List<OperationCandidate>();
            var identityCandidate = CreateIdentityCandidate(source, target);
            if (identityCandidate != null)
            {
                candidates.Add(identityCandidate);
            }

            var directCandidate = directResolver(source, target);
            if (directCandidate != null)
            {
                candidates.Add(new OperationCandidate(directCandidate, 0));
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            candidates.Sort((left, right) => right.Score.CompareTo(left.Score));
            return candidates[0].Transformation;
        }

        private static OperationCandidate CreateIdentityCandidate(CoordinateSystem source, CoordinateSystem target)
        {
            if (!ReferenceEquals(source, target) && !source.EqualParams(target))
            {
                return null;
            }

            int dimension = Math.Max(2, Math.Max(source.Dimension, target.Dimension));
            var transformation = new CoordinateTransformation(
                source,
                target,
                TransformType.Conversion,
                new IdentityMathTransform(dimension),
                string.Empty,
                string.Empty,
                -1,
                string.Empty,
                string.Empty);
            return new OperationCandidate(transformation, 1000);
        }

        private sealed class OperationCandidate
        {
            internal OperationCandidate(ICoordinateTransformation transformation, int score)
            {
                this.Transformation = transformation;
                this.Score = score;
            }

            internal int Score { get; }

            internal ICoordinateTransformation Transformation { get; }
        }
    }
}
