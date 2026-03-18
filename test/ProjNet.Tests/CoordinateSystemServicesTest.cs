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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using NUnit.Framework;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Data;

public class CoordinateSystemServicesTest
{
    [Xunit.Fact]
    public void TestConstructor()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());

        Assert.IsNotNull(css.GetCoordinateSystem(4326));
        Assert.IsNotNull(css.GetCoordinateSystem(3857));
    }

    [Xunit.Fact]
    public void TestTryGetCoordinateSystemBySrid()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());

        bool found = css.TryGetCoordinateSystem(4326, out var coordinateSystem);
        bool missing = css.TryGetCoordinateSystem(999999, out var missingCoordinateSystem);

        Assert.IsTrue(found);
        Assert.IsNotNull(coordinateSystem);
        Assert.IsFalse(missing);
        Assert.IsNull(missingCoordinateSystem);
    }

    [Xunit.Fact]
    public void TestTryGetCoordinateSystemByAuthorityCode()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());

        bool found = css.TryGetCoordinateSystem("EPSG", 3857, out var coordinateSystem);
        bool missing = css.TryGetCoordinateSystem("EPSG", -1, out var missingCoordinateSystem);

        Assert.IsTrue(found);
        Assert.IsNotNull(coordinateSystem);
        Assert.IsFalse(missing);
        Assert.IsNull(missingCoordinateSystem);
    }

    [Xunit.Fact]
    public void TestGetAvailableSridValues()
    {
        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory());

        int[] srids = css.GetAvailableSridValues();
        Assert.IsNotNull(srids);
        Assert.IsTrue(Array.IndexOf(srids, 4326) >= 0);
        Assert.IsTrue(Array.IndexOf(srids, 3857) >= 0);
    }

    [Xunit.Fact]
    public void TestManagedProviderIncludesFullGeneratedCatalog()
    {
        var provider = new ManagedCoordinateSystemDefinitionProvider();
        var definitions = provider.GetDefinitions().ToList();

        Assert.IsTrue(definitions.Count > 7000);
        Assert.IsTrue(definitions.Any(item => item.Key == 4326));
        Assert.IsTrue(definitions.Any(item => item.Key == 3857));
    }

    [Xunit.Fact]
    public void TestManagedObjectProviderBypassesWktParsing()
    {
        var provider = new TestManagedProvider();
        var css = new CoordinateSystemServices(provider);

        Assert.IsNotNull(css.GetCoordinateSystem(4326));
        Assert.IsNotNull(css.GetCoordinateSystem(3857));
    }

    [Xunit.Theory]
    [Xunit.InlineData(@"D:\temp\ConsoleApplication9\SpatialRefSys.xml")]
    public void TestConstructorLoadXml(string xmlPath)
    {
        if (!File.Exists(xmlPath))
        {
            Xunit.Assert.Skip("Specified file not found");
        }

        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory(), LoadXml(xmlPath));

        Assert.IsNotNull(css.GetCoordinateSystem(4326));
        Assert.IsNotNull(css.GetCoordinateSystem("EPSG", 4326));
        Assert.IsTrue(ReferenceEquals(css.GetCoordinateSystem("EPSG", 4326), css.GetCoordinateSystem(4326)));

    }

    [Xunit.Theory]
    [Xunit.InlineData(@"")]
    public void TestConstructorLoadCsv(string csvPath)
    {
        if (!string.IsNullOrWhiteSpace(csvPath))
            if (!File.Exists(csvPath))
            {
                Xunit.Assert.Skip("Specified file not found");
            }

        var css = new CoordinateSystemServices(
            new CoordinateSystemFactory(),
            new CoordinateTransformationFactory(), LoadCsv(csvPath));

        Assert.IsNotNull(css.GetCoordinateSystem(4326));
        Assert.IsNotNull(css.GetCoordinateSystem("EPSG", 4326));
        Assert.IsTrue(ReferenceEquals(css.GetCoordinateSystem("EPSG", 4326), css.GetCoordinateSystem(4326)));
        Thread.Sleep(1000);

    }

    internal static IEnumerable<KeyValuePair<int, string>> LoadCsv(string csvPath = null)
    {

        Console.WriteLine("Reading '{0}'.", csvPath ?? "SRID.csv from resources stream");
        var sw = new Stopwatch();
        sw.Start();

        foreach (var sridWkt in SRIDReader.GetSrids(csvPath))
        {
            yield return new KeyValuePair<int, string>(sridWkt.WktId, sridWkt.Wkt);
        }

        sw.Stop();
        Console.WriteLine("Read '{1}' in {0:N0}ms", sw.ElapsedMilliseconds, csvPath ?? "SRID.csv from resources stream");
    }

    private static IEnumerable<KeyValuePair<int, string>> LoadXml(string xmlPath)
    {
        var stream = System.IO.File.OpenRead(xmlPath);

        Console.WriteLine("Reading '{0}'.", xmlPath);
        var sw = new Stopwatch();
        sw.Start();

        var document = XDocument.Load(stream);

        var rs = from tmp in document.Elements("SpatialReference").Elements("ReferenceSystem") select tmp;

        foreach (var node in rs)
        {
            var sridElement = node.Element("SRID");
            if (sridElement != null)
            {
                int srid = int.Parse(sridElement.Value);
                yield return new KeyValuePair<int, string>(srid, node.LastNode.ToString());
            }
        }

        sw.Stop();
        Console.WriteLine("Read '{1}' in {0:N0}ms", sw.ElapsedMilliseconds, xmlPath);
    }

    private sealed class TestManagedProvider : ICoordinateSystemDefinitionProvider, IManagedCoordinateSystemProvider
    {
        public IEnumerable<KeyValuePair<int, CoordinateSystem>> GetCoordinateSystems()
        {
            yield return new KeyValuePair<int, CoordinateSystem>(4326, GeographicCoordinateSystem.WGS84);
            yield return new KeyValuePair<int, CoordinateSystem>(3857, ProjectedCoordinateSystem.WebMercator);
        }

        public IEnumerable<KeyValuePair<int, string>> GetDefinitions()
        {
            yield return new KeyValuePair<int, string>(4326, "INVALID_WKT_SHOULD_NOT_BE_USED");
        }
    }
}
