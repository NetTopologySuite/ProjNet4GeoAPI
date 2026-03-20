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
namespace ProjNet.CoordinateSystems
{
    using System;
    using System.Globalization;

    /// <summary>
    /// A named projection parameter value.
    /// </summary>
    /// <remarks>
    /// The linear units of parameters' values match the linear units of the containing
    /// projected coordinate system. The angular units of parameter values match the
    /// angular units of the geographic coordinate system that the projected coordinate
    /// system is based on. (Notice that this is different from <see cref="Parameter"/>,
    /// where the units are always meters and degrees.)
    /// </remarks>
    [Serializable]
    public class ProjectionParameter
    {
        private string name;
        private double val;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionParameter"/> class.
        /// Initializes an instance of a ProjectionParameter.
        /// </summary>
        /// <param name="name">Name of parameter.</param>
        /// <param name="value">Parameter value.</param>
        public ProjectionParameter(string name, double value)
        {
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets parameter name.
        /// </summary>
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        /// <summary>
        /// Gets or sets parameter value.
        /// The linear units of a parameters' values match the linear units of the containing
        /// projected coordinate system. The angular units of parameter values match the
        /// angular units of the geographic coordinate system that the projected coordinate
        /// system is based on.
        /// </summary>
        public double Value
        {
            get { return this.val; }
            set { this.val = value; }
        }

        /// <summary>
        /// Gets the Well-known text for this object
        /// as defined in the simple features specification.
        /// </summary>
        public string WKT
        {
            get => string.Format(CultureInfo.InvariantCulture.NumberFormat, "PARAMETER[\"{0}\", {1}]", this.Name, this.Value);

        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        public string XML
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture.NumberFormat, "<CS_ProjectionParameter Name=\"{0}\" Value=\"{1}\"/>", this.Name, this.Value);
            }
        }

        /// <summary>
        /// Function to get a textual representation of this envelope.
        /// </summary>
        /// <returns>A textual representation of this envelope.</returns>
        public override string ToString()
        {
            return $"ProjectionParameter '{this.Name}': {this.Value}";
        }
    }
}
