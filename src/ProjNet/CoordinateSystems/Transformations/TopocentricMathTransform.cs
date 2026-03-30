// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ProjNet.CoordinateSystems;

/// <summary>
/// Implements PROJ's <c>topocentric</c> 3D runtime conversion.
/// </summary>
internal sealed class TopocentricMathTransform : MathTransform
{
    private readonly double originX;
    private readonly double originY;
    private readonly double originZ;
    private readonly double sinPhi0;
    private readonly double cosPhi0;
    private readonly double sinLam0;
    private readonly double cosLam0;

    private bool isInverted;
    private MathTransform? inverse;

    private TopocentricMathTransform(
        double originX,
        double originY,
        double originZ,
        double phi0Radians,
        double lam0Radians,
        bool isInverted)
    {
        this.originX = originX;
        this.originY = originY;
        this.originZ = originZ;
        this.sinPhi0 = Math.Sin(phi0Radians);
        this.cosPhi0 = Math.Cos(phi0Radians);
        this.sinLam0 = Math.Sin(lam0Radians);
        this.cosLam0 = Math.Cos(lam0Radians);
        this.isInverted = isInverted;
    }

    private TopocentricMathTransform(TopocentricMathTransform source, bool isInverted)
    {
        this.originX = source.originX;
        this.originY = source.originY;
        this.originZ = source.originZ;
        this.sinPhi0 = source.sinPhi0;
        this.cosPhi0 = source.cosPhi0;
        this.sinLam0 = source.sinLam0;
        this.cosLam0 = source.cosLam0;
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
        this.inverse ??= new TopocentricMathTransform(this, !this.isInverted);

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
        if (double.IsNaN(z))
        {
            z = 0d;
        }

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
    /// Creates a <see cref="TopocentricMathTransform"/> from parsed PROJ arguments.
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
            skipReason = "topocentric arguments were null.";
            return false;
        }

        bool hasX0 = args.ContainsKey("X_0");
        bool hasY0 = args.ContainsKey("Y_0");
        bool hasZ0 = args.ContainsKey("Z_0");
        bool hasLon0 = args.ContainsKey("lon_0");
        bool hasLat0 = args.ContainsKey("lat_0");
        bool hasH0 = args.ContainsKey("h_0");

        if (!hasX0 && !hasLon0)
        {
            skipReason = "missing X_0 or lon_0";
            return false;
        }

        if ((hasX0 || hasY0 || hasZ0) && (hasLon0 || hasLat0 || hasH0))
        {
            skipReason = "(X_0,Y_0,Z_0) and (lon_0,lat_0,h_0) are mutually exclusive";
            return false;
        }

        if (hasX0 && (!hasY0 || !hasZ0))
        {
            skipReason = "missing Y_0 and/or Z_0";
            return false;
        }

        if (hasLon0 && !hasLat0)
        {
            skipReason = "missing lat_0";
            return false;
        }

        if (!TryResolveEllipsoid(args, out double semiMajor, out double semiMinor))
        {
            skipReason = "Unable to resolve ellipsoid for topocentric.";
            return false;
        }

        var ellipsoidParameters = new List<ProjectionParameter>
        {
            new("semi_major", semiMajor),
            new("semi_minor", semiMinor),
        };

        var geocForward = new GeocentricTransform(ellipsoidParameters, false);
        var geocInverse = (GeocentricTransform)geocForward.Inverse();

        double originX;
        double originY;
        double originZ;
        double phi0;
        double lam0;

