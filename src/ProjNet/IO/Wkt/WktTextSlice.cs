// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.Wkt;

using System;
using System.Text;
using ProjNet;

/// <summary>
/// Represents a slice of WKT text backed by a source string.
/// </summary>
internal readonly struct WktTextSlice
{
    private readonly string source;
    private readonly int start;

    /// <summary>
    /// Initializes a new instance of the <see cref="WktTextSlice"/> struct from a complete string value.
    /// </summary>
    /// <param name="value">The full text value.</param>
    internal WktTextSlice(string value)
    {
        this.source = ArgumentGuard.ThrowIfNull(value, nameof(value));
        this.start = 0;
        this.Length = value.Length;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WktTextSlice"/> struct from a source string slice.
    /// </summary>
    /// <param name="source">The source string that owns the slice.</param>
    /// <param name="start">The zero-based start index within the source string.</param>
    /// <param name="length">The length of the slice.</param>
    internal WktTextSlice(string source, int start, int length)
    {
        this.source = ArgumentGuard.ThrowIfNull(source, nameof(source));
        if (start < 0 || start > source.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(start), start, "The slice start must be within the source string.");
        }

        if (length < 0 || length > (source.Length - start))
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "The slice length must fit within the source string.");
        }

        this.start = start;
        this.Length = length;
    }

    /// <summary>
    /// Gets the length of the slice.
    /// </summary>
    internal int Length { get; }

    /// <summary>
    /// Returns the slice as a span over the backing string.
    /// </summary>
    /// <returns>The text slice as a span.</returns>
    internal ReadOnlySpan<char> AsSpan()
    {
        return this.source.AsSpan(this.start, this.Length);
    }

    /// <summary>
    /// Materializes the slice as a <see cref="string"/>.
    /// </summary>
    /// <returns>The slice text.</returns>
    internal string ToText()
    {
        return this.start == 0 && this.Length == this.source.Length
            ? this.source
            : this.source.Substring(this.start, this.Length);
    }

    /// <summary>
    /// Appends the slice to the provided string builder without materializing an intermediate string.
    /// </summary>
    /// <param name="builder">The target string builder.</param>
    internal void AppendTo(StringBuilder builder)
    {
        ArgumentGuard.ThrowIfNull(builder, nameof(builder));
        if (this.Length == 0)
        {
            return;
        }

        if (this.start == 0 && this.Length == this.source.Length)
        {
            builder.Append(this.source);
            return;
        }

        builder.Append(this.source, this.start, this.Length);
    }

    /// <summary>
    /// Determines whether the slice matches the supplied text using ordinal ignore-case comparison.
    /// </summary>
    /// <param name="value">The comparison text.</param>
    /// <returns><see langword="true"/> when both texts match; otherwise <see langword="false"/>.</returns>
    internal bool EqualsOrdinalIgnoreCase(string value)
    {
        value = ArgumentGuard.ThrowIfNull(value, nameof(value));
        return value.Length == this.Length &&
            string.Compare(this.source, this.start, value, 0, this.Length, StringComparison.OrdinalIgnoreCase) == 0;
    }

    /// <summary>
    /// Determines whether the slice contains the specified character.
    /// </summary>
    /// <param name="value">The character to locate.</param>
    /// <returns><see langword="true"/> when the character appears in the slice; otherwise <see langword="false"/>.</returns>
    internal bool Contains(char value)
    {
        return this.AsSpan().IndexOf(value) >= 0;
    }
}
