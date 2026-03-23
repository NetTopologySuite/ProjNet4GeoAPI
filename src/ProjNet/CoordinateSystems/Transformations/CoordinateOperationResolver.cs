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

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;

/// <summary>
/// Resolves the best available coordinate operation candidate for a source/target pair.
/// </summary>
internal static class CoordinateOperationResolver
{
    /// <summary>
    /// Resolves the preferred transformation from identity and direct-operation candidates.
    /// </summary>
    /// <param name="source">Source coordinate system.</param>
    /// <param name="target">Target coordinate system.</param>
    /// <param name="directResolver">Resolver delegate for non-identity operations.</param>
    /// <returns>Best scored transformation, or <see langword="null"/> when none is available.</returns>
    internal static ICoordinateTransformation Resolve(
        CoordinateSystem source,
        CoordinateSystem target,
        Func<CoordinateSystem, CoordinateSystem, ICoordinateTransformation> directResolver)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (directResolver is null)
        {
            throw new ArgumentNullException(nameof(directResolver));
        }

        OperationCandidate bestCandidate = null;
        bestCandidate = SelectHigherScore(bestCandidate, CreateIdentityCandidate(source, target));

        var directCandidate = directResolver(source, target);
        if (directCandidate is not null)
        {
            bestCandidate = SelectHigherScore(bestCandidate, new OperationCandidate(directCandidate, 0));
        }

        return bestCandidate?.Transformation;
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

    private static OperationCandidate SelectHigherScore(OperationCandidate left, OperationCandidate right)
    {
        if (right is null)
        {
            return left;
        }

        if (left is null)
        {
            return right;
        }

        return right.Score > left.Score ? right : left;
    }

    private sealed class OperationCandidate(ICoordinateTransformation transformation, int score)
    {
        internal int Score { get; } = score;

        internal ICoordinateTransformation Transformation { get; } = transformation;
    }
}
