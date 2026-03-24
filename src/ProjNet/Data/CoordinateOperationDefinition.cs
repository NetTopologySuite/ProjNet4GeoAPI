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
