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
    using ProjNet.CoordinateSystems;
    using ProjNet.Data.Generated;

    /// <summary>
    /// Provides managed, runtime-independent defaults for core coordinate system definitions.
    /// </summary>
    /// <remarks>
    /// This provider intentionally avoids runtime SQLite/native dependencies.
    /// It is the baseline managed packaging implementation and can be replaced by a generated provider in later phases.
    /// </remarks>
    public sealed class ManagedCoordinateSystemDefinitionProvider : ICoordinateSystemDefinitionProvider, IManagedCoordinateSystemProvider
    {
        /// <inheritdoc/>
        public IEnumerable<KeyValuePair<int, CoordinateSystem>> GetCoordinateSystems()
        {
            return GetManagedCoordinateSystems();
        }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <returns>The computed value.</returns>
        public IEnumerable<KeyValuePair<int, string>> GetDefinitions()
        {
            foreach (var coordinateSystem in GetManagedCoordinateSystems())
            {
                yield return new KeyValuePair<int, string>(coordinateSystem.Key, coordinateSystem.Value.WKT);
            }
        }

        private static IEnumerable<KeyValuePair<int, CoordinateSystem>> GetManagedCoordinateSystems()
        {
            var yieldedSrids = new HashSet<int>();
            foreach (var coordinateSystem in EpsgCoordinateSystemFactory.GetCoordinateSystems())
            {
                if (!yieldedSrids.Add(coordinateSystem.Key))
                {
                    continue;
                }

                yield return coordinateSystem;
            }
        }
    }
}
