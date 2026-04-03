// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the modified stereographic projection of the 50 U.S. (<c>gs50</c>).
/// </summary>
/// <remarks>
/// Modified Stereographic of 50 U.S. is a Snyder-era regional specialization of
/// <see cref="ModifiedStereographicProjectionBase"/>. It switches between the spherical and
/// ellipsoidal coefficient sets defined for the 50-state composite layout.
/// </remarks>
internal sealed class ModifiedStereographic50USProjection : ModifiedStereographicProjectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModifiedStereographic50USProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public ModifiedStereographic50USProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModifiedStereographic50USProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public ModifiedStereographic50USProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse, "Modified_Stereographic_Of_50_US")
    {
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new ModifiedStereographic50USProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void ConfigureVariant(
        out double lambda0,
        out double phi0,
        out double semiMajor,
        out double es,
        out ComplexNumber[] coefficients,
        out int polynomialOrder)
    {
        lambda0 = DegreesToRadians(-120d);
        phi0 = DegreesToRadians(45d);
        polynomialOrder = 9;

        if (this.es != 0d)
        {
            semiMajor = 6378206.4d;
            es = 0.00676866d;
            coefficients = GetGs50EllipsoidalCoefficients();
        }
        else
        {
            semiMajor = 6370997d;
            es = 0d;
            coefficients = GetGs50SphericalCoefficients();
        }
    }
}
