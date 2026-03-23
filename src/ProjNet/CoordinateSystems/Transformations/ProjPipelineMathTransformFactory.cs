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
using System.Globalization;
using System.IO;
using System.Linq;
using ProjNet.CoordinateSystems;

/// <summary>
/// Creates runtime math transforms from PROJ-style pipeline operation strings.
/// </summary>
internal static class ProjPipelineMathTransformFactory
{
    private static readonly char[] CommaSeparator = [','];
    private static readonly char[] OperationTokenSeparators = [' ', '\t'];
    private static readonly string[] HorizontalGridExtensions = [".gsb", ".tif", ".tiff"];
    private static readonly string[] VerticalGridExtensions = [".gtx", ".tif", ".tiff"];
    private static readonly string[] XyzGridExtensions = [".tif", ".tiff"];

    /// <summary>
    /// Tries to create an executable transform from a full operation or pipeline definition.
    /// </summary>
    /// <param name="operation">Operation text to parse.</param>
    /// <param name="transform">Created transform when parsing succeeds.</param>
    /// <param name="skipReason">Reason why transform creation was skipped.</param>
    /// <returns><see langword="true"/> when a transform was created.</returns>
    internal static bool TryCreateMathTransform(string operation, out MathTransform transform, out string skipReason)
    {
        transform = null;
        skipReason = null;

        if (operation is null)
        {
            skipReason = "Operation string was null.";
            return false;
        }

        if (!TrySplitPipelineSteps(operation, out IReadOnlyList<string> steps))
        {
            steps = [operation];
        }

        var stepTransforms = new List<MathTransform>(steps.Count);
        for (int i = 0; i < steps.Count; i++)
        {
            if (!TryCreateStepTransform(steps[i], out MathTransform stepTransform, out skipReason))
            {
                return false;
            }

            stepTransforms.Add(stepTransform);
        }

        if (stepTransforms.Count == 0)
        {
            skipReason = "Operation did not contain any executable step.";
            return false;
        }

        if (stepTransforms.Count == 1)
        {
            transform = stepTransforms[0];
            return true;
        }

        transform = new CompositeMathTransform(stepTransforms);
        return true;
    }

