// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;

/// <summary>
/// Nested metadata and page-model types for the GeoTIFF grid loader.
/// </summary>
internal static partial class GeoTiffGridLoader
{
    private enum GridMode
    {
        Horizontal,
        Vertical,
        Xyz,
    }

    private readonly struct GeoTransform(
        int width,
        int height,
        double a,
        double b,
        double c,
        double d,
        double e,
        double f,
        double determinant,
        double west,
        double east,
        double south,
        double north,
        double area,
        double epsilon)
    {
        internal int Width { get; } = width;

        internal int Height { get; } = height;

        internal double A { get; } = a;

        internal double B { get; } = b;

        internal double C { get; } = c;

        internal double D { get; } = d;

        internal double E { get; } = e;

        internal double F { get; } = f;

        internal double Determinant { get; } = determinant;

        internal double West { get; } = west;

        internal double East { get; } = east;

        internal double South { get; } = south;

        internal double North { get; } = north;

        internal double Area { get; } = area;

        internal double Epsilon { get; } = epsilon;
    }

    private readonly struct GeoMetadata(
        IReadOnlyDictionary<int, string> descriptionsBySample,
        IReadOnlyDictionary<int, string> positiveValueBySample,
        IReadOnlyDictionary<int, double> scaleBySample,
        IReadOnlyDictionary<int, double> offsetBySample,
        double? noDataValue,
        double angularScaleToDegree,
        IReadOnlyDictionary<int, string> unitTypeBySample,
        bool useBiquadraticInterpolation)
    {
        internal IReadOnlyDictionary<int, string> DescriptionsBySample { get; } = descriptionsBySample;

        internal IReadOnlyDictionary<int, string> PositiveValueBySample { get; } = positiveValueBySample;

        internal IReadOnlyDictionary<int, double> ScaleBySample { get; } = scaleBySample;

        internal IReadOnlyDictionary<int, double> OffsetBySample { get; } = offsetBySample;

        internal double? NoDataValue { get; } = noDataValue;

        internal double AngularScaleToDegree { get; } = angularScaleToDegree;

        internal IReadOnlyDictionary<int, string> UnitTypeBySample { get; } = unitTypeBySample;

        internal bool UseBiquadraticInterpolation { get; } = useBiquadraticInterpolation;
    }

    private sealed class LoadedPage
    {
        private readonly GeoTransform transform;
        private readonly SampleData sampleData;
        private readonly GeoMetadata metadata;
        private readonly int latitudeSample;
        private readonly int longitudeSample;
        private readonly int verticalSample;
        private readonly int xSample;
        private readonly int ySample;
        private readonly int zSample;
        private readonly bool longitudePositiveWest;
        private readonly bool projectedOffsets;
        private readonly GridMode mode;
        private readonly bool useBiquadraticInterpolation;

        private LoadedPage(
            GeoTransform transform,
            SampleData sampleData,
            GeoMetadata metadata,
            int latitudeSample,
            int longitudeSample,
            int verticalSample,
            int xSample,
            int ySample,
            int zSample,
            bool longitudePositiveWest,
            bool projectedOffsets,
            GridMode mode,
            bool useBiquadraticInterpolation)
        {
            this.transform = transform;
            this.sampleData = sampleData;
            this.metadata = metadata;
            this.latitudeSample = latitudeSample;
            this.longitudeSample = longitudeSample;
            this.verticalSample = verticalSample;
            this.xSample = xSample;
            this.ySample = ySample;
            this.zSample = zSample;
            this.longitudePositiveWest = longitudePositiveWest;
            this.projectedOffsets = projectedOffsets;
            this.mode = mode;
            this.useBiquadraticInterpolation = useBiquadraticInterpolation;
        }

        internal static LoadedPage CreateHorizontal(
            GeoTransform transform,
            SampleData sampleData,
            GeoMetadata metadata,
            int latitudeSample,
            int longitudeSample,
            bool longitudePositiveWest,
            bool projectedOffsets)
        {
            return new LoadedPage(
                transform,
                sampleData,
                metadata,
                latitudeSample,
                longitudeSample,
                -1,
                -1,
                -1,
                -1,
                longitudePositiveWest,
                projectedOffsets,
                GridMode.Horizontal,
                metadata.UseBiquadraticInterpolation);
        }

