using ProjNet.CoordinateSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjNet.Services
{
    /// <summary>
    /// Interface for fetching Coordinate Systems
    /// </summary>
    public interface ICoordinateSystemService
    {
        /// <summary>
        /// Returns the coordinate system by <paramref name="srid" /> identifier
        /// </summary>
        /// <param name="srid">The initialization for the coordinate system</param>
        /// <returns>The coordinate system.</returns>
        CoordinateSystem GetCoordinateSystem(int srid);

        /// <summary>
        /// Returns the coordinate system by <paramref name="authority" /> and <paramref name="code" />.
        /// </summary>
        /// <param name="authority">The authority for the coordinate system</param>
        /// <param name="code">The code assigned to the coordinate system by <paramref name="authority" />.</param>
        /// <returns>The coordinate system.</returns>
        CoordinateSystem GetCoordinateSystem(string authority, long code);

        /// <summary>
        /// Method to get the identifier, by which this coordinate system can be accessed.
        /// </summary>
        /// <param name="authority">The authority name</param>
        /// <param name="authorityCode">The code assigned by <paramref name="authority" /></param>
        /// <returns>The identifier or <value>null</value></returns>
        int? GetSRID(string authority, long authorityCode);

        /// <summary>
        /// Adds a coordinate system to the service based on srid
        /// </summary>
        /// <param name="srid"></param>
        /// <param name="coordinateSystem"></param>
        void AddCoordinateSystem(int srid, CoordinateSystem coordinateSystem);

        /// <summary>
        /// Adds a coordinate system to the service.
        /// The authority code is defaulted as the srid.
        /// </summary>
        /// <param name="coordinateSystem"></param>
        /// <returns></returns>
        int AddCoordinateSystem(CoordinateSystem coordinateSystem);

        /// <summary>
        /// Removes a coordinate system from the servuce
        /// </summary>
        /// <param name="srid">the id to remove</param>
        bool RemoveCoordinateSystem(int srid);

        /// <summary>
        /// Factory providing methods for creating coordinate systems
        /// </summary>
        CoordinateSystemFactory CsFactory { get; }

        /// <summary>
        /// Returns number of coordinate systems registered by the service
        /// </summary>
        int Count { get; }
    }
}