        if (hasX0)
        {
            if (!TryGetRequiredDouble(args, "X_0", out originX)
                || !TryGetRequiredDouble(args, "Y_0", out originY)
                || !TryGetRequiredDouble(args, "Z_0", out originZ))
            {
                skipReason = "Unable to parse X_0/Y_0/Z_0 for topocentric.";
                return false;
            }

            double lon0Degrees = originX;
            double lat0Degrees = originY;
            double h0 = originZ;
            geocInverse.Transform(ref lon0Degrees, ref lat0Degrees, ref h0);

            lam0 = DegreesToRadians(lon0Degrees);
            phi0 = DegreesToRadians(lat0Degrees);
        }
        else
        {
            if (!TryGetRequiredDouble(args, "lon_0", out double lon0Degrees)
                || !TryGetRequiredDouble(args, "lat_0", out double lat0Degrees))
            {
                skipReason = "Unable to parse lon_0/lat_0 for topocentric.";
                return false;
            }

            double h0 = 0d;
            if (hasH0 && !TryGetRequiredDouble(args, "h_0", out h0))
            {
                skipReason = "Unable to parse h_0 for topocentric.";
                return false;
            }

            originX = lon0Degrees;
            originY = lat0Degrees;
            originZ = h0;
            geocForward.Transform(ref originX, ref originY, ref originZ);

            lam0 = DegreesToRadians(lon0Degrees);
            phi0 = DegreesToRadians(lat0Degrees);
        }

        transform = new TopocentricMathTransform(originX, originY, originZ, phi0, lam0, false);
        if (args.ContainsKey("inv"))
        {
            transform = transform.Inverse();
        }

        return true;
    }

    private static bool TryResolveEllipsoid(
        Dictionary<string, string> args,
        out double semiMajor,
        out double semiMinor)
    {
        if (TryGetOptionalDouble(args, "r", out double radius) && radius > 0d)
        {
            semiMajor = radius;
            semiMinor = radius;
            return true;
        }

        if (TryGetOptionalDouble(args, "a", out double major) && major > 0d)
        {
            semiMajor = major;
            if (TryGetOptionalDouble(args, "b", out double minor) && minor > 0d)
            {
                semiMinor = minor;
                return true;
            }

            if (TryGetOptionalDouble(args, "rf", out double inverseFlattening) && inverseFlattening > 0d)
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

    private static bool TryGetRequiredDouble(Dictionary<string, string> args, string key, out double value)
    {
        value = 0d;
        return args.TryGetValue(key, out string? token)
            && TryParseFiniteDouble(token, out value);
    }

    private static bool TryGetOptionalDouble(Dictionary<string, string> args, string key, out double value)
    {
        value = 0d;
        return args.TryGetValue(key, out string? token)
            && TryParseFiniteDouble(token, out value);
    }

    private static bool TryParseFiniteDouble(string token, out double value)
    {
        value = 0d;
        return !string.IsNullOrWhiteSpace(token)
            && double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value)
            && !double.IsNaN(value)
            && !double.IsInfinity(value);
    }

    private void TransformForward(ref double x, ref double y, ref double z)
    {
        double dX = x - this.originX;
        double dY = y - this.originY;
        double dZ = z - this.originZ;

        double outX = (-dX * this.sinLam0) + (dY * this.cosLam0);
        double outY = (-dX * this.sinPhi0 * this.cosLam0) - (dY * this.sinPhi0 * this.sinLam0) + (dZ * this.cosPhi0);
        double outZ = (dX * this.cosPhi0 * this.cosLam0) + (dY * this.cosPhi0 * this.sinLam0) + (dZ * this.sinPhi0);

        x = outX;
        y = outY;
        z = outZ;
    }

    private void TransformInverse(ref double x, ref double y, ref double z)
    {
        double inX = x;
        double inY = y;
        double inZ = z;

        double outX = this.originX - (inX * this.sinLam0) - (inY * this.sinPhi0 * this.cosLam0) + (inZ * this.cosPhi0 * this.cosLam0);
        double outY = this.originY + (inX * this.cosLam0) - (inY * this.sinPhi0 * this.sinLam0) + (inZ * this.cosPhi0 * this.sinLam0);
        double outZ = this.originZ + (inY * this.cosPhi0) + (inZ * this.sinPhi0);

        x = outX;
        y = outY;
        z = outZ;
    }
}
