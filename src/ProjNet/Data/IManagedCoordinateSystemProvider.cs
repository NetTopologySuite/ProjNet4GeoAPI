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

using System.Collections.Generic;
using ProjNet.CoordinateSystems;

/// <summary>
/// Internal provider contract for managed coordinate systems emitted as structured objects.
/// </summary>
internal interface IManagedCoordinateSystemProvider
{
    /// <summary>
    /// Gets coordinate system objects keyed by SRID.
    /// </summary>
    /// <returns>Coordinate system objects.</returns>
    IEnumerable<KeyValuePair<int, CoordinateSystem>> GetCoordinateSystems();
}
