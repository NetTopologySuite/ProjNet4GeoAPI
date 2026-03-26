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
/// Implements the Transverse Mercator map projection.
/// </summary>
/// <remarks>
/// <para>Universal (UTM) and Modified (MTM) Transverse Mercator projections. This
/// is a cylindrical projection in which the cylinder has been rotated 90°.
/// Instead of being tangent to the equator (or to another standard latitude),
/// it is tangent to a central meridian. Distortion increases with distance from
/// the central meridian. The Transverse Mercator projection is appropriate for
/// regions which have a greater extent north-south than east-west.</para>
///
/// <para>Reference: John P. Snyder, Map Projections — A Working Manual,
/// U.S. Geological Survey Professional Paper 1395, 1987.</para>
/// </remarks>
[Serializable]
internal class TransverseMercator : MapProjection
{
    // Maximum difference allowed when comparing real numbers.
    private const double EPSILON = 1E-6;

    // A derived quantity of eccentricity, computed by <c>e'² = (a²-b²)/b² = es/(1-es)</c>
    // where <c>a</c> is the semi-major axis length and <c>b</c> is the semi-minor axis
    // length.
    private readonly double esp;

    // Meridian distance at the latitude of origin.
    // Used for calculations for the ellipsoid.
    private readonly double ml0;

    private readonly double reciprocSemiMajor;

    /// <summary>
    /// Fraction constant 1/1 used in series expansion terms.
    /// </summary>
    private const double FC1 = 1.00000000000000000000000;

    /// <summary>
    /// Fraction constant 1/2 used in series expansion terms.
    /// </summary>
    private const double FC2 = 0.50000000000000000000000;

    /// <summary>
    /// Fraction constant 1/6 used in series expansion terms.
    /// </summary>
    private const double FC3 = 0.16666666666666666666666;

    /// <summary>
    /// Fraction constant 1/12 used in series expansion terms.
    /// </summary>
    private const double FC4 = 0.08333333333333333333333;

    /// <summary>
    /// Fraction constant 1/20 used in series expansion terms.
    /// </summary>
    private const double FC5 = 0.05000000000000000000000;

    /// <summary>
    /// Fraction constant 1/30 used in series expansion terms.
    /// </summary>
    private const double FC6 = 0.03333333333333333333333;

    /// <summary>
    /// Fraction constant 1/42 used in series expansion terms.
    /// </summary>
    private const double FC7 = 0.02380952380952380952380;

