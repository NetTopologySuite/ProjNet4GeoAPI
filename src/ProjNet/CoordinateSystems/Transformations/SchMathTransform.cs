// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable
namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;

/// <summary>
/// Implements PROJ's <c>sch</c> (Spherical Cross-track Height) 3D runtime transform.
/// </summary>
[Serializable]
internal sealed class SchMathTransform : MathTransform
{
    private readonly GeocentricTransform ellipsoidForward;
    private readonly GeocentricTransform ellipsoidInverse;
    private readonly GeocentricTransform sphereForward;
    private readonly GeocentricTransform sphereInverse;

    private readonly double semiMajorAxis;
    private readonly double radiusOfCurvature;

    private readonly double offsetX;
    private readonly double offsetY;
    private readonly double offsetZ;

    private readonly double m00;
    private readonly double m01;
    private readonly double m02;
    private readonly double m10;
    private readonly double m11;
    private readonly double m12;
    private readonly double m20;
    private readonly double m21;
    private readonly double m22;

    private bool isInverted;
    private MathTransform? inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchMathTransform"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters that include SCH and ellipsoid values.</param>
    public SchMathTransform(List<ProjectionParameter> parameters)
        : this((IEnumerable<ProjectionParameter>)parameters, false)
    {
    }

    private SchMathTransform(IEnumerable<ProjectionParameter> parameters, bool isInverted)
    {
        parameters = ArgumentGuard.ThrowIfNull(parameters, nameof(parameters));
        var parameterSet = new ProjectionParameterSet(parameters);

        double pegLatitude = DegreesToRadians(parameterSet.GetParameterValue("plat_0", "peg_point_latitude"));
        double pegLongitude = DegreesToRadians(parameterSet.GetParameterValue("plon_0", "peg_point_longitude"));
        double pegHeading = DegreesToRadians(parameterSet.GetParameterValue("phdg_0", "peg_point_heading"));
        double pegHeight = parameterSet.GetOptionalParameterValue("h_0", 0d, "peg_point_height");

        this.semiMajorAxis = parameterSet.GetOptionalParameterValue("semi_major", 0d, "a");
        double semiMinorAxis = parameterSet.GetOptionalParameterValue("semi_minor", 0d, "b");
        double radius = parameterSet.GetOptionalParameterValue("r", 0d);
        if (radius > 0d)
        {
            this.semiMajorAxis = radius;
            semiMinorAxis = radius;
        }

        if (this.semiMajorAxis <= 0d)
        {
            this.semiMajorAxis = Ellipsoid.WGS84.SemiMajorAxis;
        }

        if (semiMinorAxis <= 0d)
        {
            semiMinorAxis = this.semiMajorAxis;
        }

        var ellipsoidParameters = new List<ProjectionParameter>
        {
            new ProjectionParameter("semi_major", this.semiMajorAxis),
            new ProjectionParameter("semi_minor", semiMinorAxis),
        };

        this.ellipsoidForward = new GeocentricTransform(ellipsoidParameters, false);
        this.ellipsoidInverse = (GeocentricTransform)this.ellipsoidForward.Inverse();

        double eccentricitySquared = 1d - ((semiMinorAxis * semiMinorAxis) / (this.semiMajorAxis * this.semiMajorAxis));

        double cosPegLatitude = Math.Cos(pegLatitude);
        double sinPegLatitude = Math.Sin(pegLatitude);
        double cosPegLongitude = Math.Cos(pegLongitude);
        double sinPegLongitude = Math.Sin(pegLongitude);
        double cosPegHeading = Math.Cos(pegHeading);
        double sinPegHeading = Math.Sin(pegHeading);

        double temp = Math.Sqrt(1d - (eccentricitySquared * sinPegLatitude * sinPegLatitude));
        double radiusEast = this.semiMajorAxis / temp;
        double radiusNorth = this.semiMajorAxis * (1d - eccentricitySquared) / Math.Pow(temp, 3d);
        this.radiusOfCurvature = pegHeight +
            ((radiusEast * radiusNorth) /
            ((radiusEast * cosPegHeading * cosPegHeading) + (radiusNorth * sinPegHeading * sinPegHeading)));

        if (Math.Abs(this.radiusOfCurvature) <= 1e-12d)
        {
            ArgumentGuard.ThrowArgument("sch produced a zero radius of curvature.", nameof(parameters));
        }

        var sphereParameters = new List<ProjectionParameter>
        {
            new ProjectionParameter("semi_major", this.radiusOfCurvature),
            new ProjectionParameter("semi_minor", this.radiusOfCurvature),
        };

        this.sphereForward = new GeocentricTransform(sphereParameters, false);
        this.sphereInverse = (GeocentricTransform)this.sphereForward.Inverse();

        this.m00 = cosPegLatitude * cosPegLongitude;
        this.m01 = (-sinPegHeading * sinPegLongitude) - (sinPegLatitude * cosPegLongitude * cosPegHeading);
        this.m02 = (sinPegLongitude * cosPegHeading) - (sinPegLatitude * cosPegLongitude * sinPegHeading);
        this.m10 = cosPegLatitude * sinPegLongitude;
        this.m11 = (cosPegLongitude * sinPegHeading) - (sinPegLatitude * sinPegLongitude * cosPegHeading);
        this.m12 = (-cosPegLongitude * cosPegHeading) - (sinPegLatitude * sinPegLongitude * sinPegHeading);
        this.m20 = sinPegLatitude;
        this.m21 = cosPegLatitude * cosPegHeading;
        this.m22 = cosPegLatitude * sinPegHeading;

        double pegLongitudeDegrees = RadiansToDegrees(pegLongitude);
        double pegLatitudeDegrees = RadiansToDegrees(pegLatitude);
        double pegZ = pegHeight;
        this.ellipsoidForward.Transform(ref pegLongitudeDegrees, ref pegLatitudeDegrees, ref pegZ);

        this.offsetX = pegLongitudeDegrees - (this.radiusOfCurvature * cosPegLatitude * cosPegLongitude);
        this.offsetY = pegLatitudeDegrees - (this.radiusOfCurvature * cosPegLatitude * sinPegLongitude);
        this.offsetZ = pegZ - (this.radiusOfCurvature * sinPegLatitude);

        this.isInverted = isInverted;
    }

