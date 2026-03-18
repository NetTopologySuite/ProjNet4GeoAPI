// Copyright 2005, 2006 - Morten Nielsen (www.iter.dk)
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
namespace ProjNet.CoordinateSystems
{
    using System;

    /// <summary>
    /// A named parameter value.
    /// </summary>
    [Serializable]
    public class Parameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Parameter"/> class.
        /// Creates an instance of a parameter.
        /// </summary>
        /// <remarks>Units are always either meters or degrees.</remarks>
        /// <param name="name">Name of parameter.</param>
        /// <param name="value">Value.</param>
        public Parameter(string name, double value)
        {
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets parameter name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets parameter value.
        /// </summary>
        public double Value { get; set; }
    }
}
