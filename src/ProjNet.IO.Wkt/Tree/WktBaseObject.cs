using System;
using ProjNet.IO.Wkt.Core;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// Base class for all WKT Objects.
    /// </summary>
    public abstract class WktBaseObject : IWktObject
    {
        /// <inheritdoc/>
        public string Keyword { get; internal set; }

        /// <summary>
        /// LeftDelimiter used after keyword to surround content.
        /// </summary>
        public char LeftDelimiter { get; set; }

        /// <summary>
        /// RightDelimiter used after content.
        /// </summary>
        public char RightDelimiter { get; set; }


        /// <summary>
        /// Constructor for all Wkt Object's.
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <exception cref="NotSupportedException"></exception>
        public WktBaseObject(string keyword = null, char left = '[', char right = ']')
        {
            Keyword = keyword;

            // Check the provided delimiters and store them.
            if (!((left == '[' && right == ']') || (left == '(' && right == ')')))
                throw new NotSupportedException(
                    "Delimiters are not supported or left and right delimiters don't match!");

            LeftDelimiter = left;
            RightDelimiter = right;
        }


        /// <inheritdoc/>
        public abstract void Traverse(IWktTraverseHandler handler);


        /// <inheritdoc/>
        public abstract string ToString(IWktOutputFormatter formatter);
        /*
        {
            return Keyword;
        }
        */


        /// <summary>
        /// Override default ToString calling the formatter version with a DefaultWktOutputFormatter.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.ToString(new DefaultWktOutputFormatter());
        }

    }
}
