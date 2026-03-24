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

/// <summary>
/// Shared implementation for simple spherical conic projections in <c>sconics.cpp</c>.
/// </summary>
[Serializable]
internal abstract class SimpleConicProjectionBase : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly SimpleConicType type;
    private readonly double n;
    private readonly double rhoC;
    private readonly double rho0;
    private readonly double sig;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleConicProjectionBase"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    /// <param name="type">Simple conic variant.</param>
    /// <param name="name">Projection name.</param>
    protected SimpleConicProjectionBase(
        IEnumerable<ProjectionParameter> parameters,
        MapProjection inverse,
        SimpleConicType type,
        string name)
        : base(parameters, inverse)
    {
        this.type = type;
        this.Name = name;
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        double phi1 = DegreesToRadians(this.Parameters.GetParameterValue("lat_1", "standard_parallel_1"));
        double phi2 = DegreesToRadians(this.Parameters.GetParameterValue("lat_2", "standard_parallel_2"));
        double delta = 0.5d * (phi2 - phi1);
        this.sig = 0.5d * (phi2 + phi1);

        if (Math.Abs(delta) < Eps10 || Math.Abs(this.sig) < Eps10)
        {
            throw new ArgumentException("Illegal value for lat_1 and lat_2: |lat_1 - lat_2| and |lat_1 + lat_2| should be > 0.");
        }

        switch (type)
        {
            case SimpleConicType.Tissot:
            {
                this.n = Math.Sin(this.sig);
                double cs = Math.Cos(delta);
                this.rhoC = (this.n / cs) + (cs / this.n);
                double tissotDomain = (this.rhoC - (2d * Math.Sin(this.latOrigin))) / this.n;
                if (tissotDomain < 0d)
                {
                    throw new ArgumentException("Input data outside projection domain.");
                }

                this.rho0 = Math.Sqrt(tissotDomain);
                break;
            }

            case SimpleConicType.Murdoch1:
                this.rhoC = (Math.Sin(delta) / (delta * Math.Tan(this.sig))) + this.sig;
                this.rho0 = this.rhoC - this.latOrigin;
                this.n = Math.Sin(this.sig);
                break;

            case SimpleConicType.Murdoch2:
            {
                double cosDelta = Math.Cos(delta);
                if (cosDelta < 0d)
                {
                    throw new ArgumentException("Input data outside projection domain.");
                }

                double cs = Math.Sqrt(cosDelta);
                this.rhoC = cs / Math.Tan(this.sig);
                this.rho0 = this.rhoC + Math.Tan(this.sig - this.latOrigin);
                this.n = Math.Sin(this.sig) * cs;
                break;
            }

            case SimpleConicType.Murdoch3:
                this.rhoC = (delta / (Math.Tan(this.sig) * Math.Tan(delta))) + this.sig;
                this.rho0 = this.rhoC - this.latOrigin;
                this.n = Math.Sin(this.sig) * Math.Sin(delta) * Math.Tan(delta) / (delta * delta);
                break;

            case SimpleConicType.Euler:
                this.n = Math.Sin(this.sig) * Math.Sin(delta) / delta;
                delta *= 0.5d;
                this.rhoC = (delta / (Math.Tan(delta) * Math.Tan(this.sig))) + this.sig;
                this.rho0 = this.rhoC - this.latOrigin;
                break;

            case SimpleConicType.Vitkovsky1:
            {
                double cs = Math.Tan(delta);
                this.n = cs * Math.Sin(this.sig) / delta;
                this.rhoC = (delta / (cs * Math.Tan(this.sig))) + this.sig;
                this.rho0 = this.rhoC - this.latOrigin;
                break;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double rho = this.type == SimpleConicType.Murdoch2
            ? this.rhoC + Math.Tan(this.sig - lat)
            : this.rhoC - lat;
        double theta = lambda * this.n;

        lon = this.radius * rho * Math.Sin(theta);
        lat = this.radius * (this.rho0 - (rho * Math.Cos(theta)));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = this.rho0 - (y * this.inverseRadius);
        double rho = Hypot(xUnit, yUnit);
        if (this.n < 0d)
        {
            rho = -rho;
            xUnit = -xUnit;
            yUnit = -yUnit;
        }

        double lambda = Math.Atan2(xUnit, yUnit) / this.n;
        double phi = this.type == SimpleConicType.Murdoch2
            ? this.sig - Math.Atan(rho - this.rhoC)
            : this.rhoC - rho;

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}



