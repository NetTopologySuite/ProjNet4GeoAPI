// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Miller Oblated Stereographic projection (<c>mil_os</c>).
/// </summary>
/// <remarks>
/// Miller Oblated Stereographic is a two-coefficient specialization of
/// <see cref="ModifiedStereographicProjectionBase"/> attributed to Miller. It reuses the
/// shared modified-stereographic workflow with the Miller-specific polynomial coefficients.
/// </remarks>
internal sealed class MillerOblatedStereographicProjection : ModifiedStereographicProjectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MillerOblatedStereographicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public MillerOblatedStereographicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MillerOblatedStereographicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public MillerOblatedStereographicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse, "Miller_Oblated_Stereographic")
    {
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new MillerOblatedStereographicProjection(this.Parameters.ToProjectionParameter(), this);

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
        lambda0 = DegreesToRadians(20d);
        phi0 = DegreesToRadians(18d);
        semiMajor = this.semiMajor;
        es = 0d;
        coefficients = GetMilOsCoefficients();
        polynomialOrder = 2;
    }
}
