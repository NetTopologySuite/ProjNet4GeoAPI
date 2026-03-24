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
