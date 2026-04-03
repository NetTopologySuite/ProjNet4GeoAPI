// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical McBryde-Thomas Flat-Polar Sine (No. 1) projection (<c>mbt_s</c>).
/// </summary>
/// <remarks>
/// McBryde-Thomas Flat-Polar Sine (No. 1) is the STS-family member of the McBryde-Thomas
/// series. Its numerical behavior is provided by <see cref="StsProjectionBase"/> with the
/// McBryde-Thomas constants for the first flat-polar sine variant.
/// </remarks>
internal sealed class McBrydeThomasFlatPolarSineProjection : StsProjectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McBrydeThomasFlatPolarSineProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public McBrydeThomasFlatPolarSineProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="McBrydeThomasFlatPolarSineProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public McBrydeThomasFlatPolarSineProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse, "McBryde_Thomas_Flat_Polar_Sine", 1.48875d, 1.36509d, false)
    {
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new McBrydeThomasFlatPolarSineProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }
}
