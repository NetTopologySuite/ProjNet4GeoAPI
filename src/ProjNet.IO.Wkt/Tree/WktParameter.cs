using ProjNet.IO.Wkt.Core;
using System;
using System.Text;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// WktParameter
    /// </summary>
    public class WktParameter : WktBaseObject, IEquatable<WktParameter>
    {

        /// <summary>
        /// Name property.
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// Value property.
        /// </summary>
        public double Value { get; internal set; }


        /// <summary>
        /// Constructor with optional name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="rightDelimiter"></param>
        public WktParameter(string name, double value,
                            string keyword = "PARAMETER", char leftDelimiter = '[', char rightDelimiter = ']')
            : base(keyword, leftDelimiter, rightDelimiter)
        {
            Name = name;
            Value = value;
        }



        /// <summary>
        /// IEquatable.Equals implementation checking the whole tree.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(WktParameter other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name && Value.Equals(other.Value);
        }

        /// <summary>
        /// Override of basic Equals.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WktParameter) obj);
        }

        /// <summary>
        /// Override of basic GetHashCode.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ Value.GetHashCode();
            }
        }


        /// <inheritdoc/>
        public override void Traverse(IWktTraverseHandler handler)
        {
            handler.Handle(this);
        }


        /// <inheritdoc/>
        public override string ToString(IWktOutputFormatter formatter)
        {
            formatter = formatter ?? new DefaultWktOutputFormatter();

            var result = new StringBuilder();

            formatter
                .AppendKeyword(Keyword, result)
                .AppendLeftDelimiter(LeftDelimiter, result)
                .AppendQuotedText(Name, result)
                .AppendSeparator(result, keepInside: true)
                .AppendExtraWhitespace(result)
                .Append(Value, result)
                .AppendRightDelimiter(RightDelimiter, result);

            return result.ToString();
        }
    }
}
