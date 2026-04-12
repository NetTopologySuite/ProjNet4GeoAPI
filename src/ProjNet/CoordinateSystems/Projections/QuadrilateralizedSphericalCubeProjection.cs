// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Quadrilateralized Spherical Cube projection (<c>qsc</c>).
/// </summary>
/// <remarks>
/// <para>The Quadrilateralized Spherical Cube maps the globe onto six cube faces and
/// applies an equal-area transform within each face. The ellipsoidal variant first
/// converts to a geocentric latitude before selecting the target face.</para>
/// <para>The formulation was independently verified against F. M. O'Neill and
/// R. E. Laubscher, <i>Extended Studies of a Quadrilateralized Spherical Cube Earth
/// Data Base</i>, DTIC report ADA026294, 1976. The face selection through dominant
/// direction cosines and the equal-area mapping within each face match the
/// implementation here.</para>
/// </remarks>
/// <seealso href="https://apps.dtic.mil/sti/tr/pdf/ADA026294.pdf">DTIC report ADA026294: Quadrilateralized Spherical Cube.</seealso>
/// <seealso href="https://grokipedia.com/page/quadrilateralized_spherical_cube">Background overview of the quadrilateralized spherical cube projection.</seealso>
internal sealed class QuadrilateralizedSphericalCubeProjection : MapProjection
{
    private const double QuarterPi = PI * 0.25d;
    private const double HalfPiPlusQuarterPi = HalfPi + QuarterPi;
    private const double HalfPiMinusQuarterPiHalf = HalfPi - (QuarterPi * 0.5d);
    private const double OneOverSqrt2 = 0.7071067811865475244008443621d;

    private readonly Face face;
    private readonly double aSquared;
    private readonly double sphereB;
    private readonly double oneMinusF;
    private readonly double oneMinusFSquared;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuadrilateralizedSphericalCubeProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public QuadrilateralizedSphericalCubeProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuadrilateralizedSphericalCubeProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public QuadrilateralizedSphericalCubeProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Quadrilateralized_Spherical_Cube";
        this.face = DetermineFace(this.latOrigin, this.centralMeridian);

