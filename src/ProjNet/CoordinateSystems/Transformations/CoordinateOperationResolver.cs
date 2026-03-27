// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

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
    internal static ICoordinateTransformation? Resolve(
        CoordinateSystem source,
        CoordinateSystem target,
        Func<CoordinateSystem, CoordinateSystem, ICoordinateTransformation?> directResolver)
    {
        source = ArgumentGuard.ThrowIfNull(source, nameof(source));
        target = ArgumentGuard.ThrowIfNull(target, nameof(target));
        directResolver = ArgumentGuard.ThrowIfNull(directResolver, nameof(directResolver));

        OperationCandidate? bestCandidate = null;
        bestCandidate = SelectHigherScore(bestCandidate, CreateIdentityCandidate(source, target));

        var directCandidate = directResolver(source, target);
        if (directCandidate is not null)
        {
            bestCandidate = SelectHigherScore(bestCandidate, new OperationCandidate(directCandidate, 0));
        }

        return bestCandidate?.Transformation;
    }

    private static OperationCandidate? CreateIdentityCandidate(CoordinateSystem source, CoordinateSystem target)
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

    private static OperationCandidate? SelectHigherScore(OperationCandidate? left, OperationCandidate? right)
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
