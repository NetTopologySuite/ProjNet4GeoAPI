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
    /// Enumerates all coordinate operation definitions from the generated EPSG catalog.
    /// </summary>
    /// <returns>A sequence of <see cref="CoordinateOperationDefinition"/> instances from the EPSG catalog.</returns>
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
                operation.ParameterFileName,
                operation.AreaSouthLatitude,
                operation.AreaNorthLatitude,
                operation.AreaWestLongitude,
                operation.AreaEastLongitude);
        }
    }
}
