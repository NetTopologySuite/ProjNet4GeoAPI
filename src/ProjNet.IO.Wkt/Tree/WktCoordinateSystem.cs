using ProjNet.IO.Wkt.Core;
using System;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// Base class for all WktCoordinateSystem classes.
    /// </summary>
    public class WktCoordinateSystem : WktBaseObject
    {

        /// <summary>
        /// Name property.
        /// </summary>
        public string Name { get; internal set; }



        /// <summary>
        /// Constructor for CoordinateSystem.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="rightDelimiter"></param>
        public WktCoordinateSystem(string name,
                                    string keyword, char leftDelimiter = '[', char rightDelimiter = ']')
            : base(keyword, leftDelimiter, rightDelimiter)
        {
            Name = name;
        }

        /// <inheritdoc/>>
        public override void Traverse(IWktTraverseHandler handler)
        {
            if (this is WktProjectedCoordinateSystem projcs)
                projcs.Traverse(handler);
            else if (this is WktGeographicCoordinateSystem geogcs)
                geogcs.Traverse(handler);
            else if (this is WktGeocentricCoordinateSystem geoccs)
                geoccs.Traverse(handler);
        }


        /// <inheritdoc/>
        public override string ToString(IWktOutputFormatter formatter)
        {
            formatter = formatter ?? new DefaultWktOutputFormatter();

            if (this is WktProjectedCoordinateSystem projcs)
                return projcs.ToString(formatter);
            else if (this is WktGeographicCoordinateSystem geogcs)
                return geogcs.ToString(formatter);
            else if (this is WktGeocentricCoordinateSystem geoccs)
                return geoccs.ToString(formatter);

            return Keyword;
        }
    }
}
