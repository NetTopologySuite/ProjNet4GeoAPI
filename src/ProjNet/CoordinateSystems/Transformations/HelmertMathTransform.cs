// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ProjNet.CoordinateSystems.Transformations.Numerics;

/// <summary>
/// Implements PROJ's <c>helmert</c> runtime transform for static and kinematic operations.
/// </summary>
/// <remarks>
/// The core 7-parameter Bursa-Wolf formulation was independently verified against ISO
/// 19111:2019, <i>Geographic information - Referencing by coordinates</i>. The scale
/// factor multiplies the fully rotated vector before translation, matching the standard
/// <c>T + (1 + s) * R * X</c> form implemented here.
/// The position-vector and coordinate-frame rotation conventions were independently
/// verified against IOGP, "Geomatics Guidance Note 7, part 2: Coordinate Conversions
/// and Transformations including Formulas" (publication 373-7-2, 2019), EPSG methods
/// 1033 and 1032. Those methods differ only in the sign convention for the rotation
/// parameters, and the matrix transposition used here for position-vector mode matches
/// that published relationship.
/// </remarks>
/// <seealso href="https://en.wikipedia.org/wiki/Helmert_transformation">Wikipedia: Helmert transformation.</seealso>
/// <seealso href="https://epsg.io/1033-method">EPSG method 1033: Position Vector transformation (geocentric domain).</seealso>
/// <seealso href="https://epsg.io/1032-method">EPSG method 1032: Coordinate Frame rotation (geocentric domain).</seealso>
internal sealed class HelmertMathTransform : MathTransform
{
    private readonly HelmertParameterState baseState;
    private readonly HelmertRateState rateState;
    private readonly bool hasKinematicRates;
    private readonly double epochReference;

    private readonly bool fourParameter;
    private readonly bool noRotation;
    private readonly bool exact;
    private readonly bool isPositionVector;

    private readonly HelmertParameterState staticState;
    private readonly HelmertRuntimeState staticRuntimeState;

    private readonly bool isInverted;
    private MathTransform? inverse;

    private HelmertMathTransform(
        HelmertParameterState baseState,
        HelmertRateState rateState,
        bool hasKinematicRates,
        double epochReference,
        bool fourParameter,
        bool noRotation,
        bool exact,
        bool isPositionVector,
        HelmertRuntimeState staticRuntimeState,
        HelmertParameterState staticState,
        bool isInverted)
    {
        this.baseState = baseState;
        this.rateState = rateState;
        this.hasKinematicRates = hasKinematicRates;
        this.epochReference = epochReference;
        this.fourParameter = fourParameter;
        this.noRotation = noRotation;
        this.exact = exact;
        this.isPositionVector = isPositionVector;
        this.staticRuntimeState = staticRuntimeState;
        this.staticState = staticState;
        this.isInverted = isInverted;
    }

    private HelmertMathTransform(HelmertMathTransform source, bool isInverted)
    {
        this.baseState = source.baseState;
        this.rateState = source.rateState;
        this.hasKinematicRates = source.hasKinematicRates;
        this.epochReference = source.epochReference;
        this.fourParameter = source.fourParameter;
        this.noRotation = source.noRotation;
        this.exact = source.exact;
        this.isPositionVector = source.isPositionVector;
        this.staticRuntimeState = source.staticRuntimeState.Clone();
        this.staticState = source.staticState;
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
        HelmertParameterState state = this.staticState;
        return this.fourParameter
            ? state.TranslationX == 0d
                && state.TranslationY == 0d
                && state.Theta == 0d
                && state.Scale == 1d
            : state.TranslationX == 0d
            && state.TranslationY == 0d
            && state.TranslationZ == 0d
            && this.noRotation
            && state.Scale == 0d;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new HelmertMathTransform(this, !this.isInverted);

        return this.inverse;
    }

    /// <inheritdoc />
    public override void Invert()
    {
        throw new NotSupportedException("HelmertMathTransform is immutable. Use Inverse() to obtain inverted transform.");
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        HelmertParameterState state = this.staticState;
        if (this.isInverted)
        {
            this.TransformInverse(ref x, ref y, ref z, state);
        }
        else
        {
            this.TransformForward(ref x, ref y, ref z, state);
        }
    }