    /// <summary>
    /// Fraction constant 1/56 used in series expansion terms.
    /// </summary>
    private const double FC8 = 0.01785714285714285714285;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransverseMercator"/> class.
    /// </summary>
    /// <param name="parameters">List of parameters to initialize the projection.</param>
    public TransverseMercator(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransverseMercator"/> class.
    /// </summary>
    /// <param name="parameters">List of parameters to initialize the projection.</param>
    /// <param name="inverse">The inverse projection instance, or <see langword="null"/> for a forward projection.</param>
    /// <remarks>
    /// <para>The parameters this projection expects are listed below.</para>
    /// <list type="table">
    /// <listheader><term>Parameter</term><description>Description</description></listheader>
    /// <item><term>semi_major</term><description>Semi-major axis radius of the ellipsoid.</description></item>
    /// <item><term>semi_minor</term><description>Semi-minor axis radius of the ellipsoid.</description></item>
    /// <item><term>scale_factor</term><description>Scale factor at the central meridian.</description></item>
    /// <item><term>central_meridian</term><description>Longitude of the central meridian.</description></item>
    /// <item><term>latitude_of_origin</term><description>Latitude of the projection origin.</description></item>
    /// <item><term>false_easting</term><description>Easting assigned to the natural origin.</description></item>
    /// <item><term>false_northing</term><description>Northing assigned to the natural origin.</description></item>
    /// </list>
    /// </remarks>
    protected TransverseMercator(IEnumerable<ProjectionParameter> parameters, TransverseMercator inverse)
        : base(parameters, inverse)
    {
        this.Name = "Transverse_Mercator";
        this.Authority = "EPSG";
        this.AuthorityCode = 9807;

        this.esp = this.es / (1.0 - this.es);
        this.ml0 = this.Mlfn(this.latOrigin, Math.Sin(this.latOrigin), Math.Cos(this.latOrigin));

        this.reciprocSemiMajor = 1 / this.semiMajor;
    }

    /// <summary>
    /// Converts coordinates in radians to projected meters.
    /// </summary>
    /// <param name="lon">The longitude of the point in radians.</param>
    /// <param name="lat">The latitude of the point in radians.</param>
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double x = lon;
        x = Adjust_lon(x - this.centralMeridian);

        double y = lat;
        double sinphi = Math.Sin(y);
        double cosphi = Math.Cos(y);

        double t = (Math.Abs(cosphi) > EPSILON) ? sinphi / cosphi : 0;
        t *= t;
        double al = cosphi * x;
        double als = al * al;
        al /= Math.Sqrt(1.0 - (this.es * sinphi * sinphi));
        double n = this.esp * cosphi * cosphi;

        // NOTE: meridinal distance at latitudeOfOrigin is always 0
        y = this.Mlfn(y, sinphi, cosphi) - this.ml0 +
            (sinphi * al * x *
            FC2 * (1.0 +
            (FC4 * als * (5.0 - t + (n * (9.0 + (4.0 * n))) +
            (FC6 * als * (61.0 + (t * (t - 58.0)) + (n * (270.0 - (330.0 * t))) +
            (FC8 * als * (1385.0 + (t * ((t * (543.0 - t)) - 3111.0))))))))));

        x = al * (FC1 + (FC3 * als * (1.0 - t + n +
            (FC5 * als * (5.0 + (t * (t - 18.0)) + (n * (14.0 - (58.0 * t))) +
            (FC7 * als * (61.0 + (t * ((t * (179.0 - t)) - 479.0)))))))));

        lon = this.scaleFactor * this.semiMajor * x;
        lat = this.scaleFactor * this.semiMajor * y;
    }

    /// <summary>
    /// Converts coordinates in projected meters to radians.
    /// </summary>
    /// <param name="x">The x-ordinate of the point.</param>
    /// <param name="y">The y-ordinate of the point.</param>
    protected override void MetersToRadians(ref double x, ref double y)
    {
        x *= this.reciprocSemiMajor;
        y *= this.reciprocSemiMajor;

        double phi = this.Inv_mlfn(this.ml0 + (y / this.scaleFactor));

        if (Math.Abs(phi) >= PI / 2)
        {
            y = y < 0.0 ? -(PI / 2) : (PI / 2);
            x = 0.0;
        }
        else
        {
            double sinphi = Math.Sin(phi);
            double cosphi = Math.Cos(phi);
            double t = (Math.Abs(cosphi) > EPSILON) ? sinphi / cosphi : 0.0;
            double n = this.esp * cosphi * cosphi;
            double con = 1.0 - (this.es * sinphi * sinphi);
            double d = x * Math.Sqrt(con) / this.scaleFactor;
            con *= t;
            t *= t;
            double ds = d * d;

            y = phi - ((con * ds / (1.0 - this.es)) *
                FC2 * (1.0 - (ds *
                FC4 * (5.0 + (t * (3.0 - (9.0 * n))) + (n * (1.0 - (4 * n))) - (ds *
                FC6 * (61.0 + (t * (90.0 - (252.0 * n) + (45.0 * t))) + (46.0 * n) - (ds *
                FC8 * (1385.0 + (t * (3633.0 + (t * (4095.0 + (1574.0 * t)))))))))))));

            x = Adjust_lon(this.centralMeridian + (d * (FC1 - (ds * FC3 * (1.0 + (2.0 * t) + n -
                (ds * FC5 * (5.0 + (t * (28.0 + (24 * t) + (8.0 * n))) + (6.0 * n) -
                (ds * FC7 * (61.0 + (t * (662.0 + (t * (1320.0 + (720.0 * t)))))))))))) / cosphi));
        }
    }

    /// <summary>
    /// Returns the inverse of this projection.
    /// </summary>
    /// <returns>IMathTransform that is the reverse of the current projection.</returns>
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new TransverseMercator(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }
}
