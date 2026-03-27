// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ProjNet.CoordinateSystems;

/// <summary>
/// Applies geocentric XYZ grid-shift corrections loaded from GeoTIFF grids.
/// </summary>
[Serializable]
internal sealed class GeoTiffXyzGridShiftMathTransform : MathTransform
{
    private const double RelativeTolerance = 1e-5d;
    private const int MaxInverseIterations = 10;

    private readonly ReadOnlyCollection<XyzGrid> grids;
    private readonly GeocentricTransform geocentricInverse;
    private readonly double semiMajor;
    private readonly double semiMinor;
    private readonly double multiplier;
    private readonly bool gridReferenceIsInput;
    private bool isInverted;
    private MathTransform? inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeoTiffXyzGridShiftMathTransform"/> class.
    /// </summary>
    /// <param name="gridPaths">GeoTIFF XYZ grid paths.</param>
    /// <param name="semiMajor">Ellipsoid semi-major axis.</param>
    /// <param name="semiMinor">Ellipsoid semi-minor axis.</param>
    /// <param name="multiplier">Grid shift multiplier.</param>
    /// <param name="gridReferenceIsInput">True when <c>grid_ref=input_crs</c>.</param>
    internal GeoTiffXyzGridShiftMathTransform(
        IReadOnlyList<string> gridPaths,
        double semiMajor,
        double semiMinor,
        double multiplier,
        bool gridReferenceIsInput)
    {
#if NET8_0_OR_GREATER
        gridPaths = ArgumentGuard.ThrowIfNull(gridPaths);
#else
        gridPaths = ArgumentGuard.ThrowIfNull(gridPaths, nameof(gridPaths));
#endif

        if (semiMajor <= 0d || double.IsNaN(semiMajor) || double.IsInfinity(semiMajor))
        {
            ArgumentGuard.ThrowArgument("Semi-major axis must be a positive finite value.", nameof(semiMajor));
        }

        if (semiMinor <= 0d || double.IsNaN(semiMinor) || double.IsInfinity(semiMinor))
        {
            ArgumentGuard.ThrowArgument("Semi-minor axis must be a positive finite value.", nameof(semiMinor));
        }

        if (double.IsNaN(multiplier) || double.IsInfinity(multiplier))
        {
            ArgumentGuard.ThrowArgument("Multiplier must be finite.", nameof(multiplier));
        }

        var loadedGrids = new List<XyzGrid>(gridPaths.Count);
        for (int i = 0; i < gridPaths.Count; i++)
        {
            string path = gridPaths[i];
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            loadedGrids.AddRange(GeoTiffGridLoader.LoadXyz(path));
        }

        if (loadedGrids.Count == 0)
        {
            ArgumentGuard.ThrowArgument("No XYZ grid could be loaded from GeoTIFF input.", nameof(gridPaths));
        }

        this.grids = new ReadOnlyCollection<XyzGrid>(
            loadedGrids.OrderBy(grid => grid.Area, Comparer<double>.Default).ToArray());
        this.semiMajor = semiMajor;
        this.semiMinor = semiMinor;
        this.multiplier = multiplier;
        this.gridReferenceIsInput = gridReferenceIsInput;

        var parameters = new List<ProjectionParameter>
        {
            new ProjectionParameter("semi_major", semiMajor),
            new ProjectionParameter("semi_minor", semiMinor),
        };
        var geocentricForward = new GeocentricTransform(parameters, false);
        this.geocentricInverse = (GeocentricTransform)geocentricForward.Inverse();
    }

