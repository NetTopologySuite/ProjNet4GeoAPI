// Copyright 2021 - NetTopologySuite - Team
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

// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNet.IO.CoordinateSystems
{
    /// <summary>
    /// An enumeration of possible bracket types
    /// </summary>
    internal enum WktBracket
    {
        /// <summary>
        /// Bracket type not specified.
        /// </summary>
        DontCare,
        /// <summary>
        /// Opener &quot;<c>(</c>&quot;, closer &quot;<c>)</c>&quot;
        /// </summary>
        Round,
        /// <summary>
        /// Opener &quot;<c>[</c>&quot;, closer &quot;<c>]</c>&quot;
        /// </summary>
        Square,
        //Brace
    }
}