    private static bool TryCreateStepTransform(string operation, out MathTransform transform, out string skipReason)
    {
        transform = null;
        skipReason = null;

        if (!TryParseOperationArguments(operation, out Dictionary<string, string> args))
        {
            skipReason = "Unable to parse operation parameters.";
            return false;
        }

        if (!args.TryGetValue("proj", out string projCode))
        {
            skipReason = "Operation is missing +proj.";
            return false;
        }

        if (projCode.Equals("latlong", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("longlat", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("latlon", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("lonlat", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("noop", StringComparison.OrdinalIgnoreCase))
        {
            transform = new IdentityMathTransform(3);
            return true;
        }

        if (projCode.Equals("set", StringComparison.OrdinalIgnoreCase))
        {
            return SetMathTransform.TryCreate(args, out transform, out skipReason);
        }

        if (projCode.Equals("axisswap", StringComparison.OrdinalIgnoreCase))
        {
            return TryCreateAxisSwapTransform(args, out transform, out skipReason);
        }

        if (projCode.Equals("unitconvert", StringComparison.OrdinalIgnoreCase))
        {
            return TryCreateUnitConvertTransform(args, out transform, out skipReason);
        }

        if (projCode.Equals("hgridshift", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("gridshift", StringComparison.OrdinalIgnoreCase))
        {
            return TryCreateHorizontalGridShiftTransform(args, out transform, out skipReason);
        }

        if (projCode.Equals("vgridshift", StringComparison.OrdinalIgnoreCase))
        {
            return TryCreateVerticalGridShiftTransform(args, out transform, out skipReason);
        }

        if (projCode.Equals("xyzgridshift", StringComparison.OrdinalIgnoreCase))
        {
            return TryCreateXyzGridShiftTransform(args, out transform, out skipReason);
        }

        if (projCode.Equals("defmodel", StringComparison.OrdinalIgnoreCase))
        {
            return DefModelMathTransform.TryCreate(args, out transform, out skipReason);
        }

        if (projCode.Equals("deformation", StringComparison.OrdinalIgnoreCase))
        {
            return DeformationMathTransform.TryCreate(args, out transform, out skipReason);
        }

        if (projCode.Equals("tinshift", StringComparison.OrdinalIgnoreCase))
        {
            return TinShiftMathTransform.TryCreate(args, out transform, out skipReason);
        }

        if (projCode.Equals("topocentric", StringComparison.OrdinalIgnoreCase))
        {
            return TopocentricMathTransform.TryCreate(args, out transform, out skipReason);
        }

        if (projCode.Equals("vertoffset", StringComparison.OrdinalIgnoreCase))
        {
            return VertOffsetMathTransform.TryCreate(args, out transform, out skipReason);
        }

        if (projCode.Equals("helmert", StringComparison.OrdinalIgnoreCase))
        {
            return HelmertMathTransform.TryCreate(args, out transform, out skipReason);
        }

        if (projCode.Equals("molodensky", StringComparison.OrdinalIgnoreCase))
        {
            return MolodenskyMathTransform.TryCreate(args, out transform, out skipReason);
        }

        if (projCode.Equals("horner", StringComparison.OrdinalIgnoreCase))
        {
            return HornerMathTransform.TryCreate(args, out transform, out skipReason);
        }

        if (projCode.Equals("ob_tran", StringComparison.OrdinalIgnoreCase))
        {
            return ObTranMathTransform.TryCreate(args, out transform, out skipReason);
        }

        if (projCode.Equals("sch", StringComparison.OrdinalIgnoreCase))
        {
            return SchMathTransform.TryCreate(args, out transform, out skipReason);
        }

        if (projCode.Equals("spherical_cross_track_height", StringComparison.OrdinalIgnoreCase))
        {
            return SchMathTransform.TryCreate(args, out transform, out skipReason);
        }

        skipReason = "Projection '" + projCode + "' is not part of the current builtins wave.";
        return false;
    }

    private static bool TryCreateAxisSwapTransform(
        Dictionary<string, string> args,
        out MathTransform transform,
        out string skipReason)
    {
        transform = null;
        skipReason = null;

        bool hasOrder = args.TryGetValue("order", out string orderToken) && !string.IsNullOrWhiteSpace(orderToken);
        bool hasAxis = args.TryGetValue("axis", out string axisToken) && !string.IsNullOrWhiteSpace(axisToken);
        if (hasOrder == hasAxis)
        {
            skipReason = "Axisswap requires exactly one of +order or +axis.";
            return false;
        }

        int[] order;
        if (hasOrder)
        {
            if (!TryParseAxisSwapOrder(orderToken, out order))
            {
                skipReason = "Unable to parse +order parameter for axisswap.";
                return false;
            }
        }
        else
        {
            if (!TryParseAxisOrder(axisToken, out order))
            {
                skipReason = "Unable to parse +axis parameter for axisswap.";
                return false;
            }
        }

        int dimension = order.Length;
        if (dimension is < 2 or > 3)
        {
            skipReason = "Axisswap supports only 2D or 3D coordinates in the current runtime.";
            return false;
        }

        int[] sourceIndices = [0, 1, 2];
        int[] signs = [1, 1, 1];
        for (int i = 0; i < dimension; i++)
        {
            int rawOrder = order[i];
            int sourceIndex = Math.Abs(rawOrder) - 1;
            if (sourceIndex < 0 || sourceIndex >= dimension)
            {
                skipReason = "Axisswap order references an out-of-range axis.";
                return false;
            }

            sourceIndices[i] = sourceIndex;
            signs[i] = rawOrder < 0 ? -1 : 1;
        }

        transform = new AxisSwapMathTransform(
            dimension,
            sourceIndices[0],
            signs[0],
            sourceIndices[1],
            signs[1],
            sourceIndices[2],
            signs[2]);

        return true;
    }

    private static bool TryCreateUnitConvertTransform(
        IDictionary<string, string> args,
        out MathTransform transform,
        out string skipReason)
    {
        transform = null;
        skipReason = null;

        if (!TryResolveUnitScale(args, "xy_in", "xy_out", true, out double xyScale))
        {
            skipReason = "Unable to parse XY units for unitconvert.";
            return false;
        }

        if (!TryResolveUnitScale(args, "z_in", "z_out", false, out double zScale))
        {
            skipReason = "Unable to parse Z units for unitconvert.";
            return false;
        }

        transform = new UnitConvertMathTransform(3, xyScale, zScale);
        return true;
    }

    private static bool TryCreateHorizontalGridShiftTransform(
        Dictionary<string, string> args,
        out MathTransform transform,
        out string skipReason)
    {
        transform = null;
        skipReason = null;

        if (!args.TryGetValue("grids", out string gridsToken) || string.IsNullOrWhiteSpace(gridsToken))
        {
            skipReason = "Horizontal grid shift requires +grids.";
            return false;
        }

        if (!TryResolveGridPaths(gridsToken, out IReadOnlyList<string> gridPaths, out skipReason))
        {
            return false;
        }

        if (!TryValidateGridExtensions(gridPaths, HorizontalGridExtensions, "horizontal", out skipReason))
        {
            return false;
        }

        try
        {
            bool hasGeoTiff = ContainsGeoTiffGrid(gridPaths);
            transform = hasGeoTiff
                ? (MathTransform)new GeoTiffHGridShiftMathTransform(gridPaths)
                : new Ntv2HGridShiftMathTransform(gridPaths);
        }
        catch (IOException ioException)
        {
            skipReason = "Unable to read horizontal grid: " + ioException.Message;
            return false;
        }
        catch (InvalidDataException dataException)
        {
            skipReason = "Invalid horizontal grid data: " + dataException.Message;
            return false;
        }
        catch (ArgumentException argumentException)
        {
            skipReason = "Invalid grid parameters: " + argumentException.Message;
            return false;
        }

        if (args.ContainsKey("inv"))
        {
            transform = transform.Inverse();
        }

        return true;
    }

    private static bool TryCreateVerticalGridShiftTransform(
        Dictionary<string, string> args,
        out MathTransform transform,
        out string skipReason)
    {
        transform = null;
        skipReason = null;

        if (!args.TryGetValue("grids", out string gridsToken) || string.IsNullOrWhiteSpace(gridsToken))
        {
            skipReason = "Vertical grid shift requires +grids.";
            return false;
        }

        if (!TryResolveGridPaths(gridsToken, out IReadOnlyList<string> gridPaths, out skipReason))
        {
            return false;
        }

        if (!TryValidateGridExtensions(gridPaths, VerticalGridExtensions, "vertical", out skipReason))
        {
            return false;
        }

        double multiplier = -1d;
        if (args.TryGetValue("multiplier", out string multiplierToken)
            && !double.TryParse(multiplierToken, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out multiplier))
        {
            skipReason = "Unable to parse +multiplier parameter for vgridshift.";
            return false;
        }

        if (double.IsNaN(multiplier) || double.IsInfinity(multiplier))
        {
            skipReason = "vgridshift +multiplier must be a finite numeric value.";
            return false;
        }

        try
        {
            bool hasGeoTiff = ContainsGeoTiffGrid(gridPaths);
            transform = hasGeoTiff
                ? (MathTransform)new GeoTiffVGridShiftMathTransform(gridPaths, multiplier)
                : new GtxVGridShiftMathTransform(gridPaths, multiplier);
        }
        catch (IOException ioException)
        {
            skipReason = "Unable to read vertical grid: " + ioException.Message;
            return false;
        }
        catch (InvalidDataException dataException)
        {
            skipReason = "Invalid vertical grid data: " + dataException.Message;
            return false;
        }
        catch (ArgumentException argumentException)
        {
            skipReason = "Invalid grid parameters: " + argumentException.Message;
            return false;
        }

        if (args.ContainsKey("inv"))
        {
            transform = transform.Inverse();
        }

        return true;
    }

    private static bool TryCreateXyzGridShiftTransform(
        Dictionary<string, string> args,
        out MathTransform transform,
        out string skipReason)
    {
        transform = null;
        skipReason = null;

        if (!args.TryGetValue("grids", out string gridsToken) || string.IsNullOrWhiteSpace(gridsToken))
        {
            skipReason = "Geocentric grid shift requires +grids.";
            return false;
        }

        if (!TryResolveGridPaths(gridsToken, out IReadOnlyList<string> gridPaths, out skipReason))
        {
            return false;
        }

        if (!TryValidateGridExtensions(gridPaths, XyzGridExtensions, "xyz", out skipReason))
        {
            return false;
        }

        bool gridRefIsInput = true;
        if (args.TryGetValue("grid_ref", out string gridRefToken) && !string.IsNullOrWhiteSpace(gridRefToken))
        {
            if (gridRefToken.Equals("input_crs", StringComparison.OrdinalIgnoreCase))
            {
                gridRefIsInput = true;
            }
            else if (gridRefToken.Equals("output_crs", StringComparison.OrdinalIgnoreCase))
            {
                gridRefIsInput = false;
            }
            else
            {
                skipReason = "xyzgridshift +grid_ref must be 'input_crs' or 'output_crs'.";
                return false;
            }
        }

        double multiplier = 1d;
        if (args.TryGetValue("multiplier", out string multiplierToken)
            && !double.TryParse(multiplierToken, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out multiplier))
        {
            skipReason = "Unable to parse +multiplier parameter for xyzgridshift.";
            return false;
        }

        if (double.IsNaN(multiplier) || double.IsInfinity(multiplier))
        {
            skipReason = "xyzgridshift +multiplier must be a finite numeric value.";
            return false;
        }

        if (!TryResolveEllipsoid(args, out double semiMajor, out double semiMinor, out skipReason))
        {
            return false;
        }

        try
        {
            transform = new GeoTiffXyzGridShiftMathTransform(gridPaths, semiMajor, semiMinor, multiplier, gridRefIsInput);
        }
        catch (IOException ioException)
        {
            skipReason = "Unable to read xyz grid: " + ioException.Message;
            return false;
        }
        catch (InvalidDataException dataException)
        {
            skipReason = "Invalid xyz grid data: " + dataException.Message;
            return false;
        }
        catch (ArgumentException argumentException)
        {
            skipReason = "Invalid xyz grid parameters: " + argumentException.Message;
            return false;
        }

        if (args.ContainsKey("inv"))
        {
            transform = transform.Inverse();
        }

        return true;
    }

    private static bool TryValidateGridExtensions(
        IReadOnlyList<string> gridPaths,
        IReadOnlyList<string> allowedExtensions,
        string gridFamilyName,
        out string skipReason)
    {
        skipReason = null;
        string allowedList = string.Join("/", allowedExtensions);
        for (int i = 0; i < gridPaths.Count; i++)
        {
            string path = gridPaths[i];
            if (!IsPathWithAnyExtension(path, allowedExtensions))
            {
                skipReason = "Grid '" + Path.GetFileName(path) + "' is not a supported " + gridFamilyName + " grid format (" + allowedList + ").";
                return false;
            }
        }

        return true;
    }

    private static bool ContainsGeoTiffGrid(IReadOnlyList<string> gridPaths)
    {
        for (int i = 0; i < gridPaths.Count; i++)
        {
            if (IsGeoTiffPath(gridPaths[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGeoTiffPath(string path)
    {
        return IsPathWithAnyExtension(path, XyzGridExtensions);
    }

    private static bool IsPathWithAnyExtension(string path, IReadOnlyList<string> extensions)
    {
        string extension = Path.GetExtension(path);
        for (int i = 0; i < extensions.Count; i++)
        {
            if (extension.Equals(extensions[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveGridPaths(
        string gridsToken,
        out IReadOnlyList<string> resolvedPaths,
        out string skipReason)
    {
        resolvedPaths = Array.Empty<string>();
        skipReason = null;

        string[] entries = gridsToken.Split(CommaSeparator, StringSplitOptions.RemoveEmptyEntries);
        if (entries.Length == 0)
        {
            skipReason = "Horizontal grid shift requires at least one grid name in +grids.";
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

            bool isOptional = token.Length > 0 && token[0] == '@';
            string gridName = isOptional ? token.Substring(1) : token;
            if (string.IsNullOrWhiteSpace(gridName))
            {
                continue;
            }

            if (CoordinateTransformationFactory.TryResolveGridResourcePath(gridName, out string resolvedPath))
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
            skipReason = "No grid from +grids could be resolved.";
            return false;
        }

        resolvedPaths = resolved;
        return true;
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
            && TryParseFiniteDouble(radiusToken, out double radius)
            && radius > 0d)
        {
            semiMajor = radius;
            semiMinor = radius;
            return true;
        }

        if (args.TryGetValue("a", out string majorToken)
            && TryParseFiniteDouble(majorToken, out double major)
            && major > 0d)
        {
            semiMajor = major;
            if (args.TryGetValue("b", out string minorToken)
                && TryParseFiniteDouble(minorToken, out double minor)
                && minor > 0d)
            {
                semiMinor = minor;
                return true;
            }

            if (args.TryGetValue("rf", out string inverseFlatteningToken)
                && TryParseFiniteDouble(inverseFlatteningToken, out double inverseFlattening)
                && inverseFlattening > 0d)
            {
                semiMinor = (1d - (1d / inverseFlattening)) * major;
                return true;
            }

            semiMinor = major;
            return true;
        }

        if (args.TryGetValue("ellps", out string ellps) && !string.IsNullOrWhiteSpace(ellps))
        {
            if (TryResolveKnownEllipsoid(ellps, out semiMajor, out semiMinor))
            {
                return true;
            }

            skipReason = "xyzgridshift received unsupported +ellps value.";
            return false;
        }

        if (args.TryGetValue("datum", out string datum) && !string.IsNullOrWhiteSpace(datum))
        {
            if (TryResolveKnownEllipsoid(datum, out semiMajor, out semiMinor))
            {
                return true;
            }

            skipReason = "xyzgridshift received unsupported +datum value.";
            return false;
        }

        skipReason = "xyzgridshift requires ellipsoid definition (+ellps, +datum, +r, +a/+b, or +a/+rf).";
        return false;
    }

    private static bool TryResolveKnownEllipsoid(string token, out double semiMajor, out double semiMinor)
    {
        semiMajor = 0d;
        semiMinor = 0d;

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

    private static bool TryResolveUnitScale(
        IDictionary<string, string> args,
        string inKey,
        string outKey,
        bool treatDegRadAsIdentity,
        out double scale)
    {
        scale = 1d;
        bool hasIn = args.TryGetValue(inKey, out string inToken) && !string.IsNullOrWhiteSpace(inToken);
        bool hasOut = args.TryGetValue(outKey, out string outToken) && !string.IsNullOrWhiteSpace(outToken);
        if (!hasIn && !hasOut)
        {
            return true;
        }

        if (hasIn != hasOut)
        {
            return false;
        }

        if (treatDegRadAsIdentity
            && ((inToken.Equals("deg", StringComparison.OrdinalIgnoreCase) && outToken.Equals("rad", StringComparison.OrdinalIgnoreCase))
                || (inToken.Equals("rad", StringComparison.OrdinalIgnoreCase) && outToken.Equals("deg", StringComparison.OrdinalIgnoreCase))))
        {
            scale = 1d;
            return true;
        }

        if (!TryResolveUnitFactor(inToken, out double inFactor) || !TryResolveUnitFactor(outToken, out double outFactor))
        {
            return false;
        }

        if (outFactor == 0d)
        {
            return false;
        }

        scale = inFactor / outFactor;
        return true;
    }

    private static bool TryResolveUnitFactor(string token, out double factor)
    {
        factor = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double numeric))
        {
            if (numeric <= 0d || double.IsInfinity(numeric) || double.IsNaN(numeric))
            {
                return false;
            }

            factor = numeric;
            return true;
        }

        if (token.Equals("mm", StringComparison.OrdinalIgnoreCase))
        {
            factor = 1e-3d;
            return true;
        }

        if (token.Equals("cm", StringComparison.OrdinalIgnoreCase))
        {
            factor = 1e-2d;
            return true;
        }

        if (token.Equals("dm", StringComparison.OrdinalIgnoreCase))
        {
            factor = 1e-1d;
            return true;
        }

        if (token.Equals("m", StringComparison.OrdinalIgnoreCase))
        {
            factor = 1d;
            return true;
        }

        if (token.Equals("km", StringComparison.OrdinalIgnoreCase))
        {
            factor = 1e3d;
            return true;
        }

        if (token.Equals("ft", StringComparison.OrdinalIgnoreCase))
        {
            factor = 0.3048d;
            return true;
        }

        if (token.Equals("rad", StringComparison.OrdinalIgnoreCase))
        {
            factor = 1d;
            return true;
        }

        if (token.Equals("deg", StringComparison.OrdinalIgnoreCase))
        {
            factor = Math.PI / 180d;
            return true;
        }

        if (token.Equals("grad", StringComparison.OrdinalIgnoreCase))
        {
            factor = Math.PI / 200d;
            return true;
        }

        return false;
    }

    private static bool TryParseAxisSwapOrder(string orderToken, out int[] order)
    {
        order = Array.Empty<int>();
        if (string.IsNullOrWhiteSpace(orderToken))
        {
            return false;
        }

        string[] segments = orderToken.Split(CommaSeparator, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2 || segments.Length > 4)
        {
            return false;
        }

        var parsed = new int[segments.Length];
        var seen = new HashSet<int>();
        for (int i = 0; i < segments.Length; i++)
        {
            if (!int.TryParse(segments[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                return false;
            }

            int axis = Math.Abs(value);
            if (axis < 1 || axis > 4 || !seen.Add(axis))
            {
                return false;
            }

            parsed[i] = value;
        }

        order = parsed;
        return true;
    }

    private static bool TryParseAxisOrder(string axisToken, out int[] order)
    {
        order = Array.Empty<int>();
        if (string.IsNullOrWhiteSpace(axisToken))
        {
            return false;
        }

        string axis = axisToken.Trim();
        if (axis.Length < 2 || axis.Length > 4)
        {
            return false;
        }

        var parsed = new int[axis.Length];
        var seen = new HashSet<int>();
        for (int i = 0; i < axis.Length; i++)
        {
            char c = char.ToLowerInvariant(axis[i]);
            int mapped;
            switch (c)
            {
                case 'e':
                    mapped = 1;
                    break;
                case 'w':
                    mapped = -1;
                    break;
                case 'n':
                    mapped = 2;
                    break;
                case 's':
                    mapped = -2;
                    break;
                case 'u':
                    mapped = 3;
                    break;
                case 'd':
                    mapped = -3;
                    break;
                default:
                    return false;
            }

            int absMapped = Math.Abs(mapped);
            if (!seen.Add(absMapped))
            {
                return false;
            }

            parsed[i] = mapped;
        }

        order = parsed;
        return true;
    }

    private static bool TrySplitPipelineSteps(string operation, out IReadOnlyList<string> steps)
    {
        var parsedSteps = new List<string>();
        var currentStepTokens = new List<string>();
        bool inPipeline = false;

        string[] tokens = operation.Split(OperationTokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            string normalized = token.Length > 0 && token[0] == '+'
                ? token.Substring(1)
                : token;

            if (normalized.Equals("proj=pipeline", StringComparison.OrdinalIgnoreCase))
            {
                inPipeline = true;
                continue;
            }

            if (normalized.Equals("step", StringComparison.OrdinalIgnoreCase))
            {
                inPipeline = true;
                if (currentStepTokens.Count > 0)
                {
                    parsedSteps.Add(string.Join(" ", currentStepTokens));
                    currentStepTokens.Clear();
                }

                continue;
            }

            if (!inPipeline)
            {
                continue;
            }

            currentStepTokens.Add(token);
        }

        if (currentStepTokens.Count > 0)
        {
            parsedSteps.Add(string.Join(" ", currentStepTokens));
        }

        steps = parsedSteps;
        return parsedSteps.Count > 0;
    }

    private static bool TryParseOperationArguments(string operation, out Dictionary<string, string> args)
    {
        args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (operation is null)
        {
            return false;
        }

        string[] tokens = operation.Split(OperationTokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            if (token.Length == 0 || token[0] != '+')
            {
                continue;
            }

            string body = token.Substring(1);
#if NETSTANDARD2_1_OR_GREATER
            int index = body.IndexOf('=', StringComparison.Ordinal);
#else
            int index = body.IndexOf('=');
#endif
            if (index < 0)
            {
                args[body] = "true";
            }
            else
            {
                string key = body.Substring(0, index);
                string value = body.Substring(index + 1);
                args[key] = value;
            }
        }

        return args.Count > 0;
    }
}
