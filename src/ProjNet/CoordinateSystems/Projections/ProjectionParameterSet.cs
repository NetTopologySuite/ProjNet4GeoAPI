// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// A named collection of projection parameters, supporting case-insensitive key lookup and insertion-order enumeration.
/// </summary>
public class ProjectionParameterSet : Dictionary<string, double>, IEquatable<ProjectionParameterSet>
{
    private readonly Dictionary<string, string> originalNames = [];
    private readonly Dictionary<int, string> originalIndex = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionParameterSet"/> class from an enumeration of projection parameters.
    /// </summary>
    /// <param name="parameters">The projection parameters to populate the set.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameters"/> is <see langword="null"/>.</exception>
    public ProjectionParameterSet(IEnumerable<ProjectionParameter> parameters)
    {
        parameters = ArgumentGuard.ThrowIfNull(parameters, nameof(parameters));

        foreach (ProjectionParameter pp in parameters)
        {
            string key = pp.Name.ToLowerInvariant();
            this.originalNames.Add(key, pp.Name);
            this.originalIndex.Add(this.originalIndex.Count, key);
            this.Add(key, pp.Value);
        }
    }

    /// <summary>
    /// Returns the contents of this set as an enumerable sequence of <see cref="ProjectionParameter"/> instances.
    /// </summary>
    /// <returns>An enumeration of <see cref="ProjectionParameter"/>s in insertion order.</returns>
    public IEnumerable<ProjectionParameter> ToProjectionParameter()
    {
        foreach (KeyValuePair<int, string> oi in this.originalIndex)
        {
            yield return new ProjectionParameter(this.originalNames[oi.Value], this[oi.Value]);
        }
    }

    /// <summary>
    /// Retrieves the value of a mandatory projection parameter.
    /// </summary>
    /// <param name="parameterName">The primary name of the parameter.</param>
    /// <param name="alternateNames">Optional alternate names to search when <paramref name="parameterName"/> is not found.</param>
    /// <returns>The value of the parameter.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameterName"/> or <paramref name="alternateNames"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="parameterName"/> and all <paramref name="alternateNames"/> are absent from the set.</exception>
    public double GetParameterValue(string parameterName, params string[] alternateNames)
    {
        parameterName = ArgumentGuard.ThrowIfNull(parameterName, nameof(parameterName));
        alternateNames = ArgumentGuard.ThrowIfNull(alternateNames, nameof(alternateNames));

        string name = parameterName.ToLowerInvariant();
        if (!this.ContainsKey(name))
        {
            foreach (string alternateName in alternateNames)
            {
                if (this.TryGetValue(alternateName.ToLowerInvariant(), out double res))
                {
                    return res;
                }
            }

            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "Missing projection parameter '{0}'", parameterName);
            if (alternateNames.Length > 0)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "\nIt is also not defined as '{0}'", alternateNames[0]);
                for (int i = 1; i < alternateNames.Length; i++)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, ", '{0}'", alternateNames[i]);
                }

                sb.Append('.');
            }

            ArgumentGuard.ThrowArgument(sb.ToString(), nameof(parameterName));
        }

        return this[name];
    }

    /// <summary>
    /// Retrieves the value of an optional projection parameter, returning a default value when the parameter is absent.
    /// </summary>
    /// <param name="name">The primary name of the parameter.</param>
    /// <param name="value">The default value to return when the parameter is absent.</param>
    /// <param name="alternateNames">Optional alternate names to search when <paramref name="name"/> is not found.</param>
    /// <returns>
    /// The stored parameter value, or <paramref name="value"/> if neither <paramref name="name"/> nor any of
    /// <paramref name="alternateNames"/> is present.
    /// </returns>
    public double GetOptionalParameterValue(string name, double value, params string[] alternateNames)
    {
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        alternateNames = ArgumentGuard.ThrowIfNull(alternateNames, nameof(alternateNames));

        name = name.ToLowerInvariant();
        if (!this.ContainsKey(name))
        {
            foreach (string alternateName in alternateNames)
            {
                if (this.TryGetValue(alternateName.ToLowerInvariant(), out double res))
                {
                    return res;
                }
            }

            // Add(name, value);
            return value;
        }

        return this[name];
    }

    /// <summary>
    /// Finds the parameter with the given name.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <returns>The parameter if present; otherwise <see langword="null"/>.</returns>
    public ProjectionParameter? Find(string name)
    {
        name = ArgumentGuard.ThrowIfNull(name, nameof(name));

        name = name.ToLowerInvariant();
        return this.ContainsKey(name) ? new ProjectionParameter(this.originalNames[name], this[name]) : null;
    }

    /// <summary>
    /// Returns the parameter at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the parameter.</param>
    /// <returns>The <see cref="ProjectionParameter"/> at <paramref name="index"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is outside the valid parameter range.</exception>
    public ProjectionParameter GetAtIndex(int index)
    {
        if (index < 0 || index >= this.Count)
        {
            ArgumentGuard.ThrowArgumentOutOfRange(nameof(index));
        }

        string name = this.originalIndex[index];
        return new ProjectionParameter(this.originalNames[name], this[name]);
    }

    /// <summary>
    /// Determines whether this parameter set is equal to <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The parameter set to compare with.</param>
    /// <returns><see langword="true"/> if both sets contain the same parameter names and values; otherwise <see langword="false"/>.</returns>
    public bool Equals(ProjectionParameterSet? other)
    {
        if (other is null)
        {
            return false;
        }

        if (other.Count != this.Count)
        {
            return false;
        }

        foreach (KeyValuePair<string, double> kvp in this)
        {
            if (!other.ContainsKey(kvp.Key))
            {
                return false;
            }

            double otherValue = other.GetParameterValue(kvp.Key);
            if (otherValue != kvp.Value)
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ProjectionParameterSet other && this.Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hashCode = default;
        foreach (KeyValuePair<string, double> kvp in this)
        {
            hashCode.Add(kvp.Key, StringComparer.Ordinal);
            hashCode.Add(kvp.Value);
        }

        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Sets or adds a projection parameter value using case-insensitive key matching.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <param name="value">Parameter value.</param>
    internal void SetParameterValue(string name, double value)
    {
        string key = name.ToLowerInvariant();
        if (!this.ContainsKey(key))
        {
            this.originalIndex.Add(this.originalIndex.Count, key);
            this.originalNames.Add(key, name);
            this.Add(key, value);
        }
        else
        {
            this.Remove(key);
            this.Add(key, value);
        }
    }
}
