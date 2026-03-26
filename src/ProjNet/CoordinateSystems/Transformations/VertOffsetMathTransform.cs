// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Globalization;
using ProjNet.CoordinateSystems;

/// <summary>
/// Implements PROJ's <c>vertoffset</c> runtime transform (Vertical Offset and Slope).
/// </summary>
[Serializable]
internal sealed class VertOffsetMathTransform : MathTransform
{
    private const double ArcSecondToRadians = Math.PI / (180d * 3600d);

    private readonly double latOriginRadians;
    private readonly double lonOriginDegrees;
    private readonly double verticalOffset;
    private readonly double slopeLatRadians;
    private readonly double slopeLonRadians;
    private readonly double rho0;
    private readonly double nu0;

    private bool isInverted;
    private MathTransform inverse;

    private VertOffsetMathTransform(
        double semiMajor,
        double semiMinor,
        double latOriginDegrees,
        double lonOriginDegrees,
        double verticalOffset,
        double slopeLatArcSeconds,
        double slopeLonArcSeconds,
        bool isInverted)
    {
        if (semiMajor <= 0d || double.IsNaN(semiMajor) || double.IsInfinity(semiMajor))
        {
            ArgumentGuard.ThrowArgument("vertoffset requires a positive finite semi-major axis.", nameof(semiMajor));
        }

        if (semiMinor <= 0d || double.IsNaN(semiMinor) || double.IsInfinity(semiMinor))
        {
            ArgumentGuard.ThrowArgument("vertoffset requires a positive finite semi-minor axis.", nameof(semiMinor));
        }

        this.latOriginRadians = DegreesToRadians(latOriginDegrees);
        this.lonOriginDegrees = lonOriginDegrees;
        this.verticalOffset = verticalOffset;
        this.slopeLatRadians = slopeLatArcSeconds * ArcSecondToRadians;
        this.slopeLonRadians = slopeLonArcSeconds * ArcSecondToRadians;

        double eccentricitySquared = 1d - ((semiMinor * semiMinor) / (semiMajor * semiMajor));
        double sinLat0 = Math.Sin(this.latOriginRadians);
        double oneMinusEsSinLat0Square = 1d - (eccentricitySquared * sinLat0 * sinLat0);
        if (oneMinusEsSinLat0Square <= 0d)
        {
            ArgumentGuard.ThrowArgument("vertoffset produced invalid ellipsoid curvature terms.");
        }

        double sqrtDenominator = Math.Sqrt(oneMinusEsSinLat0Square);
        this.rho0 = semiMajor * (1d - eccentricitySquared) / (oneMinusEsSinLat0Square * sqrtDenominator);
        this.nu0 = semiMajor / sqrtDenominator;
        this.isInverted = isInverted;
    }

    private VertOffsetMathTransform(VertOffsetMathTransform source, bool isInverted)
    {
        this.latOriginRadians = source.latOriginRadians;
        this.lonOriginDegrees = source.lonOriginDegrees;
        this.verticalOffset = source.verticalOffset;
        this.slopeLatRadians = source.slopeLatRadians;
        this.slopeLonRadians = source.slopeLonRadians;
        this.rho0 = source.rho0;
        this.nu0 = source.nu0;
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
            this.inverse = new VertOffsetMathTransform(this, !this.isInverted);
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
        if (double.IsNaN(z))
        {
            z = 0d;
        }

        double phi = DegreesToRadians(y);
        double lam = DegreesToRadians(x - this.lonOriginDegrees);
        double offset = this.verticalOffset
            + (this.slopeLatRadians * this.rho0 * (phi - this.latOriginRadians))
            + (this.slopeLonRadians * this.nu0 * lam * Math.Cos(phi));

        if (this.isInverted)
        {
            z -= offset;
        }
        else
        {
            z += offset;
        }
    }

