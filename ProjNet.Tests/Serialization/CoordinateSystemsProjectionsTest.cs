using NUnit.Framework;
using ProjNet.CoordinateSystems;

namespace ProjNET.Tests.Serialization
{
    public class CoordinateSystemsProjectionsTest : BaseSerializationTest
    {
        [Test]
        public void TestProjectionParameterSet() 
        {
            var ps = new ProjNet.CoordinateSystems.Projections.ProjectionParameterSet(
                new[]
                    {
                        new ProjectionParameter("latitude_of_origin", 0),
                        new ProjectionParameter("false_easting", 500)
                    }
                );

            var psD = SanD(ps, GetFormatter());

            Assert.AreEqual(ps, psD);
        }
    }
}
