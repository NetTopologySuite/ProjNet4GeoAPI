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

namespace ProjNet.CoordinateSystems.Projections;

/// <summary>
/// Shared numeric constants reused across projection implementations.
/// </summary>
internal static class ProjectionConstants
{
    /// <summary>
    /// One third.
    /// </summary>
    internal const double OneThird = 0.33333333333333333333d;

    /// <summary>
    /// Two thirds.
    /// </summary>
    internal const double TwoThirds = 0.66666666666666666666d;

    /// <summary>
    /// One plus a 1e-7 tolerance margin.
    /// </summary>
    internal const double OnePlusEps7 = 1.0000001d;

    /// <summary>
    /// One plus a 1e-6 tolerance margin.
    /// </summary>
    internal const double OnePlusEps6 = 1.000001d;

    /// <summary>
    /// Shared 1e-12 tolerance.
    /// </summary>
    internal const double Tolerance1E12 = 1e-12d;
}
