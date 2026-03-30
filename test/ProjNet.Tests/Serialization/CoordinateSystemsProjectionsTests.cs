// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.Serialization;

using ProjNet.CoordinateSystems;
using Xunit;

/// <summary>
/// Tests for coordinate system projection serialization and transformation.
/// </summary>
public class CoordinateSystemsProjectionsTests
#if !NET7_0_OR_GREATER
    : BaseSerializationTest
{
    [Xunit.Fact, Obsolete("ISerializable is deprecated")]
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

        Assert.Equal(ps, psD);
    }
#else
{
#endif

    /// <summary>
    /// Verifies that a coordinate transformation can be created from a coordinate system deserialized from its WKT representation.
    /// </summary>
    [Fact]
    public void CreateTransformationFromCoordinateSystemDeserializedFromWKT()
    {
        var utm17n_original = ProjNet.CoordinateSystems.ProjectedCoordinateSystem.WGS84_UTM(17, true);
        string utm17n_wkt = utm17n_original.WKT;

        var utm17n_fromWKT = (ProjectedCoordinateSystem)ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(utm17n_wkt);
        GeographicCoordinateSystem wgs84 = ProjNet.CoordinateSystems.GeographicCoordinateSystem.WGS84;

        var coordinateSystemServices = new CoordinateSystemServices();
        Assert.Null(Record.Exception(() => coordinateSystemServices.CreateTransformation(utm17n_fromWKT, wgs84)));
    }
}
