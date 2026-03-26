// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a documented type.
/// </summary>
[Serializable]
internal readonly struct SampleData
{
    private readonly double[][] valuesBySample;
    private readonly double[] scaleBySample;
    private readonly double[] offsetBySample;
    private readonly int width;

    /// <summary>
    /// Initializes a new instance of the <see cref="SampleData"/> struct.
    /// </summary>
    /// <param name="valuesBySample">The valuesBySample value.</param>
    /// <param name="scaleBySample">The scaleBySample value.</param>
    /// <param name="offsetBySample">The offsetBySample value.</param>
    /// <param name="width">The width value.</param>
    internal SampleData(double[][] valuesBySample, double[] scaleBySample = null, double[] offsetBySample = null, int width = 0)
    {
        this.valuesBySample = valuesBySample;
        this.scaleBySample = scaleBySample ?? CreateConstant(valuesBySample?.Length ?? 0, 1d);
        this.offsetBySample = offsetBySample ?? CreateConstant(valuesBySample?.Length ?? 0, 0d);
        this.width = width;
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="angularScaleToDegree">The angularScaleToDegree value.</param>
    /// <returns>The computed value.</returns>
    internal SampleData ApplyAngularScale(double angularScaleToDegree)
    {
        if (Math.Abs(angularScaleToDegree - 1d) <= 1e-12d)
        {
            return this;
        }

        var scaled = new double[this.valuesBySample.Length][];
        for (int i = 0; i < scaled.Length; i++)
        {
            scaled[i] = this.valuesBySample[i];
        }

        double[] adjustedScale = new double[this.scaleBySample.Length];
        double[] adjustedOffset = new double[this.offsetBySample.Length];
        for (int i = 0; i < adjustedScale.Length; i++)
        {
            adjustedScale[i] = this.scaleBySample[i] * angularScaleToDegree;
            adjustedOffset[i] = this.offsetBySample[i] * angularScaleToDegree;
        }

        return new SampleData(scaled, adjustedScale, adjustedOffset, this.width);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="scaleBySample">The scaleBySample value.</param>
    /// <param name="offsetBySample">The offsetBySample value.</param>
    /// <returns>The computed value.</returns>
    internal SampleData ApplyScaleOffset(IReadOnlyDictionary<int, double> scaleBySample, IReadOnlyDictionary<int, double> offsetBySample)
    {
        if ((scaleBySample is null || scaleBySample.Count == 0)
            && (offsetBySample is null || offsetBySample.Count == 0))
        {
            return this;
        }

        var copiedValues = new double[this.valuesBySample.Length][];
        for (int i = 0; i < copiedValues.Length; i++)
        {
            copiedValues[i] = this.valuesBySample[i];
        }

        double[] adjustedScale = new double[this.scaleBySample.Length];
        double[] adjustedOffset = new double[this.offsetBySample.Length];
        for (int i = 0; i < this.scaleBySample.Length; i++)
        {
            double scaleFactor = 1d;
            if (!(scaleBySample is null) && scaleBySample.TryGetValue(i, out double parsedScale))
            {
                scaleFactor = parsedScale;
            }

            double offsetValue = 0d;
            if (!(offsetBySample is null) && offsetBySample.TryGetValue(i, out double parsedOffset))
            {
                offsetValue = parsedOffset;
            }

            adjustedScale[i] = this.scaleBySample[i] * scaleFactor;
            adjustedOffset[i] = (this.offsetBySample[i] * scaleFactor) + offsetValue;
        }

        return new SampleData(copiedValues, adjustedScale, adjustedOffset, this.width);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="sample">The sample value.</param>
    /// <param name="x">The x value.</param>
    /// <param name="y">The y value.</param>
    /// <returns>The computed value.</returns>
    internal double GetValue(int sample, int x, int y)
    {
        int index = (y * this.width) + x;
        return (this.valuesBySample[sample][index] * this.scaleBySample[sample]) + this.offsetBySample[sample];
    }

    private static double[] CreateConstant(int count, double value)
    {
        var result = new double[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = value;
        }

        return result;
    }
}

