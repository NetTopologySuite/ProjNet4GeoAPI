using System.Collections.Generic;
using ProjNet.IO.Wkt.Tree;

namespace ProjNet.IO.Wkt.Core
{
    /// <summary>
    /// IWktTraverseHandler interface for traveling a WktObject tree.
    /// </summary>
    public interface IWktTraverseHandler
    {

        /// <summary>
        /// Handle method for WktAuthority.
        /// </summary>
        /// <param name="authority"></param>
        void Handle(WktAuthority authority);

        /// <summary>
        /// Handle method for WktAxis.
        /// </summary>
        /// <param name="axis"></param>
        void Handle(WktAxis axis);

        /// <summary>
        /// Handle method for WktDatum.
        /// </summary>
        /// <param name="datum"></param>
        void Handle(WktDatum datum);

        /// <summary>
        /// Handle method for WktEllipsoid.
        /// </summary>
        /// <param name="ellipsoid"></param>
        void Handle(WktEllipsoid ellipsoid);

        /// <summary>
        /// Handle method for WktSpheroid.
        /// </summary>
        /// <param name="spheroid"></param>
        void Handle(WktSpheroid spheroid);

        /// <summary>
        /// Handle method for WktExtension.
        /// </summary>
        /// <param name="extension"></param>
        void Handle(WktExtension extension);

        /// <summary>
        /// Handle method for WktUnit.
        /// </summary>
        /// <param name="unit"></param>
        void Handle(WktUnit unit);

        /// <summary>
        /// Handle method for WktParameter.
        /// </summary>
        /// <param name="parameter"></param>
        void Handle(WktParameter parameter);

        /// <summary>
        /// Handle method for WktParameterMathTransform. (FittedCoordinateSystem related).
        /// </summary>
        /// <param name="pmt"></param>
        void Handle(WktParameterMathTransform pmt);

        /// <summary>
        /// Handle method for WktPrimeMeridian.
        /// </summary>
        /// <param name="meridian"></param>
        void Handle(WktPrimeMeridian meridian);

        /// <summary>
        /// Handle method for WktProjection.
        /// </summary>
        /// <param name="projection"></param>
        void Handle(WktProjection projection);

        /// <summary>
        /// Handle method for WktToWgs84.
        /// </summary>
        /// <param name="toWgs84"></param>
        void Handle(WktToWgs84 toWgs84);

        /// <summary>
        /// Handle method for WktGeocentricCoordinateSystem.
        /// </summary>
        /// <param name="cs"></param>
        void Handle(WktGeocentricCoordinateSystem cs);

        /// <summary>
        /// Handle method for WktGeographicCoordinateSystem.
        /// </summary>
        /// <param name="cs"></param>
        void Handle(WktGeographicCoordinateSystem cs);

        /// <summary>
        /// Handle method for WktProjectedCoordinateSystem.
        /// </summary>
        /// <param name="cs"></param>
        void Handle(WktProjectedCoordinateSystem cs);

        /// <summary>
        /// Handle method for WktFittedCoordinateSystem.
        /// </summary>
        /// <param name="cs"></param>
        void Handle(WktFittedCoordinateSystem cs);
    }
}
