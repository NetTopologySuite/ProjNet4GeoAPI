using ProjNet.IO.Wkt.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// WktParameterMathTransform for WktFittedCoordinateSystem.
    /// </summary>
    public class WktParameterMathTransform : WktBaseObject, IEquatable<WktParameterMathTransform>
    {
        /// <summary>
        /// Name property.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Parameters property.
        /// </summary>
        public IEnumerable<WktParameter> Parameters { get; internal set; }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="rightDelimiter"></param>
        public WktParameterMathTransform(string name, IEnumerable<WktParameter> parameters,
            string keyword = "PARAM_MT", char leftDelimiter = '[', char rightDelimiter = ']')
            : base(keyword, leftDelimiter, rightDelimiter)
        {
            Name = name;
            Parameters = parameters;
        }


        /// <summary>
        /// Traverse method for this WktParametersMathTransform.
        /// </summary>
        /// <param name="handler"></param>
        public override void Traverse(IWktTraverseHandler handler)
        {
            foreach (var p in Parameters)
            {
                p.Traverse(handler);
            }

            handler.Handle(this);
        }

        /// <summary>
        /// ToString method with optional Outputformatter support.
        /// </summary>
        /// <param name="formatter"></param>
        /// <returns></returns>
        public override string ToString(IWktOutputFormatter formatter)
        {
            formatter = formatter ?? new DefaultWktOutputFormatter();

            var result = new StringBuilder();

            formatter
                .AppendKeyword(Keyword, result)
                .AppendLeftDelimiter(LeftDelimiter, result)
                .AppendQuotedText(Name, result);

            foreach (var p in Parameters)
            {
                formatter
                    .AppendSeparator(result)
                    .Append(p.ToString(formatter), result);
            }

            formatter
                .AppendRightDelimiter(RightDelimiter, result);


            return result.ToString();
        }

        /// <summary>
        /// IEquatble.Equals implementation.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(WktParameterMathTransform other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name;
        }

        /// <summary>
        /// Override of bsic Equals.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WktParameterMathTransform) obj);
        }

        /// <summary>
        /// Override of basic GetHashCode.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }
    }
}
