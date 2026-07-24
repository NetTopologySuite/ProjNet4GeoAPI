// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;

/// <summary>
/// Creates runtime math transforms from PROJ-style pipeline operation strings.
/// </summary>
internal static partial class ProjPipelineMathTransformFactory
{
    private static bool TryCreateNoOpStepTransform(
        Dictionary<string, string> args,
        PipelineExecutionContext? executionContext,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = new IdentityMathTransform(3);
        skipReason = null;
        return true;
    }

    private static bool TryCreatePushStepTransform(
        Dictionary<string, string> args,
        PipelineExecutionContext? executionContext,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        if (executionContext is null)
        {
            skipReason = "push operation requires a pipeline execution context.";
            return false;
        }

        return PipelineStackTransferMathTransform.TryCreatePush(args, executionContext, out transform, out skipReason);
    }

    private static bool TryCreatePopStepTransform(
        Dictionary<string, string> args,
        PipelineExecutionContext? executionContext,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        if (executionContext is null)
        {
            skipReason = "pop operation requires a pipeline execution context.";
            return false;
        }

        return PipelineStackTransferMathTransform.TryCreatePop(args, executionContext, out transform, out skipReason);
    }

    private static bool TryCreateHGridShiftStepTransform(
        Dictionary<string, string> args,
        PipelineExecutionContext? executionContext,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        return TryCreateHorizontalGridShiftTransform(
            args,
            useGridMetadataInterpolation: false,
            allowBiquadraticInterpolation: false,
            out transform,
            out skipReason);
    }

    private static bool TryCreateGridShiftStepTransform(
        Dictionary<string, string> args,
        PipelineExecutionContext? executionContext,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        return TryCreateHorizontalGridShiftTransform(
            args,
            useGridMetadataInterpolation: true,
            allowBiquadraticInterpolation: true,
            out transform,
            out skipReason);
    }

    private static bool TryCreateAxisSwapTransform(
        Dictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        skipReason = null;

        bool hasOrder = args.TryGetValue("order", out string? orderTokenCandidate) && !string.IsNullOrWhiteSpace(orderTokenCandidate);
        bool hasAxis = args.TryGetValue("axis", out string? axisTokenCandidate) && !string.IsNullOrWhiteSpace(axisTokenCandidate);
        if (hasOrder == hasAxis)
        {
            skipReason = "Axisswap requires exactly one of +order or +axis.";
            return false;
        }

        int[] order;
        if (hasOrder)
        {
            string orderToken = ArgumentGuard.ThrowIfNull(orderTokenCandidate, nameof(orderTokenCandidate));
            if (!TryParseAxisSwapOrder(orderToken, out order))
            {
                skipReason = "Unable to parse +order parameter for axisswap.";
                return false;
            }
        }
        else
        {
            string axisToken = ArgumentGuard.ThrowIfNull(axisTokenCandidate, nameof(axisTokenCandidate));
            if (!TryParseAxisOrder(axisToken, out order))
            {
                skipReason = "Unable to parse +axis parameter for axisswap.";
                return false;
            }
        }

        int dimension = order.Length;
        if (dimension is < 2 or > 4)
        {
            skipReason = "Axisswap supports only 2D, 3D or 4D coordinates in the current runtime.";
            return false;
        }

        int[] sourceIndices = [0, 1, 2, 3];
        int[] signs = [1, 1, 1, 1];
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
            signs[2],
            sourceIndices[3],
            signs[3]);