        if (this.es != 0d)
        {
            this.aSquared = this.semiMajor * this.semiMajor;
            this.sphereB = this.semiMajor * Math.Sqrt(1d - this.es);
            this.oneMinusF = 1d - ((this.semiMajor - this.sphereB) / this.semiMajor);
            this.oneMinusFSquared = this.oneMinusF * this.oneMinusF;
        }
    }

    private enum Face
    {
        Front = 0,
        Right = 1,
        Back = 2,
        Left = 3,
        Top = 4,
        Bottom = 5,
    }

    private enum Area
    {
        Zero = 0,
        One = 1,
        Two = 2,
        Three = 3,
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new QuadrilateralizedSphericalCubeProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double latitude = this.es != 0d
            ? Math.Atan(this.oneMinusFSquared * Math.Tan(lat))
            : lat;

        double longitude = lon;
        Area area = Area.Zero;
        double theta = 0d;
        double phi = 0d;

        if (this.face == Face.Top)
        {
            phi = HalfPi - latitude;
            if (longitude >= QuarterPi && longitude <= HalfPiPlusQuarterPi)
            {
                area = Area.Zero;
                theta = longitude - HalfPi;
            }
            else if (longitude > HalfPiPlusQuarterPi || longitude <= -HalfPiPlusQuarterPi)
            {
                area = Area.One;
                theta = longitude > 0d ? longitude - PI : longitude + PI;
            }
            else if (longitude > -HalfPiPlusQuarterPi && longitude <= -QuarterPi)
            {
                area = Area.Two;
                theta = longitude + HalfPi;
            }
            else
            {
                area = Area.Three;
                theta = longitude;
            }
        }
        else if (this.face == Face.Bottom)
        {
            phi = HalfPi + latitude;
            if (longitude >= QuarterPi && longitude <= HalfPiPlusQuarterPi)
            {
                area = Area.Zero;
                theta = -longitude + HalfPi;
            }
            else if (longitude < QuarterPi && longitude >= -QuarterPi)
            {
                area = Area.One;
                theta = -longitude;
            }
            else if (longitude < -QuarterPi && longitude >= -HalfPiPlusQuarterPi)
            {
                area = Area.Two;
                theta = -longitude - HalfPi;
            }
            else
            {
                area = Area.Three;
                theta = longitude > 0d ? -longitude + PI : -longitude - PI;
            }
        }
        else
        {
            if (this.face == Face.Right)
            {
                longitude = ShiftLongitudeOrigin(longitude, HalfPi);
            }
            else if (this.face == Face.Back)
            {
                longitude = ShiftLongitudeOrigin(longitude, PI);
            }
            else if (this.face == Face.Left)
            {
                longitude = ShiftLongitudeOrigin(longitude, -HalfPi);
            }

            double sinLatitude = Math.Sin(latitude);
            double cosLatitude = Math.Cos(latitude);
            double sinLongitude = Math.Sin(longitude);
            double cosLongitude = Math.Cos(longitude);
            double q = cosLatitude * cosLongitude;
            double r = cosLatitude * sinLongitude;
            double s = sinLatitude;

            if (this.face == Face.Front)
            {
                phi = Math.Acos(q);
                theta = ForwardEquatorialFaceTheta(phi, s, r, out area);
            }
            else if (this.face == Face.Right)
            {
                phi = Math.Acos(r);
                theta = ForwardEquatorialFaceTheta(phi, s, -q, out area);
            }
            else if (this.face == Face.Back)
            {
                phi = Math.Acos(-q);
                theta = ForwardEquatorialFaceTheta(phi, s, -r, out area);
            }
            else
            {
                phi = Math.Acos(-r);
                theta = ForwardEquatorialFaceTheta(phi, s, q, out area);
            }
        }

        double mu = Math.Atan((12d / PI) * (theta + Math.Acos(Math.Sin(theta) * Math.Cos(QuarterPi)) - HalfPi));
        double t = Math.Sqrt((1d - Math.Cos(phi)) / (Math.Cos(mu) * Math.Cos(mu)) / (1d - Math.Cos(Math.Atan(1d / Math.Cos(theta)))));

        if (area == Area.One)
        {
            mu += HalfPi;
        }
        else if (area == Area.Two)
        {
            mu += PI;
        }
        else if (area == Area.Three)
        {
            mu += PI + HalfPi;
        }

        lon = this.semiMajor * this.scaleFactor * (t * Math.Cos(mu));
        lat = this.semiMajor * this.scaleFactor * (t * Math.Sin(mu));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x / (this.semiMajor * this.scaleFactor);
        double yy = y / (this.semiMajor * this.scaleFactor);
        double nu = Math.Atan(Math.Sqrt((xx * xx) + (yy * yy)));
        double mu = Math.Atan2(yy, xx);

        Area area;
        if (xx >= 0d && xx >= Math.Abs(yy))
        {
            area = Area.Zero;
        }
        else if (yy >= 0d && yy >= Math.Abs(xx))
        {
            area = Area.One;
            mu -= HalfPi;
        }
        else if (xx < 0d && -xx >= Math.Abs(yy))
        {
            area = Area.Two;
            mu = mu < 0d ? mu + PI : mu - PI;
        }
        else
        {
            area = Area.Three;
            mu += HalfPi;
        }

        double t = (PI / 12d) * Math.Tan(mu);
        double tanTheta = Math.Sin(t) / (Math.Cos(t) - OneOverSqrt2);
        double theta = Math.Atan(tanTheta);
        double cosMu = Math.Cos(mu);
        double tanNu = Math.Tan(nu);
        double cosPhi = 1d - (cosMu * cosMu * tanNu * tanNu * (1d - Math.Cos(Math.Atan(1d / Math.Cos(theta)))));
        if (cosPhi < -1d)
        {
            cosPhi = -1d;
        }
        else if (cosPhi > 1d)
        {
            cosPhi = 1d;
        }

        double lambda = 0d;
        double phi = 0d;

        if (this.face == Face.Top)
        {
            double phiFace = Math.Acos(cosPhi);
            phi = HalfPi - phiFace;
            if (area == Area.Zero)
            {
                lambda = theta + HalfPi;
            }
            else if (area == Area.One)
            {
                lambda = theta < 0d ? theta + PI : theta - PI;
            }
            else if (area == Area.Two)
            {
                lambda = theta - HalfPi;
            }
            else
            {
                lambda = theta;
            }
        }
        else if (this.face == Face.Bottom)
        {
            double phiFace = Math.Acos(cosPhi);
            phi = phiFace - HalfPi;
            if (area == Area.Zero)
            {
                lambda = -theta + HalfPi;
            }
            else if (area == Area.One)
            {
                lambda = -theta;
            }
            else if (area == Area.Two)
            {
                lambda = -theta - HalfPi;
            }
            else
            {
                lambda = theta < 0d ? -theta - PI : -theta + PI;
            }
        }
        else
        {
            double q = cosPhi;
            t = q * q;
            double s = t >= 1d ? 0d : Math.Sqrt(1d - t) * Math.Sin(theta);
            t += s * s;
            double r = t >= 1d ? 0d : Math.Sqrt(1d - t);

            if (area == Area.One)
            {
                double swap = r;
                r = -s;
                s = swap;
            }
            else if (area == Area.Two)
            {
                r = -r;
                s = -s;
            }
            else if (area == Area.Three)
            {
                double swap = r;
                r = s;
                s = -swap;
            }

            if (this.face == Face.Right)
            {
                double swap = q;
                q = -r;
                r = swap;
            }
            else if (this.face == Face.Back)
            {
                q = -q;
                r = -r;
            }
            else if (this.face == Face.Left)
            {
                double swap = q;
                q = r;
                r = -swap;
            }

            phi = Math.Acos(-s) - HalfPi;
            lambda = Math.Atan2(r, q);
            if (this.face == Face.Right)
            {
                lambda = ShiftLongitudeOrigin(lambda, -HalfPi);
            }
            else if (this.face == Face.Back)
            {
                lambda = ShiftLongitudeOrigin(lambda, -PI);
            }
            else if (this.face == Face.Left)
            {
                lambda = ShiftLongitudeOrigin(lambda, HalfPi);
            }
        }

        if (this.es != 0d)
        {
            bool invertSign = phi < 0d;
            double tanPhi = Math.Tan(phi);
            double xa = this.sphereB / Math.Sqrt((tanPhi * tanPhi) + this.oneMinusFSquared);
            phi = Math.Atan(Math.Sqrt(this.aSquared - (xa * xa)) / (this.oneMinusF * xa));
            if (invertSign)
            {
                phi = -phi;
            }
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }

    private static double ForwardEquatorialFaceTheta(double phi, double y, double x, out Area area)
    {
        if (phi < Eps10)
        {
            area = Area.Zero;
            return 0d;
        }

        double theta = Math.Atan2(y, x);
        if (Math.Abs(theta) <= QuarterPi)
        {
            area = Area.Zero;
        }
        else if (theta > QuarterPi && theta <= HalfPiPlusQuarterPi)
        {
            area = Area.One;
            theta -= HalfPi;
        }
        else if (theta > HalfPiPlusQuarterPi || theta <= -HalfPiPlusQuarterPi)
        {
            area = Area.Two;
            theta = theta >= 0d ? theta - PI : theta + PI;
        }
        else
        {
            area = Area.Three;
            theta += HalfPi;
        }

        return theta;
    }

    private static double ShiftLongitudeOrigin(double longitude, double offset)
    {
        double shifted = longitude + offset;
        if (shifted < -PI)
        {
            shifted += TwoPi;
        }
        else if (shifted > PI)
        {
            shifted -= TwoPi;
        }

        return shifted;
    }

    private static Face DetermineFace(double phi0, double lam0)
    {
        if (phi0 >= HalfPiMinusQuarterPiHalf)
        {
            return Face.Top;
        }

        if (phi0 <= -HalfPiMinusQuarterPiHalf)
        {
            return Face.Bottom;
        }

        if (Math.Abs(lam0) <= QuarterPi)
        {
            return Face.Front;
        }

        return Math.Abs(lam0) <= HalfPiPlusQuarterPi ? lam0 > 0d ? Face.Right : Face.Left : Face.Back;
    }
}
