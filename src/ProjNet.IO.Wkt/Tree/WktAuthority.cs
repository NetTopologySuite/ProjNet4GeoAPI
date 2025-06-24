using ProjNet.IO.Wkt.Core;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ProjNet.IO.Wkt.Tree
{
    /// <summary>
    /// WktAuthority - Simple POCO for Authority info.
    /// </summary>
    public class WktAuthority : WktBaseObject, IEquatable<WktAuthority>
    {
        /// <summary>
        /// Name of this Authority.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Direction for this Authority.
        /// </summary>
        public long Code { get; internal set; }


        /// <summary>
        /// Constructor for WktAuthority.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="code"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="rightDelimiter"></param>
        public WktAuthority(string name = null, string code = null,
                            string keyword = "AUTHORITY", char leftDelimiter = '[', char rightDelimiter = ']' )
            : base(keyword, leftDelimiter, rightDelimiter)
        {
            Name = name;
            SetCode(code);
        }

        /// <summary>
        /// Setter for Code.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public WktAuthority SetCode(string code)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                Code = long.Parse(new string(code.Where(c => char.IsDigit(c)).ToArray()));
            }

            return this;
        }

        /// <summary>
        /// IEquatable.Equals implementation.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(WktAuthority other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name && Code == other.Code;
        }

        /// <summary>
        /// Basic Equals override.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WktAuthority) obj);
        }

        /// <summary>
        /// Basic GetHashCode override.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ Code.GetHashCode();
            }
        }

        /// <inheritdoc/>>
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
                .AppendKeyword(Keyword, result, false)
                .AppendLeftDelimiter(LeftDelimiter, result)
                .AppendQuotedText(Name, result)
                .AppendSeparator(result, keepInside: true)
                .AppendExtraWhitespace(result)
                .AppendQuotedText(Code.ToString(CultureInfo.InvariantCulture), result)
                .AppendRightDelimiter(RightDelimiter, result);

            return result.ToString();
        }

    }
}
