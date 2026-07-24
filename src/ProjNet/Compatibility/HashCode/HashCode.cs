// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#if NETSTANDARD2_0
namespace System;

using System.Collections.Generic;

/// <summary>
/// Provides a <c>System.HashCode</c> polyfill for <c>netstandard2.0</c>.
/// </summary>
/// <remarks>
/// <para>
/// The implementation mirrors the .NET runtime hashing strategy (xxHash32-inspired mixing) and supports
/// both the static <c>Combine</c> methods and the mutable <c>Add</c>/<c>ToHashCode</c> pattern.
/// </para>
/// </remarks>
internal struct HashCode
{
    private const uint Prime1 = 2654435761U;
    private const uint Prime2 = 2246822519U;
    private const uint Prime3 = 3266489917U;
    private const uint Prime4 = 668265263U;
    private const uint Prime5 = 374761393U;
    private const uint Seed = 0xA5A5A5A5U;

    private uint length;
    private uint v1;
    private uint v2;
    private uint v3;
    private uint v4;
    private uint queue1;
    private uint queue2;
    private uint queue3;

    /// <summary>
    /// Combines one value into a hash code.
    /// </summary>
    /// <typeparam name="T1">First value type.</typeparam>
    /// <param name="value1">First value.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1>(T1 value1)
    {
        uint hc1 = GetHashCode(value1);
        uint hash = MixEmptyState();
        hash += 4;
        hash = QueueRound(hash, hc1);
        hash = MixFinal(hash);
        return (int)hash;
    }

    /// <summary>
    /// Combines two values into a hash code.
    /// </summary>
    /// <typeparam name="T1">First value type.</typeparam>
    /// <typeparam name="T2">Second value type.</typeparam>
    /// <param name="value1">First value.</param>
    /// <param name="value2">Second value.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1, T2>(T1 value1, T2 value2)
    {
        uint hc1 = GetHashCode(value1);
        uint hc2 = GetHashCode(value2);
        uint hash = MixEmptyState();
        hash += 8;
        hash = QueueRound(hash, hc1);
        hash = QueueRound(hash, hc2);
        hash = MixFinal(hash);
        return (int)hash;
    }

    /// <summary>
    /// Combines three values into a hash code.
    /// </summary>
    /// <typeparam name="T1">First value type.</typeparam>
    /// <typeparam name="T2">Second value type.</typeparam>
    /// <typeparam name="T3">Third value type.</typeparam>
    /// <param name="value1">First value.</param>
    /// <param name="value2">Second value.</param>
    /// <param name="value3">Third value.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
    {
        uint hc1 = GetHashCode(value1);
        uint hc2 = GetHashCode(value2);
        uint hc3 = GetHashCode(value3);
        uint hash = MixEmptyState();
        hash += 12;
        hash = QueueRound(hash, hc1);
        hash = QueueRound(hash, hc2);
        hash = QueueRound(hash, hc3);
        hash = MixFinal(hash);
        return (int)hash;
    }

