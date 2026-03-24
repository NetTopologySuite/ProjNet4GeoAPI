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

namespace ProjNET.Tests.Serialization;

using System;
using Xunit;
using ProjNet.CoordinateSystems;

/// <summary>
/// Represents the documented type.
/// </summary>
public class CoordinateSystemsProjectionsTest
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
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void CreateTransformationFromCoordinateSystemDeserializedFromWKT()
    {
        var utm17n_original = ProjNet.CoordinateSystems.ProjectedCoordinateSystem.WGS84_UTM(17, true);
        string utm17n_wkt = utm17n_original.WKT;

        var utm17n_fromWKT = (ProjNet.CoordinateSystems.ProjectedCoordinateSystem)ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(utm17n_wkt);
        var wgs84 = ProjNet.CoordinateSystems.GeographicCoordinateSystem.WGS84;

        var coordinateSystemServices = new ProjNet.CoordinateSystemServices();
        Assert.Null(Record.Exception(() => coordinateSystemServices.CreateTransformation(utm17n_fromWKT, wgs84)));
    }
}

