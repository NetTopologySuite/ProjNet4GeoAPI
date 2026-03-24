// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

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
