// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.CoordinateSystems.Transformations;

using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Tests the lightweight candidate scoring in <see cref="CoordinateOperationResolver"/>.
/// </summary>
public class CoordinateOperationResolverTests
{
    /// <summary>
    /// Verifies that a direct non-identity candidate with authority metadata wins over the synthetic identity candidate
    /// when source and target are parameter-equivalent but carry distinct authority identities.
    /// </summary>
    [Fact]
    public void ResolveWithDistinctAuthorityDirectCandidatePrefersDirectTransformation()
    {
        GeographicCoordinateSystem source = GeographicCoordinateSystem.WGS84.WithAuthority("EPSG", 4326);
        GeographicCoordinateSystem target = GeographicCoordinateSystem.WGS84.WithAuthority("IGNF", 94326);
        CoordinateTransformation directCandidate = CreateDirectCandidate(source, target, "EPSG", 1234);

        ICoordinateTransformation? resolved = CoordinateOperationResolver.Resolve(source, target, (_, _) => directCandidate);

        Assert.Same(directCandidate, resolved);
    }

    /// <summary>
    /// Verifies that the resolver keeps the synthetic identity candidate when the direct non-identity candidate lacks
    /// authority metadata and therefore does not receive the higher distinct-authority score.
    /// </summary>
    [Fact]
    public void ResolveWithoutDirectAuthorityMetadataPrefersIdentityTransformation()
    {
        GeographicCoordinateSystem source = GeographicCoordinateSystem.WGS84.WithAuthority("EPSG", 4326);
        GeographicCoordinateSystem target = GeographicCoordinateSystem.WGS84.WithAuthority("IGNF", 94326);
        CoordinateTransformation directCandidate = CreateDirectCandidate(source, target, string.Empty, -1);

        ICoordinateTransformation resolved = Assert.IsAssignableFrom<ICoordinateTransformation>(
            CoordinateOperationResolver.Resolve(source, target, (_, _) => directCandidate));

        Assert.NotSame(directCandidate, resolved);
        Assert.IsType<IdentityMathTransform>(resolved.MathTransform);
        Assert.Equal(string.Empty, resolved.Authority);
        Assert.Equal(-1, resolved.AuthorityCode);
    }

    private static CoordinateTransformation CreateDirectCandidate(
        GeographicCoordinateSystem source,
        GeographicCoordinateSystem target,
        string authority,
        long authorityCode)
    {
        return new CoordinateTransformation(
            source,
            target,
            TransformType.Transformation,
            new PrimeMeridianTransform(PrimeMeridian.Greenwich, PrimeMeridian.Paris),
            "Prime meridian shift",
            authority,
            authorityCode,
            string.Empty,
            string.Empty);
    }
}
