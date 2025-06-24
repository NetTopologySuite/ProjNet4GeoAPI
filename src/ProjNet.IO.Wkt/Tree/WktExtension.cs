using ProjNet.IO.Wkt.Core;
using System;
using System.Text;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// WktExtension
    /// </summary>
    public class WktExtension : WktBaseObject, IEquatable<WktExtension>
    {
        /// <summary>
        /// Name of this Extension.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Direction for this Authority.
        /// </summary>
        public string Value { get; set; }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="text"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="rightDelimiter"></param>
        public WktExtension(string name, string text,
                            string keyword = "EXTENSION", char leftDelimiter = '[', char rightDelimiter = ']')
            : base(keyword, leftDelimiter, rightDelimiter)
        {
            Name = name;
            Value = text;
        }


        /// <summary>
        /// IEquatable.Equals implementation checking whole tree.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(WktExtension other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name && Value == other.Value;
        }

        /// <summary>
        /// Basic override for Equals.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WktExtension) obj);
        }

        /// <summary>
        /// Basic override of GetHashCode.
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
                .AppendQuotedText(Value, result)
                .AppendRightDelimiter(RightDelimiter, result);

            return result.ToString();
        }

    }
}
