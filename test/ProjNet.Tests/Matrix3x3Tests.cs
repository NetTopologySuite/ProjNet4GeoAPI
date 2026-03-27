// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Tests matrix and vector helper primitives used by transformation implementations.
/// </summary>
public class Matrix3x3Tests
{
    [Fact]
    public void Vector3DAddition_GivenTwoVectors_ReturnsComponentWiseSum()
    {
        Vector3D left = new Vector3D(1d, 2d, 3d);
        Vector3D right = new Vector3D(4d, 5d, 6d);

        Vector3D result = left + right;

        Assert.Equal(5d, result.X, 12);
        Assert.Equal(7d, result.Y, 12);
        Assert.Equal(9d, result.Z, 12);
    }

    [Fact]
    public void Vector3DSubtraction_GivenTwoVectors_ReturnsComponentWiseDifference()
    {
        Vector3D left = new Vector3D(10d, 8d, 6d);
        Vector3D right = new Vector3D(1d, 2d, 3d);

        Vector3D result = left - right;

        Assert.Equal(9d, result.X, 12);
        Assert.Equal(6d, result.Y, 12);
        Assert.Equal(3d, result.Z, 12);
    }

    [Fact]
    public void Vector3DScalarOperations_GivenScaleAndDivision_ReturnsExpectedValues()
    {
        Vector3D value = new Vector3D(2d, -4d, 6d);

        Vector3D multiplied = value * 3d;
        Vector3D divided = multiplied / 3d;

        Assert.Equal(6d, multiplied.X, 12);
        Assert.Equal(-12d, multiplied.Y, 12);
        Assert.Equal(18d, multiplied.Z, 12);
        Assert.Equal(value.X, divided.X, 12);
        Assert.Equal(value.Y, divided.Y, 12);
        Assert.Equal(value.Z, divided.Z, 12);
    }

    [Fact]
    public void Matrix3x3Transpose_GivenMatrix_ReturnsSwappedRowsAndColumns()
    {
        Matrix3x3 matrix = new Matrix3x3(
            1d,
            2d,
            3d,
            4d,
            5d,
            6d,
            7d,
            8d,
            9d);

        Matrix3x3 transposed = matrix.Transpose();

        Assert.Equal(1d, transposed.M00, 12);
        Assert.Equal(4d, transposed.M01, 12);
        Assert.Equal(7d, transposed.M02, 12);
        Assert.Equal(2d, transposed.M10, 12);
        Assert.Equal(5d, transposed.M11, 12);
        Assert.Equal(8d, transposed.M12, 12);
        Assert.Equal(3d, transposed.M20, 12);
        Assert.Equal(6d, transposed.M21, 12);
        Assert.Equal(9d, transposed.M22, 12);
    }

    [Fact]
    public void Matrix3x3MultiplyVector_GivenKnownInputs_ReturnsExpectedProduct()
    {
        Matrix3x3 matrix = new Matrix3x3(
            1d,
            2d,
            3d,
            0d,
            1d,
            4d,
            5d,
            6d,
            0d);
        Vector3D vector = new Vector3D(1d, 2d, 3d);

        Vector3D result = matrix * vector;

        Assert.Equal(14d, result.X, 12);
        Assert.Equal(14d, result.Y, 12);
        Assert.Equal(17d, result.Z, 12);
    }

    [Fact]
    public void Matrix3x3MultiplyMatrix_GivenTwoMatrices_ReturnsExpectedComposition()
    {
        Matrix3x3 left = new Matrix3x3(
            1d,
            2d,
            3d,
            4d,
            5d,
            6d,
            7d,
            8d,
            9d);
        Matrix3x3 right = new Matrix3x3(
            9d,
            8d,
            7d,
            6d,
            5d,
            4d,
            3d,
            2d,
            1d);

        Matrix3x3 result = left * right;

        Assert.Equal(30d, result.M00, 12);
        Assert.Equal(24d, result.M01, 12);
        Assert.Equal(18d, result.M02, 12);
        Assert.Equal(84d, result.M10, 12);
        Assert.Equal(69d, result.M11, 12);
        Assert.Equal(54d, result.M12, 12);
        Assert.Equal(138d, result.M20, 12);
        Assert.Equal(114d, result.M21, 12);
        Assert.Equal(90d, result.M22, 12);
    }

    [Fact]
    public void Matrix3x3Identity_GivenVector_LeavesVectorUnchanged()
    {
        Vector3D value = new Vector3D(-3d, 4d, 12d);

        Vector3D result = Matrix3x3.Identity * value;

        Assert.Equal(value.X, result.X, 12);
        Assert.Equal(value.Y, result.Y, 12);
        Assert.Equal(value.Z, result.Z, 12);
    }
}
