// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Data;

using System.Collections.Generic;
using ProjNet.Data.Generated;

/// <summary>
/// Provides managed coordinate operation definitions from generated catalog data.
/// </summary>
internal sealed class ManagedCoordinateOperationDefinitionProvider : ICoordinateOperationDefinitionProvider
{
    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <returns>The computed value.</returns>
    public IEnumerable<CoordinateOperationDefinition> GetDefinitions()
    {
        EpsgOperationRecord[] records = EpsgGeneratedCatalog.Operations;

        for (int i = 0; i < records.Length; i++)
        {
            EpsgOperationRecord operation = records[i];
            yield return new CoordinateOperationDefinition(
                (CoordinateOperationKind)operation.OperationType,
                operation.OperationCode,
                operation.SourceSrid,
                operation.TargetSrid,
                operation.Accuracy,
                operation.MethodName,
                operation.ParameterFileName);
        }
    }
}