    private SchMathTransform(SchMathTransform source, bool isInverted)
    {
        this.ellipsoidForward = source.ellipsoidForward;
        this.ellipsoidInverse = source.ellipsoidInverse;
        this.sphereForward = source.sphereForward;
        this.sphereInverse = source.sphereInverse;

        this.semiMajorAxis = source.semiMajorAxis;
        this.radiusOfCurvature = source.radiusOfCurvature;

        this.offsetX = source.offsetX;
        this.offsetY = source.offsetY;
        this.offsetZ = source.offsetZ;

        this.m00 = source.m00;
        this.m01 = source.m01;
        this.m02 = source.m02;
        this.m10 = source.m10;
        this.m11 = source.m11;
        this.m12 = source.m12;
        this.m20 = source.m20;
        this.m21 = source.m21;
        this.m22 = source.m22;

        this.isInverted = isInverted;
    }

    /// <inheritdoc />
    public override int DimSource => 3;

    /// <inheritdoc />
    public override int DimTarget => 3;

    /// <inheritdoc />
    public override string WKT => throw new NotImplementedException();

    /// <inheritdoc />
    public override string XML => throw new NotImplementedException();

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new SchMathTransform(this, !this.isInverted);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    public override void Invert()
    {
        this.isInverted = !this.isInverted;
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        if (this.isInverted)
        {
            this.TransformInverse(ref x, ref y, ref z);
        }
        else
        {
            this.TransformForward(ref x, ref y, ref z);
        }
    }

    /// <summary>
    /// Creates an <see cref="SchMathTransform"/> from parsed PROJ arguments.
    /// </summary>
    /// <param name="args">Parsed PROJ argument dictionary.</param>
    /// <param name="transform">Created transform instance on success.</param>
    /// <param name="skipReason">Failure reason when creation is not possible.</param>
    /// <returns><see langword="true"/> when a transform was created.</returns>
    internal static bool TryCreate(
        Dictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (args is null)
        {
            skipReason = "sch arguments were null.";
            return false;
        }

        if (!TryGetFromArgs(args, "plat_0", out _))
        {
            skipReason = "sch requires +plat_0.";
            return false;
        }

        if (!TryGetFromArgs(args, "plon_0", out _))
        {
            skipReason = "sch requires +plon_0.";
            return false;
        }

        if (!TryGetFromArgs(args, "phdg_0", out _))
        {
            skipReason = "sch requires +phdg_0.";
            return false;
        }

        double pegHeight = 0d;
        if (TryGetFromArgs(args, "h_0", out double h0))
        {
            pegHeight = h0;
        }

        if (!TryResolveEllipsoid(args, out double semiMajor, out double semiMinor))
        {
            skipReason = "Unable to resolve ellipsoid for sch.";
            return false;
        }

        var parameters = new List<ProjectionParameter>
        {
            new ProjectionParameter("plat_0", Parse(args["plat_0"])),
            new ProjectionParameter("plon_0", Parse(args["plon_0"])),
            new ProjectionParameter("phdg_0", Parse(args["phdg_0"])),
            new ProjectionParameter("h_0", pegHeight),
            new ProjectionParameter("semi_major", semiMajor),
            new ProjectionParameter("semi_minor", semiMinor),
        };

        try
        {
            transform = new SchMathTransform(parameters);
            if (args.ContainsKey("inv"))
            {
                transform = transform.Inverse();
            }

            return true;
        }
        catch (ArgumentException argumentException)
        {
            skipReason = argumentException.Message;
            return false;
        }
    }

