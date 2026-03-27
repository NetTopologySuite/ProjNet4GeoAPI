// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the geostationary satellite projection (<c>geos</c>).
/// </summary>
[Serializable]
internal class GeostationarySatelliteProjection : MapProjection
{
    private const double MaximumHeightRatio = 1e10;

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly bool flipAxis;
    private readonly double radiusP;
    private readonly double radiusP2;
    private readonly double radiusPInv2;
    private readonly double radiusG;
    private readonly double radiusG1;
    private readonly double c;
    private readonly bool ellipsoidal;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeostationarySatelliteProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public GeostationarySatelliteProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeostationarySatelliteProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public GeostationarySatelliteProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Geostationary_Satellite";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        double h = this.Parameters.GetParameterValue("h", "satellite_height");
        this.radiusG1 = h / this.semiMajor;
        if (this.radiusG1 <= 0d || this.radiusG1 > MaximumHeightRatio)
        {
            ArgumentGuard.ThrowArgument("Invalid value for h.");
        }

        this.radiusG = 1d + this.radiusG1;
        this.c = (this.radiusG * this.radiusG) - 1d;
        this.flipAxis = this.ReadFlipAxis();
        this.ellipsoidal = this.es != 0d;

        if (this.ellipsoidal)
        {
            this.radiusP = this.semiMinor / this.semiMajor;
            this.radiusP2 = this.radiusP * this.radiusP;
            this.radiusPInv2 = 1d / this.radiusP2;
        }
        else
        {
            this.radiusP = 1d;
            this.radiusP2 = 1d;
            this.radiusPInv2 = 1d;
        }
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new GeostationarySatelliteProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;

        if (this.ellipsoidal)
        {
            this.ForwardEllipsoidal(lambda, phi, out double xEllps, out double yEllps);
            lon = this.radius * xEllps;
            lat = this.radius * yEllps;
            return;
        }

        this.ForwardSpherical(lambda, phi, out double xSphere, out double ySphere);
        lon = this.radius * xSphere;
        lat = this.radius * ySphere;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;

        if (this.ellipsoidal)
        {
            this.InverseEllipsoidal(xx, yy, out double lambdaEllps, out double phiEllps);
            x = Adjust_lon(this.centralMeridian + lambdaEllps);
            y = phiEllps;
            return;
        }

        this.InverseSpherical(xx, yy, out double lambdaSphere, out double phiSphere);
        x = Adjust_lon(this.centralMeridian + lambdaSphere);
        y = phiSphere;
    }

    private bool ReadFlipAxis()
    {
        if (this.Parameters.ContainsKey("sweep_x"))
        {
            return this.Parameters.GetParameterValue("sweep_x") != 0d;
        }

        if (this.Parameters.ContainsKey("sweep_angle_axis"))
        {
            return this.Parameters.GetParameterValue("sweep_angle_axis") != 0d;
        }

        return false;
    }

    private void ForwardSpherical(double lambda, double phi, out double x, out double y)
    {
        double cosPhi = Math.Cos(phi);
        double vx = Math.Cos(lambda) * cosPhi;
        double vy = Math.Sin(lambda) * cosPhi;
        double vz = Math.Sin(phi);
        double tmp = this.radiusG - vx;

        if (this.flipAxis)
        {
            x = this.radiusG1 * Math.Atan(vy / Hypot(vz, tmp));
            y = this.radiusG1 * Math.Atan(vz / tmp);
        }
        else
        {
            x = this.radiusG1 * Math.Atan(vy / tmp);
            y = this.radiusG1 * Math.Atan(vz / Hypot(vy, tmp));
        }
    }

    private void ForwardEllipsoidal(double lambda, double phi, out double x, out double y)
    {
        double geocentricPhi = Math.Atan(this.radiusP2 * Math.Tan(phi));
        double cosGeocentricPhi = Math.Cos(geocentricPhi);
        double sinGeocentricPhi = Math.Sin(geocentricPhi);
        double radiusSurface = this.radiusP / Hypot(this.radiusP * cosGeocentricPhi, sinGeocentricPhi);

        double vx = radiusSurface * Math.Cos(lambda) * cosGeocentricPhi;
        double vy = radiusSurface * Math.Sin(lambda) * cosGeocentricPhi;
        double vz = radiusSurface * sinGeocentricPhi;

        if (((this.radiusG - vx) * vx) - (vy * vy) - (vz * vz * this.radiusPInv2) < 0d)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double tmp = this.radiusG - vx;
        if (this.flipAxis)
        {
            x = this.radiusG1 * Math.Atan(vy / Hypot(vz, tmp));
            y = this.radiusG1 * Math.Atan(vz / tmp);
        }
        else
        {
            x = this.radiusG1 * Math.Atan(vy / tmp);
            y = this.radiusG1 * Math.Atan(vz / Hypot(vy, tmp));
        }
    }

    private void InverseSpherical(double x, double y, out double lambda, out double phi)
    {
        double vx = -1d;
        double vy;
        double vz;

        if (this.flipAxis)
        {
            vz = Math.Tan(y / this.radiusG1);
            vy = Math.Tan(x / this.radiusG1) * Math.Sqrt(1d + (vz * vz));
        }
        else
        {
            vy = Math.Tan(x / this.radiusG1);
            vz = Math.Tan(y / this.radiusG1) * Math.Sqrt(1d + (vy * vy));
        }

        double a = (vy * vy) + (vz * vz) + (vx * vx);
        double b = 2d * this.radiusG * vx;
        double det = (b * b) - (4d * a * this.c);
        if (det < 0d)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double k = (-b - Math.Sqrt(det)) / (2d * a);
        vx = this.radiusG + (k * vx);
        vy *= k;
        vz *= k;

        lambda = Math.Atan2(vy, vx);
        phi = Math.Atan(vz / Hypot(vx, vy));
    }

    private void InverseEllipsoidal(double x, double y, out double lambda, out double phi)
    {
        double vx = -1d;
        double vy;
        double vz;

        if (this.flipAxis)
        {
            vz = Math.Tan(y / this.radiusG1);
            vy = Math.Tan(x / this.radiusG1) * Hypot(1d, vz);
        }
        else
        {
            vy = Math.Tan(x / this.radiusG1);
            vz = Math.Tan(y / this.radiusG1) * Hypot(1d, vy);
        }

        double vzOverRadiusP = vz / this.radiusP;
        double a = (vy * vy) + (vzOverRadiusP * vzOverRadiusP) + (vx * vx);
        double b = 2d * this.radiusG * vx;
        double det = (b * b) - (4d * a * this.c);
        if (det < 0d)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double k = (-b - Math.Sqrt(det)) / (2d * a);
        vx = this.radiusG + (k * vx);
        vy *= k;
        vz *= k;

        lambda = Math.Atan2(vy, vx);
        phi = Math.Atan(this.radiusPInv2 * vz / Hypot(vx, vy));
    }
}
