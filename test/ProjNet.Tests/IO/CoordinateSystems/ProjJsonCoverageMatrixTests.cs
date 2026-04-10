// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

using System;
using System.Linq;
using Xunit;

/// <summary>
/// Guards the milestone-40 PROJJSON coverage matrix.
/// </summary>
public class ProjJsonCoverageMatrixTests
{
    /// <summary>
    /// Ensures the matrix contains the expected feature rows exactly once.
    /// </summary>
    [Fact]
    public void Rows_CoverExpectedFeaturesExactlyOnce()
    {
        string[] expectedFeatures =
        [
            "BoundCRS",
            "CompoundCRS",
            "conversion",
            "conversion.parameters[].unit",
            "coordinate_system.axis",
            "coordinate_system.axis.unit object",
            "coordinate_system.axis.unit string shorthand",
            "CoordinateMetadata",
            "datum.type = DynamicGeodeticReferenceFrame",
            "datum.type = DynamicVerticalReferenceFrame",
            "datum.type = GeodeticReferenceFrame",
            "datum.type = VerticalReferenceFrame",
            "datum_ensemble",
            "DerivedCRS/FittedCoordinateSystem",
            "EngineeringCRS",
            "GeodeticCRS.cartesian",
            "GeodeticCRS.ellipsoidal",
            "GeographicCRS",
            "id",
            "ids[]",
            "ParametricCRS",
            "prime_meridian",
            "ProjectedCRS",
            "retained bound metadata on existing CRS",
            "Standalone operation objects",
            "TimeCRS",
            "unit.type = Unit",
            "usage metadata",
            "VerticalCRS",
        ];

        Assert.Equal(
            expectedFeatures.OrderBy(feature => feature, StringComparer.Ordinal),
            ProjJsonCoverageMatrix.Rows.Select(row => row.Feature));

        Assert.Equal(
            ProjJsonCoverageMatrix.Rows.Count,
            ProjJsonCoverageMatrix.Rows.Select(row => row.Feature).Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// Ensures the matrix remains fully populated and sorted.
    /// </summary>
    [Fact]
    public void Rows_AreSortedAndPopulated()
    {
        Assert.Equal(
            ProjJsonCoverageMatrix.Rows.OrderBy(row => row.Feature, StringComparer.Ordinal).Select(row => row.Feature),
            ProjJsonCoverageMatrix.Rows.Select(row => row.Feature));

        Assert.All(
            ProjJsonCoverageMatrix.Rows,
            row =>
            {
                CoverageReferenceAssert.AssertSymbolReferenceList(row.ReaderReference);
                CoverageReferenceAssert.AssertSymbolReferenceList(row.WriterReference);
                Assert.False(string.IsNullOrWhiteSpace(row.Notes));
            });
    }

    /// <summary>
    /// Ensures the matrix keeps the expected milestone anchor classifications.
    /// </summary>
    [Fact]
    public void AnchorFeatures_KeepExpectedStatuses()
    {
        var lookup = ProjJsonCoverageMatrix.Rows.ToDictionary(row => row.Feature, StringComparer.Ordinal);

        Assert.Equal(ProjJsonCoverageStatus.Supported, lookup["GeographicCRS"].ReaderStatus);
        Assert.Equal(ProjJsonCoverageStatus.Supported, lookup["GeographicCRS"].WriterStatus);
        Assert.Equal(ProjJsonCoverageStatus.Supported, lookup["BoundCRS"].ReaderStatus);
        Assert.Equal(ProjJsonCoverageStatus.Supported, lookup["BoundCRS"].WriterStatus);
        Assert.Equal(ProjJsonCoverageStatus.Supported, lookup["datum_ensemble"].ReaderStatus);
        Assert.Equal(ProjJsonCoverageStatus.Supported, lookup["datum_ensemble"].WriterStatus);
        Assert.Equal(ProjJsonCoverageStatus.Ignored, lookup["usage metadata"].ReaderStatus);
        Assert.Equal(ProjJsonCoverageStatus.Unsupported, lookup["usage metadata"].WriterStatus);
        Assert.Equal(ProjJsonCoverageStatus.Supported, lookup["ids[]"].ReaderStatus);
        Assert.Equal(ProjJsonCoverageStatus.Unsupported, lookup["ids[]"].WriterStatus);
    }
}
