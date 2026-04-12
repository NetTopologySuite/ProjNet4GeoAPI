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
using ProjNet.CoordinateSystems.Transformations.Numerics;

/// <summary>
/// Implements PROJ's <c>sch</c> (Spherical Cross-track Height) 3D runtime transform.
/// </summary>
/// <remarks>
/// <para>
/// SCH is the JPL sensor-aligned Spherical Cross-track Height system. This
/// implementation follows PROJ's <c>sch</c> operation by deriving the peg-point
/// radius of curvature from the ellipsoid, building the peg-frame rotation
/// matrix, and converting between ellipsoidal geocentric coordinates and the
/// local SCH sphere.
/// </para>
/// <para>
/// The runtime was independently verified against PROJ's published
/// <c>sch</c> documentation and <c>sch.cpp</c>. In particular, PROJ's internal
/// normalization by the semimajor axis is framework-specific; this
/// implementation works directly in metre-valued coordinates and preserves the
/// same geometry without that intermediate scaling step.
/// </para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/projections/sch.html">PROJ: sch.</seealso>
internal sealed class SchMathTransform : MathTransform
{
    private readonly GeocentricTransform ellipsoidForward;
    private readonly GeocentricTransform ellipsoidInverse;
    private readonly GeocentricTransform sphereForward;
    private readonly GeocentricTransform sphereInverse;

    private readonly double semiMajorAxis;
    private readonly double radiusOfCurvature;

    private readonly Vector3D offset;

    private readonly Matrix3x3 rotationMatrix;

    private readonly bool isInverted;
    private MathTransform? inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchMathTransform"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters that include SCH and ellipsoid values.</param>
    public SchMathTransform(List<ProjectionParameter> parameters)
        : this(parameters, false)
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
            new("semi_major", this.semiMajorAxis),
            new("semi_minor", semiMinorAxis),
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
            new("semi_major", this.radiusOfCurvature),
            new("semi_minor", this.radiusOfCurvature),
        };

        this.sphereForward = new GeocentricTransform(sphereParameters, false);
        this.sphereInverse = (GeocentricTransform)this.sphereForward.Inverse();

        this.rotationMatrix = new Matrix3x3(
            cosPegLatitude * cosPegLongitude,
            (-sinPegHeading * sinPegLongitude) - (sinPegLatitude * cosPegLongitude * cosPegHeading),
            (sinPegLongitude * cosPegHeading) - (sinPegLatitude * cosPegLongitude * sinPegHeading),
            cosPegLatitude * sinPegLongitude,
            (cosPegLongitude * sinPegHeading) - (sinPegLatitude * sinPegLongitude * cosPegHeading),
            (-cosPegLongitude * cosPegHeading) - (sinPegLatitude * sinPegLongitude * sinPegHeading),
            sinPegLatitude,
            cosPegLatitude * cosPegHeading,
            cosPegLatitude * sinPegHeading);

        double pegLongitudeDegrees = RadiansToDegrees(pegLongitude);
        double pegLatitudeDegrees = RadiansToDegrees(pegLatitude);
        double pegZ = pegHeight;
        this.ellipsoidForward.Transform(ref pegLongitudeDegrees, ref pegLatitudeDegrees, ref pegZ);

        this.offset = new Vector3D(
            pegLongitudeDegrees - (this.radiusOfCurvature * cosPegLatitude * cosPegLongitude),
            pegLatitudeDegrees - (this.radiusOfCurvature * cosPegLatitude * sinPegLongitude),
            pegZ - (this.radiusOfCurvature * sinPegLatitude));

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

        this.offset = source.offset;
        this.rotationMatrix = source.rotationMatrix;

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
        this.inverse ??= new SchMathTransform(this, !this.isInverted);

        return this.inverse;
    }

    /// <inheritdoc />
    public override void Invert()
    {
        throw new NotSupportedException("SchMathTransform is immutable. Use Inverse() to obtain inverted transform.");
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

        if (!ProjEllipsoidResolver.TryResolveEllipsoidOrDefault(
            args,
            includeDatumToken: true,
            allowClarke1880Ign: false,
            allowBessel: false,
            out double semiMajor,
            out double semiMinor))
        {
            skipReason = "Unable to resolve ellipsoid for sch.";
            return false;
        }

        var parameters = new List<ProjectionParameter>
        {
            new("plat_0", Parse(args["plat_0"])),
            new("plon_0", Parse(args["plon_0"])),
            new("phdg_0", Parse(args["phdg_0"])),
            new("h_0", pegHeight),
            new("semi_major", semiMajor),
            new("semi_minor", semiMinor),
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

    private static bool TryGetFromArgs(Dictionary<string, string> args, string key, out double value)
    {
        value = 0d;
        return args.TryGetValue(key, out string? token) && !string.IsNullOrWhiteSpace(token) && double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value)
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

        Vector3D global = new Vector3D(lon, lat, height) - this.offset;

        Vector3D local = this.rotationMatrix.Transpose() * global;
        double localX = local.X;
        double localY = local.Y;
        double localZ = local.Z;

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

        Vector3D global = (this.rotationMatrix * new Vector3D(lon, lat, height)) + this.offset;
        double globalX = global.X;
        double globalY = global.Y;
        double globalZ = global.Z;

        this.ellipsoidInverse.Transform(ref globalX, ref globalY, ref globalZ);
        x = globalX;
        y = globalY;
        z = globalZ;
    }
}