    /// <summary>
    /// Creates a <see cref="HelmertMathTransform"/> from parsed PROJ arguments.
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
            skipReason = "helmert arguments were null.";
            return false;
        }

        if (args.ContainsKey("transpose"))
        {
            skipReason = "helmert: 'transpose' argument is no longer valid. Use convention=position_vector/coordinate_frame";
            return false;
        }

        double translationX = 0d;
        double translationY = 0d;
        double translationZ = 0d;
        double rotationX = 0d;
        double rotationY = 0d;
        double rotationZ = 0d;
        double scale = 0d;
        double theta = 0d;
        bool fourParameter = false;

        double translationRateX = 0d;
        double translationRateY = 0d;
        double translationRateZ = 0d;
        double rotationRateX = 0d;
        double rotationRateY = 0d;
        double rotationRateZ = 0d;
        double scaleRate = 0d;
        double thetaRate = 0d;
        bool hasKinematicRates = false;
        double epochReference = 0d;

        bool hasTowgs84;
        if (!TryApplyTowgs84(args, out hasTowgs84, ref translationX, ref translationY, ref translationZ, ref rotationX, ref rotationY, ref rotationZ, ref scale, out skipReason))
        {
            return false;
        }

        if (!TryAssignOptionalDouble(args, "x", ref translationX, out skipReason)
            || !TryAssignOptionalDouble(args, "y", ref translationY, out skipReason)
            || !TryAssignOptionalDouble(args, "z", ref translationZ, out skipReason))
        {
            return false;
        }

        if (!TryAssignOptionalArcSeconds(args, "rx", ref rotationX, out skipReason)
            || !TryAssignOptionalArcSeconds(args, "ry", ref rotationY, out skipReason)
            || !TryAssignOptionalArcSeconds(args, "rz", ref rotationZ, out skipReason))
        {
            return false;
        }

        if (args.TryGetValue("theta", out string? thetaToken))
        {
            if (!SpanParseUtility.TryParseFiniteDouble(thetaToken, out double thetaArcSeconds))
            {
                skipReason = "Invalid value for +theta.";
                return false;
            }

            fourParameter = true;
            theta = thetaArcSeconds * TransformationMath.ArcSecondToRadians;
            scale = 1d;
        }

        if (args.TryGetValue("dtheta", out string? thetaRateToken))
        {
            if (!SpanParseUtility.TryParseFiniteDouble(thetaRateToken, out double thetaRateArcSeconds))
            {
                skipReason = "Invalid value for +dtheta.";
                return false;
            }

            thetaRate = thetaRateArcSeconds * TransformationMath.ArcSecondToRadians;
            hasKinematicRates = true;
        }

        if (args.TryGetValue("s", out string? scaleToken))
        {
            if (!SpanParseUtility.TryParseFiniteDouble(scaleToken, out scale))
            {
                skipReason = "helmert: invalid value for s.";
                return false;
            }
        }

        if (args.TryGetValue("ds", out string? scaleRateToken))
        {
            if (!SpanParseUtility.TryParseFiniteDouble(scaleRateToken, out scaleRate))
            {
                skipReason = "Invalid value for +ds.";
                return false;
            }

            hasKinematicRates = true;
        }

        if (!TryAssignOptionalDouble(args, "dx", ref translationRateX, out skipReason)
            || !TryAssignOptionalDouble(args, "dy", ref translationRateY, out skipReason)
            || !TryAssignOptionalDouble(args, "dz", ref translationRateZ, out skipReason))
        {
            return false;
        }

        if (translationRateX != 0d || translationRateY != 0d || translationRateZ != 0d)
        {
            hasKinematicRates = true;
        }

        if (!TryAssignOptionalArcSeconds(args, "drx", ref rotationRateX, out skipReason)
            || !TryAssignOptionalArcSeconds(args, "dry", ref rotationRateY, out skipReason)
            || !TryAssignOptionalArcSeconds(args, "drz", ref rotationRateZ, out skipReason))
        {
            return false;
        }

        if (rotationRateX != 0d || rotationRateY != 0d || rotationRateZ != 0d)
        {
            hasKinematicRates = true;
        }

        if (args.TryGetValue("t_epoch", out string? epochToken))
        {
            if (!SpanParseUtility.TryParseFiniteDouble(epochToken, out epochReference))
            {
                skipReason = "Invalid value for +t_epoch.";
                return false;
            }
        }

        if (scale <= TransformationMath.MinValidPpmScale || (fourParameter && scale == 0d))
        {
            skipReason = "helmert: invalid value for s.";
            return false;
        }

        bool noRotation = rotationX == 0d
            && rotationY == 0d
            && rotationZ == 0d
            && rotationRateX == 0d
            && rotationRateY == 0d
            && rotationRateZ == 0d;
        bool isPositionVector = false;
        if (!noRotation)
        {
            if (!args.TryGetValue("convention", out string? convention) || string.IsNullOrWhiteSpace(convention))
            {
                skipReason = "helmert: missing 'convention' argument";
                return false;
            }

            if (convention.Equals("position_vector", StringComparison.OrdinalIgnoreCase))
            {
                isPositionVector = true;
            }
            else if (convention.Equals("coordinate_frame", StringComparison.OrdinalIgnoreCase))
            {
                isPositionVector = false;
            }
            else
            {
                skipReason = "helmert: invalid value for 'convention' argument";
                return false;
            }

            if (hasTowgs84 && !isPositionVector)
            {
                skipReason = "helmert: towgs84 should only be used with convention=position_vector";
                return false;
            }
        }

        bool exact = args.ContainsKey("exact");
        transform = Create(
            translationX,
            translationY,
            translationZ,
            rotationX,
            rotationY,
            rotationZ,
            scale,
            theta,
            translationRateX,
            translationRateY,
            translationRateZ,
            rotationRateX,
            rotationRateY,
            rotationRateZ,
            scaleRate,
            thetaRate,
            hasKinematicRates,
            epochReference,
            fourParameter,
            noRotation,
            exact,
            isPositionVector,
            args.ContainsKey("inv"));
        return true;
    }

    /// <summary>
    /// Creates a Helmert transform from resolved numeric parameters.
    /// </summary>
    /// <param name="translationX">X translation in metres.</param>
    /// <param name="translationY">Y translation in metres.</param>
    /// <param name="translationZ">Z translation in metres.</param>
    /// <param name="rotationX">X rotation in radians.</param>
    /// <param name="rotationY">Y rotation in radians.</param>
    /// <param name="rotationZ">Z rotation in radians.</param>
    /// <param name="scale">Scale difference in ppm.</param>
    /// <param name="theta">2D rotation in radians.</param>
    /// <param name="translationRateX">Rate of change of X translation in metres/year.</param>
    /// <param name="translationRateY">Rate of change of Y translation in metres/year.</param>
    /// <param name="translationRateZ">Rate of change of Z translation in metres/year.</param>
    /// <param name="rotationRateX">Rate of change of X rotation in radians/year.</param>
    /// <param name="rotationRateY">Rate of change of Y rotation in radians/year.</param>
    /// <param name="rotationRateZ">Rate of change of Z rotation in radians/year.</param>
    /// <param name="scaleRate">Rate of change of scale difference in ppm/year.</param>
    /// <param name="thetaRate">Rate of change of 2D rotation in radians/year.</param>
    /// <param name="hasKinematicRates"><see langword="true"/> when any time-dependent rates are present.</param>
    /// <param name="epochReference">Reference epoch for kinematic parameters.</param>
    /// <param name="fourParameter"><see langword="true"/> for the 4-parameter variant.</param>
    /// <param name="noRotation"><see langword="true"/> when all rotation terms are zero.</param>
    /// <param name="exact"><see langword="true"/> to use the exact rotation formulation.</param>
    /// <param name="isPositionVector"><see langword="true"/> for position-vector convention.</param>
    /// <param name="isInverted"><see langword="true"/> to create the inverse direction.</param>
    /// <returns>The created transform.</returns>
    internal static MathTransform Create(
        double translationX,
        double translationY,
        double translationZ,
        double rotationX,
        double rotationY,
        double rotationZ,
        double scale,
        double theta,
        double translationRateX,
        double translationRateY,
        double translationRateZ,
        double rotationRateX,
        double rotationRateY,
        double rotationRateZ,
        double scaleRate,
        double thetaRate,
        bool hasKinematicRates,
        double epochReference,
        bool fourParameter,
        bool noRotation,
        bool exact,
        bool isPositionVector,
        bool isInverted = false)
    {
        var baseState = new HelmertParameterState(
            translationX,
            translationY,
            translationZ,
            rotationX,
            rotationY,
            rotationZ,
            scale,
            theta);
        var rateState = new HelmertRateState(
            translationRateX,
            translationRateY,
            translationRateZ,
            rotationRateX,
            rotationRateY,
            rotationRateZ,
            scaleRate,
            thetaRate);
        HelmertParameterState staticState = hasKinematicRates
            ? EvaluateKinematicState(baseState, rateState, epochReference, epochReference)
            : baseState;
        Matrix3x3 rotationMatrix = BuildRotationMatrix(
            staticState.RotationX,
            staticState.RotationY,
            staticState.RotationZ,
            exact,
            isPositionVector);
        var runtimeState = new HelmertRuntimeState(
            new Vector3D(staticState.TranslationX, staticState.TranslationY, staticState.TranslationZ),
            staticState.Scale,
            staticState.Theta,
            rotationMatrix,
            hasKinematicRates ? TransformationMath.MissingObservationEpoch : epochReference);

        MathTransform transform = new HelmertMathTransform(
            baseState,
            rateState,
            hasKinematicRates,
            epochReference,
            fourParameter,
            noRotation,
            exact,
            isPositionVector,
            runtimeState,
            staticState,
            false);
        return isInverted ? transform.Inverse() : transform;
    }

    /// <inheritdoc />
    internal override void Transform(ref double x, ref double y, ref double z, ref double t)
    {
        HelmertParameterState state = this.GetOrCreateStateForObservationEpoch(t);
        if (this.isInverted)
        {
            this.TransformInverse(ref x, ref y, ref z, state);
        }
        else
        {
            this.TransformForward(ref x, ref y, ref z, state);
        }
    }

    private static bool TryApplyTowgs84(
        Dictionary<string, string> args,
        out bool hasTowgs84,
        ref double translationX,
        ref double translationY,
        ref double translationZ,
        ref double rotationX,
        ref double rotationY,
        ref double rotationZ,
        ref double scale,
        out string? skipReason)
    {
        skipReason = null;
        hasTowgs84 = false;

        if (!args.TryGetValue("towgs84", out string? towgs84Token) || string.IsNullOrWhiteSpace(towgs84Token))
        {
            return true;
        }

        Span<double> values = stackalloc double[7];
        CsvParseStatus parseStatus = SpanParseUtility.TryParseCsvValues(towgs84Token.AsSpan(), values, out int valueCount);
        if (parseStatus != CsvParseStatus.Success || (valueCount != 3 && valueCount != 6 && valueCount != 7))
        {
            skipReason = "Invalid value for +towgs84.";
            return false;
        }

        translationX = values[0];
        translationY = values[1];
        translationZ = values[2];
        if (valueCount >= 6)
        {
            rotationX = values[3] * TransformationMath.ArcSecondToRadians;
            rotationY = values[4] * TransformationMath.ArcSecondToRadians;
            rotationZ = values[5] * TransformationMath.ArcSecondToRadians;
        }

        if (valueCount == 7)
        {
            scale = values[6];
        }

        hasTowgs84 = true;
        return true;
    }

    private static bool TryAssignOptionalDouble(
        Dictionary<string, string> args,
        string key,
        ref double target,
        out string? skipReason)
    {
        skipReason = null;
        if (!args.TryGetValue(key, out string? token))
        {
            return true;
        }

        if (!SpanParseUtility.TryParseFiniteDouble(token, out target))
        {
            skipReason = $"Invalid value for +{key}.";
            return false;
        }

        return true;
    }

    private static bool TryAssignOptionalArcSeconds(
        Dictionary<string, string> args,
        string key,
        ref double target,
        out string? skipReason)
    {
        skipReason = null;
        if (!args.TryGetValue(key, out string? token))
        {
            return true;
        }

        if (!SpanParseUtility.TryParseFiniteDouble(token, out double value))
        {
            skipReason = $"Invalid value for +{key}.";
            return false;
        }

        target = value * TransformationMath.ArcSecondToRadians;
        return true;
    }

    private static HelmertParameterState EvaluateKinematicState(
        HelmertParameterState baseState,
        HelmertRateState rateState,
        double epochReference,
        double observationEpoch)
    {
        double dt = observationEpoch - epochReference;
        return new HelmertParameterState(
            baseState.TranslationX + (rateState.TranslationRateX * dt),
            baseState.TranslationY + (rateState.TranslationRateY * dt),
            baseState.TranslationZ + (rateState.TranslationRateZ * dt),
            baseState.RotationX + (rateState.RotationRateX * dt),
            baseState.RotationY + (rateState.RotationRateY * dt),
            baseState.RotationZ + (rateState.RotationRateZ * dt),
            baseState.Scale + (rateState.ScaleRate * dt),
            baseState.Theta + (rateState.ThetaRate * dt));
    }

    private static Matrix3x3 BuildRotationMatrix(
        double rotationX,
        double rotationY,
        double rotationZ,
        bool exact,
        bool isPositionVector)
    {
        Matrix3x3 matrix;
        if (exact)
        {
            double cf = Math.Cos(rotationX);
            double sf = Math.Sin(rotationX);
            double ct = Math.Cos(rotationY);
            double st = Math.Sin(rotationY);
            double cp = Math.Cos(rotationZ);
            double sp = Math.Sin(rotationZ);

            matrix = new Matrix3x3(
                ct * cp,
                (cf * sp) + (sf * st * cp),
                (sf * sp) - (cf * st * cp),
                -ct * sp,
                (cf * cp) - (sf * st * sp),
                (sf * cp) + (cf * st * sp),
                st,
                -sf * ct,
                cf * ct);
        }
        else
        {
            matrix = new Matrix3x3(
                1d,
                rotationZ,
                -rotationY,
                -rotationZ,
                1d,
                rotationX,
                rotationY,
                -rotationX,
                1d);
        }

        return isPositionVector ? matrix.Transpose() : matrix;
    }

    private HelmertParameterState GetOrCreateStateForObservationEpoch(double observationEpoch)
    {
        if (!this.hasKinematicRates)
        {
            return this.staticState;
        }

        double normalizedEpoch = observationEpoch;
        if (double.IsNaN(normalizedEpoch) || double.IsInfinity(normalizedEpoch) || normalizedEpoch == TransformationMath.MissingObservationEpoch)
        {
            normalizedEpoch = this.epochReference;
        }

        return EvaluateKinematicState(this.baseState, this.rateState, this.epochReference, normalizedEpoch);
    }

    private void TransformForward(ref double x, ref double y, ref double z, HelmertParameterState state)
    {
        if (this.fourParameter)
        {
            double cosTheta = Math.Cos(state.Theta) * state.Scale;
            double sinTheta = Math.Sin(state.Theta) * state.Scale;
            double sourceX = x;
            double sourceY = y;
            x = (cosTheta * sourceX) + (sinTheta * sourceY) + state.TranslationX;
            y = (-sinTheta * sourceX) + (cosTheta * sourceY) + state.TranslationY;
            return;
        }

        if (this.noRotation && state.Scale == 0d)
        {
            x += state.TranslationX;
            y += state.TranslationY;
            z += state.TranslationZ;
            return;
        }

        Matrix3x3 rotationMatrix = this.GetRotationMatrix(state);
        var translation = new Vector3D(state.TranslationX, state.TranslationY, state.TranslationZ);
        double scaleFactor = 1d + (state.Scale * 1e-6d);
        Vector3D transformed = ((rotationMatrix * new Vector3D(x, y, z)) * scaleFactor) + translation;
        x = transformed.X;
        y = transformed.Y;
        z = transformed.Z;
    }

    private void TransformInverse(ref double x, ref double y, ref double z, HelmertParameterState state)
    {
        if (this.fourParameter)
        {
            if (state.Scale == 0d)
            {
                ArgumentGuard.ThrowArgument("helmert: inverse 4-parameter transform requires non-zero scale.", nameof(state));
            }

            double cosTheta = Math.Cos(state.Theta) / state.Scale;
            double sinTheta = Math.Sin(state.Theta) / state.Scale;
            double sourceX = x - state.TranslationX;
            double sourceY = y - state.TranslationY;
            x = (sourceX * cosTheta) - (sourceY * sinTheta);
            y = (sourceX * sinTheta) + (sourceY * cosTheta);
            return;
        }

        if (this.noRotation && state.Scale == 0d)
        {
            x -= state.TranslationX;
            y -= state.TranslationY;
            z -= state.TranslationZ;
            return;
        }

        Matrix3x3 rotationMatrix = this.GetRotationMatrix(state);
        var translation = new Vector3D(state.TranslationX, state.TranslationY, state.TranslationZ);
        double scaleFactor = 1d + (state.Scale * 1e-6d);
        Vector3D source = (new Vector3D(x, y, z) - translation) / scaleFactor;
        Vector3D transformed = rotationMatrix.Transpose() * source;
        x = transformed.X;
        y = transformed.Y;
        z = transformed.Z;
    }

    private Matrix3x3 GetRotationMatrix(HelmertParameterState state)
    {
        if (!this.hasKinematicRates)
        {
            return this.staticRuntimeState.RotationMatrix;
        }

        return BuildRotationMatrix(
            state.RotationX,
            state.RotationY,
            state.RotationZ,
            this.exact,
            this.isPositionVector);
    }

    private readonly struct HelmertParameterState(
        double translationX,
        double translationY,
        double translationZ,
        double rotationX,
        double rotationY,
        double rotationZ,
        double scale,
        double theta)
    {
        internal double TranslationX { get; } = translationX;

        internal double TranslationY { get; } = translationY;

        internal double TranslationZ { get; } = translationZ;

        internal double RotationX { get; } = rotationX;

        internal double RotationY { get; } = rotationY;

        internal double RotationZ { get; } = rotationZ;

        internal double Scale { get; } = scale;

        internal double Theta { get; } = theta;
    }

    private readonly struct HelmertRateState(
        double translationRateX,
        double translationRateY,
        double translationRateZ,
        double rotationRateX,
        double rotationRateY,
        double rotationRateZ,
        double scaleRate,
        double thetaRate)
    {
        internal double TranslationRateX { get; } = translationRateX;

        internal double TranslationRateY { get; } = translationRateY;

        internal double TranslationRateZ { get; } = translationRateZ;

        internal double RotationRateX { get; } = rotationRateX;

        internal double RotationRateY { get; } = rotationRateY;

        internal double RotationRateZ { get; } = rotationRateZ;

        internal double ScaleRate { get; } = scaleRate;

        internal double ThetaRate { get; } = thetaRate;
    }

    private sealed class HelmertRuntimeState
    {
        internal HelmertRuntimeState(
            Vector3D translation,
            double scale,
            double theta,
            Matrix3x3 rotationMatrix,
            double observationEpoch)
        {
            this.Translation = translation;
            this.Scale = scale;
            this.Theta = theta;
            this.RotationMatrix = rotationMatrix;
            this.ObservationEpoch = observationEpoch;
        }

        internal Vector3D Translation { get; set; }

        internal double Scale { get; set; }

        internal double Theta { get; set; }

        internal Matrix3x3 RotationMatrix { get; set; }

        internal double ObservationEpoch { get; set; }

        internal HelmertRuntimeState Clone()
        {
            return new HelmertRuntimeState(
                this.Translation,
                this.Scale,
                this.Theta,
                this.RotationMatrix,
                this.ObservationEpoch);
        }
    }
}
