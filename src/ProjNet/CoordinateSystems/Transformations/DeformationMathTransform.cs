// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ProjNet.CoordinateSystems;

/// <summary>
/// Implements PROJ's <c>deformation</c> runtime transform.
/// </summary>
[Serializable]
internal sealed class DeformationMathTransform : MathTransform
{
    private const double RelativeTolerance = 1e-5d;
    private const double MissingObservationEpoch = double.MaxValue;
    private const double InverseTolerance = 1e-8d;
    private const int MaxInverseIterations = 10;
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

    private bool isInverted;
    private MathTransform inverse;

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
            (velocityGrids ?? []).ToArray());
        this.horizontalGrids = new ReadOnlyCollection<CTable2Grid>((horizontalGrids ?? []).ToArray());
        this.verticalGrids = new ReadOnlyCollection<GtxGrid>((verticalGrids ?? []).ToArray());
        this.hasFixedDt = hasFixedDt;
        this.fixedDt = fixedDt;
        this.tEpoch = tEpoch;
        this.semiMajor = semiMajor;
        this.semiMinor = semiMinor;
        this.isInverted = isInverted;

        var parameters = new List<ProjectionParameter>
        {
            new ProjectionParameter("semi_major", semiMajor),
            new ProjectionParameter("semi_minor", semiMinor),
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
    public override string WKT => throw new NotImplementedException();

    /// <inheritdoc />
    public override string XML => throw new NotImplementedException();

    /// <summary>
    /// Creates a <see cref="DeformationMathTransform"/> from parsed PROJ arguments.
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
            skipReason = "deformation arguments were null.";
            return false;
        }

        bool hasGenericGrids = args.TryGetValue("grids", out string genericGridToken) && !string.IsNullOrWhiteSpace(genericGridToken);
        bool hasHorizontalGrids = args.TryGetValue("xy_grids", out string horizontalGridToken) && !string.IsNullOrWhiteSpace(horizontalGridToken);
        bool hasVerticalGrids = args.TryGetValue("z_grids", out string verticalGridToken) && !string.IsNullOrWhiteSpace(verticalGridToken);
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

        bool hasDt = args.TryGetValue("dt", out string dtToken);
        bool hasEpoch = args.TryGetValue("t_epoch", out string epochToken);
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
            if (!TryParseFiniteDouble(dtToken, out fixedDt))
            {
                skipReason = "Unable to parse +dt parameter for deformation.";
                return false;
            }

            useFixedDt = true;
        }
        else
        {
            if (!TryParseFiniteDouble(epochToken, out tEpoch))
            {
                skipReason = "Unable to parse +t_epoch parameter for deformation.";
                return false;
            }
        }

        if (!TryResolveEllipsoid(args, out double semiMajor, out double semiMinor, out skipReason))
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
                if (!TryResolveGridPaths(genericGridToken, "grids", out IReadOnlyList<string> genericGridPaths, out skipReason))
                {
                    return false;
                }

                for (int i = 0; i < genericGridPaths.Count; i++)
                {
                    string extension = Path.GetExtension(genericGridPaths[i]);
                    if (!extension.Equals(".tif", StringComparison.OrdinalIgnoreCase)
                        && !extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase))
                    {
                        skipReason = "Grid '" + Path.GetFileName(genericGridPaths[i]) + "' is not a supported deformation velocity grid format (.tif/.tiff).";
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
                if (!TryResolveGridPaths(horizontalGridToken, "xy_grids", out IReadOnlyList<string> horizontalGridPaths, out skipReason))
                {
                    return false;
                }

                if (!TryResolveGridPaths(verticalGridToken, "z_grids", out IReadOnlyList<string> verticalGridPaths, out skipReason))
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
            skipReason = "Unable to read deformation grid: " + exception.Message;
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            skipReason = "Unable to read deformation grid: " + exception.Message;
            return false;
        }
        catch (InvalidDataException exception)
        {
            skipReason = "Invalid deformation grid: " + exception.Message;
            return false;
        }
        catch (ArgumentException exception)
        {
            skipReason = "Invalid deformation configuration: " + exception.Message;
            return false;
        }
    }

    /// <inheritdoc />
    public override bool Identity() => false;

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new DeformationMathTransform(this, !this.isInverted);
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
        if (!this.TryResolveDeltaTime(MissingObservationEpoch, out double deltaTime, out bool missingTime))
        {
            if (missingTime)
            {
                throw new ArgumentException("deformation requires a valid observation time.");
            }

            throw new ArgumentException("deformation could not resolve delta time.");
        }

        this.TransformCore(ref x, ref y, ref z, deltaTime);
    }

    /// <inheritdoc />
    internal override void Transform(ref double x, ref double y, ref double z, ref double t)
    {
        if (!this.TryResolveDeltaTime(t, out double deltaTime, out bool missingTime))
        {
            if (missingTime)
            {
                throw new ArgumentException("deformation requires a valid observation time.");
            }

            throw new ArgumentException("deformation could not resolve delta time.");
        }

        this.TransformCore(ref x, ref y, ref z, deltaTime);
    }

    private static bool TryResolveGridPaths(
        string gridsToken,
        string parameterName,
        out IReadOnlyList<string> resolvedPaths,
        out string skipReason)
    {
        resolvedPaths = [];
        skipReason = null;
        if (string.IsNullOrWhiteSpace(gridsToken))
        {
            skipReason = "deformation requires +" + parameterName + ".";
            return false;
        }

        string[] entries = gridsToken.Split([','], StringSplitOptions.RemoveEmptyEntries);
        if (entries.Length == 0)
        {
            skipReason = "deformation requires at least one grid in +" + parameterName + ".";
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
            string gridName = isOptional ? token.Substring(1) : token;
            if (string.IsNullOrWhiteSpace(gridName))
            {
                continue;
            }

            if (TryResolveGridPath(gridName, out string resolvedPath))
            {
                resolved.Add(resolvedPath);
                continue;
            }

            if (!isOptional)
            {
                skipReason = "Required grid '" + gridName + "' was not found.";
                return false;
            }
        }

        if (resolved.Count == 0)
        {
            skipReason = "No grid from +" + parameterName + " could be resolved.";
            return false;
        }

        resolvedPaths = resolved;
        return true;
    }

    private static bool TryResolveGridPath(string gridToken, out string resolvedPath)
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

        if (!string.IsNullOrWhiteSpace(fileName)
            && CoordinateTransformationFactory.TryResolveGridResourcePath(fileName, out resolvedPath))
        {
            return true;
        }

        return false;
    }

    private static bool TryGetExistingPath(string candidate, out string resolvedPath)
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
        catch (Exception) when (true)
        {
            return false;
        }
    }

    private static string NormalizePathToken(string token)
    {
        return token.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
    }

    private static bool TryResolveEllipsoid(
        IReadOnlyDictionary<string, string> args,
        out double semiMajor,
        out double semiMinor,
        out string skipReason)
    {
        semiMajor = 0d;
        semiMinor = 0d;
        skipReason = null;

        if (args.TryGetValue("r", out string radiusToken)
            && TryParseFiniteDouble(radiusToken, out double radius))
        {
            if (radius <= 0d)
            {
                skipReason = "deformation +r must be positive.";
                return false;
            }

            semiMajor = radius;
            semiMinor = radius;
            return true;
        }

        if (args.TryGetValue("a", out string majorToken)
            && TryParseFiniteDouble(majorToken, out double major))
        {
            if (major <= 0d)
            {
                skipReason = "deformation +a must be positive.";
                return false;
            }

            semiMajor = major;
            if (args.TryGetValue("b", out string minorToken)
                && TryParseFiniteDouble(minorToken, out double minor))
            {
                if (minor <= 0d)
                {
                    skipReason = "deformation +b must be positive.";
                    return false;
                }

                semiMinor = minor;
                return true;
            }

            if (args.TryGetValue("rf", out string inverseFlatteningToken)
                && TryParseFiniteDouble(inverseFlatteningToken, out double inverseFlattening))
            {
                if (inverseFlattening <= 0d)
                {
                    skipReason = "deformation +rf must be positive.";
                    return false;
                }

                semiMinor = (1d - (1d / inverseFlattening)) * major;
                return true;
            }

            semiMinor = major;
            return true;
        }

        if (args.TryGetValue("ellps", out string ellipsoidToken) && !string.IsNullOrWhiteSpace(ellipsoidToken))
        {
            if (TryResolveKnownEllipsoid(ellipsoidToken, out semiMajor, out semiMinor))
            {
                return true;
            }

            skipReason = "deformation received unsupported +ellps value.";
            return false;
        }

        if (args.TryGetValue("datum", out string datumToken) && !string.IsNullOrWhiteSpace(datumToken))
        {
            if (TryResolveKnownEllipsoid(datumToken, out semiMajor, out semiMinor))
            {
                return true;
            }

            skipReason = "deformation received unsupported +datum value.";
            return false;
        }

        semiMajor = Ellipsoid.WGS84.SemiMajorAxis;
        semiMinor = Ellipsoid.WGS84.SemiMinorAxis;
        return true;
    }

    private static bool TryResolveKnownEllipsoid(string token, out double semiMajor, out double semiMinor)
    {
        semiMajor = 0d;
        semiMinor = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (token.Equals("wgs84", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.WGS84.SemiMajorAxis;
            semiMinor = Ellipsoid.WGS84.SemiMinorAxis;
            return true;
        }

        if (token.Equals("grs80", StringComparison.OrdinalIgnoreCase)
            || token.Equals("nad83", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.GRS80.SemiMajorAxis;
            semiMinor = Ellipsoid.GRS80.SemiMinorAxis;
            return true;
        }

        if (token.Equals("clrk66", StringComparison.OrdinalIgnoreCase)
            || token.Equals("nad27", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.Clarke1866.SemiMajorAxis;
            semiMinor = Ellipsoid.Clarke1866.SemiMinorAxis;
            return true;
        }

        if (token.Equals("clrk80", StringComparison.OrdinalIgnoreCase)
            || token.Equals("clrk80ign", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.Clarke1880.SemiMajorAxis;
            semiMinor = Ellipsoid.Clarke1880.SemiMinorAxis;
            return true;
        }

        if (token.Equals("intl", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.International1924.SemiMajorAxis;
            semiMinor = Ellipsoid.International1924.SemiMinorAxis;
            return true;
        }

        if (token.Equals("sphere", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.Sphere.SemiMajorAxis;
            semiMinor = Ellipsoid.Sphere.SemiMinorAxis;
            return true;
        }

        return false;
    }

    private static bool TryParseFiniteDouble(string token, out double value)
    {
        value = 0d;
        return !string.IsNullOrWhiteSpace(token)
            && double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value)
            && !double.IsNaN(value)
            && !double.IsInfinity(value);
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private static bool IsValidObservationEpoch(double epoch)
    {
        return IsFinite(epoch) && epoch != MissingObservationEpoch;
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
        cell = default(InterpolationCell);
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
        IReadOnlyList<GeoTiffXyzGridShiftMathTransform.XyzGrid> grids,
        double longitudeDegrees,
        double latitudeDegrees,
        out GeoTiffXyzGridShiftMathTransform.XyzGrid grid)
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
        out CTable2Grid grid)
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
        out GtxGrid grid)
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
                throw new ArgumentException("Coordinate is outside deformation model grid extent.");
            }

            x += deltaTime * shiftX;
            y += deltaTime * shiftY;
            z += deltaTime * shiftZ;
            return;
        }

        if (!this.TryReverseShift(x, y, z, deltaTime, out double xOut, out double yOut, out double zOut))
        {
            throw new ArgumentException("deformation inverse transformation failed.");
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

        if (!IsValidObservationEpoch(observationEpoch))
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
        if (!IsFinite(x) || !IsFinite(y) || !IsFinite(z))
        {
            return false;
        }

        double longitudeDegrees = x;
        double latitudeDegrees = y;
        double height = z;
        this.geocentricInverse.Transform(ref longitudeDegrees, ref latitudeDegrees, ref height);
        if (!IsFinite(longitudeDegrees) || !IsFinite(latitudeDegrees))
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
        return IsFinite(shiftX) && IsFinite(shiftY) && IsFinite(shiftZ);
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
        if (!TryFindXyzGrid(this.velocityGrids, longitudeDegrees, latitudeDegrees, out GeoTiffXyzGridShiftMathTransform.XyzGrid grid))
        {
            return false;
        }

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
        if (!IsFinite(east00)
            || !IsFinite(east01)
            || !IsFinite(east10)
            || !IsFinite(east11)
            || !IsFinite(north00)
            || !IsFinite(north01)
            || !IsFinite(north10)
            || !IsFinite(north11)
            || !IsFinite(up00)
            || !IsFinite(up01)
            || !IsFinite(up10)
            || !IsFinite(up11))
        {
            return false;
        }

        eastVelocity = Bilinear(east00, east01, east10, east11, cell) / 1000d;
        northVelocity = Bilinear(north00, north01, north10, north11, cell) / 1000d;
        upVelocity = Bilinear(up00, up01, up10, up11, cell) / 1000d;
        return IsFinite(eastVelocity) && IsFinite(northVelocity) && IsFinite(upVelocity);
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
        if (!TryFindHorizontalGrid(this.horizontalGrids, longitudeRadians, latitudeRadians, out CTable2Grid horizontalGrid))
        {
            return false;
        }

        if (!horizontalGrid.TryInterpolate(longitudeRadians, latitudeRadians, out double eastMmPerYear, out double northMmPerYear))
        {
            return false;
        }

        if (!TryFindVerticalGrid(this.verticalGrids, longitudeRadians, latitudeRadians, out GtxGrid verticalGrid))
        {
            return false;
        }

        if (!verticalGrid.TryInterpolate(longitudeRadians, latitudeRadians, out double upMmPerYear))
        {
            return false;
        }

        eastVelocity = eastMmPerYear / 1000d;
        northVelocity = northMmPerYear / 1000d;
        upVelocity = upMmPerYear / 1000d;
        return IsFinite(eastVelocity) && IsFinite(northVelocity) && IsFinite(upVelocity);
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

        double z0 = firstDeltaZ;
        outputX = inputX - (deltaTime * firstDeltaX);
        outputY = inputY - (deltaTime * firstDeltaY);
        outputZ = inputZ + (deltaTime * firstDeltaZ);

        for (int i = 0; i < MaxInverseIterations; i++)
        {
            if (!this.TryGetGridShift(outputX, outputY, outputZ, out double deltaX, out double deltaY, out double deltaZ))
            {
                break;
            }

            double differenceX = outputX + (deltaTime * deltaX) - inputX;
            double differenceY = outputY + (deltaTime * deltaY) - inputY;
            double differenceZ = outputZ - (deltaTime * deltaZ) - inputZ;
            outputX += differenceX;
            outputY += differenceY;
            outputZ += differenceZ;

            if (Math.Sqrt((differenceX * differenceX) + (differenceY * differenceY)) <= InverseTolerance)
            {
                break;
            }
        }

        outputZ = inputZ - (deltaTime * z0);
        return true;
    }

    [Serializable]
    private readonly struct InterpolationCell
    {
        internal InterpolationCell(int x0, int y0, int x1, int y1, double w00, double w01, double w10, double w11)
        {
            this.X0 = x0;
            this.Y0 = y0;
            this.X1 = x1;
            this.Y1 = y1;
            this.W00 = w00;
            this.W01 = w01;
            this.W10 = w10;
            this.W11 = w11;
        }

        internal int X0 { get; }

        internal int Y0 { get; }

        internal int X1 { get; }

        internal int Y1 { get; }

        internal double W00 { get; }

        internal double W01 { get; }

        internal double W10 { get; }

        internal double W11 { get; }
    }

    [Serializable]
    private sealed class CTable2Grid
    {
        private readonly float[] eastValues;
        private readonly float[] northValues;

        private CTable2Grid(
            string sourcePath,
            double west,
            double east,
            double south,
            double north,
            double resolutionX,
            double resolutionY,
            int width,
            int height,
            float[] eastValues,
            float[] northValues)
        {
            this.SourcePath = sourcePath;
            this.West = west;
            this.East = east;
            this.South = south;
            this.North = north;
            this.ResolutionX = resolutionX;
            this.ResolutionY = resolutionY;
            this.Width = width;
            this.Height = height;
            this.eastValues = eastValues;
            this.northValues = northValues;
            this.Epsilon = (Math.Abs(resolutionX) + Math.Abs(resolutionY)) * RelativeTolerance;
            this.InvResolutionX = 1d / resolutionX;
            this.InvResolutionY = 1d / resolutionY;
            this.Area = Math.Abs((east - west) * (north - south));
        }

        internal string SourcePath { get; }

        internal double West { get; }

        internal double East { get; }

        internal double South { get; }

        internal double North { get; }

        internal double ResolutionX { get; }

        internal double ResolutionY { get; }

        internal int Width { get; }

        internal int Height { get; }

        internal double Epsilon { get; }

        internal double InvResolutionX { get; }

        internal double InvResolutionY { get; }

        internal double Area { get; }

        internal static CTable2Grid Load(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.Length < 160)
            {
                throw new InvalidDataException("CTABLE2 file is too small.");
            }

            string identifier = Encoding.ASCII.GetString(bytes, 0, 9);
            if (!identifier.Equals("CTABLE V2", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Horizontal grid file is not a CTable2 grid.");
            }

            double west = ReadDoubleLittleEndian(bytes, 96);
            double south = ReadDoubleLittleEndian(bytes, 104);
            double resolutionX = ReadDoubleLittleEndian(bytes, 112);
            double resolutionY = ReadDoubleLittleEndian(bytes, 120);
            int width = ReadInt32LittleEndian(bytes, 128);
            int height = ReadInt32LittleEndian(bytes, 132);
            if (!IsFinite(west)
                || !IsFinite(south)
                || !IsFinite(resolutionX)
                || !IsFinite(resolutionY)
                || width <= 0
                || height <= 0
                || Math.Abs(west) > (4d * Math.PI)
                || Math.Abs(south) > (Math.PI + 1e-5d)
                || resolutionX <= 1e-10d
                || resolutionY <= 1e-10d)
            {
                throw new InvalidDataException("CTABLE2 header contains invalid extents or resolution.");
            }

            long expectedDataBytes = (long)width * height * 8L;
            if (bytes.Length < 160 + expectedDataBytes)
            {
                throw new InvalidDataException("CTABLE2 data is truncated.");
            }

            var eastValues = new float[width * height];
            var northValues = new float[width * height];
            int offset = 160;
            for (int i = 0; i < eastValues.Length; i++)
            {
                eastValues[i] = ReadSingleLittleEndian(bytes, offset);
                northValues[i] = ReadSingleLittleEndian(bytes, offset + 4);
                offset += 8;
            }

            double east = west + ((width - 1) * resolutionX);
            double north = south + ((height - 1) * resolutionY);
            return new CTable2Grid(path, west, east, south, north, resolutionX, resolutionY, width, height, eastValues, northValues);
        }

        internal bool Contains(double longitudeRadians, double latitudeRadians)
        {
            double lon = longitudeRadians;
            if (lon < this.West - this.Epsilon)
            {
                lon += TwoPi;
            }
            else if (lon > this.East + this.Epsilon)
            {
                lon -= TwoPi;
            }

            return lon >= this.West - this.Epsilon
                && lon <= this.East + this.Epsilon
                && latitudeRadians >= this.South - this.Epsilon
                && latitudeRadians <= this.North + this.Epsilon;
        }

        internal bool TryInterpolate(double longitudeRadians, double latitudeRadians, out double eastMmPerYear, out double northMmPerYear)
        {
            eastMmPerYear = 0d;
            northMmPerYear = 0d;
            double normalizedLongitude = longitudeRadians - this.West;
            if (normalizedLongitude + this.Epsilon < 0d)
            {
                normalizedLongitude += TwoPi;
            }
            else if (normalizedLongitude - this.Epsilon > this.East - this.West)
            {
                normalizedLongitude -= TwoPi;
            }

            double normalizedLatitude = latitudeRadians - this.South;
            double gridX = normalizedLongitude * this.InvResolutionX;
            double gridY = normalizedLatitude * this.InvResolutionY;
            int indexX = (int)Math.Floor(gridX);
            int indexY = (int)Math.Floor(gridY);
            double fractionX = gridX - indexX;
            double fractionY = gridY - indexY;
            if (!TryNormalizeInterpolationCell(this.Width, ref indexX, ref fractionX)
                || !TryNormalizeInterpolationCell(this.Height, ref indexY, ref fractionY))
            {
                return false;
            }

            int indexX2 = indexX + 1;
            int indexY2 = indexY + 1;
            double xy = fractionX * fractionY;
            double w00 = 1d - fractionX - fractionY + xy;
            double w10 = fractionX - xy;
            double w01 = fractionY - xy;
            double w11 = xy;
            eastMmPerYear = (this.GetEastValue(indexX, indexY) * w00)
                + (this.GetEastValue(indexX2, indexY) * w10)
                + (this.GetEastValue(indexX, indexY2) * w01)
                + (this.GetEastValue(indexX2, indexY2) * w11);
            northMmPerYear = (this.GetNorthValue(indexX, indexY) * w00)
                + (this.GetNorthValue(indexX2, indexY) * w10)
                + (this.GetNorthValue(indexX, indexY2) * w01)
                + (this.GetNorthValue(indexX2, indexY2) * w11);
            return IsFinite(eastMmPerYear) && IsFinite(northMmPerYear);
        }

        private static int ReadInt32LittleEndian(byte[] bytes, int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BitConverter.ToInt32(bytes, offset);
            }

            return (bytes[offset + 3] << 24)
                | (bytes[offset + 2] << 16)
                | (bytes[offset + 1] << 8)
                | bytes[offset];
        }

        private static double ReadDoubleLittleEndian(byte[] bytes, int offset)
        {
            byte[] buffer = new byte[8];
            Buffer.BlockCopy(bytes, offset, buffer, 0, 8);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }

            return BitConverter.ToDouble(buffer, 0);
        }

        private static float ReadSingleLittleEndian(byte[] bytes, int offset)
        {
            byte[] buffer = new byte[4];
            Buffer.BlockCopy(bytes, offset, buffer, 0, 4);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }

            return BitConverter.ToSingle(buffer, 0);
        }

        private float GetEastValue(int x, int y)
        {
            return this.eastValues[(y * this.Width) + x];
        }

        private float GetNorthValue(int x, int y)
        {
            return this.northValues[(y * this.Width) + x];
        }
    }

    [Serializable]
    private sealed class GtxGrid
    {
        private readonly float[] values;

        private GtxGrid(
            string sourcePath,
            double west,
            double east,
            double south,
            double north,
            double resolutionX,
            double resolutionY,
            int width,
            int height,
            bool fullWorldLongitude,
            float[] values)
        {
            this.SourcePath = sourcePath;
            this.West = west;
            this.East = east;
            this.South = south;
            this.North = north;
            this.ResolutionX = resolutionX;
            this.ResolutionY = resolutionY;
            this.Width = width;
            this.Height = height;
            this.FullWorldLongitude = fullWorldLongitude;
            this.values = values;
            this.Epsilon = (Math.Abs(resolutionX) + Math.Abs(resolutionY)) * RelativeTolerance;
            this.InvResolutionX = 1d / resolutionX;
            this.InvResolutionY = 1d / resolutionY;
            this.Area = Math.Abs((east - west) * (north - south));
        }

        internal string SourcePath { get; }

        internal double West { get; }

        internal double East { get; }

        internal double South { get; }

        internal double North { get; }

        internal double ResolutionX { get; }

        internal double ResolutionY { get; }

        internal int Width { get; }

        internal int Height { get; }

        internal bool FullWorldLongitude { get; }

        internal double Epsilon { get; }

        internal double InvResolutionX { get; }

        internal double InvResolutionY { get; }

        internal double Area { get; }

        internal static GtxGrid Load(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.Length < 40)
            {
                throw new InvalidDataException("GTX file is too small.");
            }

            double yOrigin = ReadDoubleBigEndian(bytes, 0);
            double xOrigin = ReadDoubleBigEndian(bytes, 8);
            double yStep = ReadDoubleBigEndian(bytes, 16);
            double xStep = ReadDoubleBigEndian(bytes, 24);
            int rows = ReadInt32BigEndian(bytes, 32);
            int columns = ReadInt32BigEndian(bytes, 36);
            if (columns <= 0
                || rows <= 0
                || xOrigin < -360d
                || xOrigin > 360d
                || yOrigin < -90d
                || yOrigin > 90d)
            {
                throw new InvalidDataException("GTX header contains invalid extents.");
            }

            if (xOrigin >= 180d)
            {
                xOrigin -= 360d;
            }

            if (xStep == 0d || yStep == 0d)
            {
                throw new InvalidDataException("GTX header contains invalid resolution.");
            }

            long expectedDataBytes = (long)rows * columns * sizeof(float);
            if (bytes.Length < 40 + expectedDataBytes)
            {
                throw new InvalidDataException("GTX data is truncated.");
            }

            var values = new float[rows * columns];
            int offset = 40;
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = ReadSingleBigEndian(bytes, offset);
                offset += sizeof(float);
            }

            double west = DegreesToRadians(xOrigin);
            double south = DegreesToRadians(yOrigin);
            double resolutionX = DegreesToRadians(xStep);
            double resolutionY = DegreesToRadians(yStep);
            double east = west + (resolutionX * (columns - 1));
            double north = south + (resolutionY * (rows - 1));
            double worldWidth = Math.Abs(resolutionX) * columns;
            bool fullWorldLongitude = Math.Abs(worldWidth - TwoPi) <= (Math.Abs(resolutionX) * 1e-4d);
            return new GtxGrid(path, west, east, south, north, resolutionX, resolutionY, columns, rows, fullWorldLongitude, values);
        }

        internal bool Contains(double longitudeRadians, double latitudeRadians)
        {
            double lon = longitudeRadians;
            if (lon < this.West - this.Epsilon)
            {
                lon += TwoPi;
            }
            else if (lon > this.East + this.Epsilon)
            {
                lon -= TwoPi;
            }

            return lon >= this.West - this.Epsilon
                && lon <= this.East + this.Epsilon
                && latitudeRadians >= this.South - this.Epsilon
                && latitudeRadians <= this.North + this.Epsilon;
        }

        internal bool TryInterpolate(double longitudeRadians, double latitudeRadians, out double valueMmPerYear)
        {
            valueMmPerYear = 0d;
            double gridX = (longitudeRadians - this.West) * this.InvResolutionX;
            if (longitudeRadians < this.West)
            {
                if (this.FullWorldLongitude)
                {
                    gridX = PositiveModulo(gridX, this.Width);
                }
                else
                {
                    gridX = (longitudeRadians + TwoPi - this.West) * this.InvResolutionX;
                }
            }
            else if (longitudeRadians > this.East)
            {
                if (this.FullWorldLongitude)
                {
                    gridX = PositiveModulo(gridX, this.Width);
                }
                else
                {
                    gridX = (longitudeRadians - TwoPi - this.West) * this.InvResolutionX;
                }
            }

            double gridY = (latitudeRadians - this.South) * this.InvResolutionY;
            int gridIx = (int)Math.Floor(gridX);
            int gridIy = (int)Math.Floor(gridY);
            if (gridIx < 0 || gridIx >= this.Width || gridIy < 0 || gridIy >= this.Height)
            {
                return false;
            }

            double fractionX = gridX - gridIx;
            double fractionY = gridY - gridIy;
            int gridIx2 = gridIx + 1;
            if (gridIx2 >= this.Width)
            {
                gridIx2 = this.FullWorldLongitude ? 0 : this.Width - 1;
            }

            int gridIy2 = gridIy + 1;
            if (gridIy2 >= this.Height)
            {
                gridIy2 = this.Height - 1;
            }

            float valueA = this.GetValue(gridIx, gridIy);
            float valueB = this.GetValue(gridIx2, gridIy);
            float valueC = this.GetValue(gridIx, gridIy2);
            float valueD = this.GetValue(gridIx2, gridIy2);

            double gridXy = fractionX * fractionY;
            double weightA = 1d - fractionX - fractionY + gridXy;
            double weightB = fractionX - gridXy;
            double weightC = fractionY - gridXy;
            double weightD = gridXy;

            bool aValid = !IsNoData(valueA, 1d);
            bool bValid = !IsNoData(valueB, 1d);
            bool cValid = !IsNoData(valueC, 1d);
            bool dValid = !IsNoData(valueD, 1d);
            int validCount = (aValid ? 1 : 0) + (bValid ? 1 : 0) + (cValid ? 1 : 0) + (dValid ? 1 : 0);
            if (validCount == 0)
            {
                return false;
            }

            if (validCount == 4)
            {
                valueMmPerYear = (valueA * weightA) + (valueB * weightB) + (valueC * weightC) + (valueD * weightD);
                return IsFinite(valueMmPerYear);
            }

            double weightedValue = 0d;
            double totalWeight = 0d;
            if (aValid)
            {
                weightedValue += valueA * weightA;
                totalWeight += weightA;
            }

            if (bValid)
            {
                weightedValue += valueB * weightB;
                totalWeight += weightB;
            }

            if (cValid)
            {
                weightedValue += valueC * weightC;
                totalWeight += weightC;
            }

            if (dValid)
            {
                weightedValue += valueD * weightD;
                totalWeight += weightD;
            }

            if (totalWeight == 0d)
            {
                return false;
            }

            valueMmPerYear = weightedValue / totalWeight;
            return IsFinite(valueMmPerYear);
        }

        private static bool IsNoData(float value, double multiplier)
        {
            double scaled = value * multiplier;
            return scaled > 1000d || scaled < -1000d || value == -88.88880f;
        }

        private static double PositiveModulo(double value, int modulus)
        {
            if (modulus <= 0)
            {
                return value;
            }

            double result = value % modulus;
            if (result < 0d)
            {
                result += modulus;
            }

            return result;
        }

        private static int ReadInt32BigEndian(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24)
                | (bytes[offset + 1] << 16)
                | (bytes[offset + 2] << 8)
                | bytes[offset + 3];
        }

        private static double ReadDoubleBigEndian(byte[] bytes, int offset)
        {
            byte[] buffer = new byte[8];
            Buffer.BlockCopy(bytes, offset, buffer, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }

            return BitConverter.ToDouble(buffer, 0);
        }

        private static float ReadSingleBigEndian(byte[] bytes, int offset)
        {
            byte[] buffer = new byte[4];
            Buffer.BlockCopy(bytes, offset, buffer, 0, 4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }

            return BitConverter.ToSingle(buffer, 0);
        }

        private float GetValue(int x, int y)
        {
            return this.values[(y * this.Width) + x];
        }
    }
}
