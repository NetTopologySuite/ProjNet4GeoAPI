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
/// Implements the spherical Transverse Cylindrical Equal Area projection (<c>tcea</c>).
/// </summary>
[Serializable]
internal class TransverseCylindricalEqualAreaProjection : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double inverseScaleFactor;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransverseCylindricalEqualAreaProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public TransverseCylindricalEqualAreaProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransverseCylindricalEqualAreaProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public TransverseCylindricalEqualAreaProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse)
    {
        this.Name = "Transverse_Cylindrical_Equal_Area";
        this.radius = this.semiMajor;
        this.inverseRadius = 1d / this.radius;
        this.inverseScaleFactor = 1d / this.scaleFactor;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new TransverseCylindricalEqualAreaProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);

        lon = this.radius * (Math.Cos(lat) * Math.Sin(lambda)) * this.inverseScaleFactor;
        lat = this.radius * this.scaleFactor * (Math.Atan2(Math.Tan(lat), Math.Cos(lambda)) - this.latOrigin);
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double phiPrime = (y * this.inverseRadius * this.inverseScaleFactor) + this.latOrigin;
        double xScaled = x * this.scaleFactor * this.inverseRadius;
        double t = Math.Sqrt(Math.Max(0d, 1d - (xScaled * xScaled)));

        y = Math.Asin(Clamp(t * Math.Sin(phiPrime), -1d, 1d));
        x = Adjust_lon(this.centralMeridian + Math.Atan2(xScaled, t * Math.Cos(phiPrime)));
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }
}
