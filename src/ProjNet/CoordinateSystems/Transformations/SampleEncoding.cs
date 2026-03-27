// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable
namespace ProjNet.CoordinateSystems.Transformations;

using System;
using BitMiracle.LibTiff.Classic;

/// <summary>
/// Represents a documented type.
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
    /// Represents a documented type.
    /// </summary>
    /// <param name="buffer">The buffer value.</param>
    /// <param name="offset">The offset value.</param>
    /// <returns>The computed value.</returns>
    internal delegate double ValueReader(byte[] buffer, int offset);

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal int BytesPerSample { get; }

    /// <summary>
    /// Performs the documented operation.
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
    /// Performs the documented operation.
    /// </summary>
    /// <param name="buffer">The buffer value.</param>
    /// <param name="offset">The offset value.</param>
    /// <returns>The computed value.</returns>
    internal double ReadValue(byte[] buffer, int offset)
    {
        return this.valueReader(buffer, offset);
    }
}
