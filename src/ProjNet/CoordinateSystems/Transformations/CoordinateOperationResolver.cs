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
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNet.CoordinateSystems.Transformations
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the documented type.
    /// </summary>
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
