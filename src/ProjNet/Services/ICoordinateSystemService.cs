using ProjNet.CoordinateSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjNet.Services
{
    public interface ICoordinateSystemService
    {
        CoordinateSystem GetCoordinateSystem(int srid);
        CoordinateSystem GetCoordinateSystem(string authority, long code);
        int? GetSRID(string authority, long authorityCode);
        void AddCoordinateSystem(int srid, CoordinateSystem coordinateSystem);
        int AddCoordinateSystem(CoordinateSystem coordinateSystem);
        int Count { get; }
    }
}
