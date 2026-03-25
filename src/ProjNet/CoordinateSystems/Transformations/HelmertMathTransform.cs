// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Implements PROJ's <c>helmert</c> runtime transform for static and kinematic operations.
/// </summary>
[Serializable]
internal sealed class HelmertMathTransform : MathTransform
{
    private const double ArcSecondToRadians = Math.PI / (180d * 3600d);
    private const double MissingObservationEpoch = double.MaxValue;
    private enum CsvParseStatus
    {
        Success,
        TooManyValues,
        InvalidValue,
    }

    private readonly HelmertParameterState baseState;
    private readonly HelmertRateState rateState;
    private readonly bool hasKinematicRates;
    private readonly double epochReference;

    private readonly bool fourParameter;
    private readonly bool noRotation;
    private readonly bool exact;
    private readonly bool isPositionVector;

    private readonly HelmertParameterState staticState;
    private readonly HelmertRuntimeState runtimeState;

    private bool isInverted;
    private MathTransform inverse;

    private HelmertMathTransform(
        HelmertParameterState baseState,
        HelmertRateState rateState,
        bool hasKinematicRates,
        double epochReference,
        bool fourParameter,
        bool noRotation,
        bool exact,
        bool isPositionVector,
        HelmertRuntimeState runtimeState,
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
        this.runtimeState = runtimeState;
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
        this.runtimeState = source.runtimeState.Clone();
        this.staticState = source.staticState;
        this.isInverted = isInverted;
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
        out MathTransform transform,
        out string skipReason)
    {
        transform = null;
        skipReason = null;

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

        if (args.TryGetValue("theta", out string thetaToken))
        {
            if (!TryParseFiniteDouble(thetaToken, out double thetaArcSeconds))
            {
                skipReason = "Invalid value for +theta.";
                return false;
            }

            fourParameter = true;
            theta = thetaArcSeconds * ArcSecondToRadians;
            scale = 1d;
        }

        if (args.TryGetValue("dtheta", out string thetaRateToken))
        {
            if (!TryParseFiniteDouble(thetaRateToken, out double thetaRateArcSeconds))
            {
                skipReason = "Invalid value for +dtheta.";
                return false;
            }

            thetaRate = thetaRateArcSeconds * ArcSecondToRadians;
            hasKinematicRates = true;
        }

        if (args.TryGetValue("s", out string scaleToken))
        {
            if (!TryParseFiniteDouble(scaleToken, out scale))
            {
                skipReason = "helmert: invalid value for s.";
                return false;
            }
        }

        if (args.TryGetValue("ds", out string scaleRateToken))
        {
            if (!TryParseFiniteDouble(scaleRateToken, out scaleRate))
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

        if (args.TryGetValue("t_epoch", out string epochToken))
        {
            if (!TryParseFiniteDouble(epochToken, out epochReference))
            {
                skipReason = "Invalid value for +t_epoch.";
                return false;
            }
        }

        if (scale <= -1e6d || (fourParameter && scale == 0d))
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
            if (!args.TryGetValue("convention", out string convention) || string.IsNullOrWhiteSpace(convention))
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
        BuildRotationMatrix(
            staticState.RotationX,
            staticState.RotationY,
            staticState.RotationZ,
            exact,
            isPositionVector,
            out double r00,
            out double r01,
            out double r02,
            out double r10,
            out double r11,
            out double r12,
            out double r20,
            out double r21,
            out double r22);
        var runtimeState = new HelmertRuntimeState(
            staticState.TranslationX,
            staticState.TranslationY,
            staticState.TranslationZ,
            staticState.Scale,
            staticState.Theta,
            r00,
            r01,
            r02,
            r10,
            r11,
            r12,
            r20,
            r21,
            r22,
            hasKinematicRates ? MissingObservationEpoch : epochReference);
        transform = new HelmertMathTransform(
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
        if (args.ContainsKey("inv"))
        {
            transform = transform.Inverse();
        }

        return true;
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
        out string skipReason)
    {
        skipReason = null;
        hasTowgs84 = false;

        if (!args.TryGetValue("towgs84", out string towgs84Token) || string.IsNullOrWhiteSpace(towgs84Token))
        {
            return true;
        }

        Span<double> values = stackalloc double[7];
        CsvParseStatus parseStatus = TryParseCsvValues(towgs84Token.AsSpan(), values, out int valueCount);
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
            rotationX = values[3] * ArcSecondToRadians;
            rotationY = values[4] * ArcSecondToRadians;
            rotationZ = values[5] * ArcSecondToRadians;
        }

        if (valueCount == 7)
        {
            scale = values[6];
        }

        hasTowgs84 = true;
        return true;
    }

    private static CsvParseStatus TryParseCsvValues(ReadOnlySpan<char> token, Span<double> destination, out int parsedCount)
    {
        parsedCount = 0;
        int segmentStart = 0;
        for (int i = 0; i <= token.Length; i++)
        {
            bool atDelimiter = i < token.Length && token[i] == ',';
            if (i != token.Length && !atDelimiter)
            {
                continue;
            }

            ReadOnlySpan<char> segment = TrimWhitespace(token.Slice(segmentStart, i - segmentStart));
            if (!segment.IsEmpty)
            {
                if (parsedCount >= destination.Length)
                {
                    return CsvParseStatus.TooManyValues;
                }

                if (!TryParseFiniteDouble(segment, out destination[parsedCount]))
                {
                    return CsvParseStatus.InvalidValue;
                }

                parsedCount++;
            }

            segmentStart = i + 1;
        }

        return CsvParseStatus.Success;
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

    private static bool TryAssignOptionalDouble(
        Dictionary<string, string> args,
        string key,
        ref double target,
        out string skipReason)
    {
        skipReason = null;
        if (!args.TryGetValue(key, out string token))
        {
            return true;
        }

        if (!TryParseFiniteDouble(token, out target))
        {
            skipReason = "Invalid value for +" + key + ".";
            return false;
        }

        return true;
    }

    private static bool TryAssignOptionalArcSeconds(
        Dictionary<string, string> args,
        string key,
        ref double target,
        out string skipReason)
    {
        skipReason = null;
        if (!args.TryGetValue(key, out string token))
        {
            return true;
        }

        if (!TryParseFiniteDouble(token, out double value))
        {
            skipReason = "Invalid value for +" + key + ".";
            return false;
        }

        target = value * ArcSecondToRadians;
        return true;
    }

    private static bool TryParseFiniteDouble(string token, out double value)
    {
        value = 0d;
        return !string.IsNullOrWhiteSpace(token)
            && TryParseFiniteDouble(token.AsSpan(), out value);
    }

    private static bool TryParseFiniteDouble(ReadOnlySpan<char> token, out double value)
    {
#if NETSTANDARD2_0
        bool parsed = double.TryParse(token.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
#else
        bool parsed = double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
#endif

        return parsed && !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private static ReadOnlySpan<char> TrimWhitespace(ReadOnlySpan<char> value)
    {
        int start = 0;
        while (start < value.Length && char.IsWhiteSpace(value[start]))
        {
            start++;
        }

        int end = value.Length - 1;
        while (end >= start && char.IsWhiteSpace(value[end]))
        {
            end--;
        }

        return end < start ? ReadOnlySpan<char>.Empty : value.Slice(start, (end - start) + 1);
    }

    private static void BuildRotationMatrix(
        double rotationX,
        double rotationY,
        double rotationZ,
        bool exact,
        bool isPositionVector,
        out double r00,
        out double r01,
        out double r02,
        out double r10,
        out double r11,
        out double r12,
        out double r20,
        out double r21,
        out double r22)
    {
        if (exact)
        {
            double cf = Math.Cos(rotationX);
            double sf = Math.Sin(rotationX);
            double ct = Math.Cos(rotationY);
            double st = Math.Sin(rotationY);
            double cp = Math.Cos(rotationZ);
            double sp = Math.Sin(rotationZ);

            r00 = ct * cp;
            r01 = (cf * sp) + (sf * st * cp);
            r02 = (sf * sp) - (cf * st * cp);
            r10 = -ct * sp;
            r11 = (cf * cp) - (sf * st * sp);
            r12 = (sf * cp) + (cf * st * sp);
            r20 = st;
            r21 = -sf * ct;
            r22 = cf * ct;
        }
        else
        {
            r00 = 1d;
            r01 = rotationZ;
            r02 = -rotationY;
            r10 = -rotationZ;
            r11 = 1d;
            r12 = rotationX;
            r20 = rotationY;
            r21 = -rotationX;
            r22 = 1d;
        }

        if (!isPositionVector)
        {
            return;
        }

        Swap(ref r01, ref r10);
        Swap(ref r02, ref r20);
        Swap(ref r12, ref r21);
    }

    private static void Swap(ref double left, ref double right)
    {
        double value = left;
        left = right;
        right = value;
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
        if (this.fourParameter)
        {
            return state.TranslationX == 0d
                && state.TranslationY == 0d
                && state.Theta == 0d
                && state.Scale == 1d;
        }

        return state.TranslationX == 0d
            && state.TranslationY == 0d
            && state.TranslationZ == 0d
            && this.noRotation
            && state.Scale == 0d;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new HelmertMathTransform(this, !this.isInverted);
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

    private HelmertParameterState GetOrCreateStateForObservationEpoch(double observationEpoch)
    {
        if (!this.hasKinematicRates)
        {
            return this.staticState;
        }

        double normalizedEpoch = observationEpoch;
        if (double.IsNaN(normalizedEpoch) || double.IsInfinity(normalizedEpoch) || normalizedEpoch == MissingObservationEpoch)
        {
            normalizedEpoch = this.epochReference;
        }

        if (this.runtimeState.ObservationEpoch == normalizedEpoch)
        {
            return EvaluateKinematicState(this.baseState, this.rateState, this.epochReference, normalizedEpoch);
        }

        HelmertParameterState dynamicState = EvaluateKinematicState(this.baseState, this.rateState, this.epochReference, normalizedEpoch);
        BuildRotationMatrix(
            dynamicState.RotationX,
            dynamicState.RotationY,
            dynamicState.RotationZ,
            this.exact,
            this.isPositionVector,
            out double r00,
            out double r01,
            out double r02,
            out double r10,
            out double r11,
            out double r12,
            out double r20,
            out double r21,
            out double r22);
        this.runtimeState.R00 = r00;
        this.runtimeState.R01 = r01;
        this.runtimeState.R02 = r02;
        this.runtimeState.R10 = r10;
        this.runtimeState.R11 = r11;
        this.runtimeState.R12 = r12;
        this.runtimeState.R20 = r20;
        this.runtimeState.R21 = r21;
        this.runtimeState.R22 = r22;

        this.runtimeState.TranslationX = dynamicState.TranslationX;
        this.runtimeState.TranslationY = dynamicState.TranslationY;
        this.runtimeState.TranslationZ = dynamicState.TranslationZ;
        this.runtimeState.Scale = dynamicState.Scale;
        this.runtimeState.Theta = dynamicState.Theta;
        this.runtimeState.ObservationEpoch = normalizedEpoch;
        return dynamicState;
    }

    private void TransformForward(ref double x, ref double y, ref double z, HelmertParameterState state)
    {
        if (this.fourParameter)
        {
            double cosTheta = Math.Cos(state.Theta) * state.Scale;
            double sinTheta = Math.Sin(state.Theta) * state.Scale;
            double sourceX = x;
            double sourceY = y;
            x = (cosTheta * sourceX) + (sinTheta * sourceY) + this.runtimeState.TranslationX;
            y = (-sinTheta * sourceX) + (cosTheta * sourceY) + this.runtimeState.TranslationY;
            return;
        }

        if (this.noRotation && state.Scale == 0d)
        {
            x += this.runtimeState.TranslationX;
            y += this.runtimeState.TranslationY;
            z += this.runtimeState.TranslationZ;
            return;
        }

        double scaleFactor = 1d + (state.Scale * 1e-6d);
        double sourceX3 = x;
        double sourceY3 = y;
        double sourceZ3 = z;
        x = (scaleFactor * ((this.runtimeState.R00 * sourceX3) + (this.runtimeState.R01 * sourceY3) + (this.runtimeState.R02 * sourceZ3))) + this.runtimeState.TranslationX;
        y = (scaleFactor * ((this.runtimeState.R10 * sourceX3) + (this.runtimeState.R11 * sourceY3) + (this.runtimeState.R12 * sourceZ3))) + this.runtimeState.TranslationY;
        z = (scaleFactor * ((this.runtimeState.R20 * sourceX3) + (this.runtimeState.R21 * sourceY3) + (this.runtimeState.R22 * sourceZ3))) + this.runtimeState.TranslationZ;
    }

    private void TransformInverse(ref double x, ref double y, ref double z, HelmertParameterState state)
    {
        if (this.fourParameter)
        {
            double cosTheta = Math.Cos(state.Theta) / state.Scale;
            double sinTheta = Math.Sin(state.Theta) / state.Scale;
            double sourceX = x - this.runtimeState.TranslationX;
            double sourceY = y - this.runtimeState.TranslationY;
            x = (sourceX * cosTheta) - (sourceY * sinTheta);
            y = (sourceX * sinTheta) + (sourceY * cosTheta);
            return;
        }

        if (this.noRotation && state.Scale == 0d)
        {
            x -= this.runtimeState.TranslationX;
            y -= this.runtimeState.TranslationY;
            z -= this.runtimeState.TranslationZ;
            return;
        }

        double scaleFactor = 1d + (state.Scale * 1e-6d);
        double sourceX3 = (x - this.runtimeState.TranslationX) / scaleFactor;
        double sourceY3 = (y - this.runtimeState.TranslationY) / scaleFactor;
        double sourceZ3 = (z - this.runtimeState.TranslationZ) / scaleFactor;
        x = (this.runtimeState.R00 * sourceX3) + (this.runtimeState.R10 * sourceY3) + (this.runtimeState.R20 * sourceZ3);
        y = (this.runtimeState.R01 * sourceX3) + (this.runtimeState.R11 * sourceY3) + (this.runtimeState.R21 * sourceZ3);
        z = (this.runtimeState.R02 * sourceX3) + (this.runtimeState.R12 * sourceY3) + (this.runtimeState.R22 * sourceZ3);
    }

    [Serializable]
    private readonly struct HelmertParameterState
    {
        internal HelmertParameterState(
            double translationX,
            double translationY,
            double translationZ,
            double rotationX,
            double rotationY,
            double rotationZ,
            double scale,
            double theta)
        {
            this.TranslationX = translationX;
            this.TranslationY = translationY;
            this.TranslationZ = translationZ;
            this.RotationX = rotationX;
            this.RotationY = rotationY;
            this.RotationZ = rotationZ;
            this.Scale = scale;
            this.Theta = theta;
        }

        internal double TranslationX { get; }

        internal double TranslationY { get; }

        internal double TranslationZ { get; }

        internal double RotationX { get; }

        internal double RotationY { get; }

        internal double RotationZ { get; }

        internal double Scale { get; }

        internal double Theta { get; }
    }

    [Serializable]
    private readonly struct HelmertRateState
    {
        internal HelmertRateState(
            double translationRateX,
            double translationRateY,
            double translationRateZ,
            double rotationRateX,
            double rotationRateY,
            double rotationRateZ,
            double scaleRate,
            double thetaRate)
        {
            this.TranslationRateX = translationRateX;
            this.TranslationRateY = translationRateY;
            this.TranslationRateZ = translationRateZ;
            this.RotationRateX = rotationRateX;
            this.RotationRateY = rotationRateY;
            this.RotationRateZ = rotationRateZ;
            this.ScaleRate = scaleRate;
            this.ThetaRate = thetaRate;
        }

        internal double TranslationRateX { get; }

        internal double TranslationRateY { get; }

        internal double TranslationRateZ { get; }

        internal double RotationRateX { get; }

        internal double RotationRateY { get; }

        internal double RotationRateZ { get; }

        internal double ScaleRate { get; }

        internal double ThetaRate { get; }
    }

    [Serializable]
    private sealed class HelmertRuntimeState
    {
        internal HelmertRuntimeState(
            double translationX,
            double translationY,
            double translationZ,
            double scale,
            double theta,
            double r00,
            double r01,
            double r02,
            double r10,
            double r11,
            double r12,
            double r20,
            double r21,
            double r22,
            double observationEpoch)
        {
            this.TranslationX = translationX;
            this.TranslationY = translationY;
            this.TranslationZ = translationZ;
            this.Scale = scale;
            this.Theta = theta;
            this.R00 = r00;
            this.R01 = r01;
            this.R02 = r02;
            this.R10 = r10;
            this.R11 = r11;
            this.R12 = r12;
            this.R20 = r20;
            this.R21 = r21;
            this.R22 = r22;
            this.ObservationEpoch = observationEpoch;
        }

        internal double TranslationX { get; set; }

        internal double TranslationY { get; set; }

        internal double TranslationZ { get; set; }

        internal double Scale { get; set; }

        internal double Theta { get; set; }

        internal double R00 { get; set; }

        internal double R01 { get; set; }

        internal double R02 { get; set; }

        internal double R10 { get; set; }

        internal double R11 { get; set; }

        internal double R12 { get; set; }

        internal double R20 { get; set; }

        internal double R21 { get; set; }

        internal double R22 { get; set; }

        internal double ObservationEpoch { get; set; }

        internal HelmertRuntimeState Clone()
        {
            return new HelmertRuntimeState(
                this.TranslationX,
                this.TranslationY,
                this.TranslationZ,
                this.Scale,
                this.Theta,
                this.R00,
                this.R01,
                this.R02,
                this.R10,
                this.R11,
                this.R12,
                this.R20,
                this.R21,
                this.R22,
                this.ObservationEpoch);
        }
    }
}
