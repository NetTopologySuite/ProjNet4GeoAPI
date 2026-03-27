// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements PROJ's <c>leac</c> projection by normalizing parameters to the Albers implementation.
/// </summary>
/// <remarks>
/// Maps the appropriate pole (north or south) as the first Albers standard parallel and the
/// user-supplied <c>lat_1</c> as the second. Set the <c>south</c> parameter to a non-zero
/// value to select the southern hemisphere variant.
/// </remarks>
[Serializable]
internal sealed class LambertEqualAreaConicProjection : AlbersProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LambertEqualAreaConicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public LambertEqualAreaConicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    private LambertEqualAreaConicProjection(IEnumerable<ProjectionParameter> parameters, LambertEqualAreaConicProjection? inverse)
        : base(NormalizeParameters(parameters), inverse)
    {
        this.Name = "Lambert_Equal_Area_Conic";
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new LambertEqualAreaConicProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    private static IEnumerable<ProjectionParameter> NormalizeParameters(IEnumerable<ProjectionParameter> parameters)
    {
        var normalized = new ProjectionParameterSet(parameters);
        double lat1 = normalized.GetParameterValue("lat_1", "standard_parallel_1");
        bool south = Math.Abs(normalized.GetOptionalParameterValue("south", 0d)) > 0d;

        // PROJ leac uses the pole (+90 / -90) as first standard parallel and lat_1 as second.
        normalized.SetParameterValue("lat_1", lat1);
        normalized.SetParameterValue("standard_parallel_1", south ? -90d : 90d);
        normalized.SetParameterValue("standard_parallel_2", lat1);
        return normalized.ToProjectionParameter();
    }
}
