// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Implements PROJ's <c>geogoffset</c> runtime transform (geographic offsets).
/// </summary>
/// <remarks>
/// Geographic offsets are a trivial additive transform:
/// <c>x += dlon</c>, <c>y += dlat</c>, and <c>z += dh</c>, with longitude and
/// latitude offsets converted from arc-seconds to degrees during construction.
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/transformations/geogoffset.html">PROJ: geographic offset transformation.</seealso>
internal sealed class GeogOffsetMathTransform : MathTransform
{
    private const double ArcSecondsPerDegree = 3600d;

    private readonly double longitudeOffsetDegrees;
    private readonly double latitudeOffsetDegrees;
    private readonly double heightOffsetMeters;

    private readonly bool isInverted;
    private MathTransform? inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeogOffsetMathTransform"/> class.
    /// </summary>
    /// <param name="longitudeOffsetArcSeconds">Longitude offset in arc-seconds.</param>
    /// <param name="latitudeOffsetArcSeconds">Latitude offset in arc-seconds.</param>
    /// <param name="heightOffsetMeters">Height offset in metres.</param>
    /// <param name="isInverted">
    /// <see langword="true"/> to subtract offsets; <see langword="false"/> to add offsets.
    /// </param>
    private GeogOffsetMathTransform(
        double longitudeOffsetArcSeconds,
        double latitudeOffsetArcSeconds,
        double heightOffsetMeters,
        bool isInverted)
    {
        ArgumentGuard.ThrowIfNotFinite(longitudeOffsetArcSeconds, nameof(longitudeOffsetArcSeconds), "Offset values must be finite.");
        ArgumentGuard.ThrowIfNotFinite(latitudeOffsetArcSeconds, nameof(latitudeOffsetArcSeconds), "Offset values must be finite.");
        ArgumentGuard.ThrowIfNotFinite(heightOffsetMeters, nameof(heightOffsetMeters), "Offset values must be finite.");

        this.longitudeOffsetDegrees = longitudeOffsetArcSeconds / ArcSecondsPerDegree;
        this.latitudeOffsetDegrees = latitudeOffsetArcSeconds / ArcSecondsPerDegree;
        this.heightOffsetMeters = heightOffsetMeters;
        this.isInverted = isInverted;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeogOffsetMathTransform"/> class
    /// as an inverted clone.
    /// </summary>
    /// <param name="source">Source instance to clone.</param>
    /// <param name="isInverted">Whether to apply inverse direction in the clone.</param>
    private GeogOffsetMathTransform(GeogOffsetMathTransform source, bool isInverted)
    {
        this.longitudeOffsetDegrees = source.longitudeOffsetDegrees;
        this.latitudeOffsetDegrees = source.latitudeOffsetDegrees;
        this.heightOffsetMeters = source.heightOffsetMeters;
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
    public override bool Identity()
    {
        return this.longitudeOffsetDegrees == 0d
            && this.latitudeOffsetDegrees == 0d
            && this.heightOffsetMeters == 0d;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new GeogOffsetMathTransform(this, !this.isInverted);

        return this.inverse;
    }

    /// <inheritdoc />
    public override void Invert()
    {
        throw new NotSupportedException("GeogOffsetMathTransform is immutable. Use Inverse() to obtain inverted transform.");
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        if (double.IsNaN(z))
        {
            z = 0d;
        }

        double sign = this.isInverted ? -1d : 1d;
        x += sign * this.longitudeOffsetDegrees;
        y += sign * this.latitudeOffsetDegrees;
        z += sign * this.heightOffsetMeters;
    }

    /// <summary>
    /// Creates a <see cref="GeogOffsetMathTransform"/> from parsed PROJ arguments.
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
            skipReason = "geogoffset arguments were null.";
            return false;
        }

        if (!SpanParseUtility.TryGetOptionalDouble(args, "dlon", out double dlonArcSeconds, out skipReason)
            || !SpanParseUtility.TryGetOptionalDouble(args, "dlat", out double dlatArcSeconds, out skipReason)
            || !SpanParseUtility.TryGetOptionalDouble(args, "dh", out double dhMeters, out skipReason))
        {
            return false;
        }

        transform = Create(dlonArcSeconds, dlatArcSeconds, dhMeters, args.ContainsKey("inv"));
        return true;
    }

    /// <summary>
    /// Creates a geographic offset transform from resolved numeric parameters.
    /// </summary>
    /// <param name="longitudeOffsetArcSeconds">Longitude offset in arc-seconds.</param>
    /// <param name="latitudeOffsetArcSeconds">Latitude offset in arc-seconds.</param>
    /// <param name="heightOffsetMeters">Height offset in metres.</param>
    /// <param name="isInverted"><see langword="true"/> to create the inverse direction.</param>
    /// <returns>The created transform.</returns>
    internal static MathTransform Create(
        double longitudeOffsetArcSeconds,
        double latitudeOffsetArcSeconds,
        double heightOffsetMeters,
        bool isInverted = false)
    {
        MathTransform transform = longitudeOffsetArcSeconds == 0d && latitudeOffsetArcSeconds == 0d && heightOffsetMeters == 0d
            ? new IdentityMathTransform(3)
            : new GeogOffsetMathTransform(longitudeOffsetArcSeconds, latitudeOffsetArcSeconds, heightOffsetMeters, false);
        return isInverted ? transform.Inverse() : transform;
    }
}
