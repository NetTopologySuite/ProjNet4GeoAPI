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
        /// <returns>The computed value.</returns>
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
