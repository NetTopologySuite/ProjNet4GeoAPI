using System;
using GeoAPI;
using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace ProjNet
{
    public class ProjNetCoordinateSystemServices : ICoordinateSystemServices
    {
        public ICoordinateSystem GetCoordinateSystem(int srid)
        {
            throw new System.NotImplementedException();
        }

        public ICoordinateSystem GetCoordinateSystem(string authority, long code)
        {
            throw new System.NotImplementedException();
        }

        public int? GetSRID(string authority, long authorityCode)
        {
            throw new System.NotImplementedException();
        }

        public ICoordinateTransformation CreateTransformation(int sourceSrid, int targetSrid)
        {
            throw new System.NotImplementedException();
        }

        public ICoordinateTransformation CreateTransformation(ICoordinateSystem source, ICoordinateSystem target)
        {
            throw new System.NotImplementedException();
        }

        public string GetCoordinateSystemInitializationString(int srid)
        {
            var cs = GetCoordinateSystem(srid);
            if (cs != null)
                return cs.WKT;
            throw new ArgumentException("No coordinate system with this srid available");
        }

        public ICoordinateSystemFactory CoordinateSystemFactory
        {
            get { return new CoordinateSystemFactory(); }
        }

        public ICoordinateTransformationFactory CoordinateTransformationFactory
        {
            get { return new CoordinateTransformationFactory(); }
        }
    }
}