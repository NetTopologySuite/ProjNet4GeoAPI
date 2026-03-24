// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the modified stereographic projection of Alaska (<c>alsk</c>).
/// </summary>
[Serializable]
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
    public ModifiedStereographicAlaskaProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse, "Modified_Stereographic_Of_Alaska")
    {
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new ModifiedStereographicAlaskaProjection(this.Parameters.ToProjectionParameter(), this);
        }

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
            semiMajor = 6378206.4d;
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

