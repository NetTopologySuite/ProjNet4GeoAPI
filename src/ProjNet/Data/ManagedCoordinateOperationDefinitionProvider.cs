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
        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        public IEnumerable<CoordinateOperationDefinition> GetDefinitions()
        {
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
                    operation.MethodName,
                    operation.ParameterFileName);
            }
        }
    }
}
