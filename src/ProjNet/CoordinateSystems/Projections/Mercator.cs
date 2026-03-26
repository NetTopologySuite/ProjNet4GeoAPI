// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2002 Urban Science Applications, Inc.
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from GeoTools.NET.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Mercator projection.
/// </summary>
/// <remarks>
/// <para>This map projection introduced in 1569 by Gerardus Mercator. It is often described as a cylindrical projection,
/// but it must be derived mathematically. The meridians are equally spaced, parallel vertical lines, and the
/// parallels of latitude are parallel, horizontal straight lines, spaced farther and farther apart as their distance
/// from the Equator increases. This projection is widely used for navigation charts, because any straight line
/// on a Mercator-projection map is a line of constant true bearing that enables a navigator to plot a straight-line
/// course. It is less practical for world maps because the scale is distorted; areas farther away from the equator
/// appear disproportionately large. On a Mercator projection, for example, the landmass of Greenland appears to be
/// greater than that of the continent of South America; in actual area, Greenland is smaller than the Arabian Peninsula.
/// </para>
/// </remarks>
[Serializable]
internal class Mercator : MapProjection
{
    /// <summary>
    /// Scale coefficient at the projection origin.
    /// </summary>
    private readonly double k0;

    /// <summary>
    /// Initializes a new instance of the <see cref="Mercator"/> class.
    /// </summary>
    /// <param name="parameters">List of parameters to initialize the projection.</param>
    public Mercator(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Mercator"/> class.
    /// </summary>
    /// <param name="parameters">List of parameters to initialize the projection.</param>
    /// <param name="isInverse">The inverse projection instance, or <see langword="null"/> for a forward projection.</param>
    /// <remarks>
    /// <para>The parameters this projection expects are listed below.</para>
    /// <list type="table">
    /// <listheader><term>Items</term><description>Descriptions</description></listheader>
    /// <item><term>central_meridian</term><description>The longitude of the point from which the values of both the geographical coordinates on the ellipsoid and the grid coordinates on the projection are deemed to increment or decrement for computational purposes. Alternatively it may be considered as the longitude of the point which in the absence of application of false coordinates has grid coordinates of (0,0).</description></item>
    /// <item><term>latitude_of_origin</term><description>The latitude of the point from which the values of both the geographical coordinates on the ellipsoid and the grid coordinates on the projection are deemed to increment or decrement for computational purposes. Alternatively it may be considered as the latitude of the point which in the absence of application of false coordinates has grid coordinates of (0,0).</description></item>
    /// <item><term>scale_factor</term><description>The factor by which the map grid is reduced or enlarged during the projection process, defined by its value at the natural origin.</description></item>
    /// <item><term>false_easting</term><description>Since the natural origin may be at or near the centre of the projection and under normal coordinate circumstances would thus give rise to negative coordinates over parts of the mapped area, this origin is usually given false coordinates which are large enough to avoid this inconvenience. The False Easting, FE, is the easting value assigned to the abscissa (east).</description></item>
    /// <item><term>false_northing</term><description>Since the natural origin may be at or near the centre of the projection and under normal coordinate circumstances would thus give rise to negative coordinates over parts of the mapped area, this origin is usually given false coordinates which are large enough to avoid this inconvenience. The False Northing, FN, is the northing value assigned to the ordinate.</description></item>
    /// </list>
    /// </remarks>
    protected Mercator(IEnumerable<ProjectionParameter> parameters, Mercator isInverse)
        : base(parameters, isInverse)
    {
        this.Authority = "EPSG";
        var scaleFactor = this.GetParameter("scale_factor");

        // This is a two standard parallel Mercator projection (2SP).
        if (scaleFactor is null)
        {
            this.k0 = Math.Cos(this.latOrigin) / Math.Sqrt(1.0 - (this.es * Math.Sin(this.latOrigin) * Math.Sin(this.latOrigin)));
            this.AuthorityCode = 9805;
            this.Name = "Mercator_2SP";
        }

        // This is a one standard parallel Mercator projection (1SP).
        else
        {
            this.k0 = scaleFactor.Value;
            this.Name = "Mercator_1SP";
        }
    }

    /// <summary>
    /// Converts coordinates in radians to projected meters.
    /// </summary>
    /// <param name="lon">The longitude of the point in radians.</param>
    /// <param name="lat">The latitude of the point in radians.</param>
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        if (double.IsNaN(lon) || double.IsNaN(lat))
        {
            lon = double.NaN;
            lat = double.NaN;
            return;
        }

        double dLongitude = lon;
        double dLatitude = lat;

        // Forward equations
        if (Math.Abs(Math.Abs(dLatitude) - HalfPi) <= Epsln)
        {
            ArgumentGuard.ThrowArgument("Transformation cannot be computed at the poles.");
        }

        double esinphi = this.e * Math.Sin(dLatitude);
        lon = this.semiMajor * this.k0 * (dLongitude - this.centralMeridian);
        lat = this.semiMajor * this.k0 * Math.Log(Math.Tan((PI * 0.25) + (dLatitude * 0.5)) *
                                          Math.Pow((1 - esinphi) / (1 + esinphi), this.e * 0.5));
    }

    /// <summary>
    /// Converts coordinates in projected meters to decimal degrees.
    /// </summary>
    /// <param name="x">The x-ordinate in projected meters.</param>
    /// <param name="y">The y-ordinate in projected meters.</param>
    protected override void MetersToRadians(ref double x, ref double y)
    {
        // Inverse equations
        double dX = x;
        double dY = y;
        double ts = Math.Exp(-dY / (this.semiMajor * this.k0)); // t

        double chi = HalfPi - (2 * Math.Atan(ts));
        double e4 = Math.Pow(this.e, 4);
        double e6 = Math.Pow(this.e, 6);
        double e8 = Math.Pow(this.e, 8);

        y = chi + (((this.es * 0.5) + (5 * e4 / 24) + (e6 / 12) + (13 * e8 / 360)) * Math.Sin(2 * chi))
                + (((7 * e4 / 48) + (29 * e6 / 240) + (811 * e8 / 11520)) * Math.Sin(4 * chi)) +
                (+((7 * e6 / 120) + (81 * e8 / 1120)) * Math.Sin(6 * chi)) +
                (+(4279 * e8 / 161280) * Math.Sin(8 * chi));

        x = (dX / (this.semiMajor * this.k0)) + this.centralMeridian;
    }

    /// <summary>
    /// Returns the inverse of this projection.
    /// </summary>
    /// <returns>IMathTransform that is the reverse of the current projection.</returns>
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new Mercator(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }
}
