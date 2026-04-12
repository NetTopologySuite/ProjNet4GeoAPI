// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;

/// <summary>
/// Implements PROJ's <c>ob_tran</c> runtime transform by rotating geographic coordinates and delegating to a child projection.
/// </summary>
/// <remarks>
/// <para>
/// The general oblique transformation rotates geographic coordinates into a
/// derived pole or equator definition and then delegates the rotated
/// coordinates to a child projection. This implementation supports PROJ's three
/// standard rotation parameterizations: new-pole, rotate-about-point, and
/// two-point new-equator modes.
/// </para>
/// <para>
/// The runtime was independently verified against PROJ's published
/// <c>ob_tran</c> documentation and <c>ob_tran.cpp</c>. The forward and inverse
/// paths preserve the reviewed oblique/transverse rotation logic before and
/// after the delegated child projection step.
/// </para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/projections/ob_tran.html">PROJ: ob_tran.</seealso>
internal sealed class ObTranMathTransform : MathTransform
{
    private const double Tolerance = 1e-10d;

    private readonly MathTransform childForward;
    private readonly MathTransform childInverse;
    private readonly bool childIsAngular;
    private readonly double lamp;
    private readonly double phip;
    private readonly double centralMeridian;
    private readonly double sphip;
    private readonly double cphip;
    private readonly bool isOblique;
    private readonly bool isInverted;
    private MathTransform? inverse;

    private ObTranMathTransform(
        MathTransform childForward,
        MathTransform childInverse,
        bool childIsAngular,
        double lamp,
        double phip,
        double centralMeridian,
        bool isInverted)
    {
        this.childForward = ArgumentGuard.ThrowIfNull(childForward, nameof(childForward));
        this.childInverse = ArgumentGuard.ThrowIfNull(childInverse, nameof(childInverse));
        this.childIsAngular = childIsAngular;
        this.lamp = lamp;
        this.phip = phip;
        this.centralMeridian = centralMeridian;
        this.sphip = Math.Sin(phip);
        this.cphip = Math.Cos(phip);
        this.isOblique = Math.Abs(phip) > Tolerance;
        this.isInverted = isInverted;
    }

    /// <inheritdoc />
    public override int DimSource => 2;

    /// <inheritdoc />
    public override int DimTarget => 2;

    /// <inheritdoc />
    public override string WKT => throw new NotImplementedException();

    /// <inheritdoc />
    public override string XML => throw new NotImplementedException();

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new ObTranMathTransform(
                this.childForward,
                this.childInverse,
                this.childIsAngular,
                this.lamp,
                this.phip,
                this.centralMeridian,
                !this.isInverted);

