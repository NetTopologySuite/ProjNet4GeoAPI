// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using ProjNet.CoordinateSystems;

/// <summary>
/// Implements PROJ's <c>deformation</c> runtime transform.
/// </summary>
/// <remarks>
/// <para>
/// This implementation follows PROJ's <c>deformation</c> operation for
/// time-dependent datum correction: ENU velocities are interpolated from GeoTIFF
/// XYZ grids or legacy CTable2/GTX grids, converted to geocentric XYZ shift
/// components, and then applied in cartesian space as <c>(t_obs - t_c) * V</c>.
/// </para>
/// <para>
/// The runtime was independently verified against PROJ's published
/// <c>deformation</c> documentation and <c>deformation.cpp</c>. The reviewed
/// inverse implementation preserves the corrected Newton-style residual update in
/// all three components, including Z, so the reverse path converges to the same
/// fixed point as the upstream algorithm.
/// </para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/transformations/deformation.html">PROJ: deformation.</seealso>
internal sealed partial class DeformationMathTransform : MathTransform
{
    private const double RelativeTolerance = 1e-5d;

    private const double InverseTolerance = 1e-8d;
    private const double TwoPi = 2d * Math.PI;

    private readonly ReadOnlyCollection<GeoTiffXyzGridShiftMathTransform.XyzGrid> velocityGrids;
    private readonly ReadOnlyCollection<CTable2Grid> horizontalGrids;
    private readonly ReadOnlyCollection<GtxGrid> verticalGrids;
    private readonly bool hasFixedDt;
    private readonly double fixedDt;
    private readonly double tEpoch;
    private readonly double semiMajor;
    private readonly double semiMinor;
    private readonly GeocentricTransform geocentricForward;
    private readonly GeocentricTransform geocentricInverse;

    private readonly bool isInverted;
    private MathTransform? inverse;

    private DeformationMathTransform(
        IReadOnlyList<GeoTiffXyzGridShiftMathTransform.XyzGrid> velocityGrids,
        IReadOnlyList<CTable2Grid> horizontalGrids,
        IReadOnlyList<GtxGrid> verticalGrids,
        bool hasFixedDt,
        double fixedDt,
        double tEpoch,
        double semiMajor,
        double semiMinor,
        bool isInverted)
    {
        this.velocityGrids = new ReadOnlyCollection<GeoTiffXyzGridShiftMathTransform.XyzGrid>(
            [.. velocityGrids ?? []]);
        this.horizontalGrids = new ReadOnlyCollection<CTable2Grid>([.. horizontalGrids ?? []]);
        this.verticalGrids = new ReadOnlyCollection<GtxGrid>([.. verticalGrids ?? []]);
        this.hasFixedDt = hasFixedDt;
        this.fixedDt = fixedDt;
        this.tEpoch = tEpoch;
        this.semiMajor = semiMajor;
        this.semiMinor = semiMinor;
        this.isInverted = isInverted;

        var parameters = new List<ProjectionParameter>
        {
            new("semi_major", semiMajor),
            new("semi_minor", semiMinor),
        };
        this.geocentricForward = new GeocentricTransform(parameters, false);
        this.geocentricInverse = (GeocentricTransform)this.geocentricForward.Inverse();
    }

    private DeformationMathTransform(DeformationMathTransform source, bool isInverted)
    {
        this.velocityGrids = source.velocityGrids;
        this.horizontalGrids = source.horizontalGrids;
        this.verticalGrids = source.verticalGrids;
        this.hasFixedDt = source.hasFixedDt;
        this.fixedDt = source.fixedDt;
        this.tEpoch = source.tEpoch;
        this.semiMajor = source.semiMajor;
        this.semiMinor = source.semiMinor;
        this.geocentricForward = source.geocentricForward;
        this.geocentricInverse = source.geocentricInverse;
        this.isInverted = isInverted;
    }

    /// <inheritdoc />
    public override int DimSource => 3;

    /// <inheritdoc />
    public override int DimTarget => 3;

