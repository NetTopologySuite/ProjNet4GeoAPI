// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;

/// <summary>
/// Nested model, extent, interpolation, and time-function types for the defmodel transform.
/// </summary>
internal sealed partial class DefModelMathTransform
{
    private enum DisplacementType : byte
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
        ThreeDimensional = 3,
    }

    private enum InterpolationMethod : byte
    {
        Bilinear = 0,
        GeocentricBilinear = 1,
    }

    /// <summary>
    /// Represents a time-dependent scale-factor function.
    /// </summary>
    private interface ITimeFunction
    {
        /// <summary>
        /// Evaluates the scale factor for the supplied observation epoch.
        /// </summary>
        /// <param name="observationEpoch">Observation epoch expressed in decimal years.</param>
        /// <returns>The scale factor for the supplied epoch.</returns>
        double Evaluate(double observationEpoch);
    }

    private readonly struct SpatialExtent(double minX, double minY, double maxX, double maxY)
    {
        internal double MinX { get; } = minX;

        internal double MinY { get; } = minY;

        internal double MaxX { get; } = maxX;

        internal double MaxY { get; } = maxY;
    }

    private readonly struct TimeExtent(double first, double last)
    {
        internal double First { get; } = first;

        internal double Last { get; } = last;
    }

    private readonly struct InterpolationCell(
        int x0,
        int y0,
        int x1,
        int y1,
        double fractionX,
        double fractionY,
        double w00,
        double w01,
        double w10,
        double w11)
    {
        internal int X0 { get; } = x0;

        internal int Y0 { get; } = y0;

        internal int X1 { get; } = x1;

        internal int Y1 { get; } = y1;

        internal double FractionX { get; } = fractionX;

        internal double FractionY { get; } = fractionY;

        internal double W00 { get; } = w00;

        internal double W01 { get; } = w01;

        internal double W10 { get; } = w10;

        internal double W11 { get; } = w11;
    }

    private readonly struct XyzCornerValues(
        double x00,
        double x01,
        double x10,
        double x11,
        double y00,
        double y01,
        double y10,
        double y11,
        double z00,
        double z01,
        double z10,
        double z11)
    {
        internal double X00 { get; } = x00;

        internal double X01 { get; } = x01;

        internal double X10 { get; } = x10;

        internal double X11 { get; } = x11;

        internal double Y00 { get; } = y00;

        internal double Y01 { get; } = y01;

        internal double Y10 { get; } = y10;

        internal double Y11 { get; } = y11;

        internal double Z00 { get; } = z00;

        internal double Z01 { get; } = z01;

        internal double Z10 { get; } = z10;

        internal double Z11 { get; } = z11;
    }

    private sealed class ModelDefinition
    {
        internal string FileType { get; set; } = string.Empty;

        internal string FormatVersion { get; set; } = string.Empty;

        internal string SourceCrs { get; set; } = string.Empty;

        internal string TargetCrs { get; set; } = string.Empty;

        internal string DefinitionCrs { get; set; } = string.Empty;

        internal string HorizontalOffsetUnit { get; set; } = string.Empty;

        internal string VerticalOffsetUnit { get; set; } = string.Empty;

        internal string HorizontalOffsetMethod { get; set; } = string.Empty;

        internal SpatialExtent Extent { get; set; }

        internal TimeExtent TimeExtent { get; set; }

        internal ComponentDefinition[] Components { get; set; } = [];
    }

    private sealed class ComponentDefinition
    {
        internal string Description { get; set; } = string.Empty;

        internal DisplacementType DisplacementType { get; set; }

        internal InterpolationMethod InterpolationMethod { get; set; }

        internal SpatialExtent Extent { get; set; }

        internal string SpatialModelFileName { get; set; } = string.Empty;

        internal ITimeFunction TimeFunction { get; set; } = ConstantTimeFunction.Instance;
    }

    private sealed class ComponentRuntime
    {
        internal ComponentRuntime(
            ComponentDefinition definition,
            IReadOnlyList<GeoTiffXyzGridShiftMathTransform.XyzGrid>? xyzGrids,
            IReadOnlyList<GeoTiffVGridShiftMathTransform.VerticalGrid>? verticalGrids)
        {
            this.Definition = definition;
            this.XyzGrids = xyzGrids ?? [];
            this.VerticalGrids = verticalGrids ?? [];
        }

        internal ComponentDefinition Definition { get; }

        internal IReadOnlyList<GeoTiffXyzGridShiftMathTransform.XyzGrid> XyzGrids { get; }

        internal IReadOnlyList<GeoTiffVGridShiftMathTransform.VerticalGrid> VerticalGrids { get; }
    }

    private sealed class ConstantTimeFunction : ITimeFunction
    {
        internal static readonly ConstantTimeFunction Instance = new();

        private ConstantTimeFunction()
        {
        }

        public double Evaluate(double observationEpoch)
        {
            _ = observationEpoch;
            return 1d;
        }
    }

    private sealed class VelocityTimeFunction(double referenceEpoch) : ITimeFunction
    {
        public double Evaluate(double observationEpoch)
        {
            return observationEpoch - referenceEpoch;
        }
    }

    private sealed class StepTimeFunction(double stepEpoch) : ITimeFunction
    {
        public double Evaluate(double observationEpoch)
        {
            return observationEpoch < stepEpoch ? 0d : 1d;
        }
    }

    private sealed class ReverseStepTimeFunction(double stepEpoch) : ITimeFunction
    {
        public double Evaluate(double observationEpoch)
        {
            return observationEpoch < stepEpoch ? -1d : 0d;
        }
    }

    private sealed class PiecewiseTimeFunction : ITimeFunction
    {
        private readonly string beforeFirst;
        private readonly string afterLast;
        private readonly EpochScaleTuple[] model;

        internal PiecewiseTimeFunction(string beforeFirst, string afterLast, EpochScaleTuple[] model)
        {
            this.beforeFirst = beforeFirst;
            this.afterLast = afterLast;
            this.model = model ?? [];
        }

        public double Evaluate(double observationEpoch)
        {
            if (this.model.Length == 0)
            {
                return 0d;
            }

            double firstEpoch = this.model[0].Epoch;
            if (observationEpoch < firstEpoch)
            {
                if (this.beforeFirst == "ZERO")
                {
                    return 0d;
                }

                if (this.beforeFirst == "CONSTANT" || this.model.Length == 1)
                {
                    return this.model[0].ScaleFactor;
                }

                double f1 = this.model[0].ScaleFactor;
                double secondEpoch = this.model[1].Epoch;
                double f2 = this.model[1].ScaleFactor;
                return firstEpoch == secondEpoch
                    ? f1
                    : ((f1 * (secondEpoch - observationEpoch)) + (f2 * (observationEpoch - firstEpoch))) / (secondEpoch - firstEpoch);
            }

            for (int i = 1; i < this.model.Length; i++)
            {
                double epochIp1 = this.model[i].Epoch;
                if (observationEpoch < epochIp1)
                {
                    double epochI = this.model[i - 1].Epoch;
                    double factorIp1 = this.model[i].ScaleFactor;
                    double factorI = this.model[i - 1].ScaleFactor;
                    return ((factorI * (epochIp1 - observationEpoch)) + (factorIp1 * (observationEpoch - epochI))) / (epochIp1 - epochI);
                }
            }

            if (this.afterLast == "ZERO")
            {
                return 0d;
            }

            if (this.afterLast == "CONSTANT" || this.model.Length == 1)
            {
                return this.model[^1].ScaleFactor;
            }

            double previousEpoch = this.model[^2].Epoch;
            double previousFactor = this.model[^2].ScaleFactor;
            double lastEpoch = this.model[^1].Epoch;
            double lastFactor = this.model[^1].ScaleFactor;
            return previousEpoch == lastEpoch
                ? lastFactor
                : ((previousFactor * (lastEpoch - observationEpoch)) + (lastFactor * (observationEpoch - previousEpoch))) / (lastEpoch - previousEpoch);
        }

        internal readonly struct EpochScaleTuple(double epoch, double scaleFactor)
        {
            internal double Epoch { get; } = epoch;

            internal double ScaleFactor { get; } = scaleFactor;
        }
    }

    private sealed class ExponentialTimeFunction : ITimeFunction
    {
        private readonly double referenceEpoch;
        private readonly double? endEpoch;
        private readonly double relaxationConstant;
        private readonly double beforeScaleFactor;
        private readonly double initialScaleFactor;
        private readonly double finalScaleFactor;

        internal ExponentialTimeFunction(
            double referenceEpoch,
            double? endEpoch,
            double relaxationConstant,
            double beforeScaleFactor,
            double initialScaleFactor,
            double finalScaleFactor)
        {
            this.referenceEpoch = referenceEpoch;
            this.endEpoch = endEpoch;
            this.relaxationConstant = relaxationConstant;
            this.beforeScaleFactor = beforeScaleFactor;
            this.initialScaleFactor = initialScaleFactor;
            this.finalScaleFactor = finalScaleFactor;
        }

        public double Evaluate(double observationEpoch)
        {
            if (observationEpoch < this.referenceEpoch)
            {
                return this.beforeScaleFactor;
            }

            double clampedEpoch = observationEpoch;
            if (this.endEpoch.HasValue)
            {
                clampedEpoch = Math.Min(clampedEpoch, this.endEpoch.Value);
            }

            return this.initialScaleFactor
                + ((this.finalScaleFactor - this.initialScaleFactor)
                    * (1d - Math.Exp(-(clampedEpoch - this.referenceEpoch) / this.relaxationConstant)));
        }
    }
}
