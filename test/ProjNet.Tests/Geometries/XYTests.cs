// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using ProjNet.Geometries;
using Xunit;

/// <summary>
/// Tests for the <see cref="XY"/> and <see cref="XYZ"/> geometry structs.
/// </summary>
public class XYTests
{
    /// <summary>
    /// Verifies that the constructor assigns X and Y correctly.
    /// </summary>
    [Fact]
    public void XY_Constructor_SetsXAndY()
    {
        var xy = new XY(3.5, -7.2);

        Assert.Equal(3.5, xy.X);
        Assert.Equal(-7.2, xy.Y);
    }

    /// <summary>
    /// Verifies that the default constructor initializes both fields to zero.
    /// </summary>
    [Fact]
    public void XY_DefaultConstructor_InitializesToZero()
    {
        XY xy = default;

        Assert.Equal(0.0, xy.X);
        Assert.Equal(0.0, xy.Y);
    }

    /// <summary>
    /// Verifies that fields can be assigned after construction.
    /// </summary>
    [Fact]
    public void XY_FieldAssignment_UpdatesValues()
    {
        var xy = new XY(1.0, 2.0);
        xy.X = 10.0;
        xy.Y = 20.0;

        Assert.Equal(10.0, xy.X);
        Assert.Equal(20.0, xy.Y);
    }

    /// <summary>
    /// Verifies the equality operator for various value combinations.
    /// </summary>
    [Theory]
    [InlineData(1.0, 2.0, 1.0, 2.0, true)]
    [InlineData(1.0, 2.0, 1.0, 3.0, false)]
    [InlineData(1.0, 2.0, 3.0, 2.0, false)]
    [InlineData(0.0, 0.0, 0.0, 0.0, true)]
    [InlineData(-1.0, -2.0, -1.0, -2.0, true)]
    [InlineData(-1.0, 2.0, 1.0, 2.0, false)]
    public void XY_EqualityOperator_ReturnsExpected(double x1, double y1, double x2, double y2, bool expected)
    {
        var a = new XY(x1, y1);
        var b = new XY(x2, y2);

        Assert.Equal(expected, a == b);
    }

    /// <summary>
    /// Verifies the inequality operator for various value combinations.
    /// </summary>
    [Theory]
    [InlineData(1.0, 2.0, 1.0, 2.0, false)]
    [InlineData(1.0, 2.0, 1.0, 3.0, true)]
    [InlineData(0.0, 0.0, 0.0, 0.0, false)]
    [InlineData(-1.0, 2.0, 1.0, 2.0, true)]
    public void XY_InequalityOperator_ReturnsExpected(double x1, double y1, double x2, double y2, bool expected)
    {
        var a = new XY(x1, y1);
        var b = new XY(x2, y2);

        Assert.Equal(expected, a != b);
    }

    /// <summary>
    /// Verifies that <see cref="XY.Equals(XY)"/> returns <see langword="true"/> for identical values.
    /// </summary>
    [Fact]
    public void XY_EqualsTyped_SameValues_ReturnsTrue()
    {
        var a = new XY(1.5, 2.5);
        var b = new XY(1.5, 2.5);

        Assert.True(a.Equals(b));
    }

    /// <summary>
    /// Verifies that <see cref="XY.Equals(XY)"/> returns <see langword="false"/> for different values.
    /// </summary>
    [Fact]
    public void XY_EqualsTyped_DifferentValues_ReturnsFalse()
    {
        var a = new XY(1.5, 2.5);
        var b = new XY(3.5, 2.5);

        Assert.False(a.Equals(b));
    }

    /// <summary>
    /// Verifies that <see cref="XY.Equals(object)"/> returns <see langword="true"/> for a boxed XY with the same values.
    /// </summary>
    [Fact]
    public void XY_EqualsObject_SameXY_ReturnsTrue()
    {
        var a = new XY(1.0, 2.0);
        object b = new XY(1.0, 2.0);

        Assert.True(a.Equals(b));
    }

    /// <summary>
    /// Verifies that <see cref="XY.Equals(object)"/> returns <see langword="false"/> for a different type.
    /// </summary>
    [Fact]
    public void XY_EqualsObject_DifferentType_ReturnsFalse()
    {
        var a = new XY(1.0, 2.0);

        Assert.False(a.Equals("not an XY"));
    }

