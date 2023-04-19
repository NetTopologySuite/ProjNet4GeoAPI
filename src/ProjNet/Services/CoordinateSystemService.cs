using ProjNet.CoordinateSystems;

namespace ProjNet.Services
{
    /// <summary>
    /// Abstract class for providing Coordinate Systems
    /// </summary>
    public abstract class CoordinateSystemService 
    {

        /// <summary>
        /// Creates a coordinate system service with a coordinate factory
        /// </summary>
        /// <param name="crsFactory"></param>
        public CoordinateSystemService(CoordinateSystemFactory crsFactory)
        {
            CsFactory = crsFactory;
        }

        /// <summary>
        /// Factory providing methods for creating coordinate systems
        /// </summary>
        public CoordinateSystemFactory CsFactory { get; }


    }
}
