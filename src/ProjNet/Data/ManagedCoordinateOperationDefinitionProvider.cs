// <copyright file="ManagedCoordinateOperationDefinitionProvider.cs" company="NetTopologySuite - Team">
// Copyright (c) NetTopologySuite - Team. All rights reserved.
// </copyright>

namespace ProjNet.Data
{
    using System.Collections.Generic;
    using ProjNet.Data.Generated;

    /// <summary>
    /// Provides managed coordinate operation definitions from generated catalog data.
    /// </summary>
    internal sealed class ManagedCoordinateOperationDefinitionProvider : ICoordinateOperationDefinitionProvider
    {
        /// <inheritdoc />
        public IEnumerable<CoordinateOperationDefinition> GetDefinitions()
        {
            var stringPool = EpsgGeneratedCatalog.StringPool;
            var records = EpsgGeneratedCatalog.Operations;

            for (int i = 0; i < records.Length; i++)
            {
                var operation = records[i];
                yield return new CoordinateOperationDefinition(
                    (CoordinateOperationKind)operation.OperationType,
                    operation.OperationCode,
                    operation.SourceSrid,
                    operation.TargetSrid,
                    operation.Accuracy,
                    GetStringValue(stringPool, operation.MethodNameIndex),
                    GetStringValue(stringPool, operation.ParameterFileIndex));
            }
        }

        private static string GetStringValue(string[] stringPool, int index)
        {
            return index >= 0 && index < stringPool.Length
                ? stringPool[index]
                : null;
        }
    }
}
