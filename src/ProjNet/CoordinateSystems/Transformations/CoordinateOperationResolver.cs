// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// Resolves the best available coordinate operation candidate for a source/target pair.
/// </summary>
/// <remarks>
/// Resolution is intentionally lightweight: it scores at most two candidates,
/// an identity operation when source and target are parameter-equivalent, and a
/// direct non-identity operation returned by the supplied resolver. The higher
/// score wins, so exact identity is preferred whenever it is valid.
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/operations_computation.html">PROJ: computation of coordinate operations between two CRS.</seealso>
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

        ICoordinateTransformation? directCandidate = directResolver(source, target);
        if (directCandidate is not null)
        {
            int directScore = GetDirectCandidateScore(source, target, directCandidate);
            bestCandidate = SelectHigherScore(bestCandidate, new OperationCandidate(directCandidate, directScore));
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

        return left is null ? right : right.Score > left.Score ? right : left;
    }

    private static int GetDirectCandidateScore(
        CoordinateSystem source,
        CoordinateSystem target,
        ICoordinateTransformation directCandidate)
    {
        return !ReferenceEquals(source, target)
            && source.EqualParams(target)
            && HasDistinctAuthorityIdentity(source, target)
            && HasAuthorityMetadata(directCandidate)
            && directCandidate.MathTransform is not IdentityMathTransform
            ? 1001
            : 0;
    }

    private static bool HasAuthorityMetadata(ICoordinateTransformation transformation)
    {
        return transformation is not null
            && !string.IsNullOrWhiteSpace(transformation.Authority)
            && transformation.AuthorityCode >= 0;
    }

    private static bool HasDistinctAuthorityIdentity(CoordinateSystem source, CoordinateSystem target)
    {
        return !string.IsNullOrWhiteSpace(source.Authority)
            && !string.IsNullOrWhiteSpace(target.Authority)
            && source.AuthorityCode > 0
            && target.AuthorityCode > 0
            && (!source.Authority.Equals(target.Authority, StringComparison.OrdinalIgnoreCase)
                || source.AuthorityCode != target.AuthorityCode);
    }

    private sealed class OperationCandidate(ICoordinateTransformation transformation, int score)
    {
        internal int Score { get; } = score;

        internal ICoordinateTransformation Transformation { get; } = transformation;
    }
}
