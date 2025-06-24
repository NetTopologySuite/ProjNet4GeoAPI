using System;
using System.Text;
using ProjNet.IO.Wkt.Core;


namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// WktUnit class.
    /// </summary>
    public class WktUnit : WktBaseObject, IEquatable<WktUnit>
    {
        /// <summary>
        /// Name property.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// ConversionFactor property.
        /// </summary>
        public double ConversionFactor { get; internal set; }

        /// <summary>
        /// Authority property. (Optional)
        /// </summary>
        public WktAuthority Authority { get; internal set; }


        /// <summary>
        /// Constructor for WKT Unit.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="conversionFactor"></param>
        /// <param name="authority"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="rightDelimiter"></param>
        public WktUnit(string name, double conversionFactor, WktAuthority authority,
                        string keyword = "UNIT", char leftDelimiter = '[', char rightDelimiter = ']')
            : base(keyword, leftDelimiter, rightDelimiter)
        {
            Name = name;
            ConversionFactor = conversionFactor;
            Authority = authority;
        }


        /// <summary>
        /// Implementing IEquatable.Equals checking the whole tree.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(WktUnit other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name && ConversionFactor.Equals(other.ConversionFactor) && Equals(Authority, other.Authority);
        }

        /// <summary>
        /// Overriding basic Equals.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WktUnit) obj);
        }

        /// <summary>
        /// Overriding basic GetHashCode method.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ConversionFactor.GetHashCode();
                hashCode = (hashCode * 397) ^ (Authority != null ? Authority.GetHashCode() : 0);
                return hashCode;
            }
        }


        /// <inheritdoc/>
        public override void Traverse(IWktTraverseHandler handler)
        {
            if (Authority != null)
                Authority.Traverse(handler);

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
                .Append(ConversionFactor, result);

            if (Authority != null)
            {
                formatter
                    .AppendSeparator(result, keepInside: true)
                    .AppendExtraWhitespace(result)
                    .Append(Authority.ToString(formatter), result);
            }

            formatter
                .AppendRightDelimiter(RightDelimiter, result);

            return result.ToString();
        }
    }
}
