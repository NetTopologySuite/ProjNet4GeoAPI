using ProjNet.IO.Wkt.Core;

namespace ProjNet.IO.Wkt.Tree
{

    /// <summary>
    /// Base interface for all WKT Objects
    /// </summary>
    public interface IWktObject
    {
        /// <summary>
        /// The Keyword as set and used by the WktParser.
        /// </summary>
        string Keyword { get; }



        /// <summary>
        /// Traverse this object and its descendants calling the handler as we go down.
        /// </summary>
        /// <param name="handler"></param>
        void Traverse(IWktTraverseHandler handler);


        /// <summary>
        /// Ouput this WktObject to string using the provided formatter.
        /// If formatter is null the DefaultWktOutputFormatter is used.
        /// </summary>
        /// <param name="formatter"></param>
        /// <returns></returns>
        string ToString(IWktOutputFormatter formatter);
    }
}
