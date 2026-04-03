// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Represents the geographic identity projection used by PROJ's latlong/longlat aliases.
/// </summary>
/// <remarks>
/// This projection is the geographic identity mapping: longitude and latitude are passed
/// through unchanged except for the configured central-meridian and latitude-of-origin
/// offsets. It therefore corresponds to the trivial relation <c>x = lon</c>,
/// <c>y = lat</c> in normalized geographic coordinates.
/// </remarks>
internal class LatLongProjection : MapProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LatLongProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public LatLongProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LatLongProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public LatLongProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "LatLong";
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new LatLongProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        lon -= this.centralMeridian;
        lat -= this.latOrigin;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        x += this.centralMeridian;
        y += this.latOrigin;
    }
}
