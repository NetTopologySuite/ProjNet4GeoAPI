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

namespace ProjNET.Tests
{
    public class CoordinateSystemServicesTest
    {
        [Xunit.Fact]
        public void TestConstructor()
        {
            var css = new CoordinateSystemServices(new CoordinateSystemFactory(),
                new CoordinateTransformationFactory());

            Assert.IsNotNull(css.GetCoordinateSystem(4326));
            Assert.IsNotNull(css.GetCoordinateSystem(3857));
        }

        [Xunit.Fact]
        public void TestTryGetCoordinateSystemBySrid()
        {
            var css = new CoordinateSystemServices(new CoordinateSystemFactory(),
                new CoordinateTransformationFactory());

            var found = css.TryGetCoordinateSystem(4326, out var coordinateSystem);
            var missing = css.TryGetCoordinateSystem(999999, out var missingCoordinateSystem);

            Assert.IsTrue(found);
            Assert.IsNotNull(coordinateSystem);
            Assert.IsFalse(missing);
            Assert.IsNull(missingCoordinateSystem);
        }

        [Xunit.Fact]
        public void TestTryGetCoordinateSystemByAuthorityCode()
        {
            var css = new CoordinateSystemServices(new CoordinateSystemFactory(),
                new CoordinateTransformationFactory());

            var found = css.TryGetCoordinateSystem("EPSG", 3857, out var coordinateSystem);
            var missing = css.TryGetCoordinateSystem("EPSG", -1, out var missingCoordinateSystem);

            Assert.IsTrue(found);
            Assert.IsNotNull(coordinateSystem);
            Assert.IsFalse(missing);
            Assert.IsNull(missingCoordinateSystem);
        }

        [Xunit.Fact]
        public void TestGetAvailableSridValues()
        {
            var css = new CoordinateSystemServices(new CoordinateSystemFactory(),
                new CoordinateTransformationFactory());

            var srids = css.GetAvailableSridValues();
            Assert.IsNotNull(srids);
            Assert.IsTrue(Array.IndexOf(srids, 4326) >= 0);
            Assert.IsTrue(Array.IndexOf(srids, 3857) >= 0);
        }

        [Xunit.Theory]
        [Xunit.InlineData(@"D:\temp\ConsoleApplication9\SpatialRefSys.xml")]
        public void TestConstructorLoadXml(string xmlPath)
        {
            if (!File.Exists(xmlPath))
                Xunit.Assert.Skip("Specified file not found");

            var css = new CoordinateSystemServices(new CoordinateSystemFactory(),
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
                    Xunit.Assert.Skip("Specified file not found");

            var css = new CoordinateSystemServices(new CoordinateSystemFactory(),
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
                yield return new KeyValuePair<int, string>(sridWkt.WktId, sridWkt.Wkt);

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

    }
}

