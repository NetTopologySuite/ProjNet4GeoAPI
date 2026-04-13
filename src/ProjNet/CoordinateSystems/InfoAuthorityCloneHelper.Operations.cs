// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Rebuilds immutable info-backed operation and projection objects while replacing top-level metadata.
/// </summary>
internal static partial class InfoAuthorityCloneHelper
{
    /// <summary>
    /// Creates a deep clone of the supplied projection with replacement authority metadata.
    /// </summary>
    /// <param name="projection">Projection to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned projection with the requested authority metadata.</returns>
    internal static Projection CloneWithAuthority(Projection projection, string authority, long authorityCode)
    {
        projection = ArgumentGuard.ThrowIfNull(projection, nameof(projection));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneProjection(projection, authority: authority, authorityCode: authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied coordinate operation with replacement authority metadata.
    /// </summary>
    /// <param name="coordinateOperation">Coordinate operation to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned coordinate operation with the requested authority metadata.</returns>
    internal static CoordinateOperation CloneWithAuthority(CoordinateOperation coordinateOperation, string authority, long authorityCode)
    {
        coordinateOperation = ArgumentGuard.ThrowIfNull(coordinateOperation, nameof(coordinateOperation));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneCoordinateOperation(coordinateOperation, authority: authority, authorityCode: authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied concatenated operation with replacement authority metadata.
    /// </summary>
    /// <param name="concatenatedOperation">Concatenated operation to clone.</param>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned concatenated operation with the requested authority metadata.</returns>
    internal static ConcatenatedOperation CloneWithAuthority(ConcatenatedOperation concatenatedOperation, string authority, long authorityCode)
    {
        concatenatedOperation = ArgumentGuard.ThrowIfNull(concatenatedOperation, nameof(concatenatedOperation));
        authority = ArgumentGuard.ThrowIfNull(authority, nameof(authority));
        return CloneConcatenatedOperation(concatenatedOperation, authority: authority, authorityCode: authorityCode);
    }

    /// <summary>
    /// Creates a deep clone of the supplied projection with a replacement name.
    /// </summary>
    /// <param name="projection">Projection to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned projection with the requested name.</returns>
    internal static Projection CloneWithName(Projection projection, string name)
    {
        projection = ArgumentGuard.ThrowIfNull(projection, nameof(projection));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneProjection(projection, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied coordinate operation with a replacement name.
    /// </summary>
    /// <param name="coordinateOperation">Coordinate operation to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned coordinate operation with the requested name.</returns>
    internal static CoordinateOperation CloneWithName(CoordinateOperation coordinateOperation, string name)
    {
        coordinateOperation = ArgumentGuard.ThrowIfNull(coordinateOperation, nameof(coordinateOperation));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneCoordinateOperation(coordinateOperation, name: name);
    }

    /// <summary>
    /// Creates a deep clone of the supplied concatenated operation with a replacement name.
    /// </summary>
    /// <param name="concatenatedOperation">Concatenated operation to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <returns>A cloned concatenated operation with the requested name.</returns>
    internal static ConcatenatedOperation CloneWithName(ConcatenatedOperation concatenatedOperation, string name)
    {
        concatenatedOperation = ArgumentGuard.ThrowIfNull(concatenatedOperation, nameof(concatenatedOperation));
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        return CloneConcatenatedOperation(concatenatedOperation, name: name);
    }

    private static Projection CloneProjection(IProjection projection, string? authority = null, long? authorityCode = null, string? name = null)
    {
        return new Projection(
            projection.ClassName,
            CloneProjectionParameters(projection),
            name ?? projection.Name,
            authority ?? projection.Authority,
            authorityCode ?? projection.AuthorityCode,
            projection.Alias,
            projection.Remarks,
            projection.Abbreviation);
    }

    private static CoordinateOperation CloneCoordinateOperation(
        CoordinateOperation coordinateOperation,
        string? authority = null,
        long? authorityCode = null,
        string? name = null)
    {
        return new CoordinateOperation(
            coordinateOperation.MethodName,
            CloneParameters(coordinateOperation.Parameters),
            CloneCoordinateSystem(coordinateOperation.SourceCoordinateSystem),
            CloneCoordinateSystem(coordinateOperation.TargetCoordinateSystem),
            name ?? coordinateOperation.Name,
            authority ?? coordinateOperation.Authority,
            authorityCode ?? coordinateOperation.AuthorityCode,
            coordinateOperation.Alias,
            coordinateOperation.Abbreviation,
            coordinateOperation.Remarks);
    }

    private static ConcatenatedOperation CloneConcatenatedOperation(
        ConcatenatedOperation concatenatedOperation,
        string? authority = null,
        long? authorityCode = null,
        string? name = null)
    {
        return new ConcatenatedOperation(
            CloneCoordinateOperations(concatenatedOperation.Steps),
            CloneCoordinateSystem(concatenatedOperation.SourceCoordinateSystem),
            CloneCoordinateSystem(concatenatedOperation.TargetCoordinateSystem),
            name ?? concatenatedOperation.Name,
            authority ?? concatenatedOperation.Authority,
            authorityCode ?? concatenatedOperation.AuthorityCode,
            concatenatedOperation.Alias,
            concatenatedOperation.Abbreviation,
            concatenatedOperation.Remarks);
    }

    private static List<ProjectionParameter> CloneProjectionParameters(IProjection projection)
    {
        var clone = new List<ProjectionParameter>(projection.NumParameters);
        for (int i = 0; i < projection.NumParameters; i++)
        {
            ProjectionParameter parameter = projection.GetParameter(i);
            clone.Add(new ProjectionParameter(parameter.Name, parameter.Value));
        }

        return clone;
    }

    private static List<Parameter> CloneParameters(IReadOnlyList<Parameter> parameters)
    {
        var clone = new List<Parameter>(parameters.Count);
        for (int i = 0; i < parameters.Count; i++)
        {
            clone.Add(new Parameter(parameters[i].Name, parameters[i].Value));
        }

        return clone;
    }

    private static List<CoordinateOperation> CloneCoordinateOperations(IReadOnlyList<CoordinateOperation> steps)
    {
        var clone = new List<CoordinateOperation>(steps.Count);
        for (int i = 0; i < steps.Count; i++)
        {
            clone.Add(CloneCoordinateOperation(steps[i]));
        }

        return clone;
    }

    private static BoundTransformation CloneBoundTransformation(BoundTransformation transformation)
    {
        if (transformation.Wgs84Parameters is not null)
        {
            return new BoundTransformation(
                transformation.MethodName,
                CloneWgs84ConversionInfo(transformation.Wgs84Parameters));
        }

        return new BoundTransformation(
            transformation.MethodName,
            ArgumentGuard.ThrowIfNull(transformation.ParameterFileName, nameof(transformation.ParameterFileName)));
    }

    private static VerticalBoundGridTransformation? CloneVerticalBoundGridTransformation(VerticalBoundGridTransformation? transformation)
    {
        if (transformation is null)
        {
            return null;
        }

        return new VerticalBoundGridTransformation(
            transformation.MethodName,
            transformation.ParameterFileName,
            CloneCompoundCoordinateSystem(transformation.HubCoordinateSystem));
    }
}
