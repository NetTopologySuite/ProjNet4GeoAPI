// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;

/// <summary>
/// Describes the transformation metadata carried by a bound coordinate reference system.
/// </summary>
/// <remarks>
/// <para>
/// A bound coordinate reference system links a source coordinate system to a target or hub
/// coordinate system through either Bursa-Wolf-style WGS84 parameters or a parameter-file-based
/// grid transformation.
/// </para>
/// <para>
/// Exactly one of <see cref="Wgs84Parameters"/> or <see cref="ParameterFileName"/> is populated
/// for any given instance.
/// </para>
/// </remarks>
public sealed class BoundTransformation : IEquatable<BoundTransformation>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoundTransformation"/> class for a Bursa-Wolf-style transformation.
    /// </summary>
    /// <param name="methodName">Transformation method name.</param>
    /// <param name="wgs84Parameters">WGS84 conversion parameters carried by the bound CRS.</param>
    public BoundTransformation(string methodName, Wgs84ConversionInfo wgs84Parameters)
    {
        this.MethodName = ValidateMethodName(methodName, nameof(methodName));
        this.Wgs84Parameters = ArgumentGuard.ThrowIfNull(wgs84Parameters, nameof(wgs84Parameters));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundTransformation"/> class for a parameter-file-based transformation.
    /// </summary>
    /// <param name="methodName">Transformation method name.</param>
    /// <param name="parameterFileName">Grid or parameter file referenced by the bound CRS.</param>
    public BoundTransformation(string methodName, string parameterFileName)
    {
        this.MethodName = ValidateMethodName(methodName, nameof(methodName));
        if (string.IsNullOrWhiteSpace(parameterFileName))
        {
            ArgumentGuard.ThrowArgument("Invalid parameter file name", nameof(parameterFileName));
        }

        this.ParameterFileName = parameterFileName;
    }

    /// <summary>
    /// Gets the transformation method name.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Gets the Bursa-Wolf-style WGS84 conversion parameters when the bound transformation is parameter based.
    /// </summary>
    public Wgs84ConversionInfo? Wgs84Parameters { get; }

    /// <summary>
    /// Gets the referenced grid or parameter file when the bound transformation is file based.
    /// </summary>
    public string? ParameterFileName { get; }

    /// <summary>
    /// Gets a value indicating whether this bound transformation uses WGS84 parameters.
    /// </summary>
    public bool UsesWgs84Parameters => this.Wgs84Parameters is not null;

    /// <summary>
    /// Gets a value indicating whether this bound transformation uses a parameter file.
    /// </summary>
    public bool UsesParameterFile => !string.IsNullOrWhiteSpace(this.ParameterFileName);

    /// <summary>
    /// Determines whether this instance equals another bound transformation.
    /// </summary>
    /// <param name="other">The other bound transformation.</param>
    /// <returns><see langword="true"/> when both instances describe the same transformation; otherwise <see langword="false"/>.</returns>
    public bool Equals(BoundTransformation? other) => this.EqualsCore(other);

    /// <summary>
    /// Determines whether this instance equals another object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> when both instances describe the same transformation; otherwise <see langword="false"/>.</returns>
    public override bool Equals(object? obj) => this.EqualsCore(obj as BoundTransformation);

    /// <summary>
    /// Returns a hash code for this bound transformation.
    /// </summary>
    /// <returns>A hash code for this instance.</returns>
    public override int GetHashCode() => HashCode.Combine(this.MethodName, this.ParameterFileName, this.Wgs84Parameters);

    private static string ValidateMethodName(string methodName, string paramName)
    {
        if (string.IsNullOrWhiteSpace(methodName))
        {
            ArgumentGuard.ThrowArgument("Invalid method name", paramName);
        }

        return methodName;
    }

    private bool EqualsCore(BoundTransformation? other)
    {
        return other is not null
            && string.Equals(this.MethodName, other.MethodName, StringComparison.Ordinal)
            && string.Equals(this.ParameterFileName, other.ParameterFileName, StringComparison.Ordinal)
            && Equals(this.Wgs84Parameters, other.Wgs84Parameters);
    }
}