        return this.inverse;
    }

    /// <inheritdoc />
    public override void Invert()
    {
        throw new NotSupportedException("ObTranMathTransform is immutable. Use Inverse() to obtain inverted transform.");
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        if (this.isInverted)
        {
            this.TransformInverse(ref x, ref y);
        }
        else
        {
            this.TransformForward(ref x, ref y);
        }
    }

    /// <summary>
    /// Creates an <see cref="ObTranMathTransform"/> from parsed PROJ arguments.
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
        if (!args.TryGetValue("o_proj", out string? childProjCode) || string.IsNullOrWhiteSpace(childProjCode))
        {
            skipReason = "ob_tran requires +o_proj.";
            return false;
        }

        if (childProjCode.Equals("ob_tran", StringComparison.OrdinalIgnoreCase))
        {
            skipReason = "Nested ob_tran is not supported.";
            return false;
        }

        Dictionary<string, string> childArgs = BuildChildProjectionArguments(args, childProjCode);
        if (!TryCreateProjectionTransform(childArgs, out MathTransform? childForwardCandidate, out MathTransform? childInverseCandidate, out bool childIsAngular, out skipReason))
        {
            return false;
        }

        MathTransform childForward = ArgumentGuard.ThrowIfNull(childForwardCandidate, nameof(childForwardCandidate));
        MathTransform childInverse = ArgumentGuard.ThrowIfNull(childInverseCandidate, nameof(childInverseCandidate));

        if (!TryResolveRotation(args, out double lamp, out double phip, out skipReason))
        {
            return false;
        }

        double centralMeridian = 0d;
        if (TryGetFromArgs(args, "lon_0", out double lon0Degrees))
        {
            centralMeridian = ToRadians(lon0Degrees);
        }

        transform = new ObTranMathTransform(childForward, childInverse, childIsAngular, lamp, phip, centralMeridian, false);
        if (args.ContainsKey("inv"))
        {
            transform = transform.Inverse();
        }

        return true;
    }

    private static Dictionary<string, string> BuildChildProjectionArguments(
        Dictionary<string, string> args,
        string childProjCode)
    {
        var childArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string> kvp in args)
        {
            if (kvp.Key.StartsWith("o_", StringComparison.OrdinalIgnoreCase)
                || kvp.Key.Equals("proj", StringComparison.OrdinalIgnoreCase)
                || kvp.Key.Equals("inv", StringComparison.OrdinalIgnoreCase)
                || kvp.Key.Equals("lon_0", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            childArgs[kvp.Key] = kvp.Value;
        }

        childArgs["proj"] = childProjCode;
        return childArgs;
    }

    private static bool TryResolveRotation(
        Dictionary<string, string> args,
        out double lamp,
        out double phip,
        out string? skipReason)
    {
        lamp = 0d;
        phip = 0d;
        skipReason = null;

        if (args.ContainsKey("o_alpha"))
        {
            if (!TryGetRequiredDegrees(args, "o_lon_c", out double lamc)
                || !TryGetRequiredDegrees(args, "o_lat_c", out double phic)
                || !TryGetRequiredDegrees(args, "o_alpha", out double alpha))
            {
                skipReason = "ob_tran with +o_alpha requires +o_lon_c, +o_lat_c and +o_alpha.";
                return false;
            }

            if (Math.Abs(Math.Abs(phic) - (Math.PI * 0.5d)) <= Tolerance)
            {
                skipReason = "Invalid value for o_lat_c: |o_lat_c| should be < 90°.";
                return false;
            }

            lamp = lamc + Math.Atan2(-Math.Cos(alpha), -Math.Sin(alpha) * Math.Sin(phic));
            phip = SafeAsin(Math.Cos(phic) * Math.Sin(alpha));
            return true;
        }

        if (args.ContainsKey("o_lat_p"))
        {
            if (!TryGetRequiredDegrees(args, "o_lat_p", out phip))
            {
                skipReason = "ob_tran requires numeric +o_lat_p when using pole mode.";
                return false;
            }

            if (args.TryGetValue("o_lon_p", out string? lonPoleToken) && !string.IsNullOrWhiteSpace(lonPoleToken))
            {
                if (!TryGetDouble(lonPoleToken, out double lonPoleDegrees))
                {
                    skipReason = "Invalid value for +o_lon_p.";
                    return false;
                }

                lamp = ToRadians(lonPoleDegrees);
            }

            return true;
        }

        if (!TryGetRequiredDegrees(args, "o_lon_1", out double lam1)
            || !TryGetRequiredDegrees(args, "o_lat_1", out double phi1)
            || !TryGetRequiredDegrees(args, "o_lon_2", out double lam2)
            || !TryGetRequiredDegrees(args, "o_lat_2", out double phi2))
        {
            skipReason = "ob_tran requires either (+o_lon_p,+o_lat_p), (+o_alpha,+o_lon_c,+o_lat_c), or (+o_lon_1,+o_lat_1,+o_lon_2,+o_lat_2).";
            return false;
        }

        if (Math.Abs(phi1) > (Math.PI * 0.5d) - Tolerance || Math.Abs(phi2) > (Math.PI * 0.5d) - Tolerance)
        {
            skipReason = "Invalid values for o_lat_1/o_lat_2: |lat| should be < 90°.";
            return false;
        }

        if (Math.Abs(phi1 - phi2) < Tolerance)
        {
            skipReason = "Invalid values for o_lat_1 and o_lat_2: they must differ.";
            return false;
        }

        if (Math.Abs(phi1) < Tolerance)
        {
            skipReason = "Invalid value for o_lat_1: it should differ from 0.";
            return false;
        }

        lamp = Math.Atan2(
            (Math.Cos(phi1) * Math.Sin(phi2) * Math.Cos(lam1)) - (Math.Sin(phi1) * Math.Cos(phi2) * Math.Cos(lam2)),
            (Math.Sin(phi1) * Math.Cos(phi2) * Math.Sin(lam2)) - (Math.Cos(phi1) * Math.Sin(phi2) * Math.Sin(lam1)));
        phip = Math.Atan(-Math.Cos(lamp - lam1) / Math.Tan(phi1));
        return true;
    }

    private static bool TryCreateProjectionTransform(
        Dictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? forward,
        [NotNullWhen(true)] out MathTransform? inverse,
        out bool childIsAngular,
        out string? skipReason)
    {
        forward = null;
        inverse = null;
        childIsAngular = false;
        skipReason = null;

        if (!args.TryGetValue("proj", out string? projCode) || string.IsNullOrWhiteSpace(projCode))
        {
            skipReason = "Projection argument +proj is required.";
            return false;
        }

        if (projCode.Equals("latlon", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("latlong", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("lonlat", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("longlat", StringComparison.OrdinalIgnoreCase))
        {
            forward = new IdentityMathTransform(2);
            inverse = forward;
            childIsAngular = true;
            return true;
        }

        List<ProjectionParameter> parameters = BuildProjectionParameters(args);
        if (!TryResolveEllipsoid(args, out double semiMajor, out double semiMinor))
        {
            skipReason = "Unable to resolve ellipsoid for ob_tran child projection.";
            return false;
        }

        ReplaceOrAdd(parameters, "semi_major", semiMajor);
        ReplaceOrAdd(parameters, "semi_minor", semiMinor);
        ReplaceOrAdd(parameters, "unit", 1d);

        try
        {
            forward = ProjectionsRegistry.CreateProjection(projCode, parameters);
            inverse = forward.Inverse();
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException || exception is InvalidOperationException || exception is System.Reflection.TargetInvocationException)
        {
            skipReason = $"Unable to create ob_tran child projection '{projCode}': {exception.Message}";
            return false;
        }
    }

    private static List<ProjectionParameter> BuildProjectionParameters(Dictionary<string, string> args)
    {
        var parameters = new List<ProjectionParameter>
        {
            new("latitude_of_origin", 0d),
            new("central_meridian", 0d),
            new("scale_factor", 1d),
            new("false_easting", 0d),
            new("false_northing", 0d),
        };

        AddOptionalParameter(parameters, args, "lat_0", "latitude_of_origin");
        AddOptionalParameter(parameters, args, "lon_0", "central_meridian");
        AddOptionalParameter(parameters, args, "k_0", "scale_factor");
        AddOptionalParameter(parameters, args, "k", "scale_factor");
        AddOptionalParameter(parameters, args, "x_0", "false_easting");
        AddOptionalParameter(parameters, args, "y_0", "false_northing");

        AddOptionalParameter(parameters, args, "lat_1", "lat_1");
        AddOptionalParameter(parameters, args, "lat_2", "lat_2");
        AddOptionalParameter(parameters, args, "lon_1", "lon_1");
        AddOptionalParameter(parameters, args, "lon_2", "lon_2");
        AddOptionalParameter(parameters, args, "lat_3", "lat_3");
        AddOptionalParameter(parameters, args, "lon_3", "lon_3");
        AddOptionalParameter(parameters, args, "lat_b", "lat_b");
        AddOptionalParameter(parameters, args, "lat_ts", "lat_ts");
        AddOptionalParameter(parameters, args, "alpha", "alpha");
        AddOptionalParameter(parameters, args, "azi", "azi");
        AddOptionalParameter(parameters, args, "lonc", "longitude_of_center");
        AddOptionalParameter(parameters, args, "h", "h");
        AddOptionalParameter(parameters, args, "satellite_height", "h");
        AddOptionalParameter(parameters, args, "m", "m");
        AddOptionalParameter(parameters, args, "n", "n");
        AddOptionalParameter(parameters, args, "q", "q");

        if (args.ContainsKey("no_cut"))
        {
            ReplaceOrAdd(parameters, "no_cut", 1d);
        }

        if (args.ContainsKey("ns") || args.ContainsKey("noskew"))
        {
            ReplaceOrAdd(parameters, "ns", 1d);
        }

        if (args.TryGetValue("sweep", out string? sweepAxis))
        {
            double sweepX = sweepAxis.Equals("x", StringComparison.OrdinalIgnoreCase) ? 1d : 0d;
            ReplaceOrAdd(parameters, "sweep_x", sweepX);
        }

        return parameters;
    }

    private static void AddOptionalParameter(
        List<ProjectionParameter> parameters,
        Dictionary<string, string> args,
        string sourceName,
        string targetName)
    {
        if (!args.TryGetValue(sourceName, out string? valueToken) || string.IsNullOrWhiteSpace(valueToken))
        {
            return;
        }

        if (!TryGetDouble(valueToken, out double value))
        {
            return;
        }

        ReplaceOrAdd(parameters, targetName, value);
    }

    private static void ReplaceOrAdd(List<ProjectionParameter> parameters, string name, double value)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                parameters[i] = new ProjectionParameter(name, value);
                return;
            }
        }

        parameters.Add(new ProjectionParameter(name, value));
    }

    private static bool TryResolveEllipsoid(
        Dictionary<string, string> args,
        out double semiMajor,
        out double semiMinor)
    {
        if (TryGetFromArgs(args, "r", out double radius) && radius > 0d)
        {
            semiMajor = radius;
            semiMinor = radius;
            return true;
        }

        if (TryGetFromArgs(args, "a", out double a) && a > 0d)
        {
            semiMajor = a;

            if (TryGetFromArgs(args, "b", out double b) && b > 0d)
            {
                semiMinor = b;
                return true;
            }

            if (TryGetFromArgs(args, "rf", out double inverseFlattening) && inverseFlattening > 0d)
            {
                semiMinor = (1d - (1d / inverseFlattening)) * a;
                return true;
            }

            semiMinor = a;
            return true;
        }

        if (args.TryGetValue("ellps", out string? ellps) && !string.IsNullOrWhiteSpace(ellps))
        {
            if (ProjEllipsoidResolver.TryResolveKnownEllipsoid(
                ellps,
                allowClarke1880Ign: false,
                allowBessel: false,
                out semiMajor,
                out semiMinor))
            {
                return true;
            }
        }

        semiMajor = Ellipsoid.WGS84.SemiMajorAxis;
        semiMinor = Ellipsoid.WGS84.SemiMinorAxis;
        return true;
    }

    private static bool TryGetRequiredDegrees(
        Dictionary<string, string> args,
        string key,
        out double radians)
    {
        radians = 0d;
        if (!TryGetFromArgs(args, key, out double degrees))
        {
            return false;
        }

        radians = ToRadians(degrees);
        return true;
    }

    private static bool TryGetFromArgs(Dictionary<string, string> args, string key, out double value)
    {
        value = 0d;
        return args.TryGetValue(key, out string? token) && !string.IsNullOrWhiteSpace(token) && TryGetDouble(token, out value);
    }

    private static bool TryGetDouble(string token, out double value)
    {
        return double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value)
            && !double.IsNaN(value)
            && !double.IsInfinity(value);
    }

    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180d);
    }

    private static double ToDegrees(double radians)
    {
        return radians * (180d / Math.PI);
    }

    private static double SafeAsin(double value)
    {
        if (value > 1d)
        {
            value = 1d;
        }
        else if (value < -1d)
        {
            value = -1d;
        }

        return Math.Asin(value);
    }

    private static double AdjustLon(double value)
    {
        while (value > Math.PI)
        {
            value -= 2d * Math.PI;
        }

        while (value < -Math.PI)
        {
            value += 2d * Math.PI;
        }

        return value;
    }

    private static void RotateForward(
        ref double lam,
        ref double phi,
        double lamp,
        double sphip,
        double cphip,
        bool isOblique)
    {
        double cosPhi = Math.Cos(phi);
        double cosLam = Math.Cos(lam);

        if (isOblique)
        {
            double sinPhi = Math.Sin(phi);
            lam = AdjustLon(Math.Atan2(cosPhi * Math.Sin(lam), (sphip * cosPhi * cosLam) + (cphip * sinPhi)) + lamp);
            phi = SafeAsin((sphip * sinPhi) - (cphip * cosPhi * cosLam));
            return;
        }

        lam = AdjustLon(Math.Atan2(cosPhi * Math.Sin(lam), Math.Sin(phi)) + lamp);
        phi = SafeAsin(-cosPhi * cosLam);
    }

    private static void RotateInverse(
        ref double lam,
        ref double phi,
        double lamp,
        double sphip,
        double cphip,
        bool isOblique)
    {
        if (isOblique)
        {
            lam -= lamp;
            double cosLam = Math.Cos(lam);
            double sinPhi = Math.Sin(phi);
            double cosPhi = Math.Cos(phi);
            phi = SafeAsin((sphip * sinPhi) + (cphip * cosPhi * cosLam));
            lam = Math.Atan2(cosPhi * Math.Sin(lam), (sphip * cosPhi * cosLam) - (cphip * sinPhi));
            return;
        }

        double cosPhiTransverse = Math.Cos(phi);
        double t = lam - lamp;
        lam = Math.Atan2(cosPhiTransverse * Math.Sin(t), -Math.Sin(phi));
        phi = SafeAsin(cosPhiTransverse * Math.Cos(t));
    }

    private void TransformForward(ref double x, ref double y)
    {
        double lam = AdjustLon(ToRadians(x) - this.centralMeridian);
        double phi = ToRadians(y);
        RotateForward(ref lam, ref phi, this.lamp, this.sphip, this.cphip, this.isOblique);

        Span<double> childInput = stackalloc double[2];
        if (this.childIsAngular)
        {
            childInput[0] = lam;
            childInput[1] = phi;
        }
        else
        {
            childInput[0] = ToDegrees(lam);
            childInput[1] = ToDegrees(phi);
        }

        Span<double> childOutput = stackalloc double[3];
        this.childForward.Transform((ReadOnlySpan<double>)childInput, childOutput);
        x = childOutput[0];
        y = childOutput[1];
    }

    private void TransformInverse(ref double x, ref double y)
    {
        Span<double> childInput = stackalloc double[2];
        childInput[0] = x;
        childInput[1] = y;

        Span<double> rotated = stackalloc double[3];
        this.childInverse.Transform((ReadOnlySpan<double>)childInput, rotated);
        double lam = this.childIsAngular ? rotated[0] : ToRadians(rotated[0]);
        double phi = this.childIsAngular ? rotated[1] : ToRadians(rotated[1]);
        RotateInverse(ref lam, ref phi, this.lamp, this.sphip, this.cphip, this.isOblique);
        x = ToDegrees(AdjustLon(lam + this.centralMeridian));
        y = ToDegrees(phi);
    }
}