    /// <summary>
    /// Combines four values into a hash code.
    /// </summary>
    /// <typeparam name="T1">First value type.</typeparam>
    /// <typeparam name="T2">Second value type.</typeparam>
    /// <typeparam name="T3">Third value type.</typeparam>
    /// <typeparam name="T4">Fourth value type.</typeparam>
    /// <param name="value1">First value.</param>
    /// <param name="value2">Second value.</param>
    /// <param name="value3">Third value.</param>
    /// <param name="value4">Fourth value.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
    {
        uint hc1 = GetHashCode(value1);
        uint hc2 = GetHashCode(value2);
        uint hc3 = GetHashCode(value3);
        uint hc4 = GetHashCode(value4);

        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);
        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 16;
        hash = MixFinal(hash);
        return (int)hash;
    }

    /// <summary>
    /// Combines five values into a hash code.
    /// </summary>
    /// <typeparam name="T1">First value type.</typeparam>
    /// <typeparam name="T2">Second value type.</typeparam>
    /// <typeparam name="T3">Third value type.</typeparam>
    /// <typeparam name="T4">Fourth value type.</typeparam>
    /// <typeparam name="T5">Fifth value type.</typeparam>
    /// <param name="value1">First value.</param>
    /// <param name="value2">Second value.</param>
    /// <param name="value3">Third value.</param>
    /// <param name="value4">Fourth value.</param>
    /// <param name="value5">Fifth value.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
    {
        uint hc1 = GetHashCode(value1);
        uint hc2 = GetHashCode(value2);
        uint hc3 = GetHashCode(value3);
        uint hc4 = GetHashCode(value4);
        uint hc5 = GetHashCode(value5);

        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);
        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 20;
        hash = QueueRound(hash, hc5);
        hash = MixFinal(hash);
        return (int)hash;
    }

    /// <summary>
    /// Combines six values into a hash code.
    /// </summary>
    /// <typeparam name="T1">First value type.</typeparam>
    /// <typeparam name="T2">Second value type.</typeparam>
    /// <typeparam name="T3">Third value type.</typeparam>
    /// <typeparam name="T4">Fourth value type.</typeparam>
    /// <typeparam name="T5">Fifth value type.</typeparam>
    /// <typeparam name="T6">Sixth value type.</typeparam>
    /// <param name="value1">First value.</param>
    /// <param name="value2">Second value.</param>
    /// <param name="value3">Third value.</param>
    /// <param name="value4">Fourth value.</param>
    /// <param name="value5">Fifth value.</param>
    /// <param name="value6">Sixth value.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
    {
        uint hc1 = GetHashCode(value1);
        uint hc2 = GetHashCode(value2);
        uint hc3 = GetHashCode(value3);
        uint hc4 = GetHashCode(value4);
        uint hc5 = GetHashCode(value5);
        uint hc6 = GetHashCode(value6);

        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);
        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 24;
        hash = QueueRound(hash, hc5);
        hash = QueueRound(hash, hc6);
        hash = MixFinal(hash);
        return (int)hash;
    }

    /// <summary>
    /// Combines seven values into a hash code.
    /// </summary>
    /// <typeparam name="T1">First value type.</typeparam>
    /// <typeparam name="T2">Second value type.</typeparam>
    /// <typeparam name="T3">Third value type.</typeparam>
    /// <typeparam name="T4">Fourth value type.</typeparam>
    /// <typeparam name="T5">Fifth value type.</typeparam>
    /// <typeparam name="T6">Sixth value type.</typeparam>
    /// <typeparam name="T7">Seventh value type.</typeparam>
    /// <param name="value1">First value.</param>
    /// <param name="value2">Second value.</param>
    /// <param name="value3">Third value.</param>
    /// <param name="value4">Fourth value.</param>
    /// <param name="value5">Fifth value.</param>
    /// <param name="value6">Sixth value.</param>
    /// <param name="value7">Seventh value.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1, T2, T3, T4, T5, T6, T7>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)
    {
        uint hc1 = GetHashCode(value1);
        uint hc2 = GetHashCode(value2);
        uint hc3 = GetHashCode(value3);
        uint hc4 = GetHashCode(value4);
        uint hc5 = GetHashCode(value5);
        uint hc6 = GetHashCode(value6);
        uint hc7 = GetHashCode(value7);

        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);
        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 28;
        hash = QueueRound(hash, hc5);
        hash = QueueRound(hash, hc6);
        hash = QueueRound(hash, hc7);
        hash = MixFinal(hash);
        return (int)hash;
    }

    /// <summary>
    /// Combines eight values into a hash code.
    /// </summary>
    /// <typeparam name="T1">First value type.</typeparam>
    /// <typeparam name="T2">Second value type.</typeparam>
    /// <typeparam name="T3">Third value type.</typeparam>
    /// <typeparam name="T4">Fourth value type.</typeparam>
    /// <typeparam name="T5">Fifth value type.</typeparam>
    /// <typeparam name="T6">Sixth value type.</typeparam>
    /// <typeparam name="T7">Seventh value type.</typeparam>
    /// <typeparam name="T8">Eighth value type.</typeparam>
    /// <param name="value1">First value.</param>
    /// <param name="value2">Second value.</param>
    /// <param name="value3">Third value.</param>
    /// <param name="value4">Fourth value.</param>
    /// <param name="value5">Fifth value.</param>
    /// <param name="value6">Sixth value.</param>
    /// <param name="value7">Seventh value.</param>
    /// <param name="value8">Eighth value.</param>
    /// <returns>The combined hash code.</returns>
    public static int Combine<T1, T2, T3, T4, T5, T6, T7, T8>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)
    {
        uint hc1 = GetHashCode(value1);
        uint hc2 = GetHashCode(value2);
        uint hc3 = GetHashCode(value3);
        uint hc4 = GetHashCode(value4);
        uint hc5 = GetHashCode(value5);
        uint hc6 = GetHashCode(value6);
        uint hc7 = GetHashCode(value7);
        uint hc8 = GetHashCode(value8);

        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);
        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        v1 = Round(v1, hc5);
        v2 = Round(v2, hc6);
        v3 = Round(v3, hc7);
        v4 = Round(v4, hc8);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 32;
        hash = MixFinal(hash);
        return (int)hash;
    }

    /// <summary>
    /// Adds a value into the hash.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="value">Value to add.</param>
    public void Add<T>(T value)
    {
        this.Add(value, comparer: null);
    }

    /// <summary>
    /// Adds a value into the hash using a comparer.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="value">Value to add.</param>
    /// <param name="comparer">Comparer to obtain value hash code.</param>
    public void Add<T>(T value, IEqualityComparer<T>? comparer)
    {
        int valueHashCode;
        if (comparer is null)
        {
            valueHashCode = value?.GetHashCode() ?? 0;
        }
        else
        {
            valueHashCode = value is null ? 0 : comparer.GetHashCode(value);
        }

        this.AddHash((uint)valueHashCode);
    }

    /// <summary>
    /// Converts the accumulated state into a final hash code.
    /// </summary>
    /// <returns>The final hash code.</returns>
    public int ToHashCode()
    {
        uint hash = this.length < 4
            ? MixEmptyState()
            : MixState(this.v1, this.v2, this.v3, this.v4);

        hash += this.length * 4;

        uint position = this.length % 4;
        if (position > 0)
        {
            hash = QueueRound(hash, this.queue1);
            if (position > 1)
            {
                hash = QueueRound(hash, this.queue2);
                if (position > 2)
                {
                    hash = QueueRound(hash, this.queue3);
                }
            }
        }

        hash = MixFinal(hash);
        return (int)hash;
    }

    private static uint GetHashCode<T>(T value)
    {
        return (uint)(value?.GetHashCode() ?? 0);
    }

    private static void Initialize(out uint v1, out uint v2, out uint v3, out uint v4)
    {
        v1 = unchecked(Seed + Prime1 + Prime2);
        v2 = unchecked(Seed + Prime2);
        v3 = Seed;
        v4 = unchecked(Seed - Prime1);
    }

    private static uint Round(uint hash, uint input)
    {
        return RotateLeft(hash + (input * Prime2), 13) * Prime1;
    }

    private static uint QueueRound(uint hash, uint queuedValue)
    {
        return RotateLeft(hash + (queuedValue * Prime3), 17) * Prime4;
    }

    private static uint MixState(uint v1, uint v2, uint v3, uint v4)
    {
        return RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
    }

    private static uint MixEmptyState()
    {
        return Seed + Prime5;
    }

    private static uint MixFinal(uint hash)
    {
        hash ^= hash >> 15;
        hash *= Prime2;
        hash ^= hash >> 13;
        hash *= Prime3;
        hash ^= hash >> 16;
        return hash;
    }

    private static uint RotateLeft(uint value, int offset)
    {
        return (value << offset) | (value >> (32 - offset));
    }

    private void AddHash(uint value)
    {
        uint previousLength = this.length++;
        uint position = previousLength % 4;

        if (position == 0)
        {
            this.queue1 = value;
            return;
        }

        if (position == 1)
        {
            this.queue2 = value;
            return;
        }

        if (position == 2)
        {
            this.queue3 = value;
            return;
        }

        if (previousLength == 3)
        {
            Initialize(out this.v1, out this.v2, out this.v3, out this.v4);
        }

        this.v1 = Round(this.v1, this.queue1);
        this.v2 = Round(this.v2, this.queue2);
        this.v3 = Round(this.v3, this.queue3);
        this.v4 = Round(this.v4, value);
    }
}
#endif
