// <copyright file="ICoordinateOperationDefinitionProvider.cs" company="NetTopologySuite - Team">
// Copyright (c) NetTopologySuite - Team. All rights reserved.
// </copyright>

namespace ProjNet.Data
{
    using System.Collections.Generic;

    /// <summary>
    /// Provides coordinate operation definitions from a backing catalog.
    /// </summary>
    internal interface ICoordinateOperationDefinitionProvider
    {
        /// <summary>
        /// Gets the coordinate operation definitions.
        /// </summary>
        /// <returns>A sequence of coordinate operation definitions.</returns>
        IEnumerable<CoordinateOperationDefinition> GetDefinitions();
    }
}
