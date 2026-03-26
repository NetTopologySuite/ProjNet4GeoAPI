// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements PROJ's <c>ups</c> projection with fixed UPS defaults.
/// </summary>
[Serializable]
internal sealed class UpsProjection : PolarStereographicProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpsProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public UpsProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    private UpsProjection(IEnumerable<ProjectionParameter> parameters, UpsProjection inverse)
        : base(NormalizeParameters(parameters), inverse)
    {
        this.Name = "Universal_Polar_Stereographic";
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new UpsProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    private static IEnumerable<ProjectionParameter> NormalizeParameters(IEnumerable<ProjectionParameter> parameters)
    {
        var normalized = new ProjectionParameterSet(parameters);
        bool south = Math.Abs(normalized.GetOptionalParameterValue("south", 0d)) > 0d;

        normalized.SetParameterValue("latitude_of_origin", south ? -90d : 90d);
        normalized.SetParameterValue("central_meridian", 0d);
        normalized.SetParameterValue("scale_factor", 0.994d);
        normalized.SetParameterValue("false_easting", 2000000d);
        normalized.SetParameterValue("false_northing", 2000000d);
        return normalized.ToProjectionParameter();
    }
}