    private GeoTiffXyzGridShiftMathTransform(GeoTiffXyzGridShiftMathTransform source, bool isInverted)
    {
#if NET8_0_OR_GREATER
        source = ArgumentGuard.ThrowIfNull(source);
#else
        source = ArgumentGuard.ThrowIfNull(source, nameof(source));
#endif

        this.grids = source.grids;
        this.geocentricInverse = source.geocentricInverse;
        this.semiMajor = source.semiMajor;
        this.semiMinor = source.semiMinor;
        this.multiplier = source.multiplier;
        this.gridReferenceIsInput = source.gridReferenceIsInput;
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
    public override bool Identity() => false;

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new GeoTiffXyzGridShiftMathTransform(this, !this.isInverted);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    public override void Invert()
    {
        this.isInverted = !this.isInverted;
        this.inverse = null;
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        if (!this.isInverted)
        {
            if (this.gridReferenceIsInput)
            {
                this.ApplyDirect(ref x, ref y, ref z, 1d);
            }
            else
            {
                this.ApplyIterative(ref x, ref y, ref z, 1d);
            }

            return;
        }

        if (this.gridReferenceIsInput)
        {
            this.ApplyIterative(ref x, ref y, ref z, -1d);
        }
        else
        {
            this.ApplyDirect(ref x, ref y, ref z, -1d);
        }
    }

    private static void NormalizeInterpolationCell(int size, ref int index, ref double fraction)
    {
        if (index < 0)
        {
            if (index == -1 && fraction > 1d - (10d * RelativeTolerance))
            {
                index = 0;
                fraction = 0d;
                return;
            }

            ArgumentGuard.ThrowArgument("Coordinate is outside the XYZ GeoTIFF grid extent.");
        }

        if (index + 1 < size)
        {
            return;
        }

        if (index + 1 == size && fraction < 10d * RelativeTolerance)
        {
            index = size - 2;
            fraction = 1d;
            return;
        }

        ArgumentGuard.ThrowArgument("Coordinate is outside the XYZ GeoTIFF grid extent.");
    }

    private static bool IsConverged(double error)
    {
        return error < 1e-10d;
    }

    private static void InterpolateShift(XyzGrid grid, double longitude, double latitude, out double dx, out double dy, out double dz)
    {
        if (!grid.TryMapToGridCoordinates(longitude, latitude, out double gridX, out double gridY))
        {
            ArgumentGuard.ThrowArgument("Coordinate is outside the XYZ GeoTIFF grid extent.");
        }

        int indexX = (int)Math.Floor(gridX);
        int indexY = (int)Math.Floor(gridY);
        double fractionX = gridX - indexX;
        double fractionY = gridY - indexY;
        NormalizeInterpolationCell(grid.Width, ref indexX, ref fractionX);
        NormalizeInterpolationCell(grid.Height, ref indexY, ref fractionY);

        int indexX2 = indexX + 1;
        int indexY2 = indexY + 1;
        double xA = grid.GetXShift(indexX, indexY);
        double xB = grid.GetXShift(indexX2, indexY);
        double xC = grid.GetXShift(indexX, indexY2);
        double xD = grid.GetXShift(indexX2, indexY2);

        double yA = grid.GetYShift(indexX, indexY);
        double yB = grid.GetYShift(indexX2, indexY);
        double yC = grid.GetYShift(indexX, indexY2);
        double yD = grid.GetYShift(indexX2, indexY2);

        double zA = grid.GetZShift(indexX, indexY);
        double zB = grid.GetZShift(indexX2, indexY);
        double zC = grid.GetZShift(indexX, indexY2);
        double zD = grid.GetZShift(indexX2, indexY2);

        double xy = fractionX * fractionY;
        double wA = 1d - fractionX - fractionY + xy;
        double wB = fractionX - xy;
        double wC = fractionY - xy;
        double wD = xy;

        dx = (xA * wA) + (xB * wB) + (xC * wC) + (xD * wD);
        dy = (yA * wA) + (yB * wB) + (yC * wC) + (yD * wD);
        dz = (zA * wA) + (zB * wB) + (zC * wC) + (zD * wD);
    }

    private void ApplyDirect(ref double x, ref double y, ref double z, double factor)
    {
        if (!this.TryGetShift(x, y, z, out double dx, out double dy, out double dz))
        {
            ArgumentGuard.ThrowArgument("Coordinate is outside the XYZ GeoTIFF grid extent.");
        }

        x += factor * dx;
        y += factor * dy;
        z += factor * dz;
    }

    private void ApplyIterative(ref double x, ref double y, ref double z, double factor)
    {
        double targetX = x;
        double targetY = y;
        double targetZ = z;
        double candidateX = x;
        double candidateY = y;
        double candidateZ = z;

        bool converged = false;
        for (int i = 0; i < MaxInverseIterations; i++)
        {
            if (!this.TryGetShift(candidateX, candidateY, candidateZ, out double dx, out double dy, out double dz))
            {
                ArgumentGuard.ThrowArgument("Coordinate is outside the XYZ GeoTIFF grid extent.");
            }

            dx *= factor;
            dy *= factor;
            dz *= factor;

            double error = ((candidateX - targetX - dx) * (candidateX - targetX - dx))
                + ((candidateY - targetY - dy) * (candidateY - targetY - dy))
                + ((candidateZ - targetZ - dz) * (candidateZ - targetZ - dz));

            candidateX = targetX + dx;
            candidateY = targetY + dy;
            candidateZ = targetZ + dz;

            if (IsConverged(error))
            {
                converged = true;
                break;
            }
        }

        if (!converged)
        {
            ArgumentGuard.ThrowArgument("Inverse XYZ GeoTIFF grid shift did not converge.");
        }

        x = candidateX;
        y = candidateY;
        z = candidateZ;
    }

    private bool TryFindGridForPoint(double longitude, double latitude, [NotNullWhen(true)] out XyzGrid? grid)
    {
        for (int i = 0; i < this.grids.Count; i++)
        {
            if (this.grids[i].Contains(longitude, latitude))
            {
                grid = this.grids[i];
                return true;
            }
        }

        grid = null;
        return false;
    }

    private bool TryGetShift(double x, double y, double z, out double dx, out double dy, out double dz)
    {
        dx = 0d;
        dy = 0d;
        dz = 0d;

        double lon = x;
        double lat = y;
        double h = z;
        this.geocentricInverse.Transform(ref lon, ref lat, ref h);

        if (!this.TryFindGridForPoint(lon, lat, out XyzGrid? grid))
        {
            return false;
        }

        grid = ArgumentGuard.ThrowIfNull(grid, nameof(grid));
        GeoTiffXyzGridShiftMathTransform.InterpolateShift(grid, lon, lat, out dx, out dy, out dz);
        dx *= this.multiplier;
        dy *= this.multiplier;
        dz *= this.multiplier;
        return true;
    }

    /// <summary>
    /// Represents an XYZ shift page loaded from a GeoTIFF.
    /// </summary>
    [Serializable]
    internal sealed class XyzGrid : BaseGeoGrid
    {
        private readonly int sampleX;
        private readonly int sampleY;
        private readonly int sampleZ;

        /// <summary>
        /// Initializes a new instance of the <see cref="XyzGrid"/> class.
        /// </summary>
        /// <param name="sourcePath">Source grid file path.</param>
        /// <param name="width">Grid width.</param>
        /// <param name="height">Grid height.</param>
        /// <param name="area">Grid area.</param>
        /// <param name="epsilon">Grid epsilon.</param>
        /// <param name="west">Grid western bound.</param>
        /// <param name="east">Grid eastern bound.</param>
        /// <param name="south">Grid southern bound.</param>
        /// <param name="north">Grid northern bound.</param>
        /// <param name="a">GeoTransform A.</param>
        /// <param name="b">GeoTransform B.</param>
        /// <param name="c">GeoTransform C.</param>
        /// <param name="d">GeoTransform D.</param>
        /// <param name="e">GeoTransform E.</param>
        /// <param name="f">GeoTransform F.</param>
        /// <param name="sampleData">Decoded sample data.</param>
        /// <param name="sampleX">X-shift sample index.</param>
        /// <param name="sampleY">Y-shift sample index.</param>
        /// <param name="sampleZ">Z-shift sample index.</param>
        internal XyzGrid(
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
            SampleData sampleData,
            int sampleX,
            int sampleY,
            int sampleZ)
            : base(sourcePath, width, height, area, epsilon, west, east, south, north, a, b, c, d, e, f, sampleData)
        {
            this.sampleX = sampleX;
            this.sampleY = sampleY;
            this.sampleZ = sampleZ;
        }

        /// <summary>
        /// Gets interpolated X-shift source sample value.
        /// </summary>
        /// <param name="x">Horizontal sample index.</param>
        /// <param name="y">Vertical sample index.</param>
        /// <returns>The interpolated X-shift sample value.</returns>
        internal double GetXShift(int x, int y) => this.GetSampleValue(this.sampleX, x, y);

        /// <summary>
        /// Gets interpolated Y-shift source sample value.
        /// </summary>
        /// <param name="x">Horizontal sample index.</param>
        /// <param name="y">Vertical sample index.</param>
        /// <returns>The interpolated Y-shift sample value.</returns>
        internal double GetYShift(int x, int y) => this.GetSampleValue(this.sampleY, x, y);

        /// <summary>
        /// Gets interpolated Z-shift source sample value.
        /// </summary>
        /// <param name="x">Horizontal sample index.</param>
        /// <param name="y">Vertical sample index.</param>
        /// <returns>The interpolated Z-shift sample value.</returns>
        internal double GetZShift(int x, int y) => this.GetSampleValue(this.sampleZ, x, y);
    }
}
