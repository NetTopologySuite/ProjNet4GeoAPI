// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// Abstract base class for a geo-referenced raster grid that maps geographic coordinates
/// to grid pixel coordinates using an affine transformation.
/// </summary>
internal abstract class BaseGeoGrid
{
    private readonly SampleData sampleData;
    private readonly double determinant;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseGeoGrid"/> class.
    /// </summary>
    /// <param name="sourcePath">Path of the source file the grid was loaded from.</param>
    /// <param name="width">Number of grid columns.</param>
    /// <param name="height">Number of grid rows.</param>
    /// <param name="area">Geographic coverage area used to order grids by specificity.</param>
    /// <param name="epsilon">Tolerance used for geographic boundary checks in degrees.</param>
    /// <param name="west">Western boundary of the grid in degrees.</param>
    /// <param name="east">Eastern boundary of the grid in degrees.</param>
    /// <param name="south">Southern boundary of the grid in degrees.</param>
    /// <param name="north">Northern boundary of the grid in degrees.</param>
    /// <param name="a">Affine coefficient: longitude change per grid column.</param>
    /// <param name="b">Affine coefficient: longitude change per grid row.</param>
    /// <param name="c">Affine coefficient: longitude of the grid origin.</param>
    /// <param name="d">Affine coefficient: latitude change per grid column.</param>
    /// <param name="e">Affine coefficient: latitude change per grid row.</param>
    /// <param name="f">Affine coefficient: latitude of the grid origin.</param>
    /// <param name="sampleData">The raster sample data store for this grid.</param>
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
    /// Gets the path of the source file from which this grid was loaded.
    /// </summary>
    internal string SourcePath { get; }

    /// <summary>
    /// Gets the number of grid columns.
    /// </summary>
    internal int Width { get; }

    /// <summary>
    /// Gets the number of grid rows.
    /// </summary>
    internal int Height { get; }

    /// <summary>
    /// Gets the geographic coverage area, used to order grids by specificity.
    /// </summary>
    internal double Area { get; }

    /// <summary>
    /// Gets the tolerance in degrees used for geographic boundary checks.
    /// </summary>
    internal double Epsilon { get; }

    /// <summary>
    /// Gets the western geographic boundary of the grid in degrees.
    /// </summary>
    internal double West { get; }

    /// <summary>
    /// Gets the eastern geographic boundary of the grid in degrees.
    /// </summary>
    internal double East { get; }

    /// <summary>
    /// Gets the southern geographic boundary of the grid in degrees.
    /// </summary>
    internal double South { get; }

    /// <summary>
    /// Gets the northern geographic boundary of the grid in degrees.
    /// </summary>
    internal double North { get; }

    /// <summary>
    /// Gets the affine coefficient representing longitude change per grid column.
    /// </summary>
    internal double A { get; }

    /// <summary>
    /// Gets the affine coefficient representing longitude change per grid row.
    /// </summary>
    internal double B { get; }

    /// <summary>
    /// Gets the affine coefficient representing the longitude of the grid origin.
    /// </summary>
    internal double C { get; }

    /// <summary>
    /// Gets the affine coefficient representing latitude change per grid column.
    /// </summary>
    internal double D { get; }

    /// <summary>
    /// Gets the affine coefficient representing latitude change per grid row.
    /// </summary>
    internal double E { get; }

    /// <summary>
    /// Gets the affine coefficient representing the latitude of the grid origin.
    /// </summary>
    internal double F { get; }

    /// <summary>
    /// Determines whether the specified geographic coordinate falls within the extent of this grid.
    /// </summary>
    /// <param name="longitude">Longitude in degrees.</param>
    /// <param name="latitude">Latitude in degrees.</param>
    /// <returns><see langword="true"/> when the coordinate is within the grid extent; otherwise <see langword="false"/>.</returns>
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
    /// Attempts to map a geographic coordinate to fractional grid pixel coordinates.
    /// </summary>
    /// <param name="longitude">Longitude in degrees.</param>
    /// <param name="latitude">Latitude in degrees.</param>
    /// <param name="gridX">Fractional column index in grid space on success.</param>
    /// <param name="gridY">Fractional row index in grid space on success.</param>
    /// <returns><see langword="true"/> when the mapping succeeds; otherwise <see langword="false"/>.</returns>
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

        return this.TryMapRaw(longitude - 360d, latitude, out gridX, out gridY) && BaseGeoGrid.IsWithinGrid(gridX, gridY);
    }

    /// <summary>
    /// Gets the raw sample value at the specified grid cell for the given sample band index.
    /// </summary>
    /// <param name="sampleIndex">Zero-based index of the sample band.</param>
    /// <param name="x">Column index.</param>
    /// <param name="y">Row index.</param>
    /// <returns>The raw sample value stored at position (<paramref name="x"/>, <paramref name="y"/>) in band <paramref name="sampleIndex"/>.</returns>
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
