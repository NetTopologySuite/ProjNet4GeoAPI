// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using ProjNet.CoordinateSystems;
using Xunit;

/// <summary>
/// Tests for <see cref="Datum"/>.
/// </summary>
public class DatumTests
{
    /// <summary>
    /// Verifies that the constructor stores the supplied datum type.
    /// </summary>
    [Fact]
    public void Constructor_SetsDatumType()
    {
        var datum = new TestDatum(DatumType.VD_Orthometric, "Test datum");

        Assert.Equal(DatumType.VD_Orthometric, datum.DatumType);
    }

    /// <summary>
    /// Verifies that the datum type can be updated after construction.
    /// </summary>
    [Fact]
    public void DatumType_CanBeUpdated()
    {
        var datum = new TestDatum(DatumType.VD_Orthometric, "Test datum")
        {
            DatumType = DatumType.VD_Depth,
        };

        Assert.Equal(DatumType.VD_Depth, datum.DatumType);
    }

    /// <summary>
    /// Verifies that equality is based on datum type and ignores metadata.
    /// </summary>
    [Fact]
    public void EqualParams_SameDatumTypeDifferentMetadata_ReturnsTrue()
    {
        var first = new TestDatum(DatumType.VD_Orthometric, "First datum", "EPSG", 1, "a1", "r1", "abbr1");
        var second = new TestDatum(DatumType.VD_Orthometric, "Second datum", "OTHER", 2, "a2", "r2", "abbr2");

        Assert.True(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that different datum types compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentDatumType_ReturnsFalse()
    {
        var first = new TestDatum(DatumType.VD_Orthometric, "First datum");
        var second = new TestDatum(DatumType.VD_Depth, "Second datum");

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that non-datum objects compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentType_ReturnsFalse()
    {
        var datum = new TestDatum(DatumType.VD_Orthometric, "Test datum");

        Assert.False(datum.EqualParams("not a datum"));
    }

    private sealed class TestDatum : Datum
    {
        public TestDatum(
            DatumType type,
            string name,
            string authority = "AUTH",
            long code = 1,
            string alias = "",
            string remarks = "",
            string abbreviation = "")
            : base(type, name, authority, code, alias, remarks, abbreviation)
        {
        }

        public override string WKT => "TEST_DATUM";

        public override string XML => "<TEST_DATUM />";
    }
}