        return true;
    }

    private static bool TryCreateUnitConvertTransform(
        IDictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
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

    private static bool TryCreateGeocentricCartesianTransform(
        Dictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        if (!ProjEllipsoidResolver.TryResolveRequiredEllipsoidWithOverrides(
            args,
            operationName: "geocent/cart",
            allowClarke1880Ign: true,
            allowBessel: true,
            out double semiMajor,
            out double semiMinor,
            out skipReason))
        {
            return false;
        }

        if (!TryResolveGeocentricScale(args, out double toMeterScale, out skipReason))
        {
            return false;
        }

        // PROJ cart/geocent applies to_meter to cartesian output units. Keep internal
        // ellipsoid units aligned by scaling axis values accordingly.
        semiMajor /= toMeterScale;
        semiMinor /= toMeterScale;
        if (semiMajor <= 0d || semiMinor <= 0d || double.IsNaN(semiMajor) || double.IsInfinity(semiMajor) || double.IsNaN(semiMinor) || double.IsInfinity(semiMinor))
        {
            skipReason = "geocent/cart resolved ellipsoid axes must be finite and positive.";
            return false;
        }

        var geocentricParameters = new List<ProjectionParameter>(2)
        {
            new("semi_major", semiMajor),
            new("semi_minor", semiMinor),
        };

        transform = new GeocentricTransform(geocentricParameters, false);
        if (args.ContainsKey("inv"))
        {
            transform = transform.Inverse();
        }

        return true;
    }

    private static bool TryCreateGeocentricLatitudeTransform(
        Dictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        if (!ProjEllipsoidResolver.TryResolveRequiredEllipsoidWithOverrides(
            args,
            operationName: "geoc",
            allowClarke1880Ign: true,
            allowBessel: true,
            out double semiMajor,
            out double semiMinor,
            out skipReason))
        {
            return false;
        }

        transform = new GeocentricLatitudeMathTransform(semiMajor, semiMinor, args.ContainsKey("inv"));
        return true;
    }

    private static bool TryCreateProjectionStepTransform(
        Dictionary<string, string> args,
        string projCode,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        if (!TryBuildProjectionStepParameters(args, projCode, out List<ProjectionParameter>? parametersCandidate, out skipReason))
        {
            return false;
        }

        List<ProjectionParameter> parameters = ArgumentGuard.ThrowIfNull(parametersCandidate, nameof(parametersCandidate));
        string projectionImplementationCode = ResolveProjectionImplementationCode(args, projCode);

        if (RequiresDatumAwareCrsStep(args))
        {
            if (!TryCreateDatumAwareProjectedStepTransform(args, projectionImplementationCode, parameters, out transform, out skipReason))
            {
                return false;
            }
        }
        else
        {
            try
            {
                transform = ProjectionsRegistry.CreateProjection(projectionImplementationCode, parameters);
            }
            catch (NotSupportedException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                skipReason = $"{projCode} projection could not be created with the parsed parameter set.";
                return false;
            }
            catch (InvalidOperationException)
            {
                skipReason = $"{projCode} projection operation could not be constructed for this step.";
                return false;
            }
            catch (TargetInvocationException)
            {
                skipReason = $"{projCode} projection constructor rejected the current parameter set.";
                return false;
            }
        }

        if (!TryApplyOptionalVerticalUnitScale(args, ArgumentGuard.ThrowIfNull(transform, nameof(transform)), out MathTransform? verticallyScaledTransform, out skipReason))
        {
            transform = null;
            return false;
        }

        MathTransform currentTransform = ArgumentGuard.ThrowIfNull(verticallyScaledTransform, nameof(verticallyScaledTransform));

        if (args.ContainsKey("inv"))
        {
            currentTransform = currentTransform.Inverse();
        }

        transform = currentTransform;
        return true;
    }

    private static string ResolveProjectionImplementationCode(
        Dictionary<string, string> args,
        string projCode)
    {
        if (args.ContainsKey("approx")
            && (projCode.Equals("utm", StringComparison.OrdinalIgnoreCase)
                || projCode.Equals("tmerc", StringComparison.OrdinalIgnoreCase)
                || projCode.Equals("transverse_mercator", StringComparison.OrdinalIgnoreCase)
                || projCode.Equals("gauss_kruger", StringComparison.OrdinalIgnoreCase)
                || projCode.Equals("transverse_mercator_south_oriented", StringComparison.OrdinalIgnoreCase)))
        {
            // PROJ's +approx flag opts the transverse Mercator family back into
            // the classic Evenden/Snyder implementation.
            return "approx_tmerc";
        }

        if (projCode.Equals("utm", StringComparison.OrdinalIgnoreCase) && !args.ContainsKey("approx"))
        {
            // PROJ routes UTM through the exact Poder/Engsager transverse Mercator kernel
            // unless the caller opts back into the approximate Snyder path with +approx.
            return "etmerc";
        }

        return projCode;
    }

    private static bool RequiresDatumAwareCrsStep(Dictionary<string, string> args)
    {
        return args.ContainsKey("towgs84") || args.ContainsKey("datum");
    }

    private static bool TryCreateDatumAwareProjectedStepTransform(
        Dictionary<string, string> args,
        string projectionImplementationCode,
        List<ProjectionParameter> parameters,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (!TryResolveProjectionEllipsoid(args, out double semiMajor, out double semiMinor, out skipReason))
        {
            return false;
        }

        if (!TryResolveProjectionUnitFactor(args, out double unitFactor, out skipReason))
        {
            return false;
        }

        if (!TryResolveDatumToWgs84Parameters(args, out Wgs84ConversionInfo? toWgs84, out skipReason))
        {
            return false;
        }

        var csFactory = new CoordinateSystemFactory();
        GeographicCoordinateSystem localGeographic = CreatePipelineGeographicCoordinateSystem(csFactory, semiMajor, semiMinor, toWgs84);

        IProjection projection = csFactory.CreateProjection("PROJ pipeline projection", projectionImplementationCode, parameters);
        LinearUnit linearUnit = CreateProjectionLinearUnit(unitFactor);
        ProjectedCoordinateSystem projected = csFactory.CreateProjectedCoordinateSystem(
            "PROJ pipeline projected",
            localGeographic,
            projection,
            linearUnit,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        GeographicCoordinateSystem wgs84Geographic = GeographicCoordinateSystem.WGS84;

        try
        {
            transform = new CoordinateTransformationFactory().CreateFromCoordinateSystems(wgs84Geographic, projected).MathTransform;
            return true;
        }
        catch (ArgumentException)
        {
            skipReason = $"{projectionImplementationCode} projected datum step could not be created with the parsed parameter set.";
            return false;
        }
        catch (NotSupportedException)
        {
            skipReason = $"{projectionImplementationCode} projected datum step is not supported by the current runtime.";
            return false;
        }
        catch (InvalidOperationException)
        {
            skipReason = $"{projectionImplementationCode} projected datum step could not be constructed for this step.";
            return false;
        }
    }

    private static GeographicCoordinateSystem CreatePipelineGeographicCoordinateSystem(
        CoordinateSystemFactory csFactory,
        double semiMajor,
        double semiMinor,
        Wgs84ConversionInfo? toWgs84)
    {
        Ellipsoid ellipsoid = csFactory.CreateEllipsoid("PROJ pipeline ellipsoid", semiMajor, semiMinor, LinearUnit.Metre);
        HorizontalDatum localDatum = csFactory.CreateHorizontalDatum("PROJ pipeline datum", DatumType.HD_Geocentric, ellipsoid, toWgs84);
        return csFactory.CreateGeographicCoordinateSystem(
            "PROJ pipeline geographic",
            AngularUnit.Degrees,
            localDatum,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
    }

    private static bool TryCreateGeographicIdentityTransform(
        Dictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        skipReason = null;
        var transforms = new List<MathTransform>(4);

        if (RequiresDatumAwareCrsStep(args))
        {
            if (!TryCreateDatumAwareGeographicStepTransform(args, out MathTransform? datumAwareTransform, out skipReason))
            {
                transform = null;
                return false;
            }

            transforms.Add(ArgumentGuard.ThrowIfNull(datumAwareTransform, nameof(datumAwareTransform)));
        }
        else
        {
            transforms.Add(new IdentityMathTransform(3));
        }

        if (args.ContainsKey("geoc"))
        {
            if (!ProjEllipsoidResolver.TryResolveRequiredEllipsoidWithOverrides(
                args,
                operationName: "geoc",
                allowClarke1880Ign: true,
                allowBessel: true,
                out double semiMajor,
                out double semiMinor,
                out skipReason))
            {
                transform = null;
                return false;
            }

            // PROJ's legacy +proj=longlat/+proj=latlong +geoc flag behaves like the
            // inverse of the dedicated geoc step, and +inv toggles it back again.
            transforms.Add(new GeocentricLatitudeMathTransform(semiMajor, semiMinor, isInverse: true));
        }

        if (args.TryGetValue("pm", out string? pmToken) && !string.IsNullOrWhiteSpace(pmToken))
        {
            if (!TryResolveProjPrimeMeridianLongitudeDegrees(pmToken, out double primeMeridianLongitudeDegrees))
            {
                skipReason = "Unable to parse +pm value for geographic identity step.";
                transform = null;
                return false;
            }

            var customPrimeMeridian = new PrimeMeridian(
                primeMeridianLongitudeDegrees,
                AngularUnit.Degrees,
                "PROJ pipeline pm",
                string.Empty,
                -1,
                string.Empty,
                string.Empty,
                string.Empty);
            transforms.Add(new PrimeMeridianTransform(PrimeMeridian.Greenwich, customPrimeMeridian));
        }

        if (args.TryGetValue("lon_wrap", out string? lonWrapToken) && !string.IsNullOrWhiteSpace(lonWrapToken))
        {
            if (!SpanParseUtility.TryParseFiniteDouble(lonWrapToken, out double wrapCenterDegrees))
            {
                skipReason = "Unable to parse +lon_wrap value for geographic identity step.";
                transform = null;
                return false;
            }

            transforms.Add(new LongitudeWrapMathTransform(wrapCenterDegrees));
        }

        if (!TryResolveVerticalUnitFactor(args, out double verticalUnitFactor, out skipReason))
        {
            transform = null;
            return false;
        }

        if (!verticalUnitFactor.Equals(1d))
        {
            transforms.Add(new UnitConvertMathTransform(3, 1d, 1d / verticalUnitFactor));
        }

        MathTransform currentTransform = transforms.Count == 1
            ? transforms[0]
            : new CompositeMathTransform(transforms);

        if (args.ContainsKey("inv"))
        {
            currentTransform = currentTransform.Inverse();
        }

        transform = currentTransform;
        return true;
    }

    private static bool TryCreateDatumAwareGeographicStepTransform(
        Dictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (!TryResolveProjectionEllipsoid(args, out double semiMajor, out double semiMinor, out skipReason))
        {
            return false;
        }

        if (!TryResolveDatumToWgs84Parameters(args, out Wgs84ConversionInfo? toWgs84, out skipReason))
        {
            return false;
        }

        var csFactory = new CoordinateSystemFactory();
        GeographicCoordinateSystem localGeographic = CreatePipelineGeographicCoordinateSystem(csFactory, semiMajor, semiMinor, toWgs84);
        GeographicCoordinateSystem wgs84Geographic = GeographicCoordinateSystem.WGS84;

        try
        {
            transform = new CoordinateTransformationFactory().CreateFromCoordinateSystems(wgs84Geographic, localGeographic).MathTransform;
            return true;
        }
        catch (ArgumentException)
        {
            skipReason = "Geographic datum step could not be created with the parsed parameter set.";
            return false;
        }
        catch (NotSupportedException)
        {
            skipReason = "Geographic datum step is not supported by the current runtime.";
            return false;
        }
        catch (InvalidOperationException)
        {
            skipReason = "Geographic datum step could not be constructed for this step.";
            return false;
        }
    }

    private static bool TryApplyOptionalVerticalUnitScale(
        Dictionary<string, string> args,
        MathTransform transform,
        [NotNullWhen(true)] out MathTransform? result,
        out string? skipReason)
    {
        if (!TryResolveVerticalUnitFactor(args, out double verticalUnitFactor, out skipReason))
        {
            result = null;
            return false;
        }

        if (verticalUnitFactor.Equals(1d))
        {
            result = transform;
            return true;
        }

        result = new CompositeMathTransform(
        [
            transform,
            new UnitConvertMathTransform(3, 1d, 1d / verticalUnitFactor),
        ]);
        return true;
    }

    private static LinearUnit CreateProjectionLinearUnit(double unitFactor)
    {
        return unitFactor.Equals(LinearUnit.Metre.MetersPerUnit)
            ? LinearUnit.Metre
            : new LinearUnit(unitFactor, "PROJ pipeline unit", string.Empty, -1, string.Empty, string.Empty, string.Empty);
    }

    private static bool TryCreateHorizontalGridShiftTransform(
        Dictionary<string, string> args,
        bool useGridMetadataInterpolation,
        bool allowBiquadraticInterpolation,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        if (!args.TryGetValue("grids", out string? gridsToken) || string.IsNullOrWhiteSpace(gridsToken))
        {
            skipReason = "Horizontal grid shift requires +grids.";
            return false;
        }

        if (!TryResolveGridPaths(gridsToken, out IReadOnlyList<string>? gridPathsCandidate, out skipReason))
        {
            return false;
        }

        IReadOnlyList<string> gridPaths = ArgumentGuard.ThrowIfNull(gridPathsCandidate, nameof(gridPathsCandidate));

        if (!TryValidateGridExtensions(gridPaths, HorizontalGridExtensions, "horizontal", out skipReason))
        {
            return false;
        }

        try
        {
            bool hasGeoTiff = ContainsGeoTiffGrid(gridPaths);
            if (hasGeoTiff)
            {
                if (!TryResolveGeoTiffHorizontalInterpolationOverride(
                    args,
                    useGridMetadataInterpolation,
                    allowBiquadraticInterpolation,
                    out bool? biquadraticInterpolationOverride,
                    out skipReason))
                {
                    return false;
                }

                transform = new GeoTiffHGridShiftMathTransform(gridPaths, biquadraticInterpolationOverride);
            }
            else
            {
                if (!TryValidateNtv2HorizontalInterpolation(args, allowBiquadraticInterpolation, out skipReason))
                {
                    return false;
                }

                transform = new Ntv2HGridShiftMathTransform(gridPaths);
            }
        }
        catch (IOException ioException)
        {
            skipReason = $"Unable to read horizontal grid: {ioException.Message}";
            return false;
        }
        catch (InvalidDataException dataException)
        {
            skipReason = $"Invalid horizontal grid data: {dataException.Message}";
            return false;
        }
        catch (ArgumentException argumentException)
        {
            skipReason = $"Invalid grid parameters: {argumentException.Message}";
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
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        if (!args.TryGetValue("grids", out string? gridsToken) || string.IsNullOrWhiteSpace(gridsToken))
        {
            skipReason = "Vertical grid shift requires +grids.";
            return false;
        }

        if (!TryResolveGridPaths(gridsToken, out IReadOnlyList<string>? gridPathsCandidate, out skipReason))
        {
            return false;
        }

        IReadOnlyList<string> gridPaths = ArgumentGuard.ThrowIfNull(gridPathsCandidate, nameof(gridPathsCandidate));

        if (!TryValidateGridExtensions(gridPaths, VerticalGridExtensions, "vertical", out skipReason))
        {
            return false;
        }

        double multiplier = -1d;
        if (args.TryGetValue("multiplier", out string? multiplierToken)
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
                ? new GeoTiffVGridShiftMathTransform(gridPaths, multiplier)
                : new GtxVGridShiftMathTransform(gridPaths, multiplier);
        }
        catch (IOException ioException)
        {
            skipReason = $"Unable to read vertical grid: {ioException.Message}";
            return false;
        }
        catch (InvalidDataException dataException)
        {
            skipReason = $"Invalid vertical grid data: {dataException.Message}";
            return false;
        }
        catch (ArgumentException argumentException)
        {
            skipReason = $"Invalid grid parameters: {argumentException.Message}";
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
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        if (!args.TryGetValue("grids", out string? gridsToken) || string.IsNullOrWhiteSpace(gridsToken))
        {
            skipReason = "Geocentric grid shift requires +grids.";
            return false;
        }

        if (!TryResolveGridPaths(gridsToken, out IReadOnlyList<string>? gridPathsCandidate, out skipReason))
        {
            return false;
        }

        IReadOnlyList<string> gridPaths = ArgumentGuard.ThrowIfNull(gridPathsCandidate, nameof(gridPathsCandidate));

        if (!TryValidateGridExtensions(gridPaths, XyzGridExtensions, "xyz", out skipReason))
        {
            return false;
        }

        bool gridRefIsInput = true;
        if (args.TryGetValue("grid_ref", out string? gridRefToken) && !string.IsNullOrWhiteSpace(gridRefToken))
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
        if (args.TryGetValue("multiplier", out string? multiplierToken)
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

        if (!ProjEllipsoidResolver.TryResolveRequiredEllipsoidWithOverrides(
            args,
            operationName: "xyzgridshift",
            allowClarke1880Ign: true,
            allowBessel: true,
            out double semiMajor,
            out double semiMinor,
            out skipReason))
        {
            return false;
        }

        try
        {
            transform = new GeoTiffXyzGridShiftMathTransform(gridPaths, semiMajor, semiMinor, multiplier, gridRefIsInput);
        }
        catch (IOException ioException)
        {
            skipReason = $"Unable to read xyz grid: {ioException.Message}";
            return false;
        }
        catch (InvalidDataException dataException)
        {
            skipReason = $"Invalid xyz grid data: {dataException.Message}";
            return false;
        }
        catch (ArgumentException argumentException)
        {
            skipReason = $"Invalid xyz grid parameters: {argumentException.Message}";
            return false;
        }

        if (args.ContainsKey("inv"))
        {
            transform = transform.Inverse();
        }

        return true;
    }
}
