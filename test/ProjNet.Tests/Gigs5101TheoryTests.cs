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

namespace ProjNET.Tests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class Gigs5101TheoryTests
{
    private static readonly string[] Fixture5101Files =
    {
        "5101.1-jhs.gie",
        "5101.2-jhs.gie",
        "5101.3-jhs.gie",
        "5101.4-jhs-etmerc.gie",
    };

    private static readonly string[] Fixture5102And5103Files =
    {
        "5102.1.gie",
        "5102.2.gie",
        "5103.1.gie",
        "5103.2.gie",
        "5103.3.gie",
    };

    private static readonly string[] Fixture5104To5113Files =
    {
        "5104.gie",
        "5105.2.gie",
        "5106.gie",
        "5107.gie",
        "5108.gie",
        "5109.gie",
        "5111.1.gie",
        "5112.gie",
        "5113.gie",
    };

    private static readonly string[] Fixture5200Files =
    {
        "5201.gie",
        "5208.gie",
    };

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();
    private static readonly CoordinateSystemServices CoordinateSystemServices = new CoordinateSystemServices();

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    [Trait("Category", "Gigs5101")]
    public void Gigs5101Cases_ForSupportedPipelines_StayWithinTolerance()
    {
        AssertFixtureCoverage(Fixture5101Files, 50, 50, "5101", requireToleranceMatch: true);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    [Trait("Category", "Gigs5102")]
    [Trait("Category", "Gigs5103")]
    public void Gigs5102And5103Cases_ForSupportedPipelines_StayWithinTolerance()
    {
        AssertFixtureCoverage(Fixture5102And5103Files, 70, 0, "5102/5103", requireToleranceMatch: false);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    [Trait("Category", "Gigs5104")]
    [Trait("Category", "Gigs5113")]
    public void Gigs5104To5113Cases_ForSupportedPipelines_Execute()
    {
        AssertFixtureCoverage(Fixture5104To5113Files, 80, 0, "5104-5113", requireToleranceMatch: false);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    [Trait("Category", "Gigs5200")]
    public void Gigs5200Cases_ForSupportedPipelines_Execute()
    {
        AssertFixtureCoverage(Fixture5200Files, 20, 0, "5200", requireToleranceMatch: false);
    }

    private static void AssertFixtureCoverage(
        IReadOnlyList<string> fixtureFiles,
        int minTransformed,
        int minWithinTolerance,
        string label,
        bool requireToleranceMatch)
    {
        int parsedCases = 0;
        int transformedCases = 0;
        int withinToleranceCases = 0;

        foreach (GieCase testCase in EnumerateFixtureCases(fixtureFiles))
        {
            parsedCases++;
            if (testCase.ExpectsFailure || testCase.Accept is null || testCase.Expect is null || testCase.Accept.Length < 2 || testCase.Expect.Length < 2)
            {
                continue;
            }

            if (!TryCreatePipelineTransform(testCase.Operation, out MathTransform transform))
            {
                continue;
            }

            double[] output;
            try
            {
                output = transform.Transform(testCase.Accept);
            }
            catch (ArgumentException)
            {
                continue;
            }

            if (output is null || output.Length < 2 || double.IsNaN(output[0]) || double.IsNaN(output[1]))
            {
                continue;
            }

            transformedCases++;
            double tolerance = Math.Max(ToNumericTolerance(testCase.ToleranceValue, testCase.ToleranceUnit), 1e-3d);
            if (LooksLikeGeographicExpect(testCase.Expect))
            {
                tolerance = tolerance / 111319.49079327358d;
            }

            double deltaX = Math.Abs(output[0] - testCase.Expect[0]);
            double deltaY = Math.Abs(output[1] - testCase.Expect[1]);
            if (deltaX <= tolerance && deltaY <= tolerance)
            {
                withinToleranceCases++;
            }
        }

        Assert.True(parsedCases > 0, "Expected parsed GIGS " + label + " cases.");
        Assert.True(
            transformedCases > minTransformed,
            "Expected to execute a substantial subset of GIGS " + label + " cases. transformed="
            + transformedCases.ToString(CultureInfo.InvariantCulture)
            + ", min="
            + minTransformed.ToString(CultureInfo.InvariantCulture)
            + ", parsed="
            + parsedCases.ToString(CultureInfo.InvariantCulture)
            + ".");
        if (requireToleranceMatch)
        {
            Assert.True(
                withinToleranceCases > minWithinTolerance,
                "Expected a substantial subset of executed GIGS " + label + " cases to match tolerance. within="
                + withinToleranceCases.ToString(CultureInfo.InvariantCulture)
                + ", min="
                + minWithinTolerance.ToString(CultureInfo.InvariantCulture)
                + ", transformed="
                + transformedCases.ToString(CultureInfo.InvariantCulture)
                + ".");
        }
    }

    private static IEnumerable<GieCase> EnumerateFixtureCases(IReadOnlyList<string> fileNames)
    {
        string gigsDirectory = FindGigsDirectory();
        if (gigsDirectory is null)
        {
            yield break;
        }

        foreach (string fileName in fileNames)
        {
            string path = Path.Combine(gigsDirectory, fileName);
            if (!File.Exists(path))
            {
                continue;
            }

            IReadOnlyList<GieCase> parsed = GieParser.ParseFile(
                path,
                new GieParserOptions
                {
                    IgnoreUnknownDirectives = true,
                    AllowOperationContinuation = true,
                });

            foreach (GieCase item in parsed)
            {
                yield return item;
            }
        }
    }

    private static bool TryCreatePipelineTransform(string operation, out MathTransform transform)
    {
        transform = null;
        if (string.IsNullOrWhiteSpace(operation))
        {
            return false;
        }

        if (!TrySplitPipelineSteps(operation, out IReadOnlyList<string> steps) || steps.Count != 2)
        {
            return false;
        }

        if (!TryParseOperationArguments(steps[0], out Dictionary<string, string> firstArgs)
            || !TryParseOperationArguments(steps[1], out Dictionary<string, string> secondArgs))
        {
            return false;
        }

        if (!firstArgs.ContainsKey("inv") || secondArgs.ContainsKey("inv"))
        {
            return false;
        }

        if (!TryResolveDeclaredCoordinateSystem(firstArgs, out CoordinateSystem source)
            || !TryResolveDeclaredCoordinateSystem(secondArgs, out CoordinateSystem target))
        {
            return false;
        }

        try
        {
            var coordinateTransformation = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target);
            transform = coordinateTransformation.MathTransform;
            return transform is not null;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.Reflection.TargetInvocationException)
        {
            return false;
        }
    }

    private static bool TrySplitPipelineSteps(string operation, out IReadOnlyList<string> steps)
    {
        var parsedSteps = new List<string>();
        var currentStepTokens = new List<string>();
        bool inPipeline = false;

        string[] tokens = operation.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            string normalized = token.StartsWith("+", StringComparison.Ordinal)
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

    private static bool TryResolveDeclaredCoordinateSystem(Dictionary<string, string> args, out CoordinateSystem coordinateSystem)
    {
        coordinateSystem = null;

        if (args.TryGetValue("init", out string initValue)
            && TryParseEpsgCode(initValue, out int srid))
        {
            coordinateSystem = CoordinateSystemServices.GetCoordinateSystem(srid);
            coordinateSystem = NormalizeAxisOrder(coordinateSystem);
            return coordinateSystem is not null;
        }

        if (!args.TryGetValue("proj", out string projCode))
        {
            return false;
        }

        if (!TryMapProjectionClass(projCode, out string projectionClassName))
        {
            return false;
        }

        if (!TryCreateGeographicCoordinateSystem(args, out GeographicCoordinateSystem geographicCoordinateSystem))
        {
            return false;
        }

        if (!TryBuildProjectionParameters(args, out List<ProjectionParameter> parameters))
        {
            return false;
        }

        try
        {
            var projection = CoordinateSystemFactory.CreateProjection("GIGS " + projectionClassName, projectionClassName, parameters);
            coordinateSystem = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
                "GIGS projected",
                geographicCoordinateSystem,
                projection,
                LinearUnit.Metre,
                new AxisInfo("East", AxisOrientationEnum.East),
                new AxisInfo("North", AxisOrientationEnum.North));
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
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.Reflection.TargetInvocationException)
        {
            return false;
        }
    }

    private static bool TryParseEpsgCode(string token, out int srid)
    {
        srid = 0;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string value = token.Trim();
        const string epsgPrefix = "epsg:";
        if (value.StartsWith(epsgPrefix, StringComparison.OrdinalIgnoreCase))
        {
            value = value.Substring(epsgPrefix.Length);
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out srid);
    }

    private static CoordinateSystem NormalizeAxisOrder(CoordinateSystem coordinateSystem)
    {
        if (coordinateSystem is null)
        {
            return null;
        }

        var geographic = coordinateSystem as GeographicCoordinateSystem;
        if (geographic is not null)
        {
            return CoordinateSystemFactory.CreateGeographicCoordinateSystem(
                geographic.Name,
                geographic.AngularUnit,
                geographic.HorizontalDatum,
                geographic.PrimeMeridian,
                new AxisInfo("Lon", AxisOrientationEnum.East),
                new AxisInfo("Lat", AxisOrientationEnum.North));
        }

        var projected = coordinateSystem as ProjectedCoordinateSystem;
        if (projected is not null)
        {
            CoordinateSystem normalizedGeographic = NormalizeAxisOrder(projected.GeographicCoordinateSystem);
            var normalizedProjectedGeographic = (GeographicCoordinateSystem)normalizedGeographic;
            return CoordinateSystemFactory.CreateProjectedCoordinateSystem(
                projected.Name,
                normalizedProjectedGeographic,
                projected.Projection,
                projected.LinearUnit,
                new AxisInfo("East", AxisOrientationEnum.East),
                new AxisInfo("North", AxisOrientationEnum.North));
        }

        return coordinateSystem;
    }

    private static bool TryMapProjectionClass(string projCode, out string projectionClassName)
    {
        projectionClassName = null;
        if (projCode is null)
        {
            return false;
        }

        if (projCode.Equals("etmerc", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("tmerc", StringComparison.OrdinalIgnoreCase))
        {
            projectionClassName = "transverse_mercator";
            return true;
        }

        if (projCode.Equals("utm", StringComparison.OrdinalIgnoreCase))
        {
            projectionClassName = "utm";
            return true;
        }

        if (projCode.Equals("poly", StringComparison.OrdinalIgnoreCase))
        {
            projectionClassName = "polyconic";
            return true;
        }

        return false;
    }

    private static bool TryCreateGeographicCoordinateSystem(Dictionary<string, string> args, out GeographicCoordinateSystem gcs)
    {
        gcs = null;

        if (!TryResolveEllipsoid(args, out Ellipsoid ellipsoid))
        {
            return false;
        }

        var datum = CoordinateSystemFactory.CreateHorizontalDatum("GIGS datum", DatumType.HD_Geocentric, ellipsoid, null);
        gcs = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "GIGS geographic",
            AngularUnit.Degrees,
            datum,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        return true;
    }

    private static bool TryResolveEllipsoid(Dictionary<string, string> args, out Ellipsoid ellipsoid)
    {
        ellipsoid = null;
        if (args.TryGetValue("ellps", out string ellps))
        {
            if (ellps.Equals("wgs84", StringComparison.OrdinalIgnoreCase))
            {
                ellipsoid = Ellipsoid.WGS84;
                return true;
            }

            if (ellps.Equals("grs80", StringComparison.OrdinalIgnoreCase))
            {
                ellipsoid = Ellipsoid.GRS80;
                return true;
            }
        }

        ellipsoid = Ellipsoid.WGS84;
        return true;
    }

    private static bool TryBuildProjectionParameters(Dictionary<string, string> args, out List<ProjectionParameter> parameters)
    {
        parameters = new List<ProjectionParameter>
        {
            new ProjectionParameter("latitude_of_origin", 0d),
            new ProjectionParameter("central_meridian", 0d),
            new ProjectionParameter("scale_factor", 1d),
            new ProjectionParameter("false_easting", 0d),
            new ProjectionParameter("false_northing", 0d),
        };

        if (TryGetDouble(args, "lat_0", out double lat0))
        {
            ReplaceParameter(parameters, "latitude_of_origin", lat0);
        }

        if (TryGetDouble(args, "lon_0", out double lon0))
        {
            ReplaceParameter(parameters, "central_meridian", lon0);
        }

        if (TryGetDouble(args, "k_0", out double k0))
        {
            ReplaceParameter(parameters, "scale_factor", k0);
        }
        else if (TryGetDouble(args, "k", out double k))
        {
            ReplaceParameter(parameters, "scale_factor", k);
        }

        if (TryGetDouble(args, "x_0", out double x0))
        {
            ReplaceParameter(parameters, "false_easting", x0);
        }

        if (TryGetDouble(args, "y_0", out double y0))
        {
            ReplaceParameter(parameters, "false_northing", y0);
        }

        if (args.TryGetValue("proj", out string projCode) && projCode.Equals("utm", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryGetZoneCentralMeridian(args, out double utmCentralMeridian))
            {
                return false;
            }

            ReplaceParameter(parameters, "latitude_of_origin", 0d);
            ReplaceParameter(parameters, "central_meridian", utmCentralMeridian);
            ReplaceParameter(parameters, "scale_factor", 0.9996d);
            ReplaceParameter(parameters, "false_easting", 500000d);
            ReplaceParameter(parameters, "false_northing", args.ContainsKey("south") ? 10000000d : 0d);
        }

        return true;
    }

    private static bool TryGetZoneCentralMeridian(Dictionary<string, string> args, out double centralMeridian)
    {
        centralMeridian = 0d;
        if (!args.TryGetValue("zone", out string zoneToken) || string.IsNullOrWhiteSpace(zoneToken))
        {
            return false;
        }

        string digits = zoneToken.Trim();
        int i = 0;
        while (i < digits.Length && char.IsDigit(digits[i]))
        {
            i++;
        }

        if (i == 0 || !int.TryParse(digits.Substring(0, i), NumberStyles.Integer, CultureInfo.InvariantCulture, out int zone))
        {
            return false;
        }

        centralMeridian = (zone * 6d) - 183d;
        return true;
    }

    private static bool TryGetDouble(Dictionary<string, string> args, string key, out double value)
    {
        value = 0d;
        if (!args.TryGetValue(key, out string raw) || string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
    }

    private static void ReplaceParameter(List<ProjectionParameter> parameters, string name, double value)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                parameters[i] = new ProjectionParameter(name, value);
                return;
            }
        }

        parameters.Add(new ProjectionParameter(name, value));
    }

    private static bool TryParseOperationArguments(string operation, out Dictionary<string, string> args)
    {
        args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (operation is null)
        {
            return false;
        }

        string[] tokens = operation.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            string body = token.StartsWith("+", StringComparison.Ordinal)
                ? token.Substring(1)
                : token;

            if (body.Length == 0
                || body.Equals("step", StringComparison.OrdinalIgnoreCase)
                || body.Equals("proj=pipeline", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int index = body.IndexOf('=');
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

    private static string FindGigsDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "spec", "PROJ", "test", "gigs");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    private static double ToNumericTolerance(double value, string unit)
    {
        if (unit is null)
        {
            return value;
        }

        if (unit.Equals("mm", StringComparison.OrdinalIgnoreCase))
        {
            return value / 1000d;
        }

        if (unit.Equals("cm", StringComparison.OrdinalIgnoreCase))
        {
            return value / 100d;
        }

        if (unit.Equals("nm", StringComparison.OrdinalIgnoreCase))
        {
            return value * 1e-9d;
        }

        return value;
    }

    private static bool LooksLikeGeographicExpect(double[] expect)
    {
        if (expect is null || expect.Length < 2)
        {
            return false;
        }

        return Math.Abs(expect[0]) <= 360d && Math.Abs(expect[1]) <= 90d;
    }
}
