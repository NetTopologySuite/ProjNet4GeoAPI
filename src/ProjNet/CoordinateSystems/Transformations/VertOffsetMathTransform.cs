// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ProjNet.CoordinateSystems;

/// <summary>
/// Implements PROJ's <c>vertoffset</c> runtime transform (Vertical Offset and Slope).
/// </summary>
/// <remarks>
/// This runtime applies the EPSG Vertical Offset and Slope model: a constant
/// vertical offset plus latitude- and longitude-dependent slope terms scaled by
/// the local meridional and prime-vertical radii of curvature at the origin.
/// </remarks>
/// <seealso href="https://epsg.io/9657-method">EPSG method 9657: Vertical Offset and Slope.</seealso>
internal sealed class VertOffsetMathTransform : MathTransform
{
    private readonly double latOriginRadians;
    private readonly double lonOriginDegrees;
    private readonly double verticalOffset;
    private readonly double slopeLatRadians;
    private readonly double slopeLonRadians;
    private readonly double rho0;
    private readonly double nu0;

    private readonly bool isInverted;
    private MathTransform? inverse;

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
        this.slopeLatRadians = slopeLatArcSeconds * TransformationMath.ArcSecondToRadians;
        this.slopeLonRadians = slopeLonArcSeconds * TransformationMath.ArcSecondToRadians;

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
    public override MathTransform Inverse()
    {
        this.inverse ??= new VertOffsetMathTransform(this, !this.isInverted);

        return this.inverse;
    }

    /// <inheritdoc />
    public override void Invert()
    {
        throw new NotSupportedException("VertOffsetMathTransform is immutable. Use Inverse() to obtain inverted transform.");
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
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        if (args is null)
        {
            skipReason = "vertoffset arguments were null.";
            return false;
        }

        if (!SpanParseUtility.TryGetOptionalDouble(args, "lat_0", 0d, out double lat0, out skipReason)
            || !SpanParseUtility.TryGetOptionalDouble(args, "lon_0", 0d, out double lon0, out skipReason)
            || !SpanParseUtility.TryGetOptionalDouble(args, "dh", 0d, out double dh, out skipReason)
            || !SpanParseUtility.TryGetOptionalDouble(args, "slope_lat", 0d, out double slopeLat, out skipReason)
            || !SpanParseUtility.TryGetOptionalDouble(args, "slope_lon", 0d, out double slopeLon, out skipReason))
        {
            return false;
        }

        if (!ProjEllipsoidResolver.TryResolveEllipsoidOrDefault(
            args,
            includeDatumToken: true,
            allowClarke1880Ign: false,
            allowBessel: false,
            out double semiMajor,
            out double semiMinor))
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
}