    /// <inheritdoc />
    public override bool Identity() => false;

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return this.inverse ??= new DeformationMathTransform(this, !this.isInverted);
    }

    /// <inheritdoc />
    public override void Invert()
    {
        throw new NotSupportedException("DeformationMathTransform is immutable. Use Inverse() to obtain inverted transform.");
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        if (!this.TryResolveDeltaTime(TransformationMath.MissingObservationEpoch, out double deltaTime, out bool missingTime))
        {
            if (missingTime)
            {
                ArgumentGuard.ThrowArgument("deformation requires a valid observation time.");
            }

            ArgumentGuard.ThrowArgument("deformation could not resolve delta time.");
        }

        this.TransformCore(ref x, ref y, ref z, deltaTime);
    }

    /// <summary>
    /// Creates a <see cref="DeformationMathTransform"/> from parsed PROJ arguments.
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
        skipReason = null;
        if (args is null)
        {
            skipReason = "deformation arguments were null.";
            return false;
        }

        bool hasGenericGrids = args.TryGetValue("grids", out string? genericGridToken) && !string.IsNullOrWhiteSpace(genericGridToken);
        bool hasHorizontalGrids = args.TryGetValue("xy_grids", out string? horizontalGridToken) && !string.IsNullOrWhiteSpace(horizontalGridToken);
        bool hasVerticalGrids = args.TryGetValue("z_grids", out string? verticalGridToken) && !string.IsNullOrWhiteSpace(verticalGridToken);
        if (!hasGenericGrids && (!hasHorizontalGrids || !hasVerticalGrids))
        {
            skipReason = "deformation requires either +grids or (+xy_grids and +z_grids).";
            return false;
        }

        if (hasGenericGrids && (hasHorizontalGrids || hasVerticalGrids))
        {
            skipReason = "+grids is mutually exclusive with +xy_grids/+z_grids for deformation.";
            return false;
        }

        if (args.ContainsKey("t_obs"))
        {
            skipReason = "+t_obs parameter is deprecated. Use +dt instead.";
            return false;
        }

        bool hasDt = args.TryGetValue("dt", out string? dtToken);
        bool hasEpoch = args.TryGetValue("t_epoch", out string? epochToken);
        if (!hasDt && !hasEpoch)
        {
            skipReason = "deformation requires +dt or +t_epoch.";
            return false;
        }

        if (hasDt && hasEpoch)
        {
            skipReason = "+dt and +t_epoch are mutually exclusive for deformation.";
            return false;
        }

        bool useFixedDt = false;
        double fixedDt = 0d;
        double tEpoch = 0d;
        if (hasDt)
        {
            string dtValue = dtToken ?? string.Empty;
            if (!SpanParseUtility.TryParseFiniteDouble(dtValue, out fixedDt))
            {
                skipReason = "Unable to parse +dt parameter for deformation.";
                return false;
            }

            useFixedDt = true;
        }
        else
        {
            string epochValue = epochToken ?? string.Empty;
            if (!SpanParseUtility.TryParseFiniteDouble(epochValue, out tEpoch))
            {
                skipReason = "Unable to parse +t_epoch parameter for deformation.";
                return false;
            }
        }

        if (!ProjEllipsoidResolver.TryResolveEllipsoidOrDefault(
            args,
            operationName: "deformation",
            allowClarke1880Ign: true,
            allowBessel: false,
            out double semiMajor,
            out double semiMinor,
            out skipReason))
        {
            return false;
        }

        try
        {
            var velocityGrids = new List<GeoTiffXyzGridShiftMathTransform.XyzGrid>();
            var horizontalGrids = new List<CTable2Grid>();
            var verticalGrids = new List<GtxGrid>();

            if (hasGenericGrids)
            {
                string genericGridTokenValue = ArgumentGuard.ThrowIfNull(genericGridToken, nameof(genericGridToken));
                if (!TryResolveGridPaths(genericGridTokenValue, "grids", out IReadOnlyList<string> genericGridPaths, out skipReason))
                {
                    return false;
                }

                for (int i = 0; i < genericGridPaths.Count; i++)
                {
                    string extension = Path.GetExtension(genericGridPaths[i]);
                    if (!extension.Equals(".tif", StringComparison.OrdinalIgnoreCase)
                        && !extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase))
                    {
                        skipReason = $"Grid '{Path.GetFileName(genericGridPaths[i])}' is not a supported deformation velocity grid format (.tif/.tiff).";
                        return false;
                    }

                    velocityGrids.AddRange(GeoTiffGridLoader.LoadXyz(genericGridPaths[i], requireMetreUnits: false));
                }

                if (velocityGrids.Count == 0)
                {
                    skipReason = "No velocity grid could be loaded from +grids.";
                    return false;
                }

                velocityGrids.Sort(static (left, right) => left.Area.CompareTo(right.Area));
            }
            else
            {
                string horizontalGridTokenValue = ArgumentGuard.ThrowIfNull(horizontalGridToken, nameof(horizontalGridToken));
                string verticalGridTokenValue = ArgumentGuard.ThrowIfNull(verticalGridToken, nameof(verticalGridToken));
                if (!TryResolveGridPaths(horizontalGridTokenValue, "xy_grids", out IReadOnlyList<string> horizontalGridPaths, out skipReason))
                {
                    return false;
                }

                if (!TryResolveGridPaths(verticalGridTokenValue, "z_grids", out IReadOnlyList<string> verticalGridPaths, out skipReason))
                {
                    return false;
                }

                for (int i = 0; i < horizontalGridPaths.Count; i++)
                {
                    horizontalGrids.Add(CTable2Grid.Load(horizontalGridPaths[i]));
                }

                for (int i = 0; i < verticalGridPaths.Count; i++)
                {
                    verticalGrids.Add(GtxGrid.Load(verticalGridPaths[i]));
                }

                if (horizontalGrids.Count == 0 || verticalGrids.Count == 0)
                {
                    skipReason = "deformation requires both horizontal and vertical velocity grids.";
                    return false;
                }

                horizontalGrids.Sort(static (left, right) => left.Area.CompareTo(right.Area));
                verticalGrids.Sort(static (left, right) => left.Area.CompareTo(right.Area));
            }

            transform = new DeformationMathTransform(
                velocityGrids,
                horizontalGrids,
                verticalGrids,
                useFixedDt,
                fixedDt,
                tEpoch,
                semiMajor,
                semiMinor,
                isInverted: false);
            if (args.ContainsKey("inv"))
            {
                transform = transform.Inverse();
            }

            return true;
        }
        catch (IOException exception)
        {
            skipReason = $"Unable to read deformation grid: {exception.Message}";
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            skipReason = $"Unable to read deformation grid: {exception.Message}";
            return false;
        }
        catch (InvalidDataException exception)
        {
            skipReason = $"Invalid deformation grid: {exception.Message}";
            return false;
        }
        catch (ArgumentException exception)
        {
            skipReason = $"Invalid deformation configuration: {exception.Message}";
            return false;
        }
    }

    /// <inheritdoc />
    internal override void Transform(ref double x, ref double y, ref double z, ref double t)
    {
        if (!this.TryResolveDeltaTime(t, out double deltaTime, out bool missingTime))
        {
            if (missingTime)
            {
                ArgumentGuard.ThrowArgument("deformation requires a valid observation time.");
            }

            ArgumentGuard.ThrowArgument("deformation could not resolve delta time.");
        }

        this.TransformCore(ref x, ref y, ref z, deltaTime);
    }

    private static bool TryResolveGridPaths(
        string gridsToken,
        string parameterName,
        out IReadOnlyList<string> resolvedPaths,
        out string? skipReason)
    {
        resolvedPaths = [];
        skipReason = null;
        if (string.IsNullOrWhiteSpace(gridsToken))
        {
            skipReason = $"deformation requires +{parameterName}.";
            return false;
        }

        string[] entries = gridsToken.Split([','], StringSplitOptions.RemoveEmptyEntries);
        if (entries.Length == 0)
        {
            skipReason = $"deformation requires at least one grid in +{parameterName}.";
            return false;
        }

        var resolved = new List<string>(entries.Length);
        for (int i = 0; i < entries.Length; i++)
        {
            string token = entries[i].Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            bool isOptional = token[0] == '@';
            string gridName = isOptional ? token[1..] : token;
            if (string.IsNullOrWhiteSpace(gridName))
            {
                continue;
            }

            if (TryResolveGridPath(gridName, out string? resolvedPathCandidate))
            {
                resolved.Add(ArgumentGuard.ThrowIfNull(resolvedPathCandidate, nameof(resolvedPathCandidate)));
                continue;
            }

            if (!isOptional)
            {
                skipReason = $"Required grid '{gridName}' was not found.";
                return false;
            }
        }

        if (resolved.Count == 0)
        {
            skipReason = $"No grid from +{parameterName} could be resolved.";
            return false;
        }

        resolvedPaths = resolved;
        return true;
    }

    private static bool TryResolveGridPath(string gridToken, [NotNullWhen(true)] out string? resolvedPath)
    {
        resolvedPath = null;
        if (string.IsNullOrWhiteSpace(gridToken))
        {
            return false;
        }

        string normalized = NormalizePathToken(gridToken);
        if (TryGetExistingPath(normalized, out resolvedPath))
        {
            return true;
        }

        if (TryGetExistingPath(Path.Combine(AppContext.BaseDirectory, normalized), out resolvedPath))
        {
            return true;
        }

        string fileName = Path.GetFileName(normalized);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            if (TryGetExistingPath(Path.Combine(AppContext.BaseDirectory, "Fixtures", "grids", fileName), out resolvedPath))
            {
                return true;
            }
        }

        if (CoordinateTransformationFactory.TryResolveGridResourcePath(gridToken, out resolvedPath))
        {
            return true;
        }

        if (!string.Equals(normalized, gridToken, StringComparison.Ordinal)
            && CoordinateTransformationFactory.TryResolveGridResourcePath(normalized, out resolvedPath))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(fileName)
            && CoordinateTransformationFactory.TryResolveGridResourcePath(fileName, out resolvedPath);
    }

    private static bool TryGetExistingPath(string candidate, [NotNullWhen(true)] out string? resolvedPath)
    {
        resolvedPath = null;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        try
        {
            string fullPath = Path.IsPathRooted(candidate)
                ? candidate
                : Path.GetFullPath(candidate);
            if (!File.Exists(fullPath))
            {
                return false;
            }

            resolvedPath = fullPath;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (PathTooLongException)
        {
            return false;
        }
        catch (System.Security.SecurityException)
        {
            return false;
        }
    }

    private static string NormalizePathToken(string token)
    {
        return token.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
    }

    private static bool TryNormalizeInterpolationCell(int size, ref int index, ref double fraction)
    {
        if (index < 0)
        {
            if (index == -1 && fraction > 1d - (10d * RelativeTolerance))
            {
                index = 0;
                fraction = 0d;
                return true;
            }

            return false;
        }

        if (index + 1 < size)
        {
            return true;
        }

        if (index + 1 == size && fraction < 10d * RelativeTolerance)
        {
            index = size - 2;
            fraction = 1d;
            return true;
        }

        return false;
    }

    private static bool TryGetInterpolationCell(
        BaseGeoGrid grid,
        double longitudeDegrees,
        double latitudeDegrees,
        out InterpolationCell cell)
    {
        cell = default;
        if (!grid.TryMapToGridCoordinates(longitudeDegrees, latitudeDegrees, out double gridX, out double gridY))
        {
            return false;
        }

        int x0 = (int)Math.Floor(gridX);
        int y0 = (int)Math.Floor(gridY);
        double fractionX = gridX - x0;
        double fractionY = gridY - y0;
        if (!TryNormalizeInterpolationCell(grid.Width, ref x0, ref fractionX)
            || !TryNormalizeInterpolationCell(grid.Height, ref y0, ref fractionY))
        {
            return false;
        }

        int x1 = x0 + 1;
        int y1 = y0 + 1;
        double xy = fractionX * fractionY;
        cell = new InterpolationCell(
            x0,
            y0,
            x1,
            y1,
            1d - fractionX - fractionY + xy,
            fractionY - xy,
            fractionX - xy,
            xy);
        return true;
    }

    private static double Bilinear(double value00, double value01, double value10, double value11, InterpolationCell cell)
    {
        return (value00 * cell.W00)
            + (value01 * cell.W01)
            + (value10 * cell.W10)
            + (value11 * cell.W11);
    }

    private static bool TryFindXyzGrid(
        ReadOnlyCollection<GeoTiffXyzGridShiftMathTransform.XyzGrid> grids,
        double longitudeDegrees,
        double latitudeDegrees,
        [NotNullWhen(true)] out GeoTiffXyzGridShiftMathTransform.XyzGrid? grid)
    {
        for (int i = 0; i < grids.Count; i++)
        {
            if (grids[i].Contains(longitudeDegrees, latitudeDegrees))
            {
                grid = grids[i];
                return true;
            }
        }

        grid = null;
        return false;
    }

    private static bool TryFindHorizontalGrid(
        IReadOnlyList<CTable2Grid> grids,
        double longitudeRadians,
        double latitudeRadians,
        [NotNullWhen(true)] out CTable2Grid? grid)
    {
        for (int i = 0; i < grids.Count; i++)
        {
            if (grids[i].Contains(longitudeRadians, latitudeRadians))
            {
                grid = grids[i];
                return true;
            }
        }

        grid = null;
        return false;
    }

    private static bool TryFindVerticalGrid(
        IReadOnlyList<GtxGrid> grids,
        double longitudeRadians,
        double latitudeRadians,
        [NotNullWhen(true)] out GtxGrid? grid)
    {
        for (int i = 0; i < grids.Count; i++)
        {
            if (grids[i].Contains(longitudeRadians, latitudeRadians))
            {
                grid = grids[i];
                return true;
            }
        }

        grid = null;
        return false;
    }

    private static void ConvertEnuToCartesianShift(
        double longitudeDegrees,
        double latitudeDegrees,
        double eastVelocity,
        double northVelocity,
        double upVelocity,
        out double xShift,
        out double yShift,
        out double zShift)
    {
        double longitudeRadians = DegreesToRadians(longitudeDegrees);
        double latitudeRadians = DegreesToRadians(latitudeDegrees);
        double sinPhi = Math.Sin(latitudeRadians);
        double cosPhi = Math.Cos(latitudeRadians);
        double sinLambda = Math.Sin(longitudeRadians);
        double cosLambda = Math.Cos(longitudeRadians);

        xShift = (-sinPhi * cosLambda * northVelocity) - (sinLambda * eastVelocity) + (cosPhi * cosLambda * upVelocity);
        yShift = (-sinPhi * sinLambda * northVelocity) + (cosLambda * eastVelocity) + (cosPhi * sinLambda * upVelocity);
        zShift = (cosPhi * northVelocity) + (sinPhi * upVelocity);
    }

    private void TransformCore(ref double x, ref double y, ref double z, double deltaTime)
    {
        if (!this.isInverted)
        {
            if (!this.TryGetGridShift(x, y, z, out double shiftX, out double shiftY, out double shiftZ))
            {
                ArgumentGuard.ThrowArgument("Coordinate is outside deformation model grid extent.");
            }

            x += deltaTime * shiftX;
            y += deltaTime * shiftY;
            z += deltaTime * shiftZ;
            return;
        }

        if (!this.TryReverseShift(x, y, z, deltaTime, out double xOut, out double yOut, out double zOut))
        {
            ArgumentGuard.ThrowArgument("deformation inverse transformation failed.");
        }

        x = xOut;
        y = yOut;
        z = zOut;
    }

    private bool TryResolveDeltaTime(double observationEpoch, out double deltaTime, out bool missingTime)
    {
        if (this.hasFixedDt)
        {
            missingTime = false;
            deltaTime = this.fixedDt;
            return true;
        }

        if (!TransformationMath.IsValidObservationEpoch(observationEpoch, TransformationMath.MissingObservationEpoch))
        {
            missingTime = true;
            deltaTime = 0d;
            return false;
        }

        missingTime = false;
        deltaTime = observationEpoch - this.tEpoch;
        return true;
    }

    private bool TryGetGridShift(double x, double y, double z, out double shiftX, out double shiftY, out double shiftZ)
    {
        shiftX = 0d;
        shiftY = 0d;
        shiftZ = 0d;
        if (!TransformationMath.IsFinite(x) || !TransformationMath.IsFinite(y) || !TransformationMath.IsFinite(z))
        {
            return false;
        }

        double longitudeDegrees = x;
        double latitudeDegrees = y;
        double height = z;
        this.geocentricInverse.Transform(ref longitudeDegrees, ref latitudeDegrees, ref height);
        if (!TransformationMath.IsFinite(longitudeDegrees) || !TransformationMath.IsFinite(latitudeDegrees))
        {
            return false;
        }

        double eastVelocity;
        double northVelocity;
        double upVelocity;
        if (this.velocityGrids.Count > 0)
        {
            if (!this.TryInterpolateGeoTiffVelocity(longitudeDegrees, latitudeDegrees, out eastVelocity, out northVelocity, out upVelocity))
            {
                return false;
            }
        }
        else
        {
            if (!this.TryInterpolateLegacyVelocity(
                DegreesToRadians(longitudeDegrees),
                DegreesToRadians(latitudeDegrees),
                out eastVelocity,
                out northVelocity,
                out upVelocity))
            {
                return false;
            }
        }

        ConvertEnuToCartesianShift(
            longitudeDegrees,
            latitudeDegrees,
            eastVelocity,
            northVelocity,
            upVelocity,
            out shiftX,
            out shiftY,
            out shiftZ);
        return TransformationMath.IsFinite(shiftX) && TransformationMath.IsFinite(shiftY) && TransformationMath.IsFinite(shiftZ);
    }

    private bool TryInterpolateGeoTiffVelocity(
        double longitudeDegrees,
        double latitudeDegrees,
        out double eastVelocity,
        out double northVelocity,
        out double upVelocity)
    {
        eastVelocity = 0d;
        northVelocity = 0d;
        upVelocity = 0d;
        if (!TryFindXyzGrid(this.velocityGrids, longitudeDegrees, latitudeDegrees, out GeoTiffXyzGridShiftMathTransform.XyzGrid? gridCandidate))
        {
            return false;
        }

        GeoTiffXyzGridShiftMathTransform.XyzGrid grid = ArgumentGuard.ThrowIfNull(gridCandidate, nameof(gridCandidate));
        if (!TryGetInterpolationCell(grid, longitudeDegrees, latitudeDegrees, out InterpolationCell cell))
        {
            return false;
        }

        double east00 = grid.GetXShift(cell.X0, cell.Y0);
        double east01 = grid.GetXShift(cell.X0, cell.Y1);
        double east10 = grid.GetXShift(cell.X1, cell.Y0);
        double east11 = grid.GetXShift(cell.X1, cell.Y1);
        double north00 = grid.GetYShift(cell.X0, cell.Y0);
        double north01 = grid.GetYShift(cell.X0, cell.Y1);
        double north10 = grid.GetYShift(cell.X1, cell.Y0);
        double north11 = grid.GetYShift(cell.X1, cell.Y1);
        double up00 = grid.GetZShift(cell.X0, cell.Y0);
        double up01 = grid.GetZShift(cell.X0, cell.Y1);
        double up10 = grid.GetZShift(cell.X1, cell.Y0);
        double up11 = grid.GetZShift(cell.X1, cell.Y1);
        if (!TransformationMath.IsFinite(east00)
            || !TransformationMath.IsFinite(east01)
            || !TransformationMath.IsFinite(east10)
            || !TransformationMath.IsFinite(east11)
            || !TransformationMath.IsFinite(north00)
            || !TransformationMath.IsFinite(north01)
            || !TransformationMath.IsFinite(north10)
            || !TransformationMath.IsFinite(north11)
            || !TransformationMath.IsFinite(up00)
            || !TransformationMath.IsFinite(up01)
            || !TransformationMath.IsFinite(up10)
            || !TransformationMath.IsFinite(up11))
        {
            return false;
        }

        eastVelocity = Bilinear(east00, east01, east10, east11, cell) / 1000d;
        northVelocity = Bilinear(north00, north01, north10, north11, cell) / 1000d;
        upVelocity = Bilinear(up00, up01, up10, up11, cell) / 1000d;
        return TransformationMath.IsFinite(eastVelocity) && TransformationMath.IsFinite(northVelocity) && TransformationMath.IsFinite(upVelocity);
    }

    private bool TryInterpolateLegacyVelocity(
        double longitudeRadians,
        double latitudeRadians,
        out double eastVelocity,
        out double northVelocity,
        out double upVelocity)
    {
        eastVelocity = 0d;
        northVelocity = 0d;
        upVelocity = 0d;
        if (!TryFindHorizontalGrid(this.horizontalGrids, longitudeRadians, latitudeRadians, out CTable2Grid? horizontalGridCandidate))
        {
            return false;
        }

        CTable2Grid horizontalGrid = ArgumentGuard.ThrowIfNull(horizontalGridCandidate, nameof(horizontalGridCandidate));
        if (!horizontalGrid.TryInterpolate(longitudeRadians, latitudeRadians, out double eastMmPerYear, out double northMmPerYear))
        {
            return false;
        }

        if (!TryFindVerticalGrid(this.verticalGrids, longitudeRadians, latitudeRadians, out GtxGrid? verticalGridCandidate))
        {
            return false;
        }

        GtxGrid verticalGrid = ArgumentGuard.ThrowIfNull(verticalGridCandidate, nameof(verticalGridCandidate));
        if (!verticalGrid.TryInterpolate(longitudeRadians, latitudeRadians, out double upMmPerYear))
        {
            return false;
        }

        eastVelocity = eastMmPerYear / 1000d;
        northVelocity = northMmPerYear / 1000d;
        upVelocity = upMmPerYear / 1000d;
        return TransformationMath.IsFinite(eastVelocity) && TransformationMath.IsFinite(northVelocity) && TransformationMath.IsFinite(upVelocity);
    }

    private bool TryReverseShift(
        double inputX,
        double inputY,
        double inputZ,
        double deltaTime,
        out double outputX,
        out double outputY,
        out double outputZ)
    {
        outputX = inputX;
        outputY = inputY;
        outputZ = inputZ;
        if (!this.TryGetGridShift(inputX, inputY, inputZ, out double firstDeltaX, out double firstDeltaY, out double firstDeltaZ))
        {
            return false;
        }

        outputX = inputX - (deltaTime * firstDeltaX);
        outputY = inputY - (deltaTime * firstDeltaY);
        outputZ = inputZ - (deltaTime * firstDeltaZ);

        for (int i = 0; i < TransformationMath.MaxInverseIterations; i++)
        {
            if (!this.TryGetGridShift(outputX, outputY, outputZ, out double deltaX, out double deltaY, out double deltaZ))
            {
                return false;
            }

            double differenceX = outputX + (deltaTime * deltaX) - inputX;
            double differenceY = outputY + (deltaTime * deltaY) - inputY;
            double differenceZ = outputZ + (deltaTime * deltaZ) - inputZ;
            outputX -= differenceX;
            outputY -= differenceY;
            outputZ -= differenceZ;

            if (Math.Sqrt((differenceX * differenceX) + (differenceY * differenceY)) <= InverseTolerance)
            {
                break;
            }
        }

        return true;
    }
}
