// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Oblique Mercator map projection (EPSG method 9815).
/// </summary>
/// <remarks>
/// <para>The Oblique Mercator is a variant of the Hotine Oblique Mercator in which
/// false easting and northing are referenced to the natural origin of the projection
/// rather than to the centre of the initial line. This variant corresponds to EPSG
/// method 9815.</para>
/// </remarks>
internal class ObliqueMercatorProjection : HotineObliqueMercatorProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObliqueMercatorProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public ObliqueMercatorProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObliqueMercatorProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public ObliqueMercatorProjection(IEnumerable<ProjectionParameter> parameters, ObliqueMercatorProjection? inverse)
        : base(parameters, inverse)
    {
        this.AuthorityCode = 9815;
        this.Name = "Oblique_Mercator";
    }

    /// <inheritdoc/>
    public override MathTransform Inverse()
    {
        this.inverse ??= new ObliqueMercatorProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }
}
