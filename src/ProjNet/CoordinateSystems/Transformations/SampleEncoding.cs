// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using BitMiracle.LibTiff.Classic;

/// <summary>
/// Describes the binary encoding of a single TIFF sample, capturing the byte width and the delegate used to decode a value from a buffer.
/// </summary>
internal readonly struct SampleEncoding
{
    private readonly ValueReader valueReader;

    private SampleEncoding(int bytesPerSample, ValueReader valueReader)
    {
        this.BytesPerSample = bytesPerSample;
        this.valueReader = valueReader;
    }

    /// <summary>
    /// Represents a method that reads a double-precision value from a byte buffer at the specified byte offset.
    /// </summary>
    /// <param name="buffer">The buffer value.</param>
    /// <param name="offset">The offset value.</param>
    /// <returns>The computed value.</returns>
    internal delegate double ValueReader(byte[] buffer, int offset);

    /// <summary>
    /// Gets the number of bytes occupied by one sample value in the raw buffer.
    /// </summary>
    internal int BytesPerSample { get; }

    /// <summary>
    /// Attempts to create a <see cref="SampleEncoding"/> for the given bits-per-sample count and TIFF sample format.
    /// </summary>
    /// <param name="bitsPerSample">The bitsPerSample value.</param>
    /// <param name="sampleFormat">The sampleFormat value.</param>
    /// <param name="encoding">The encoding value.</param>
    /// <returns>The computed value.</returns>
    internal static bool TryCreate(int bitsPerSample, SampleFormat sampleFormat, out SampleEncoding encoding)
    {
        switch (sampleFormat)
        {
            case SampleFormat.INT:
                if (bitsPerSample == 16)
                {
                    encoding = new SampleEncoding(2, (buffer, offset) => BitConverter.ToInt16(buffer, offset));
                    return true;
                }

                if (bitsPerSample == 32)
                {
                    encoding = new SampleEncoding(4, (buffer, offset) => BitConverter.ToInt32(buffer, offset));
                    return true;
                }

                break;
            case SampleFormat.UINT:
                if (bitsPerSample == 16)
                {
                    encoding = new SampleEncoding(2, (buffer, offset) => BitConverter.ToUInt16(buffer, offset));
                    return true;
                }

                if (bitsPerSample == 32)
                {
                    encoding = new SampleEncoding(4, (buffer, offset) => BitConverter.ToUInt32(buffer, offset));
                    return true;
                }

                break;
            case SampleFormat.IEEEFP:
                if (bitsPerSample == 32)
                {
                    encoding = new SampleEncoding(4, (buffer, offset) => BitConverter.ToSingle(buffer, offset));
                    return true;
                }

                if (bitsPerSample == 64)
                {
                    encoding = new SampleEncoding(8, (buffer, offset) => BitConverter.ToDouble(buffer, offset));
                    return true;
                }

                break;
        }

        encoding = default;
        return false;
    }

    /// <summary>
    /// Reads a sample value from <paramref name="buffer"/> starting at the given byte <paramref name="offset"/>.
    /// </summary>
    /// <param name="buffer">The buffer value.</param>
    /// <param name="offset">The offset value.</param>
    /// <returns>The computed value.</returns>
    internal double ReadValue(byte[] buffer, int offset)
    {
        return this.valueReader(buffer, offset);
    }
}
