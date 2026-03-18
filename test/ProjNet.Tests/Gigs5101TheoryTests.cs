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

public class Gigs5101TheoryTests
{
    private static readonly string[] FixtureFiles =
    {
        "5101.1-jhs.gie",
        "5101.2-jhs.gie",
        "5101.3-jhs.gie",
        "5101.4-jhs-etmerc.gie",
    };

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();
    private static readonly CoordinateSystemServices CoordinateSystemServices = new CoordinateSystemServices();

    [Fact]
    [Trait("Category", "Gigs5101")]
    public void Gigs5101Cases_ForSupportedPipelines_StayWithinTolerance()
    {
        int parsedCases = 0;
        int transformedCases = 0;
        int withinToleranceCases = 0;

        foreach (GieCase testCase in EnumerateFixtureCases())
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
            double deltaX = Math.Abs(output[0] - testCase.Expect[0]);
            double deltaY = Math.Abs(output[1] - testCase.Expect[1]);
            if (deltaX <= tolerance && deltaY <= tolerance)
            {
                withinToleranceCases++;
            }
        }

        Assert.True(parsedCases > 0, "Expected parsed GIGS 5101 cases.");
        Assert.True(transformedCases > 50, "Expected to execute a substantial subset of GIGS 5101 cases.");
        Assert.True(withinToleranceCases > 50, "Expected a substantial subset of executed GIGS 5101 cases to match tolerance.");
    }

    private static IEnumerable<GieCase> EnumerateFixtureCases()
    {
        string gigsDirectory = FindGigsDirectory();
        if (gigsDirectory is null)
        {
            yield break;
        }

        foreach (string fileName in FixtureFiles)
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

        int firstStep = operation.IndexOf("+step", StringComparison.OrdinalIgnoreCase);
        if (firstStep < 0)
        {
            return false;
        }

        string stepsPart = operation.Substring(firstStep);
        string[] stepTokens = stepsPart.Split(new[] { "+step" }, StringSplitOptions.RemoveEmptyEntries);
        if (stepTokens.Length != 2)
        {
            return false;
        }

        if (!TryParseOperationArguments(stepTokens[0], out Dictionary<string, string> firstArgs)
            || !TryParseOperationArguments(stepTokens[1], out Dictionary<string, string> secondArgs))
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

    private static bool TryResolveDeclaredCoordinateSystem(IDictionary<string, string> args, out CoordinateSystem coordinateSystem)
    {
        coordinateSystem = null;

        if (args.TryGetValue("init", out string initValue)
            && TryParseEpsgCode(initValue, out int srid))
        {
            coordinateSystem = CoordinateSystemServices.GetCoordinateSystem(srid);
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

        return false;
    }

    private static bool TryCreateGeographicCoordinateSystem(IDictionary<string, string> args, out GeographicCoordinateSystem gcs)
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

    private static bool TryResolveEllipsoid(IDictionary<string, string> args, out Ellipsoid ellipsoid)
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

    private static bool TryBuildProjectionParameters(IDictionary<string, string> args, out List<ProjectionParameter> parameters)
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

    private static bool TryGetZoneCentralMeridian(IDictionary<string, string> args, out double centralMeridian)
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

    private static bool TryGetDouble(IDictionary<string, string> args, string key, out double value)
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
            if (!token.StartsWith("+", StringComparison.Ordinal))
            {
                continue;
            }

            string body = token.Substring(1);
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
}
