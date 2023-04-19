// Copyright 2015 - Spartaco Giubbolini, Felix Obermaier (www.ivv-aachen.de)
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
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Services;

namespace ProjNet
{
    /// <summary>
    /// A coordinate system services class
    /// </summary>
    public class CoordinateSystemServices
    {

        private readonly ICoordinateSystemService _service;
        private readonly CoordinateSystemFactory _coordinateSystemFactory;
        private readonly CoordinateTransformationFactory _ctFactory;

        /// <summary>
        /// Creates an instance of this class
        /// </summary>
        /// <param name="coordinateSystemFactory">The coordinate sequence factory to use.</param>
        /// <param name="coordinateTransformationFactory">The coordinate transformation factory to use</param>
        public CoordinateSystemServices(CoordinateSystemFactory coordinateSystemFactory,
            CoordinateTransformationFactory coordinateTransformationFactory)
            : this(coordinateSystemFactory, coordinateTransformationFactory, new DefaultCoordinateService(null))
        {
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="definitions">An enumeration of coordinate system definitions (WKT)</param>
        public CoordinateSystemServices(IEnumerable<KeyValuePair<int, string>> definitions)
            : this(new CoordinateSystemFactory(), new CoordinateTransformationFactory(), new DefaultCoordinateService(definitions))
        {
        }

        /// <summary>
        /// Instantiates the class with a FileCoordinateService
        /// </summary>
        /// <param name="filename">filename of csv to parse</param>
        public CoordinateSystemServices(string filename)
            : this(new CoordinateSystemFactory(), new CoordinateTransformationFactory(), new FileCoordinateService(filename))
        {
        }

        /// <summary>
        /// Creates an instance of this class
        /// </summary>
        public CoordinateSystemServices()
            : this(new CoordinateSystemFactory(), new CoordinateTransformationFactory(), new DefaultCoordinateService(null))
        {
        }

        /// <summary>
        /// Creates an instance of this class
        /// </summary>
        /// <param name="coordinateSystemFactory">The coordinate sequence factory to use.</param>
        /// <param name="coordinateTransformationFactory">The coordinate transformation factory to use</param>
        /// <param name="enumeration">An enumeration of coordinate system definitions (WKT)</param>
        [Obsolete ("Please pass in DefaultCoordinateService(enumeration) instead.")]
        public CoordinateSystemServices(CoordinateSystemFactory coordinateSystemFactory,
            CoordinateTransformationFactory coordinateTransformationFactory,
            IEnumerable<KeyValuePair<int, string>> enumeration)
        {
            if (coordinateSystemFactory == null)
                throw new ArgumentNullException(nameof(coordinateSystemFactory));
            _coordinateSystemFactory = coordinateSystemFactory;

            if (coordinateTransformationFactory == null)
                throw new ArgumentNullException(nameof(coordinateTransformationFactory));
            _ctFactory = coordinateTransformationFactory;

            _service = new DefaultCoordinateService(_coordinateSystemFactory, enumeration);
        }

        /// <summary>
        /// Creates an instance of this class from an ICoordinateSystemService
        /// </summary>
        public CoordinateSystemServices(ICoordinateSystemService service)
            : this(new CoordinateSystemFactory(), new CoordinateTransformationFactory(), service)
        {
        }

        /// <summary>
        /// Creates an instance of this class from an ICoordinateSystemService
        /// </summary>
        /// <param name="coordinateSystemFactory">The coordinate sequence factory to use.</param>
        /// <param name="coordinateTransformationFactory">The coordinate transformation factory to use</param>
        /// <param name="service">The service to use for fetching coordinate systems (ie. Default, File, Database)</param>
        public CoordinateSystemServices(CoordinateSystemFactory coordinateSystemFactory,
            CoordinateTransformationFactory coordinateTransformationFactory, ICoordinateSystemService service)
        {
            if (coordinateSystemFactory == null)
                throw new ArgumentNullException(nameof(coordinateSystemFactory));
            _coordinateSystemFactory = coordinateSystemFactory;

            if (coordinateTransformationFactory == null)
                throw new ArgumentNullException(nameof(coordinateTransformationFactory));
            _ctFactory = coordinateTransformationFactory;
            _service = service;
        }

        /// <summary>
        /// Returns a CoordinateSystem from a url containing the wkt
        /// </summary>
        /// <param name="url"></param>
        /// <returns>Returns null if not found</returns>
        public async Task<CoordinateSystem> GetCoordinateSystemFromWeb(string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    string wkt = await client.GetStringAsync(url);
                    return _coordinateSystemFactory.CreateFromWkt(wkt);
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Creates a coordinate system from the wkt string
        /// </summary>
        /// <param name="wkt"></param>
        public CoordinateSystem CreateFromWkt(string wkt)
        {
            return _coordinateSystemFactory.CreateFromWkt(wkt);
        }

        /// <summary>
        /// Returns the coordinate system by <paramref name="srid" /> identifier
        /// </summary>
        /// <param name="srid">The initialization for the coordinate system</param>
        /// <returns>The coordinate system.</returns>
        public CoordinateSystem GetCoordinateSystem(int srid)
        {
            return _service.GetCoordinateSystem(srid);
        }

        /// <summary>
        /// Returns the coordinate system by <paramref name="authority" /> and <paramref name="code" />.
        /// </summary>
        /// <param name="authority">The authority for the coordinate system</param>
        /// <param name="code">The code assigned to the coordinate system by <paramref name="authority" />.</param>
        /// <returns>The coordinate system.</returns>
        public CoordinateSystem GetCoordinateSystem(string authority, long code)
        {
            return _service.GetCoordinateSystem(authority, code);
        }

        /// <summary>
        /// Method to get the identifier, by which this coordinate system can be accessed.
        /// </summary>
        /// <param name="authority">The authority name</param>
        /// <param name="authorityCode">The code assigned by <paramref name="authority" /></param>
        /// <returns>The identifier or <value>null</value></returns>
        public int? GetSRID(string authority, long authorityCode)
        {
            return _service.GetSRID(authority, authorityCode);
        }

        /// <summary>
        /// Method to create a coordinate transformation between two spatial reference systems, defined by their identifiers
        /// </summary>
        /// <remarks>This is a convenience function for <see cref="M:GeoAPI.ICoordinateSystemServices.CreateTransformation(GeoAPI.CoordinateSystems.ICoordinateSystem,GeoAPI.CoordinateSystems.ICoordinateSystem)" />.</remarks>
        /// <param name="sourceSrid">The identifier for the source spatial reference system.</param>
        /// <param name="targetSrid">The identifier for the target spatial reference system.</param>
        /// <returns>A coordinate transformation, <value>null</value> if no transformation could be created.</returns>
        public ICoordinateTransformation CreateTransformation(int sourceSrid, int targetSrid)
        {
            return CreateTransformation(GetCoordinateSystem(sourceSrid),
                GetCoordinateSystem(targetSrid));
        }

        /// <summary>
        /// Method to create a coordinate transformation between two spatial reference systems
        /// </summary>
        /// <param name="source">The source spatial reference system.</param>
        /// <param name="target">The target spatial reference system.</param>
        /// <returns>A coordinate transformation, <value>null</value> if no transformation could be created.</returns>
        public ICoordinateTransformation CreateTransformation(CoordinateSystem source, CoordinateSystem target)
        {
            return _ctFactory.CreateFromCoordinateSystems(source, target);
        }

        /// <summary>
        /// Adds a coordinate system to the service
        /// </summary>
        public void AddCoordinateSystem(int srid, CoordinateSystem coordinateSystem)
        {
            _service.AddCoordinateSystem(srid, coordinateSystem);
        }

        /// <summary>
        /// Adds a coordinate system to the service
        /// </summary>
        public virtual int AddCoordinateSystem(CoordinateSystem coordinateSystem)
        {
            return _service.AddCoordinateSystem(coordinateSystem);
        }

        /// <summary>
        /// Returns the number of coordinate systems in the service
        /// </summary>
        public int Count
        {
            get { return _service.Count; }
        }


        /// <summary>
        /// Removes a coordinate system
        /// </summary>
        [Obsolete]
        public bool RemoveCoordinateSystem(int srid)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes all coordinate systems
        /// </summary>
        [Obsolete]
        protected void Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Provides enumeration of the coordinate systems
        /// </summary>
        [Obsolete]
        public IEnumerator<KeyValuePair<int, CoordinateSystem>> GetEnumerator()
        {
            throw new NotSupportedException();
        }
    }
}
