// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the modified stereographic projection of Alaska (<c>alsk</c>).
/// </summary>
/// <remarks>
/// Modified Stereographic of Alaska is the Alaska specialization of
/// <see cref="ModifiedStereographicProjectionBase"/>. It uses the shared complex polynomial
/// workflow with separate spherical and ellipsoidal coefficient sets for the Alaska map.
/// </remarks>
internal sealed class ModifiedStereographicAlaskaProjection : ModifiedStereographicProjectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModifiedStereographicAlaskaProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public ModifiedStereographicAlaskaProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModifiedStereographicAlaskaProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public ModifiedStereographicAlaskaProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse, "Modified_Stereographic_Of_Alaska")
    {
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new ModifiedStereographicAlaskaProjection(this.Parameters.ToProjectionParameter(), this);

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
        lambda0 = DegreesToRadians(-152d);
        phi0 = DegreesToRadians(64d);
        polynomialOrder = 5;

        if (this.es != 0d)
        {
            semiMajor = Ellipsoid.Clarke1866.SemiMajorAxis;
            es = 0.00676866d;
            coefficients = GetAlskEllipsoidalCoefficients();
        }
        else
        {
            semiMajor = 6370997d;
            es = 0d;
            coefficients = GetAlskSphericalCoefficients();
        }
    }
}
