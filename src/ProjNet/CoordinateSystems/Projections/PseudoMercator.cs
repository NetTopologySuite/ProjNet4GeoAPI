// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Pseudo-Mercator (Web Mercator) projection (EPSG:3856).
/// </summary>
/// <remarks>
/// Applies a spherical Mercator formula by treating the ellipsoidal semi-major axis as
/// the sphere radius and forcing the scale factor to 1. Geodetic latitude is projected
/// without ellipsoidal correction, producing the projection used by most web mapping services.
/// </remarks>
[Serializable]
internal class PseudoMercator : Mercator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PseudoMercator"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public PseudoMercator(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PseudoMercator"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    protected PseudoMercator(IEnumerable<ProjectionParameter> parameters, Mercator inverse)
        : base(VerifyParameters(parameters), inverse)
    {
        this.Name = "Pseudo-Mercator";
        this.Authority = "EPSG";
        this.AuthorityCode = 3856;
    }

    private static IEnumerable<ProjectionParameter> VerifyParameters(IEnumerable<ProjectionParameter> parameters)
    {
        var p = new ProjectionParameterSet(parameters);
        double semi_major = p.GetParameterValue("semi_major");
        p.SetParameterValue("semi_minor", semi_major);
        p.SetParameterValue("scale_factor", 1);

        return p.ToProjectionParameter();
    }

    /// <inheritdoc/>
    public override MathTransform Inverse()
    {
        if (this.inverse == null)
        {
            this.inverse = new PseudoMercator(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }
}
