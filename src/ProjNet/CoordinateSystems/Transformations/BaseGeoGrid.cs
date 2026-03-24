// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// Represents a documented type.
/// </summary>
[Serializable]
internal abstract class BaseGeoGrid
{
    private readonly SampleData sampleData;
    private readonly double determinant;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseGeoGrid"/> class.
    /// </summary>
    /// <param name="sourcePath">The sourcePath value.</param>
    /// <param name="width">The width value.</param>
    /// <param name="height">The height value.</param>
    /// <param name="area">The area value.</param>
    /// <param name="epsilon">The epsilon value.</param>
    /// <param name="west">The west value.</param>
    /// <param name="east">The east value.</param>
    /// <param name="south">The south value.</param>
    /// <param name="north">The north value.</param>
    /// <param name="a">The a value.</param>
    /// <param name="b">The b value.</param>
    /// <param name="c">The c value.</param>
    /// <param name="d">The d value.</param>
    /// <param name="e">The e value.</param>
    /// <param name="f">The f value.</param>
    /// <param name="sampleData">The sampleData value.</param>
    protected BaseGeoGrid(
        string sourcePath,
        int width,
        int height,
        double area,
        double epsilon,
        double west,
        double east,
        double south,
        double north,
        double a,
        double b,
        double c,
        double d,
        double e,
        double f,
        SampleData sampleData)
    {
        this.SourcePath = sourcePath;
        this.Width = width;
        this.Height = height;
        this.Area = area;
        this.Epsilon = epsilon;
        this.West = west;
        this.East = east;
        this.South = south;
        this.North = north;
        this.A = a;
        this.B = b;
        this.C = c;
        this.D = d;
        this.E = e;
        this.F = f;
        this.sampleData = sampleData;
        this.determinant = (a * e) - (b * d);
    }

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal string SourcePath { get; }

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal int Width { get; }

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal int Height { get; }

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal double Area { get; }

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal double Epsilon { get; }

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal double West { get; }

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal double East { get; }

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal double South { get; }

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal double North { get; }

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal double A { get; }

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal double B { get; }

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal double C { get; }

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal double D { get; }

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal double E { get; }

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal double F { get; }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="longitude">The longitude value.</param>
    /// <param name="latitude">The latitude value.</param>
    /// <returns>The computed value.</returns>
    internal bool Contains(double longitude, double latitude)
    {
        double lon = longitude;
        if (lon < this.West - this.Epsilon)
        {
            lon += 360d;
        }
        else if (lon > this.East + this.Epsilon)
        {
            lon -= 360d;
        }

        return lon >= this.West - this.Epsilon
            && lon <= this.East + this.Epsilon
            && latitude >= this.South - this.Epsilon
            && latitude <= this.North + this.Epsilon;
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="longitude">The longitude value.</param>
    /// <param name="latitude">The latitude value.</param>
    /// <param name="gridX">The gridX value.</param>
    /// <param name="gridY">The gridY value.</param>
    /// <returns>The computed value.</returns>
    internal bool TryMapToGridCoordinates(double longitude, double latitude, out double gridX, out double gridY)
    {
        if (this.TryMapRaw(longitude, latitude, out gridX, out gridY) && BaseGeoGrid.IsWithinGrid(gridX, gridY))
        {
            return true;
        }

        if (this.TryMapRaw(longitude + 360d, latitude, out gridX, out gridY) && BaseGeoGrid.IsWithinGrid(gridX, gridY))
        {
            return true;
        }

        if (this.TryMapRaw(longitude - 360d, latitude, out gridX, out gridY) && BaseGeoGrid.IsWithinGrid(gridX, gridY))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="sampleIndex">The sampleIndex value.</param>
    /// <param name="x">The x value.</param>
    /// <param name="y">The y value.</param>
    /// <returns>The computed value.</returns>
    internal double GetSampleValue(int sampleIndex, int x, int y)
    {
        return this.sampleData.GetValue(sampleIndex, x, y);
    }

    private static bool IsWithinGrid(double x, double y)
    {
        return x >= -1e-8d && y >= -1e-8d;
    }

    private bool TryMapRaw(double longitude, double latitude, out double x, out double y)
    {
        double localX = longitude - this.C;
        double localY = latitude - this.F;
        x = ((localX * this.E) - (this.B * localY)) / this.determinant;
        y = ((this.A * localY) - (localX * this.D)) / this.determinant;
        return !double.IsNaN(x) && !double.IsNaN(y) && !double.IsInfinity(x) && !double.IsInfinity(y);
    }
}
