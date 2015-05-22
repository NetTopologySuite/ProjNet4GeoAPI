using System.IO;
using System.Text;
using NUnit.Framework;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace ProjNet
{
    public class CoordinateSystemServicesTest
    {
        [Test]
        public void TestConstructor()
        {
            var css = new CoordinateSystemServices(new CoordinateSystemFactory(Encoding.UTF8),
                new CoordinateTransformationFactory());

            css = new CoordinateSystemServices(new CoordinateSystemFactory(Encoding.UTF8),
                new CoordinateTransformationFactory(), CoordinateSystemServices.DefaultInitialization);


            Assert.IsNotNull(css.GetCoordinateSystem(4326));

        }

        [Test]
        public void TestConstructorLoadXml()
        {
            const string xmlPath = @"D:\temp\ConsoleApplication9\SpatialRefSys.xml";
            if (!File.Exists(xmlPath))
                throw new IgnoreException("Specified file not found");

            var css = new CoordinateSystemServices(new CoordinateSystemFactory(Encoding.UTF8),
                new CoordinateTransformationFactory(), CoordinateSystemServices.LoadXml, System.IO.File.OpenRead(xmlPath));

            Assert.IsNotNull(css.GetCoordinateSystem(4326));
            Assert.IsNotNull(css.GetCoordinateSystem("EPSG", 4326));
            Assert.IsTrue(ReferenceEquals(css.GetCoordinateSystem("EPSG", 4326), css.GetCoordinateSystem(4326)));

        }


    }
}