    /// <summary>
    /// Verifies that <see cref="XY.Equals(object)"/> returns <see langword="false"/> for null.
    /// </summary>
    [Fact]
    public void XY_EqualsObject_Null_ReturnsFalse()
    {
        var a = new XY(1.0, 2.0);

        Assert.False(a.Equals(null));
    }

    /// <summary>
    /// Verifies that equal XY values produce the same hash code.
    /// </summary>
    [Fact]
    public void XY_GetHashCode_SameValues_ReturnsSameHash()
    {
        var a = new XY(1.5, 2.5);
        var b = new XY(1.5, 2.5);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that different XY values produce different hash codes (not guaranteed, but expected for distinct values).
    /// </summary>
    [Fact]
    public void XY_GetHashCode_DifferentValues_ReturnsDifferentHash()
    {
        var a = new XY(1.0, 2.0);
        var b = new XY(3.0, 4.0);

        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that <see cref="XY.ToString"/> produces the expected parenthesized format.
    /// </summary>
    [Fact]
    public void XY_ToString_ReturnsParenthesizedCoordinates()
    {
        var xy = new XY(3.5, -7.2);

        // Use identical interpolation to ensure culture-independence.
        Assert.Equal($"({3.5}, {-7.2})", xy.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="XY.ToString"/> formats zero values correctly.
    /// </summary>
    [Fact]
    public void XY_ToString_ZeroValues_FormatsCorrectly()
    {
        var xy = new XY(0.0, 0.0);

        Assert.Equal("(0, 0)", xy.ToString());
    }

    // ---- XYZ tests ----

    /// <summary>
    /// Verifies that the constructor assigns X, Y, and Z correctly.
    /// </summary>
    [Fact]
    public void XYZ_Constructor_SetsXYZ()
    {
        var xyz = new XYZ(3.5, -7.2, 1.0);

        Assert.Equal(3.5, xyz.X);
        Assert.Equal(-7.2, xyz.Y);
        Assert.Equal(1.0, xyz.Z);
    }

    /// <summary>
    /// Verifies that the default constructor initializes all fields to zero.
    /// </summary>
    [Fact]
    public void XYZ_DefaultConstructor_InitializesToZero()
    {
        XYZ xyz = default;

        Assert.Equal(0.0, xyz.X);
        Assert.Equal(0.0, xyz.Y);
        Assert.Equal(0.0, xyz.Z);
    }

    /// <summary>
    /// Verifies that fields can be assigned after construction.
    /// </summary>
    [Fact]
    public void XYZ_FieldAssignment_UpdatesValues()
    {
        var xyz = new XYZ(1.0, 2.0, 3.0);
        xyz.X = 10.0;
        xyz.Y = 20.0;
        xyz.Z = 30.0;

        Assert.Equal(10.0, xyz.X);
        Assert.Equal(20.0, xyz.Y);
        Assert.Equal(30.0, xyz.Z);
    }

    /// <summary>
    /// Verifies the equality operator for various XYZ value combinations.
    /// </summary>
    [Theory]
    [InlineData(1.0, 2.0, 3.0, 1.0, 2.0, 3.0, true)]
    [InlineData(1.0, 2.0, 3.0, 1.0, 2.0, 4.0, false)]
    [InlineData(1.0, 2.0, 3.0, 1.0, 4.0, 3.0, false)]
    [InlineData(1.0, 2.0, 3.0, 4.0, 2.0, 3.0, false)]
    [InlineData(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, true)]
    [InlineData(-1.0, -2.0, -3.0, -1.0, -2.0, -3.0, true)]
    public void XYZ_EqualityOperator_ReturnsExpected(double x1, double y1, double z1, double x2, double y2, double z2, bool expected)
    {
        var a = new XYZ(x1, y1, z1);
        var b = new XYZ(x2, y2, z2);

        Assert.Equal(expected, a == b);
    }

    /// <summary>
    /// Verifies the inequality operator for various XYZ value combinations.
    /// </summary>
    [Theory]
    [InlineData(1.0, 2.0, 3.0, 1.0, 2.0, 3.0, false)]
    [InlineData(1.0, 2.0, 3.0, 1.0, 2.0, 4.0, true)]
    [InlineData(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, false)]
    public void XYZ_InequalityOperator_ReturnsExpected(double x1, double y1, double z1, double x2, double y2, double z2, bool expected)
    {
        var a = new XYZ(x1, y1, z1);
        var b = new XYZ(x2, y2, z2);

        Assert.Equal(expected, a != b);
    }

    /// <summary>
    /// Verifies that <see cref="XYZ.Equals(XYZ)"/> returns <see langword="true"/> for identical values.
    /// </summary>
    [Fact]
    public void XYZ_EqualsTyped_SameValues_ReturnsTrue()
    {
        var a = new XYZ(1.5, 2.5, 3.5);
        var b = new XYZ(1.5, 2.5, 3.5);

        Assert.True(a.Equals(b));
    }

    /// <summary>
    /// Verifies that <see cref="XYZ.Equals(XYZ)"/> returns <see langword="false"/> for different values.
    /// </summary>
    [Fact]
    public void XYZ_EqualsTyped_DifferentValues_ReturnsFalse()
    {
        var a = new XYZ(1.5, 2.5, 3.5);
        var b = new XYZ(1.5, 2.5, 4.5);

        Assert.False(a.Equals(b));
    }

    /// <summary>
    /// Verifies that <see cref="XYZ.Equals(object)"/> returns <see langword="true"/> for a boxed XYZ with the same values.
    /// </summary>
    [Fact]
    public void XYZ_EqualsObject_SameXYZ_ReturnsTrue()
    {
        var a = new XYZ(1.0, 2.0, 3.0);
        object b = new XYZ(1.0, 2.0, 3.0);

        Assert.True(a.Equals(b));
    }

    /// <summary>
    /// Verifies that <see cref="XYZ.Equals(object)"/> returns <see langword="false"/> for a different type.
    /// </summary>
    [Fact]
    public void XYZ_EqualsObject_DifferentType_ReturnsFalse()
    {
        var a = new XYZ(1.0, 2.0, 3.0);

        Assert.False(a.Equals("not an XYZ"));
    }

    /// <summary>
    /// Verifies that <see cref="XYZ.Equals(object)"/> returns <see langword="false"/> for null.
    /// </summary>
    [Fact]
    public void XYZ_EqualsObject_Null_ReturnsFalse()
    {
        var a = new XYZ(1.0, 2.0, 3.0);

        Assert.False(a.Equals(null));
    }

    /// <summary>
    /// Verifies that equal XYZ values produce the same hash code.
    /// </summary>
    [Fact]
    public void XYZ_GetHashCode_SameValues_ReturnsSameHash()
    {
        var a = new XYZ(1.5, 2.5, 3.5);
        var b = new XYZ(1.5, 2.5, 3.5);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that different XYZ values produce different hash codes (not guaranteed, but expected for distinct values).
    /// </summary>
    [Fact]
    public void XYZ_GetHashCode_DifferentValues_ReturnsDifferentHash()
    {
        var a = new XYZ(1.0, 2.0, 3.0);
        var b = new XYZ(4.0, 5.0, 6.0);

        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that <see cref="XYZ.ToString"/> produces the expected parenthesized format.
    /// </summary>
    [Fact]
    public void XYZ_ToString_ReturnsParenthesizedCoordinates()
    {
        var xyz = new XYZ(3.5, -7.2, 1.0);

        Assert.Equal($"({3.5}, {-7.2}, {1.0})", xyz.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="XYZ.ToString"/> formats zero values correctly.
    /// </summary>
    [Fact]
    public void XYZ_ToString_ZeroValues_FormatsCorrectly()
    {
        var xyz = new XYZ(0.0, 0.0, 0.0);

        Assert.Equal("(0, 0, 0)", xyz.ToString());
    }

    /// <summary>
    /// Verifies that an XY value does not equal an XYZ value when boxed.
    /// </summary>
    [Fact]
    public void XY_EqualsObject_XYZ_ReturnsFalse()
    {
        var xy = new XY(1.0, 2.0);
        object xyz = new XYZ(1.0, 2.0, 0.0);

        Assert.False(xy.Equals(xyz));
    }

    /// <summary>
    /// Verifies that an XYZ value does not equal an XY value when boxed.
    /// </summary>
    [Fact]
    public void XYZ_EqualsObject_XY_ReturnsFalse()
    {
        var xyz = new XYZ(1.0, 2.0, 0.0);
        object xy = new XY(1.0, 2.0);

        Assert.False(xyz.Equals(xy));
    }
}