    private static bool TryResolveEllipsoid(
        Dictionary<string, string> args,
        out double semiMajor,
        out double semiMinor)
    {
        semiMajor = 0d;
        semiMinor = 0d;

        if (TryGetFromArgs(args, "r", out double radius) && radius > 0d)
        {
            semiMajor = radius;
            semiMinor = radius;
            return true;
        }

        if (TryGetFromArgs(args, "a", out double major) && major > 0d)
        {
            semiMajor = major;

            if (TryGetFromArgs(args, "b", out double minor) && minor > 0d)
            {
                semiMinor = minor;
                return true;
            }

            if (TryGetFromArgs(args, "rf", out double inverseFlattening) && inverseFlattening > 0d)
            {
                semiMinor = (1d - (1d / inverseFlattening)) * major;
                return true;
            }

            semiMinor = major;
            return true;
        }

        if (args.TryGetValue("ellps", out string? ellps) && !string.IsNullOrWhiteSpace(ellps))
        {
            return TryResolveKnownEllipsoid(ellps, out semiMajor, out semiMinor);
        }

        if (args.TryGetValue("datum", out string? datum) && !string.IsNullOrWhiteSpace(datum))
        {
            return TryResolveKnownEllipsoid(datum, out semiMajor, out semiMinor);
        }

        semiMajor = Ellipsoid.WGS84.SemiMajorAxis;
        semiMinor = Ellipsoid.WGS84.SemiMinorAxis;
        return true;
    }

    private static bool TryResolveKnownEllipsoid(string token, out double semiMajor, out double semiMinor)
    {
        semiMajor = 0d;
        semiMinor = 0d;

        if (token.Equals("wgs84", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.WGS84.SemiMajorAxis;
            semiMinor = Ellipsoid.WGS84.SemiMinorAxis;
            return true;
        }

        if (token.Equals("grs80", StringComparison.OrdinalIgnoreCase)
            || token.Equals("nad83", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.GRS80.SemiMajorAxis;
            semiMinor = Ellipsoid.GRS80.SemiMinorAxis;
            return true;
        }

        if (token.Equals("clrk66", StringComparison.OrdinalIgnoreCase)
            || token.Equals("nad27", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.Clarke1866.SemiMajorAxis;
            semiMinor = Ellipsoid.Clarke1866.SemiMinorAxis;
            return true;
        }

        if (token.Equals("clrk80", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.Clarke1880.SemiMajorAxis;
            semiMinor = Ellipsoid.Clarke1880.SemiMinorAxis;
            return true;
        }

        if (token.Equals("intl", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.International1924.SemiMajorAxis;
            semiMinor = Ellipsoid.International1924.SemiMinorAxis;
            return true;
        }

        if (token.Equals("sphere", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.Sphere.SemiMajorAxis;
            semiMinor = Ellipsoid.Sphere.SemiMinorAxis;
            return true;
        }

        return false;
    }

    private static bool TryGetFromArgs(Dictionary<string, string> args, string key, out double value)
    {
        value = 0d;
        if (!args.TryGetValue(key, out string? token) || string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        return double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value)
            && !double.IsNaN(value)
            && !double.IsInfinity(value);
    }

    private static double Parse(string token)
    {
        return double.Parse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
    }

    private void TransformForward(ref double x, ref double y, ref double z)
    {
        double lon = x;
        double lat = y;
        double height = double.IsNaN(z) ? 0d : z;
        this.ellipsoidForward.Transform(ref lon, ref lat, ref height);

        lon -= this.offsetX;
        lat -= this.offsetY;
        height -= this.offsetZ;

        double localX = (this.m00 * lon) + (this.m10 * lat) + (this.m20 * height);
        double localY = (this.m01 * lon) + (this.m11 * lat) + (this.m21 * height);
        double localZ = (this.m02 * lon) + (this.m12 * lat) + (this.m22 * height);

        this.sphereInverse.Transform(ref localX, ref localY, ref localZ);

        x = DegreesToRadians(localX) * this.radiusOfCurvature;
        y = DegreesToRadians(localY) * this.radiusOfCurvature;
        z = localZ;
    }

    private void TransformInverse(ref double x, ref double y, ref double z)
    {
        double lon = RadiansToDegrees(x / this.radiusOfCurvature);
        double lat = RadiansToDegrees(y / this.radiusOfCurvature);
        double height = z;
        this.sphereForward.Transform(ref lon, ref lat, ref height);

        double globalX = (this.m00 * lon) + (this.m01 * lat) + (this.m02 * height);
        double globalY = (this.m10 * lon) + (this.m11 * lat) + (this.m12 * height);
        double globalZ = (this.m20 * lon) + (this.m21 * lat) + (this.m22 * height);

        globalX += this.offsetX;
        globalY += this.offsetY;
        globalZ += this.offsetZ;

        this.ellipsoidInverse.Transform(ref globalX, ref globalY, ref globalZ);
        x = globalX;
        y = globalY;
        z = globalZ;
    }
}
