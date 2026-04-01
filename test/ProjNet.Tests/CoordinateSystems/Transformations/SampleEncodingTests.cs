// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using BitMiracle.LibTiff.Classic;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Tests for <see cref="SampleEncoding"/>.
/// </summary>
public class SampleEncodingTests
{
    /// <summary>
    /// Verifies that signed 16-bit samples are decoded correctly.
    /// </summary>
    [Fact]
    public void TryCreate_SignedInt16_ReturnsTwoByteReader()
    {
        bool created = SampleEncoding.TryCreate(16, SampleFormat.INT, out SampleEncoding encoding);
        byte[] buffer = CreatePaddedBuffer(BitConverter.GetBytes((short)-12345), 2);

        Assert.True(created);
        Assert.Equal(2, encoding.BytesPerSample);
        Assert.Equal(-12345d, encoding.ReadValue(buffer, 2));
    }

    /// <summary>
    /// Verifies that signed 32-bit samples are decoded correctly.
    /// </summary>
    [Fact]
    public void TryCreate_SignedInt32_ReturnsFourByteReader()
    {
        bool created = SampleEncoding.TryCreate(32, SampleFormat.INT, out SampleEncoding encoding);
        byte[] buffer = CreatePaddedBuffer(BitConverter.GetBytes(-123456789), 1);

        Assert.True(created);
        Assert.Equal(4, encoding.BytesPerSample);
        Assert.Equal(-123456789d, encoding.ReadValue(buffer, 1));
    }

    /// <summary>
    /// Verifies that unsupported signed integer widths are rejected.
    /// </summary>
    [Fact]
    public void TryCreate_UnsupportedSignedWidth_ReturnsFalse()
    {
        bool created = SampleEncoding.TryCreate(8, SampleFormat.INT, out SampleEncoding encoding);

        Assert.False(created);
        Assert.Equal(0, encoding.BytesPerSample);
    }

    /// <summary>
    /// Verifies that unsigned 16-bit samples are decoded correctly.
    /// </summary>
    [Fact]
    public void TryCreate_UnsignedInt16_ReturnsTwoByteReader()
    {
        bool created = SampleEncoding.TryCreate(16, SampleFormat.UINT, out SampleEncoding encoding);
        byte[] buffer = CreatePaddedBuffer(BitConverter.GetBytes((ushort)54321), 3);

        Assert.True(created);
        Assert.Equal(2, encoding.BytesPerSample);
        Assert.Equal(54321d, encoding.ReadValue(buffer, 3));
    }

    /// <summary>
    /// Verifies that unsigned 32-bit samples are decoded correctly.
    /// </summary>
    [Fact]
    public void TryCreate_UnsignedInt32_ReturnsFourByteReader()
    {
        bool created = SampleEncoding.TryCreate(32, SampleFormat.UINT, out SampleEncoding encoding);
        byte[] buffer = CreatePaddedBuffer(BitConverter.GetBytes((uint)3456789012), 2);

        Assert.True(created);
        Assert.Equal(4, encoding.BytesPerSample);
        Assert.Equal(3456789012d, encoding.ReadValue(buffer, 2));
    }

    /// <summary>
    /// Verifies that unsupported unsigned integer widths are rejected.
    /// </summary>
    [Fact]
    public void TryCreate_UnsupportedUnsignedWidth_ReturnsFalse()
    {
        bool created = SampleEncoding.TryCreate(8, SampleFormat.UINT, out SampleEncoding encoding);

        Assert.False(created);
        Assert.Equal(0, encoding.BytesPerSample);
    }

    /// <summary>
    /// Verifies that 32-bit floating-point samples are decoded correctly.
    /// </summary>
    [Fact]
    public void TryCreate_Float32_ReturnsFourByteReader()
    {
        bool created = SampleEncoding.TryCreate(32, SampleFormat.IEEEFP, out SampleEncoding encoding);
        byte[] buffer = CreatePaddedBuffer(BitConverter.GetBytes(123.25f), 1);

        Assert.True(created);
        Assert.Equal(4, encoding.BytesPerSample);
        Assert.Equal(123.25d, encoding.ReadValue(buffer, 1), 6);
    }

    /// <summary>
    /// Verifies that 64-bit floating-point samples are decoded correctly.
    /// </summary>
    [Fact]
    public void TryCreate_Float64_ReturnsEightByteReader()
    {
        bool created = SampleEncoding.TryCreate(64, SampleFormat.IEEEFP, out SampleEncoding encoding);
        byte[] buffer = CreatePaddedBuffer(BitConverter.GetBytes(-9876.5d), 4);

        Assert.True(created);
        Assert.Equal(8, encoding.BytesPerSample);
        Assert.Equal(-9876.5d, encoding.ReadValue(buffer, 4), 12);
    }

    /// <summary>
    /// Verifies that unsupported floating-point widths are rejected.
    /// </summary>
    [Fact]
    public void TryCreate_UnsupportedFloatingWidth_ReturnsFalse()
    {
        bool created = SampleEncoding.TryCreate(16, SampleFormat.IEEEFP, out SampleEncoding encoding);

        Assert.False(created);
        Assert.Equal(0, encoding.BytesPerSample);
    }

    /// <summary>
    /// Verifies that unsupported TIFF sample formats are rejected.
    /// </summary>
    [Fact]
    public void TryCreate_UnsupportedSampleFormat_ReturnsFalse()
    {
        bool created = SampleEncoding.TryCreate(16, (SampleFormat)999, out SampleEncoding encoding);

        Assert.False(created);
        Assert.Equal(0, encoding.BytesPerSample);
    }

    private static byte[] CreatePaddedBuffer(byte[] valueBytes, int offset)
    {
        byte[] buffer = new byte[offset + valueBytes.Length + 1];
        Array.Copy(valueBytes, 0, buffer, offset, valueBytes.Length);
        return buffer;
    }
}