        internal static LoadedPage CreateVertical(
            GeoTransform transform,
            SampleData sampleData,
            GeoMetadata metadata,
            int verticalSample)
        {
            return new LoadedPage(
                transform,
                sampleData,
                metadata,
                -1,
                -1,
                verticalSample,
                -1,
                -1,
                -1,
                false,
                false,
                GridMode.Vertical,
                false);
        }

        internal static LoadedPage CreateXyz(
            GeoTransform transform,
            SampleData sampleData,
            GeoMetadata metadata,
            int xSample,
            int ySample,
            int zSample)
        {
            return new LoadedPage(
                transform,
                sampleData,
                metadata,
                -1,
                -1,
                -1,
                xSample,
                ySample,
                zSample,
                false,
                false,
                GridMode.Xyz,
                false);
        }

        internal GeoTiffHGridShiftMathTransform.HorizontalGrid? ToHorizontalGrid(string sourcePath)
        {
            if (this.mode != GridMode.Horizontal)
            {
                return null;
            }

            double latitudeScale = ResolveHorizontalShiftScale(this.metadata, this.latitudeSample, this.projectedOffsets);
            double longitudeScale = ResolveHorizontalShiftScale(this.metadata, this.longitudeSample, this.projectedOffsets);
            return new GeoTiffHGridShiftMathTransform.HorizontalGrid(
                sourcePath,
                this.transform.Width,
                this.transform.Height,
                this.transform.Area,
                this.transform.Epsilon,
                this.transform.West,
                this.transform.East,
                this.transform.South,
                this.transform.North,
                this.transform.A,
                this.transform.B,
                this.transform.C,
                this.transform.D,
                this.transform.E,
                this.transform.F,
                this.sampleData,
                this.latitudeSample,
                this.longitudeSample,
                this.longitudePositiveWest,
                latitudeScale,
                longitudeScale,
                !this.projectedOffsets,
                this.useBiquadraticInterpolation);
        }

        internal GeoTiffVGridShiftMathTransform.VerticalGrid? ToVerticalGrid(string sourcePath)
        {
            return this.mode != GridMode.Vertical
                ? null
                : new GeoTiffVGridShiftMathTransform.VerticalGrid(
                    sourcePath,
                    this.transform.Width,
                    this.transform.Height,
                    this.transform.Area,
                    this.transform.Epsilon,
                    this.transform.West,
                    this.transform.East,
                    this.transform.South,
                    this.transform.North,
                    this.transform.A,
                    this.transform.B,
                    this.transform.C,
                    this.transform.D,
                    this.transform.E,
                    this.transform.F,
                    this.sampleData,
                    this.verticalSample,
                    this.metadata.NoDataValue);
        }

        internal GeoTiffXyzGridShiftMathTransform.XyzGrid? ToXyzGrid(string sourcePath)
        {
            return this.mode != GridMode.Xyz
                ? null
                : new GeoTiffXyzGridShiftMathTransform.XyzGrid(
                    sourcePath,
                    this.transform.Width,
                    this.transform.Height,
                    this.transform.Area,
                    this.transform.Epsilon,
                    this.transform.West,
                    this.transform.East,
                    this.transform.South,
                    this.transform.North,
                    this.transform.A,
                    this.transform.B,
                    this.transform.C,
                    this.transform.D,
                    this.transform.E,
                    this.transform.F,
                    this.sampleData,
                    this.xSample,
                    this.ySample,
                    this.zSample);
        }

        private static double ResolveHorizontalShiftScale(GeoMetadata metadata, int sampleIndex, bool projectedOffsets)
        {
            if (metadata.UnitTypeBySample.TryGetValue(sampleIndex, out string? unitType))
            {
                string unit = unitType.Trim();
                if (unit.Equals("degree", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("degrees", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("deg", StringComparison.OrdinalIgnoreCase))
                {
                    return 1d;
                }

                if (unit.Equals("radian", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("radians", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("rad", StringComparison.OrdinalIgnoreCase))
                {
                    return 180d / Math.PI;
                }

                if (unit.Equals("arc-second", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("arc-seconds", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("arc_second", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("arc_seconds", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("arcsecond", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("arcseconds", StringComparison.OrdinalIgnoreCase)
                    || unit.Equals("arcsec", StringComparison.OrdinalIgnoreCase))
                {
                    return 1d / 3600d;
                }
            }

            return projectedOffsets ? 1d : 1d / 3600d;
        }
    }
}