    /// <summary>
    /// Creates a <see cref="VertOffsetMathTransform"/> from parsed PROJ arguments.
    /// </summary>
    /// <param name="args">Parsed PROJ argument dictionary.</param>
    /// <param name="transform">Created transform instance on success.</param>
    /// <param name="skipReason">Failure reason when creation is not possible.</param>
    /// <returns><see langword="true"/> when a transform was created.</returns>
    internal static bool TryCreate(
        Dictionary<string, string> args,
        out MathTransform transform,
        out string skipReason)
    {
        transform = null;
        skipReason = null;

        if (args is null)
        {
            skipReason = "vertoffset arguments were null.";
            return false;
        }

        if (!TryGetOptionalDouble(args, "lat_0", 0d, out double lat0, out skipReason)
            || !TryGetOptionalDouble(args, "lon_0", 0d, out double lon0, out skipReason)
            || !TryGetOptionalDouble(args, "dh", 0d, out double dh, out skipReason)
            || !TryGetOptionalDouble(args, "slope_lat", 0d, out double slopeLat, out skipReason)
            || !TryGetOptionalDouble(args, "slope_lon", 0d, out double slopeLon, out skipReason))
        {
            return false;
        }

        if (!TryResolveEllipsoid(args, out double semiMajor, out double semiMinor))
        {
            skipReason = "Unable to resolve ellipsoid for vertoffset.";
            return false;
        }

        try
        {
            transform = new VertOffsetMathTransform(
                semiMajor,
                semiMinor,
                lat0,
                lon0,
                dh,
                slopeLat,
                slopeLon,
                false);
        }
        catch (ArgumentException exception)
        {
            skipReason = exception.Message;
            return false;
        }

        if (args.ContainsKey("inv"))
        {
            transform = transform.Inverse();
        }

        return true;
    }

    private static bool TryGetOptionalDouble(
        Dictionary<string, string> args,
        string key,
        double defaultValue,
        out double value,
        out string skipReason)
    {
        value = defaultValue;
        skipReason = null;
        if (!args.TryGetValue(key, out string token))
        {
            return true;
        }

        if (!TryParseFiniteDouble(token, out value))
        {
            skipReason = "Invalid value for +" + key + ".";
            return false;
        }

        return true;
    }

    private static bool TryResolveEllipsoid(
        Dictionary<string, string> args,
        out double semiMajor,
        out double semiMinor)
    {
        semiMajor = 0d;
        semiMinor = 0d;

        if (args.TryGetValue("r", out string radiusToken)
            && TryParseFiniteDouble(radiusToken, out double radius)
            && radius > 0d)
        {
            semiMajor = radius;
            semiMinor = radius;
            return true;
        }

        if (args.TryGetValue("a", out string majorToken)
            && TryParseFiniteDouble(majorToken, out double major)
            && major > 0d)
        {
            semiMajor = major;
            if (args.TryGetValue("b", out string minorToken)
                && TryParseFiniteDouble(minorToken, out double minor)
                && minor > 0d)
            {
                semiMinor = minor;
                return true;
            }

            if (args.TryGetValue("rf", out string inverseFlatteningToken)
                && TryParseFiniteDouble(inverseFlatteningToken, out double inverseFlattening)
                && inverseFlattening > 0d)
            {
                semiMinor = (1d - (1d / inverseFlattening)) * major;
                return true;
            }

            semiMinor = major;
            return true;
        }

        if (args.TryGetValue("ellps", out string ellps) && !string.IsNullOrWhiteSpace(ellps))
        {
            return TryResolveKnownEllipsoid(ellps, out semiMajor, out semiMinor);
        }

        if (args.TryGetValue("datum", out string datum) && !string.IsNullOrWhiteSpace(datum))
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

    private static bool TryParseFiniteDouble(string token, out double value)
    {
        value = 0d;
        return !string.IsNullOrWhiteSpace(token)
            && double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value)
            && !double.IsNaN(value)
            && !double.IsInfinity(value);
    }
}
