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
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates M8 runtime parity for <c>topocentric</c>.
/// </summary>
public class Phase8TopocentricRuntimeTests
{
    private const string TopocentricGeocentricOriginOperation = "+proj=topocentric +ellps=WGS84 +X_0=3652755.3058 +Y_0=319574.6799 +Z_0=5201547.3536";
    private const string TopocentricGeographicOriginOperation = "+proj=topocentric +ellps=WGS84 +lon_0=5 +lat_0=55 +h_0=200";
    private const string TopocentricGeographicOriginInverseOperation = "+proj=topocentric +ellps=WGS84 +lon_0=5 +lat_0=55 +h_0=200 +inv";

    /// <summary>
    /// Verifies geocentric input to topocentric output for the builtins vector.
    /// </summary>
    [Fact]
    public void TopocentricWithXyzOriginMatchesBuiltinsVector()
    {
        MathTransform transform = CreateTransform(TopocentricGeocentricOriginOperation);
        double[] output = transform.Transform(CreatePoint(3771793.968d, 140253.342d, 5124304.349d));

        Assert.InRange(Math.Abs(output[0] - (-189013.869d)), 0d, 1e-3);
        Assert.InRange(Math.Abs(output[1] - (-128642.040d)), 0d, 1e-3);
        Assert.InRange(Math.Abs(output[2] - (-4220.171d)), 0d, 1e-3);
    }

    /// <summary>
    /// Verifies geographic-origin case from builtins pipeline vector.
    /// </summary>
    [Fact]
    public void TopocentricWithLonLatOriginMatchesBuiltinsVector()
    {
        MathTransform transform = CreateTransform(TopocentricGeographicOriginOperation);
        double[] output = transform.Transform(CreatePoint(3771793.968d, 140253.342d, 5124304.349d));

        Assert.InRange(Math.Abs(output[0] - (-189013.869d)), 0d, 1e-3);
        Assert.InRange(Math.Abs(output[1] - (-128642.040d)), 0d, 1e-3);
        Assert.InRange(Math.Abs(output[2] - (-4220.171d)), 0d, 1e-3);
    }

    /// <summary>
    /// Verifies inverse conversion back to geocentric coordinates.
    /// </summary>
    [Fact]
    public void TopocentricInverseRecoversGeocentricCoordinates()
    {
        MathTransform inverse = CreateTransform(TopocentricGeographicOriginInverseOperation);
        double[] geocentric = inverse.Transform(CreatePoint(-189013.869d, -128642.040d, -4220.171d));

        Assert.InRange(Math.Abs(geocentric[0] - 3771793.968d), 0d, 1e-3);
        Assert.InRange(Math.Abs(geocentric[1] - 140253.342d), 0d, 1e-3);
        Assert.InRange(Math.Abs(geocentric[2] - 5124304.349d), 0d, 1e-3);
    }

    /// <summary>
    /// Verifies required argument validation parity for topocentric.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    /// <param name="expectedToken">Expected diagnostic token.</param>
    [Theory]
    [InlineData("+proj=topocentric +ellps=WGS84", "X_0 or lon_0")]
    [InlineData("+proj=topocentric +ellps=WGS84 +X_0=0 +Y_0=0", "Y_0 and/or Z_0")]
    [InlineData("+proj=topocentric +ellps=WGS84 +lon_0=0", "lat_0")]
    [InlineData("+proj=topocentric +ellps=WGS84 +X_0=0 +lon_0=0", "mutually exclusive")]
    public void TopocentricCreationFailsForMissingOrExclusiveParameters(string operation, string expectedToken)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string skipReason);

        Assert.False(ok);
        Assert.Contains(expectedToken, skipReason, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies topocentric aliases and inversion flag are accepted.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    [Theory]
    [InlineData("+proj=topocentric +ellps=WGS84 +X_0=3652755.3058 +Y_0=319574.6799 +Z_0=5201547.3536")]
    [InlineData("+proj=topocentric +ellps=WGS84 +lon_0=5 +lat_0=55 +h_0=200 +inv")]
    public void TopocentricOperationCanBeCreated(string operation)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        Assert.NotNull(transform);
    }

    private static MathTransform CreateTransform(string operation)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);
        Assert.True(ok, skipReason);
        return transform;
    }

    private static double[] CreatePoint(double x, double y, double z) => [x, y, z];
}
