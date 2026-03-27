// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Data;

/// <summary>
/// Represents a coordinate operation catalog definition.
/// </summary>
internal sealed class CoordinateOperationDefinition(
    CoordinateOperationKind operationKind,
    int operationCode,
    int sourceSrid,
    int targetSrid,
    double accuracy,
    string methodName,
    string parameterFileName)
{
    /// <summary>
    /// Gets the expected operation accuracy.
    /// </summary>
    internal double Accuracy { get; } = accuracy;

    /// <summary>
    /// Gets the operation method name.
    /// </summary>
    internal string MethodName { get; } = methodName;

    /// <summary>
    /// Gets the operation code.
    /// </summary>
    internal int OperationCode { get; } = operationCode;

    /// <summary>
    /// Gets the operation kind.
    /// </summary>
    internal CoordinateOperationKind OperationKind { get; } = operationKind;

    /// <summary>
    /// Gets the optional parameter file name.
    /// </summary>
    internal string ParameterFileName { get; } = parameterFileName;

    /// <summary>
    /// Gets the source SRID.
    /// </summary>
    internal int SourceSrid { get; } = sourceSrid;

    /// <summary>
    /// Gets the target SRID.
    /// </summary>
    internal int TargetSrid { get; } = targetSrid;
}
